using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogGrokX.Data.Index;
using LogGrokX.Data.IndexTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class ConcurrentIndexReadTests
{
    [TestMethod]
    public void IndexTreeCanBeReadWhileGrowing()
    {
        const int total = 100_000;
        var tree = new IndexTree<int, SimpleLeaf<int>>(16, value => new SimpleLeaf<int>(value, 0));
        var ready = new CountdownEvent(2);
        var done = false;

        var readers = Enumerable.Range(0, 2).Select(_ => Task.Factory.StartNew(() =>
        {
            ready.Signal();
            while (!Volatile.Read(ref done))
            {
                var count = tree.Count;
                if (count == 0)
                    continue;
                var index = count / 2;
                Assert.AreEqual(index, tree[index]);
                Assert.AreEqual(index, tree.FindIndexByValue(index));
                Assert.AreEqual(index, tree.GetEnumerableFromIndex(index).First());
            }
        }, TaskCreationOptions.LongRunning)).ToArray();

        ready.Wait();
        try
        {
            for (var i = 0; i < total; i++)
            {
                tree.Add(i);
                if (i % 256 == 0)
                    Thread.Yield();
            }
        }
        finally
        {
            Volatile.Write(ref done, true);
            Task.WaitAll(readers);
        }

        Assert.AreEqual(total, tree.Count);
        Assert.AreEqual(total - 1, tree[total - 1]);
    }

    [TestMethod]
    public void LongsTreeCanBeReadAcrossStorageGrowthAndLeafTransitions()
    {
        const int total = 150_000;
        const long offset = 1_000_000;
        var tree = new IndexTree<long, LongsLeaf>(16, value => new LongsLeaf(value, 0));
        using var ready = new CountdownEvent(2);
        var done = false;
        var readers = Enumerable.Range(0, 2).Select(_ => Task.Factory.StartNew(() =>
        {
            ready.Signal();
            while (!Volatile.Read(ref done))
            {
                var count = tree.Count;
                if (count == 0)
                    continue;
                var start = Math.Max(0, count - 128);
                Assert.AreEqual(offset + count - 1, tree[count - 1]);
                Assert.AreEqual(count - 1, tree.FindIndexByValue(offset + count - 1));
                CollectionAssert.AreEqual(Enumerable.Range(start, count - start)
                    .Select(index => offset + index).ToArray(),
                    tree.GetEnumerableFromIndex(start).Take(count - start).ToArray());
            }
        }, TaskCreationOptions.LongRunning)).ToArray();

        ready.Wait();
        try
        {
            for (var i = 0; i < total; i++)
            {
                tree.Add(offset + i);
                if (i % 256 == 0)
                    Thread.Yield();
            }
        }
        finally
        {
            Volatile.Write(ref done, true);
            Task.WaitAll(readers);
        }

        Assert.AreEqual(total, tree.Count);
    }

    [TestMethod]
    public void ChunkedListCanBeReadWhileGrowing()
    {
        const int total = 100_000;
        var list = new ChunkedList<int>(64);
        var ready = new CountdownEvent(2);
        var done = false;

        var readers = Enumerable.Range(0, 2).Select(_ => Task.Factory.StartNew(() =>
        {
            ready.Signal();
            while (!Volatile.Read(ref done))
            {
                var count = list.Count;
                if (count > 0)
                    Assert.AreEqual(count - 1, list[count - 1]);
            }
        }, TaskCreationOptions.LongRunning)).ToArray();

        ready.Wait();
        try
        {
            for (var i = 0; i < total; i++)
            {
                list.Add(i);
                if (i % 256 == 0)
                    Thread.Yield();
            }
        }
        finally
        {
            Volatile.Write(ref done, true);
            Task.WaitAll(readers);
        }

        Assert.AreEqual(total, list.Count);
        Assert.AreEqual(total - 1, list[total - 1]);
    }

    [TestMethod]
    public void LongsLeafFindAndEnumeratePreserveOffset()
    {
        var tree = new IndexTree<long, LongsLeaf>(16, value => new LongsLeaf(value, 0));
        for (var i = 0; i < 70_000; i++)
            tree.Add(1_000_000L + i);

        Assert.AreEqual(65_536, tree.FindIndexByValue(1_065_536));
        Assert.AreEqual(1_065_536L, tree.GetEnumerableFromIndex(65_536).First());
    }
}
