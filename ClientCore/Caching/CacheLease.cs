#nullable enable
using System;
using System.Threading;

namespace ClientCore.Caching;

/// <summary>
/// A disposable lease on a cached value. The caller must dispose this lease when done
/// with the value to release the reference. If the underlying value is <see cref="IDisposable"/>,
/// it is disposed only when all leases and the cache itself have released their references.
/// </summary>
public sealed class CacheLease<T> : IDisposable
{
    private readonly T value;
    private readonly Action? onRelease;
    private int disposeFlag = 0;

    /// <summary>
    /// Creates a lease that directly owns the value.
    /// Disposing this lease invokes <paramref name="onRelease"/> if provided.
    /// </summary>
    /// <param name="value">The directly owned value.</param>
    /// <param name="onRelease">Action to invoke when the lease is disposed, or <c>null</c>.</param>
    public static CacheLease<T> CreateOwned(T value, Action? onRelease) => new CacheLease<T>(value, onRelease);

    /// <summary>
    /// Creates a lease that directly owns the value.
    /// Disposing this lease invokes <paramref name="onRelease"/> if provided.
    /// </summary>
    internal CacheLease(T value, Action? onRelease)
    {
        this.value = value;
        this.onRelease = onRelease;
    }

    /// <summary>
    /// Creates a lease backed by a ref-counted value.
    /// The ref count was already incremented by <see cref="RefCountedValue{T}.AcquireLease"/>;
    /// disposing this lease calls <see cref="RefCountedValue{T}.Release"/>.
    /// </summary>
    internal CacheLease(RefCountedValue<T> refCounted)
    {
        value = refCounted.Value;
        onRelease = refCounted.Release;
    }

    /// <summary>
    /// Gets the leased value.
    /// </summary>
    public T Value => value;

    /// <summary>
    /// Releases this lease. If this was the last reference to a ref-counted value,
    /// the underlying value is disposed.
    /// Safe to call multiple times; only the first call takes effect.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposeFlag, 1) == 0)
            onRelease?.Invoke();
    }
}
