using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using LogGrokX.Data;
using LogGrokX.Data.Index;
using LogGrokX.Data.IndexTree;
using LogGrokX.Data.Monikers;

namespace LogGrokX.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class DataPortBenchmark
{
    private CountIndex<IndexTree<int, SimpleLeaf<int>>> _countIndex = null!;
    private BenchmarkIndexer _indexer = null!;

    private sealed class BenchmarkIndexer : Indexer
    {
        public int GetUncachedCount(int componentIndex, string componentValue) => Indices
            .Where(pair => NumbersToKeys[pair.Key].GetComponent(componentIndex).SequenceEqual(componentValue.AsSpan()))
            .Sum(pair => pair.Value.Count);
    }

    [GlobalSetup]
    public void Setup()
    {
        var indices = new Dictionary<IndexKeyNum, IndexTree<int, SimpleLeaf<int>>>();
        for (var key = 0; key < 256; key++)
        {
            var tree = new IndexTree<int, SimpleLeaf<int>>(16, value => new SimpleLeaf<int>(value, 0));
            for (var line = 0; line < 64; line++)
                tree.Add(line);
            indices.Add(new IndexKeyNum { KeyNum = key }, tree);
        }

        _countIndex = new CountIndex<IndexTree<int, SimpleLeaf<int>>>(indices);
        _countIndex.Add(1, indices);

        _indexer = new BenchmarkIndexer();
        var meta = new LogMetaInformation(new LogFormat
        {
            Regex = @"^(?'Value'\w+)$",
            IndexedFields = ["Value"]
        });
        var parser = new RegexBasedLineParser(meta, true);
        for (var key = 0; key < 256; key++)
        {
            var line = $"value{key}";
            var placeholder = new int[LineMetaInformation.GetSizeInts(1)];
            var lineMeta = new LineMetaInformation(placeholder.AsSpan(), 1);
            if (!parser.TryParse(line, 0, line.Length, lineMeta.ParsedLineComponents, out _))
                throw new System.InvalidOperationException();
            var metaChars = MemoryMarshal.Cast<int, char>(placeholder.AsSpan());
            var chars = new char[metaChars.Length + line.Length];
            metaChars.CopyTo(chars.AsSpan());
            line.AsSpan().CopyTo(chars.AsSpan(metaChars.Length));
            _indexer.Add(new IndexKey(new string(chars), 0, 1), key);
        }
    }

    [Benchmark]
    public int ReadLiveCounts() => _countIndex.Counts[^1].Count;

    [Benchmark]
    public int ReadCachedComponentCount() => _indexer.GetIndexCountForComponent(0, "value128");

    [Benchmark]
    public int ReadUncachedComponentCount() => _indexer.GetUncachedCount(0, "value128");

    [Benchmark]
    public int AppendIndexLines()
    {
        var tree = new IndexTree<int, SimpleLeaf<int>>(16, value => new SimpleLeaf<int>(value, 0));
        for (var line = 0; line < 100_000; line++)
            tree.Add(line);
        return tree.Count;
    }
}
