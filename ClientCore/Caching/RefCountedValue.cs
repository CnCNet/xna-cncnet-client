#nullable enable
using System;
using System.Threading;

namespace ClientCore.Caching;

/// <summary>
/// Thread-safe ref-counted wrapper around a value.
/// The initial ref count is 1, representing the cache's own reference.
/// When the ref count reaches zero, the optional <see cref="disposeAction"/> is invoked.
/// </summary>
internal sealed class RefCountedValue<T>
{
    private int refCount = 1;
    private readonly T value;
    private readonly Action? disposeAction;

    /// <param name="value">The value to wrap.</param>
    /// <param name="disposeAction">
    /// Called when the ref count reaches zero. Pass <c>null</c> for non-disposable values.
    /// </param>
    internal RefCountedValue(T value, Action? disposeAction)
    {
        this.value = value;
        this.disposeAction = disposeAction;
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
    /// Decrements the ref count and invokes the dispose action when the count reaches zero.
    /// Safe to call from any thread without holding the cache lock.
    /// </summary>
    internal void Release()
    {
        if (Interlocked.Decrement(ref refCount) == 0)
            disposeAction?.Invoke();
    }
}
