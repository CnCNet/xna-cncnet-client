#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Thread-safe message de-duplicator for LAN lobby messages.
/// Generates unique message IDs for outgoing messages and tracks received message IDs
/// to filter out duplicates. Message IDs expire after a configurable timeout to prevent
/// memory leaks. Cleanup is performed automatically in a background thread.
/// </summary>
internal class LANMessageDeduplicator : IDisposable
{
    private readonly Random random;
    private readonly object lockObject = new();

    // Track received message IDs with their expiration time
    private readonly ConcurrentDictionary<string, DateTime> receivedMessageIds = new();

    // Message ID expiration time in seconds
    private readonly double messageIdExpirationSeconds;

    // Background cleanup
    private readonly Timer cleanupTimer;
    private const double CLEANUP_INTERVAL_SECONDS = 30.0;

    private int disposed = 0;

    /// <summary>
    /// Initializes a new instance of the LANMessageDeduplicator class.
    /// </summary>
    /// <param name="randomSeed">Seed for the random number generator used to create message IDs.</param>
    /// <param name="messageIdExpirationSeconds">How long to keep message IDs before expiring them (default 60 seconds).</param>
    public LANMessageDeduplicator(int randomSeed, double messageIdExpirationSeconds = 60.0)
    {
        random = new Random(randomSeed);
        this.messageIdExpirationSeconds = messageIdExpirationSeconds;

        // Start automatic cleanup timer
        int cleanupIntervalMs = (int)(CLEANUP_INTERVAL_SECONDS * 1000);
        cleanupTimer = new Timer(CleanupCallback, null, cleanupIntervalMs, cleanupIntervalMs);
    }

    private void CleanupCallback(object? state)
    {
        if (disposed == 0)
            CleanupExpiredMessageIds();
    }

    /// <summary>
    /// Generates a unique random message ID.
    /// Message IDs are prefixed with "MID_" followed by 8 alphanumeric characters
    /// to avoid collision with legitimate message parameters.
    /// </summary>
    /// <returns>A unique message ID string.</returns>
    public string GenerateMessageId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] id = new char[8];

        // Lock is required because Random is not thread-safe
        lock (lockObject)
        {
            for (int i = 0; i < id.Length; i++)
                id[i] = chars[random.Next(chars.Length)];
        }

        string messageId = "MID_" + new string(id);
        Debug.Assert(IsValidMessageId(messageId), "Invalid message ID generated.");
        return messageId;
    }

    public const int MESSAGE_ID_PREFIX_LENGTH = 4; // "MID_"
    public const int MESSAGE_ID_LENGTH = 12; // "MID_" + 8 characters

    /// <summary>
    /// Checks if a string is a valid message ID.
    /// Message IDs must start with "MID_" followed by 8 alphanumeric characters.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid message ID, false otherwise.</returns>
    public static bool IsValidMessageId(string value)
    {
        return !string.IsNullOrEmpty(value) &&
               value.StartsWith("MID_") &&
               value.Length == MESSAGE_ID_LENGTH &&
               value[MESSAGE_ID_PREFIX_LENGTH..].All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Records a received message ID and determines if it's a duplicate.
    /// Note: Uses DateTime.UtcNow for expiration timing. While a monotonic time source
    /// would be more robust against system clock adjustments, DateTime is sufficient
    /// for LAN lobby traffic where the 60-second expiration window is large.
    /// </summary>
    /// <param name="messageId">The message ID to record.</param>
    /// <param name="isDuplicate">True if this message ID was already recorded (duplicate), false if it's new.</param>
    public void AddMessage(string messageId, out bool isDuplicate)
    {
        if (string.IsNullOrEmpty(messageId))
        {
            // If no message ID provided, consider it not a duplicate
            // This maintains backward compatibility with old clients
            isDuplicate = false;
            return;
        }

        DateTime expirationTime = DateTime.UtcNow.AddSeconds(messageIdExpirationSeconds);

        // Try to add the message ID with expiration time in one atomic operation
        // If it already exists, it's a duplicate
        isDuplicate = !receivedMessageIds.TryAdd(messageId, expirationTime);
    }

    /// <summary>
    /// Wraps a message payload with a message ID at the beginning.
    /// </summary>
    /// <param name="payload">The original message payload.</param>
    /// <returns>The wrapped message with message ID prepended.</returns>
    public string WrapMessage(string payload)
    {
        string messageId = GenerateMessageId();
        return messageId + payload;
    }

    /// <summary>
    /// Unwraps a message, extracting the message ID from the beginning and returning the payload.
    /// Also checks if the message is a duplicate.
    /// </summary>
    /// <param name="wrappedMessage">The wrapped message with message ID at the beginning.</param>
    /// <param name="payload">The unwrapped message payload.</param>
    /// <param name="isDuplicate">True if this message ID was already recorded (duplicate), false if it's new.</param>
    public void UnwrapMessage(string wrappedMessage, out string payload, out bool isDuplicate)
    {
        // Check if the message starts with a valid message ID
        if (!string.IsNullOrEmpty(wrappedMessage) && wrappedMessage.Length >= MESSAGE_ID_LENGTH)
        {
            string potentialMessageId = wrappedMessage[..MESSAGE_ID_LENGTH];
            if (IsValidMessageId(potentialMessageId))
            {
                // Extract message ID and payload
                string messageId = potentialMessageId;
                payload = wrappedMessage[MESSAGE_ID_LENGTH..];

                // Check for duplicate
                AddMessage(messageId, out isDuplicate);
                return;
            }
        }

        // No valid message ID found - treat as non-duplicate for backward compatibility
        payload = wrappedMessage;
        isDuplicate = false;
    }

    /// <summary>
    /// Removes expired message IDs from the tracking dictionary.
    /// This is called automatically by the background cleanup timer.
    /// Note: This performs O(n) enumeration of all tracked IDs. For typical LAN lobby
    /// traffic this is acceptable, but for high-traffic scenarios a more efficient
    /// data structure (e.g., priority queue) could be considered.
    /// ConcurrentDictionary operations (TryRemove, enumeration) are thread-safe.
    /// </summary>
    private void CleanupExpiredMessageIds()
    {
        // Check if disposed
        if (disposed != 0)
            return;

        // Quick exit if there's nothing to clean up
        if (receivedMessageIds.IsEmpty)
            return;

        DateTime now = DateTime.UtcNow;

        // Find all expired message IDs
        // ConcurrentDictionary enumeration is thread-safe
        List<string> expiredIds = receivedMessageIds
            .Where(kvp => kvp.Value < now)
            .Select(kvp => kvp.Key)
            .ToList();

        // Remove expired IDs
        // TryRemove is thread-safe
        foreach (string id in expiredIds)
            _ = receivedMessageIds.TryRemove(id, out _);
    }

    /// <summary>
    /// Gets the current count of tracked message IDs.
    /// Useful for monitoring and debugging.
    /// </summary>
    public int TrackedMessageCount => receivedMessageIds.Count;

    /// <summary>
    /// Clears all tracked message IDs.
    /// </summary>
    public void Clear()
    {
        receivedMessageIds.Clear();
    }

    /// <summary>
    /// Disposes the message deduplicator and stops the cleanup timer.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
            cleanupTimer?.Dispose();

        GC.SuppressFinalize(this);
    }
}
