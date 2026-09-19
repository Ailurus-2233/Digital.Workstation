using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Settings.Views;
using DigitalWorkstation.Core.Framework.Contributions;

namespace DigitalWorkstation.Settings;

/// <summary>
///     Settings 模块：向 MainContent 贡献设置页主视图（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 5）——
///     普通主视图贡献，走既有「收集 → OpenMainViewEvent → MainContent 切换」管线，无特权机制；
///     模块自身不注册演示设置项
/// </summary>
public class SettingsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterShellContribution<IMainViewContribution, SettingsMainView>();
        containerRegistry.Register<SettingsPageView>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
