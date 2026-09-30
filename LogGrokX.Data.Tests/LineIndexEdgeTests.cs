using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class LineIndexEdgeTests
{
    [TestMethod]
    public void FetchThrowsWhenStartBeyondEnd()
    {
        var index = new LineIndex();
        index.Add(0);
        index.Add(10);
        index.Add(20);
        index.Finish(5);

        Assert.Throws<IndexOutOfRangeException>(() => index.Fetch(3, new (long, int)[1]));
    }

    [TestMethod]
    public void FetchThrowsWhenBufferExceedsRemainingOnUnfinishedIndex()
    {
        var index = new LineIndex();
        index.Add(0);
        index.Add(10);
        index.Add(20);

        Assert.Throws<IndexOutOfRangeException>(() => index.Fetch(0, new (long, int)[5]));
    }

    [TestMethod]
    public async Task FetchRangesRefreshesCountAfterDelay()
    {
        var index = new LineIndex();
        index.Add(0);

        var finisher = Task.Run(async () =>
        {
            await Task.Delay(50);
            index.Finish(10);
        });

        var ranges = new List<(int, int)>();
        await foreach (var range in index.FetchRanges(CancellationToken.None))
            ranges.Add(range);

        await finisher;

        CollectionAssert.AreEqual(new[] { (0, 1) }, ranges);
    }

    [TestMethod]
    public async Task FetchRangesWakesWhenEnoughLinesArrive()
    {
        var index = new LineIndex();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var ranges = index.FetchRanges(cancellation.Token).GetAsyncEnumerator();
        var next = ranges.MoveNextAsync().AsTask();

        for (var i = 0; i <= 256; i++)
            index.Add(i * 10L);

        Assert.IsTrue(await next.WaitAsync(TimeSpan.FromMilliseconds(200)));
        Assert.AreEqual((0, 256), ranges.Current);
        index.Finish(10);
    }

    [TestMethod]
    public async Task FetchRangesWakesWhenFinishedBelowBatchSize()
    {
        var index = new LineIndex();
        index.Add(0);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var ranges = index.FetchRanges(cancellation.Token).GetAsyncEnumerator();
        var next = ranges.MoveNextAsync().AsTask();

        index.Finish(10);

        Assert.IsTrue(await next.WaitAsync(TimeSpan.FromMilliseconds(200)));
        Assert.AreEqual((0, 1), ranges.Current);
    }
}
