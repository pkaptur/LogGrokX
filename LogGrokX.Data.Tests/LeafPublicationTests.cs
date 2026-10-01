using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogGrokX.Data.IndexTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class LeafPublicationTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void EnumerationDoesNotSkipTailWhenNextLeafIsPublished(int start)
    {
        var first = new TransitionLeaf(0, [0, 1]);
        first.OnReadNext = () =>
        {
            first.Values.AddRange([2, 3]);
            first.NextLeaf = new TransitionLeaf(4, [4, 5]);
        };

        CollectionAssert.AreEqual(Enumerable.Range(start, 6 - start).ToArray(),
            first.GetEnumerableFromIndex(start).ToArray());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SparseLeavesDoNotAllocateTheirFullCapacity(bool longs)
    {
        const int count = 1_000;
        var leaves = new object[count];
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < count; i++)
            leaves[i] = longs ? new LongsLeaf(i, i) : new SimpleLeaf<int>(i, i);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(allocated < count * 512L, $"Allocated {allocated} bytes for {count} sparse leaves.");
        GC.KeepAlive(leaves);
    }

    private sealed class TransitionLeaf(int minIndex, IEnumerable<int> values)
        : ILeaf<int, TransitionLeaf>, ITreeNode<int>
    {
        public List<int> Values { get; } = new(values);
        public Action? OnReadNext { get; set; }
        public TransitionLeaf? NextLeaf { get; set; }
        public int FirstValue => Values[0];
        public int MinIndex => minIndex;
        public int Count => Values.Count;
        public int this[int index] => Values[index];

        public TransitionLeaf? Next
        {
            get
            {
                var callback = OnReadNext;
                OnReadNext = null;
                callback?.Invoke();
                return NextLeaf;
            }
        }

        public TransitionLeaf? Add(int value, int valueIndex) => throw new NotSupportedException();
        public IEnumerable<int> GetEnumerableFromIndex(int index) =>
            this.GetEnumerableFromIndex<int, TransitionLeaf>(index);
        public IEnumerator<int> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
