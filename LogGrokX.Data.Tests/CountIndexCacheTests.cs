using System.Collections.Generic;
using LogGrokX.Data.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class CountIndexCacheTests
{
    [TestMethod]
    public void CountsReuseSnapshotUntilNextLineAndFinish()
    {
        var key = new IndexKeyNum { KeyNum = 1 };
        var index = new LogGrokX.Data.Index.Index(4);
        var indices = new Dictionary<IndexKeyNum, LogGrokX.Data.Index.Index> { [key] = index };
        var counts = new CountIndex<LogGrokX.Data.Index.Index>(indices);

        index.Add(0);
        counts.Add(0, indices);
        var first = counts.Counts;
        Assert.AreSame(first, counts.Counts);
        Assert.AreEqual(1, first[^1][0].Item2);

        index.Add(1);
        counts.Add(1, indices);
        var second = counts.Counts;
        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, second[^1][0].Item2);

        counts.Finish(indices);
        var finished = counts.Counts;
        Assert.AreSame(finished, counts.Counts);
        Assert.AreEqual(2, finished[^1][0].Item2);
    }
}
