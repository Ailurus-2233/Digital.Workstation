# Core/Resource — 对外接口与调用方式

命名空间：`DigitalWorkstation.Core.Resource`。

| API | 行为 |
|---|---|
| `ResourceText.Get(Type resourceType, string key): string` | 使用 owner 类型全名与程序集查找当前 UI 语言文本；缺键返回 key |
| `SharedResources.ProductName: string` | 中文「数字工作站」，en-US `Digital Workstation`；本模块唯一共享文案 |

```csharp
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Workstation.Resources;

var product = SharedResources.ProductName;
var title = ResourceText.Get(typeof(WorkstationResources), nameof(WorkstationResources.MenuExitTitle));
```

```csharp
[MenuGroup("shell.file", typeof(WorkstationResources), nameof(WorkstationResources.MenuFileTitle))]
public class FileMenus
{
    [MenuItem(typeof(WorkstationResources), nameof(WorkstationResources.MenuExitTitle))]
    public void Exit() { /* 业务动作 */ }
}
```

XAML 使用共享命名空间的 `{x:Static resource:SharedResources.ProductName}`；模块私有内容绑定本模块 `.Resources` 下的 owner 静态属性。没有全局 Language 门面或隐式所有者重载。

菜单挂接使用 `[MenuGroup("shell.file")]`，只引用稳定路径，不提供或猜测资源所有者；该形式的 ResourceType/TitleKey 为 null，贡献 PathTitle 为 null，不抢占首次标题声明。真正声明节点标题才使用三参数形式。外部模块的 MenuItem/Command 使用自己的资源 owner，无需依赖 Workstation。

## 设置消费者

设置项名称：`ResourceText.Get(contribution.ResourceType, contribution.Name)`。枚举选项在同一 owner 中查「名称键 + 枚举成员名」，缺键返回完整组合键，而非单独成员名。

设置组以 `contribution.Id` 为 Key；显式组用其 ResourceType/Name 查标题，隐式组 ResourceType 为 null，直接显示 Name（等于 Id）。Framework 常规组 Id 为 `framework.general`，`GeneralSettings.LanguageSettingId` 与 Language 属性不变，原用户设置继续可读。
