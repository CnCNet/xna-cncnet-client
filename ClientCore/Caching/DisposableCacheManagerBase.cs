#nullable enable
using System;

namespace ClientCore.Caching;

/// <summary>
/// Extends <see cref="CacheManagerBase{TInput, TOutput}"/> for outputs that implement
/// <see cref="IDisposable"/>. Overrides <see cref="CacheManagerBase{TInput, TOutput}.GetDisposeAction"/>
/// to call <see cref="IDisposable.Dispose"/> when an entry's ref count reaches zero,
/// ensuring values are disposed once they have been evicted from the cache and all
/// caller leases have been released.
/// </summary>
public abstract class DisposableCacheManagerBase<TInput, TOutput> : CacheManagerBase<TInput, TOutput>
    where TInput : notnull
    where TOutput : IDisposable
{
    protected DisposableCacheManagerBase(int capacity) : base(capacity) { }

    protected override Action? GetDisposeAction(TOutput? value) => value is null ? null : value.Dispose;
}
