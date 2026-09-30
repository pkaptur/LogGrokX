using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogGrokX.Data.IndexTree;

namespace LogGrokX.Data.Index;

public abstract class IndexerBase : IDisposable
{
    protected readonly ConcurrentDictionary<IndexKeyNum, IndexTree<int, SimpleLeaf<int>>> Indices =
        new(1, 16384);

    protected readonly CountIndex<IndexTree<int, SimpleLeaf<int>>> CountIndex;

    protected readonly ConcurrentDictionary<IndexKey, IndexKeyNum> KeysToNumbers;
    protected readonly ConcurrentDictionary<IndexKeyNum, IndexKey> NumbersToKeys;
    private readonly ConcurrentDictionary<(int componentIndex, string value), (int keyCount, IndexKeyNum[] keys)>
        _componentKeyCache = new();

    public IndexerBase(ConcurrentDictionary<IndexKey, IndexKeyNum> keysToNumbers,
        ConcurrentDictionary<IndexKeyNum, IndexKey> numbersToKeys)
    {
        KeysToNumbers = keysToNumbers;
        NumbersToKeys = numbersToKeys;
        
        CountIndex = new CountIndex<IndexTree<int, SimpleLeaf<int>>>(Indices);
    }

    public IIndexedLinesProvider GetIndexedLinesProvider(
        IReadOnlyDictionary<int, IEnumerable<string>> excludedComponents)
    {
        var updatableCounts =
            UpdatableValue.Create(() => CountIndex.Counts);
        
        return new IndexedLinesProvider(this, updatableCounts,
            CountIndex<IndexTree<int, SimpleLeaf<int>>>.Granularity, excludedComponents, NumbersToKeys);
    }

    public IndexTree<int, SimpleLeaf<int>> GetIndex(IndexKeyNum key) => Indices[key];
    
    
    public int GetIndexCountForComponent(int componentIndex, string componentValue)
    {
        var keyCount = Indices.Count;
        var cacheKey = (componentIndex, componentValue);
        if (!_componentKeyCache.TryGetValue(cacheKey, out var cached) || cached.keyCount != keyCount)
        {
            var keys = Indices.Keys
                .Where(key => NumbersToKeys[key].GetComponent(componentIndex).SequenceEqual(componentValue.AsSpan()))
                .ToArray();
            cached = (keyCount, keys);
            _componentKeyCache[cacheKey] = cached;
        }

        var sum = 0;
        foreach (var key in cached.keys)
        {
            if (Indices.TryGetValue(key, out var index))
                sum += index.Count;
        }
        return sum;
    }

    private protected static IndexTree<int, SimpleLeaf<int>> CreateIndexTree()
    {
        return new IndexTree<int, SimpleLeaf<int>>(16,
            static value => new SimpleLeaf<int>(value, 0));
    }

    public void Dispose()
    {
        Indices.Clear();
        _componentKeyCache.Clear();
    }

    public void Finish()
    {
        CountIndex.Finish(Indices);
    }
}
