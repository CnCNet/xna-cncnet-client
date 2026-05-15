#nullable enable
using System;
using System.Threading;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Thread-safe ref-counted wrapper around a disposable value.
/// The initial ref count is 1, representing the cache's own reference.
/// The wrapped value is disposed once the ref count reaches zero.
/// </summary>
internal sealed class RefCountedValue<T> where T : IDisposable
{
    private int refCount = 1;
    private readonly T value;

    internal RefCountedValue(T value)
    {
        this.value = value;
    }

    internal T Value => value;

    /// <summary>
    /// Increments the ref count and returns a new lease for the caller.
    /// Must be called while holding the cache lock to prevent a race with eviction.
    /// </summary>
    internal CacheLease<T> AcquireLease()
    {
        Interlocked.Increment(ref refCount);
        return new CacheLease<T>(this);
    }

    /// <summary>
    /// Decrements the ref count and disposes the value when the count reaches zero.
    /// Safe to call from any thread without holding the cache lock.
    /// </summary>
    internal void Release()
    {
        if (Interlocked.Decrement(ref refCount) == 0)
            value.Dispose();
    }
}
