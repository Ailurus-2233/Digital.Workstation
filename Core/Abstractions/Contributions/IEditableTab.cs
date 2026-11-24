using System.ComponentModel;

namespace DigitalWorkstation.Core.Abstractions.Contributions;

/// <summary>主标签页的草稿状态。状态变化时通知 HasUnsavedChanges，供后台标签同步显示标记。</summary>
public interface IEditableTab : INotifyPropertyChanged
{
    bool HasUnsavedChanges { get; }
    string ClosePrompt { get; }
    /// <summary>每次草稿内容改变时递增，包括页面已经有未保存修改时。</summary>
    long DraftVersion { get; }

    /// <summary>页面自行启动且尚未结束的保存任务。没有独立保存入口时使用默认值。</summary>
    Task? PendingSave => null;

    /// <summary>保存成功返回 true。保存失败时由页面显示原因，宿主保留标签。</summary>
    Task<bool> SaveChangesAsync();
}
