using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

public static class AsyncExtensions
{
    /// <summary>
    /// Extensions on collection
    /// Lambda style extensions to cater a foreach with concurrency. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The collection please make sure the collection can handle the concurrency. If writing back to the objects in the collection</param>
    /// <param name="maxDegreeOfParallelism">Concurrent threads doing the async</param>
    /// <param name="body">The work that needs to be done.</param>
    /// <returns></returns>
    public static Task ForEachAsync<T>(this IEnumerable<T> source, int maxDegreeOfParallelism, Func<T, Task> body)
    {
        return Task.WhenAll(
            from partition in Partitioner.Create(source).GetPartitions(maxDegreeOfParallelism)
            select Task.Run(async delegate
            {
                using (partition)
                    while (partition.MoveNext())
                        await body(partition.Current);
            }));
    }
}