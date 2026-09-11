using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Settings.Views;

namespace DigitalWorkstation.Settings;

/// <summary>
///     Settings 模块：向 MainContent 贡献设置页主视图（ADR-0006 决策 5）——
///     普通主视图贡献，走既有「收集 → OpenMainViewEvent → MainContent 切换」管线，无特权机制；
///     模块自身不注册演示设置项
/// </summary>
public class SettingsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IMainViewContribution, SettingsMainView>();
        containerRegistry.Register<SettingsPageView>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
