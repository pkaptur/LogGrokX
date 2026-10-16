using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using LogGrokX.Data.Index;
using LogGrokX.Data.Monikers;

namespace LogGrokX.Data.Tests;

internal static class TestHelpers
{
    public static LogMetaInformation CreateMeta(string regex,
        string[]? indexedFields = null,
        string? timeField = null,
        string? timeFormat = null,
        byte xorMask = 0)
    {
        var format = new LogFormat
        {
            Regex = regex,
            IndexedFields = indexedFields ?? Array.Empty<string>(),
            TimeField = timeField ?? string.Empty,
            TimeFormat = timeFormat ?? string.Empty,
            XorMask = xorMask
        };
        return new LogMetaInformation(format);
    }

    public static IndexKey CreateIndexKey(LogMetaInformation meta, string line, out string buffer)
    {
        return CreateIndexKey(meta, line, true, out buffer);
    }

    public static IndexKey CreateIndexKey(LogMetaInformation meta, string line, bool onlyIndexed, out string buffer)
    {
        var componentCount = onlyIndexed ? meta.IndexedFieldNumbers.Length : meta.ComponentCount;
        var parser = new RegexBasedLineParser(meta, onlyIndexed);
        var placeholder = new int[LineMetaInformation.GetSizeInts(componentCount)];
        var lineMetaInformation = new LineMetaInformation(placeholder.AsSpan(), componentCount);

        if (!parser.TryParse(line, 0, line.Length, lineMetaInformation.ParsedLineComponents, out _))
            throw new InvalidOperationException($"Line does not match the format: {line}");

        buffer = BuildBuffer(placeholder, line);
        return new IndexKey(buffer, 0, componentCount);
    }

    private static string BuildBuffer(int[] placeholder, string line)
    {
        var metaChars = MemoryMarshal.Cast<int, char>(placeholder.AsSpan());
        var chars = new char[metaChars.Length + line.Length];
        metaChars.CopyTo(chars.AsSpan());
        line.AsSpan().CopyTo(chars.AsSpan(metaChars.Length));
        return new string(chars);
    }

    public static string WriteTempFile(string content, System.Text.Encoding? encoding = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"loggrokx-{Guid.NewGuid():N}.log");
        File.WriteAllText(path, content, encoding ?? new System.Text.UTF8Encoding(false));
        return path;
    }

    public static string WriteTempBytes(byte[] content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"loggrokx-{Guid.NewGuid():N}.log");
        File.WriteAllBytes(path, content);
        return path;
    }

    public static LogModelFacade LoadFile(string path, LogMetaInformation meta, byte xorMask = 0, int bufferSize = 4096)
    {
        var logFile = new LogFile(path, xorMask);
        var lineIndex = new LineIndex();
        var indexer = new Indexer();
        var stringPool = new StringPool();
        var timeIndex = new TimeIndex();
        using var parsedBufferConsumer = new ParsedBufferConsumer(lineIndex, indexer, meta, stringPool);
        var indexParser = new RegexBasedLineParser(meta, true);
        var lineProcessor = new LineProcessor(logFile, meta, indexParser, parsedBufferConsumer, stringPool, timeIndex);
        var loaderImpl = new LoaderImpl(bufferSize, lineProcessor);

        var encoding = logFile.Encoding;
        using (var stream = logFile.OpenForSequentialRead())
        {
            loaderImpl.Load(stream, encoding.GetBytes("\r"), encoding.GetBytes("\n"), CancellationToken.None);
        }

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!lineIndex.IsFinished && DateTime.UtcNow < deadline)
            Thread.Sleep(5);

        if (!lineIndex.IsFinished)
            throw new TimeoutException("Loading did not finish in time.");

        var lineProvider = new LineProvider(lineIndex, logFile);
        var displayParser = new RegexBasedLineParser(meta);
        return new LogModelFacade(logFile, lineIndex, lineProvider, displayParser, indexer, meta);
    }
}
