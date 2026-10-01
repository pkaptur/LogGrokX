using System;
using System.IO;
using LogGrokX.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class LineProviderRangeTests
{
    private sealed class HugeRangeLineIndex : ILineIndex
    {
        public int Count => 2;
        public (long offset, int length) GetLine(int index) =>
            index == 0 ? (0, 0) : (3_000_000_000L, 10);
        public void Fetch(int start, Span<(long offset, int length)> values) =>
            throw new InvalidOperationException("Unexpected read");
    }

    [TestMethod]
    public void FetchRejectsRangeLargerThanInt32()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "sample");
            var provider = new LineProvider(new HugeRangeLineIndex(), new LogFile(path, 0));
            Assert.ThrowsExactly<InvalidOperationException>(() => provider.Fetch(0, new (int, string)[2]));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
