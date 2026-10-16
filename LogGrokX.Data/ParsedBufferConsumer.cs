using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LogGrokX.Data.Index;
using LogGrokX.Data.Monikers;

namespace LogGrokX.Data
{
    public sealed class ParsedBufferConsumer : IDisposable
    {
        private readonly BlockingCollection<(long startOffset, int lineCount, string buffer)> _queue
            = new(4);

        private readonly LineIndex _lineIndex;
        private readonly Indexer _indexer;
        private readonly LogMetaInformation _logMetaInformation;
        private readonly StringPool _stringPool;
        private readonly Task _consumingTask;
        private long? _totalBytesRead;
        private int _disposed;

        public ParsedBufferConsumer(
            LineIndex lineIndex,
            Indexer indexer,
            LogMetaInformation logMetaInformation,
            StringPool stringPool)
        {
            _lineIndex = lineIndex;
            _indexer = indexer;
            _logMetaInformation = logMetaInformation;
            _stringPool = stringPool;
            _consumingTask = Task.Factory.StartNew(ConsumeBuffers);
        }

        public void AddParsedBuffer(long bufferStartOffset, int lineCount, string parsedBuffer)
        {
            _queue.Add((bufferStartOffset, lineCount, parsedBuffer));
        }

        public void CompleteAdding(long totalBytesRead)
        {
            _totalBytesRead = totalBytesRead;
            _queue.CompleteAdding();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _queue.CompleteAdding();
            try
            {
                _consumingTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Trace.TraceError($"Indexing failed: {exception}");
            }
            finally
            {
                while (_queue.TryTake(out var pending))
                    _stringPool.Return(pending.buffer);
                _queue.Dispose();
            }
        }

        private unsafe void ConsumeBuffers()
        {
            long lineOffsetFromBufferStart = 0;

            var componentsCount = _logMetaInformation.IndexedFieldNumbers.Length;
            
#pragma warning disable CS8619
            foreach (var (bufferStartOffset, lineCount, buffer) in _queue.GetConsumingEnumerable())
#pragma warning restore CS8619
            {
                try
                {
                    var metaOffset = 0;
                    fixed (char* start = buffer)
                    {
                        for (int idx = 0; idx < lineCount; idx++)
                        {
                            var lineMetaInformation = LineMetaInformation.Get(start + metaOffset, componentsCount);
                            lineOffsetFromBufferStart = bufferStartOffset +
                                                            lineMetaInformation.LineOffsetFromBufferStart;
                            var lineNum = _lineIndex.Add(lineOffsetFromBufferStart);

                            var indexKey = new IndexKey(buffer, metaOffset, componentsCount);
                            _indexer.Add(indexKey, lineNum);
                            metaOffset += lineMetaInformation.TotalSizeWithPayloadCharsAligned;
                        }
                    }
                }
                finally
                {
                    _stringPool.Return(buffer);
                }
            }

            if (_totalBytesRead is { } fileSize)
            {
                _lineIndex.Finish((int) (fileSize - lineOffsetFromBufferStart));
                _indexer.Finish();
            }
        }
    }
}
