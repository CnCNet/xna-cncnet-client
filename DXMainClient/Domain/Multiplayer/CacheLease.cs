#nullable enable
using System;
using System.Threading;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// A disposable lease on a cached value. The caller must dispose this lease when done
/// with the value to release the reference. The underlying value is disposed only when
/// all leases and the cache itself have released their references.
/// </summary>
public sealed class CacheLease<T> : IDisposable where T : IDisposable
{
    private readonly T value;
    private readonly Action onDispose;
    private int disposeFlag = 0;

    /// <summary>
    /// Creates a lease that directly owns the value.
    /// Disposing this lease disposes the value immediately.
    /// </summary>
    internal CacheLease(T value)
    {
        this.value = value;
        onDispose = value.Dispose;
    }

    /// <summary>
    /// Creates a lease backed by a ref-counted value.
    /// The ref count was already incremented by <see cref="RefCountedValue{T}.AcquireLease"/>;
    /// disposing this lease calls <see cref="RefCountedValue{T}.Release"/>.
    /// </summary>
    internal CacheLease(RefCountedValue<T> refCounted)
    {
        value = refCounted.Value;
        onDispose = refCounted.Release;
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
            onDispose();
    }
}
