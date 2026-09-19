using System.Text.Json;
using DigitalWorkstation.Core.Common;

namespace DigitalWorkstation.Core.Framework.Persistence;

/// <summary>
///     单文件的防抖写入、强制保存与删除共享同一把锁；调用方交入独立快照，回调不访问调用方状态。
/// </summary>
internal sealed class DebouncedJsonFile<T>(string path, JsonSerializerOptions options) : IConfigurationFile
    where T : class
{
    private const int DebounceMilliseconds = 500;
    private readonly Lock _gate = new();
    private T? _pending;
    private Timer? _timer;
    private bool _disposed;

    public void ScheduleSave(T snapshot)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pending = snapshot;
            _timer ??= new Timer(Flush, null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }

    public bool FlushPending()
    {
        lock (_gate)
        {
            if (_disposed) return _pending is null;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            return SavePending();
        }
    }

    public void Delete()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pending = null;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                File.Delete(path);
            }
            catch (Exception exception)
            {
                Logger.Warning($"Failed to delete configuration ({exception.GetType().Name}): {path}",
                    nameof(ConfigurationPersistence));
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            SavePending();
            _disposed = true;
            _timer?.Dispose();
        }
    }

    private void Flush(object? state)
    {
        lock (_gate)
        {
            if (!_disposed) SavePending();
        }
    }

    // 调用方须持有 _gate，锁覆盖文件提交，避免旧回调在删除或较新快照提交后再次写回。
    private bool SavePending()
    {
        if (_pending is null) return true;

        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, _pending, options);
                stream.Flush(flushToDisk: true);
            }

            // 完整写入同目录临时文件后再替换；序列化或写入失败时旧配置仍可读取。
            File.Move(temporaryPath, path, overwrite: true);
            _pending = null;
            return true;
        }
        catch (Exception exception)
        {
            // Timer 回调不能让异常逃逸；保留快照，下一次保存或退出时仍可重试。
            Logger.Warning($"Failed to write configuration ({exception.GetType().Name}): {path}",
                nameof(ConfigurationPersistence));
            return false;
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception)
                {
                    Logger.Warning($"Failed to remove temporary configuration ({exception.GetType().Name}): {temporaryPath}",
                        nameof(ConfigurationPersistence));
                }
            }
        }
    }
}
