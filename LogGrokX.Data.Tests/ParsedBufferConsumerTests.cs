using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogGrokX.Data.Index;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class ParsedBufferConsumerTests
{
    [TestMethod]
    public async Task InitializationFailureStopsProducerAndReturnsRejectedBuffer()
    {
        var meta = TestHelpers.CreateMeta("^(?<Message>.*)$", ["Process"]);
        var pool = new StringPool();
        using var consumer = new ParsedBufferConsumer(new LineIndex(), new Indexer(), meta, pool);
        var producer = Task.Run(() =>
        {
            for (var i = 0; i < 16; i++)
                consumer.AddParsedBuffer(0, 0, pool.Rent(32));
        });
        try
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => producer.WaitAsync(TimeSpan.FromSeconds(5)));
            consumer.Dispose();
            var pooled = pool.GetPooledCount(32);
            Assert.IsTrue(pooled > 0);
            var buffer = pool.Rent(32);
            Assert.ThrowsExactly<ObjectDisposedException>(() => consumer.AddParsedBuffer(0, 0, buffer));
            Assert.AreEqual(pooled, pool.GetPooledCount(32));
        }
        finally
        {
            if (!producer.IsCompleted)
            {
                consumer.CompleteAdding(0);
                try { await producer.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (InvalidOperationException) { }
            }
        }
    }

    [TestMethod]
    public async Task IndexingFailureUnblocksFullQueueAndReturnsPendingBuffers()
    {
        var path = TestHelpers.WriteTempFile("INFO hello");
        using var indexing = new ManualResetEventSlim();
        using var fail = new ManualResetEventSlim();
        var meta = TestHelpers.CreateMeta("^(?<Level>INFO) (?<Message>.*)$", ["Level"]);
        var pool = new StringPool();
        var indexer = new Indexer();
        indexer.NewComponentAdded += _ =>
        {
            indexing.Set();
            fail.Wait();
            throw new InvalidOperationException("Indexing failed in component subscriber");
        };
        using var consumer = new ParsedBufferConsumer(new LineIndex(), indexer, meta, pool);
        Task? producer = null;
        try
        {
            var processor = new LineProcessor(new LogFile(path, 0), meta,
                new RegexBasedLineParser(meta, true), consumer, pool, new TimeIndex());
            processor.AddLineData(0, Encoding.UTF8.GetBytes("INFO hello"));
            processor.AddLineData(10, Encoding.UTF8.GetBytes("INFO " + new string('x', 128 * 1024)));
            Assert.IsTrue(indexing.Wait(TimeSpan.FromSeconds(5)));
            for (var i = 0; i < 4; i++)
                consumer.AddParsedBuffer(0, 0, pool.Rent(32));
            var rejected = pool.Rent(32);
            producer = Task.Run(() => consumer.AddParsedBuffer(0, 0, rejected));

            fail.Set();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => producer.WaitAsync(TimeSpan.FromSeconds(5)));
            await Task.Run(consumer.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(5, pool.GetPooledCount(32));
        }
        finally
        {
            fail.Set();
            consumer.Dispose();
            if (producer != null)
            {
                try { await producer.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (InvalidOperationException) { }
            }
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task IndexingFailureDoesNotBlockLoaderDisposal()
    {
        var path = TestHelpers.WriteTempFile(string.Concat(
            Enumerable.Repeat("INFO " + new string('x', 64 * 1024) + "\n", 32)));
        var meta = TestHelpers.CreateMeta("^(?<Level>INFO) (?<Message>.*)$", ["Level"]);
        var pool = new StringPool();
        var indexer = new Indexer();
        indexer.NewComponentAdded += _ => throw new InvalidOperationException("Indexing failed");
        using var consumer = new ParsedBufferConsumer(new LineIndex(), indexer, meta, pool);
        Loader? loader = null;
        try
        {
            var file = new LogFile(path, 0);
            var processor = new LineProcessor(file, meta, new RegexBasedLineParser(meta, true),
                consumer, pool, new TimeIndex());
            loader = new Loader(file, processor, NullLogger.Instance);

            Assert.IsTrue(await Task.Run(() => SpinWait.SpinUntil(() => !loader.IsLoading,
                TimeSpan.FromSeconds(5))));
            await Task.Run(loader.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
            loader = null;
        }
        finally
        {
            consumer.Dispose();
            loader?.Dispose();
            File.Delete(path);
        }
    }
}
