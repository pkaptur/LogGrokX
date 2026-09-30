using LogGrokX.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class StringPoolLimitTests
{
    [TestMethod]
    public void ReturnCapsEachBucketAndRentDecrementsCount()
    {
        var pool = new StringPool();
        _ = pool.Rent(50);
        for (var i = 0; i < 200; i++)
            pool.Return(new string('\0', 64));

        Assert.AreEqual(64, pool.GetPooledCount(50));
        _ = pool.Rent(50);
        Assert.AreEqual(63, pool.GetPooledCount(50));
        Assert.AreEqual(0, pool.GetPooledCount(100));
    }
}
