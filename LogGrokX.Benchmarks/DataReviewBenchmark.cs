using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LogGrokX.Data;
using LogGrokX.Data.IndexTree;

namespace LogGrokX.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class DataReviewBenchmark
{
    private const int LineCount = 2_000_000;

    [Benchmark]
    public int AddWithoutReader()
    {
        var index = new LineIndex();
        for (var i = 0; i < LineCount; i++)
            index.Add(i * 10L);
        index.Finish(10);
        return index.Count;
    }

    [Benchmark]
    public async Task<int> AddWithReader()
    {
        var index = new LineIndex();
        var reader = ReadRanges(index);
        for (var i = 0; i < LineCount; i++)
            index.Add(i * 10L);
        index.Finish(10);
        return await reader;
    }

    private static async Task<int> ReadRanges(LineIndex index)
    {
        var count = 0;
        await foreach (var range in index.FetchRanges(CancellationToken.None))
            count += range.count;
        return count;
    }

    [Benchmark]
    public SimpleLeaf<int>[] CreateSparseSimpleLeaves()
    {
        var leaves = new SimpleLeaf<int>[1_000];
        for (var i = 0; i < leaves.Length; i++)
            leaves[i] = new SimpleLeaf<int>(i, i);
        return leaves;
    }

    [Benchmark]
    public LongsLeaf[] CreateSparseLongsLeaves()
    {
        var leaves = new LongsLeaf[1_000];
        for (var i = 0; i < leaves.Length; i++)
            leaves[i] = new LongsLeaf(i, i);
        return leaves;
    }
}
