using System.Text.Json;

namespace DigitalWorkstation.Core.Framework.Persistence;

/// <summary>
///     统一管理配置文件的写入生命周期：重启前完成保存，应用退出时完成保存并释放计时器。
/// </summary>
public sealed class ConfigurationPersistence : IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<IConfigurationFile> _files = [];
    private bool _disposed;

    internal DebouncedJsonFile<T> CreateFile<T>(string path, JsonSerializerOptions options) where T : class
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var file = new DebouncedJsonFile<T>(path, options);
            _files.Add(file);
            return file;
        }
    }

    /// <summary>
    ///     等待在途写入并保存所有待写快照；任一文件失败返回 false，失败快照保留以供重试。
    /// </summary>
    public bool FlushPending()
    {
        lock (_gate)
        {
            var succeeded = true;
            foreach (var file in _files)
            {
                succeeded &= file.FlushPending();
            }

            return succeeded;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var file in _files)
            {
                file.Dispose();
            }
        }
    }
}

internal interface IConfigurationFile : IDisposable
{
    bool FlushPending();
}
