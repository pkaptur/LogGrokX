using System.Collections;
using System.Collections.Generic;
using LogGrokX.Data.IndexTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class IndexTreeEdgeTests
{
    [TestMethod]
    public void LongsLeafFindByValue()
    {
        var leaf = new LongsLeaf(1000, 0);
        leaf.Add(1001, 1);
        leaf.Add(1005, 2);

        var (foundIndex, foundLeaf) = leaf.FindByValue(1001);
        Assert.AreEqual(1, foundIndex);
        Assert.AreSame(leaf, foundLeaf);

        var (missingIndex, _) = leaf.FindByValue(1002);
        Assert.AreEqual(2, missingIndex);
    }

    [TestMethod]
    public void LongsLeafUsesFirstValueWhenIndexDiffers()
    {
        var leaf = new LongsLeaf(1_000_000, 5);
        leaf.Add(1_000_004, 6);

        Assert.AreEqual(6, leaf.FindByValue(1_000_004).index);
        CollectionAssert.AreEqual(new[] { 1_000_000L, 1_000_004L }, new List<long>(leaf));
    }

    [TestMethod]
    public void LongsLeafChainsWhenFull()
    {
        var first = new LongsLeaf(0, 0);
        var current = first;
        for (var i = 1; i <= 64 * 1024; i++)
        {
            var next = current.Add(i, i);
            if (next != null)
                current = next;
        }

        Assert.IsNotNull(first.Next);
        Assert.AreNotSame(first, current);
        Assert.AreEqual(0, first[0]);
        Assert.AreEqual(64 * 1024, current[0]);
    }

    [TestMethod]
    public void LongsLeafNonGenericEnumerator()
    {
        var leaf = new LongsLeaf(5, 0);
        leaf.Add(6, 1);

        var values = new List<long>();
        var enumerator = ((IEnumerable)leaf).GetEnumerator();
        while (enumerator.MoveNext())
            values.Add((long)enumerator.Current!);

        CollectionAssert.AreEqual(new[] { 5L, 6L }, values);
    }

    [TestMethod]
    public void SimpleLeafNonGenericEnumerator()
    {
        var leaf = new SimpleLeaf<int>(5, 0);
        leaf.Add(6, 1);

        var values = new List<int>();
        var enumerator = ((IEnumerable)leaf).GetEnumerator();
        while (enumerator.MoveNext())
            values.Add((int)enumerator.Current!);

        CollectionAssert.AreEqual(new[] { 5, 6 }, values);
    }
}
