#nullable enable
using System;


namespace ClientCore.Caching;

public interface ICacheManager<TInput, TOutput> : IDisposable
{
    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Clears all cached items, releasing the cache's reference to each entry.
    /// Entries that are still leased by callers are disposed once all leases are released.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Requests an output to be computed for the specified input.
    /// </summary>
    /// <param name="input">The input to get the output.</param>
    /// <param name="lease">
    /// A lease for the cached output if found or computed. The caller must dispose the lease when
    /// done with the value. The lease is null when the output is cached but its value is null,
    /// or when the output is not yet available.
    /// </param>
    /// <param name="syncComputeOnCacheMiss">If true, the method will attempt to compute the output immediately if it's not cached, which may be CPU-intensive. If false, the input will be queued for asynchronous processing if <see cref="addToQueue"/> holds.</param>
    /// <param name="addToQueue">This parameter is ignored if <see cref="syncComputeOnCacheMiss"/> is true. Otherwise, if true, the input will be added to the processing queue if not already cached; if false, the method will simply return null on cache miss without queuing.</param>
    /// <returns>True if the output was found in cache or computed synchronously; false if the output is not available yet.</returns>
    public bool Request(TInput input, out CacheLease<TOutput>? lease, bool syncComputeOnCacheMiss = false, bool addToQueue = true);
}
