using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Logging.Configuration;

namespace VenEl.MCPAssistant.Logging.Providers;

[ProviderAlias("File")]
public class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerOptions _options;
    private readonly string _logFilePath;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _writeTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public FileLoggerProvider(IOptions<FileLoggerOptions> options)
    {
        _options = options.Value;
        
        if (!Path.IsPathRooted(_options.LogDirectory))
        {
            _options.LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.LogDirectory);
        }

        if (!Directory.Exists(_options.LogDirectory))
        {
            Directory.CreateDirectory(_options.LogDirectory);
        }

        CleanUpOldLogs();

        var dateSuffix = DateTime.Now.ToString("yyyyMMdd");
        _logFilePath = Path.Combine(_options.LogDirectory, $"{_options.LogFileNamePrefix}-{dateSuffix}.log");

        _writeTask = Task.Run(ProcessQueueAsync);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    internal void EnqueueLog(string logEntry)
    {
        if (!_disposed)
        {
            _logQueue.Enqueue(logEntry);
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            await WritePendingLogsAsync();
            await Task.Delay(100);
        }

        // Flush any remaining
        await WritePendingLogsAsync();
    }

    private async Task WritePendingLogsAsync()
    {
        if (_logQueue.IsEmpty) return;

        await _semaphore.WaitAsync();
        try
        {
            using var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);

            while (_logQueue.TryDequeue(out var logEntry))
            {
                await writer.WriteLineAsync(logEntry);
            }
        }
        catch
        {
            // Fail silently if we can't write to avoid crashing the process.
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CleanUpOldLogs()
    {
        try
        {
            var files = new DirectoryInfo(_options.LogDirectory)
                .GetFiles($"{_options.LogFileNamePrefix}-*.log")
                .OrderByDescending(f => f.CreationTime)
                .Skip(_options.RetainedFileCount)
                .ToList();

            foreach (var file in files)
            {
                file.Delete();
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cancellationTokenSource.Cancel();
            try
            {
                _writeTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
            _cancellationTokenSource.Dispose();
            _semaphore.Dispose();
        }
    }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logBuilder = new System.Text.StringBuilder();
        logBuilder.Append($"[{timestamp}] [{logLevel}] [{_categoryName}] {message}");

        if (exception != null)
        {
            logBuilder.AppendLine();
            logBuilder.Append(exception.ToString());
        }

        _provider.EnqueueLog(logBuilder.ToString());
    }
}
