# Core/Resource — 模块关系链

Core/Resource 仅依赖 .NET BCL：ConcurrentDictionary 缓存、ResourceManager 查找、CultureInfo.CurrentUICulture 选择语言；不依赖 Framework 或 Modules。

Framework、Workstation、DashBoard、Settings 保留 Core/Resource 项目引用。各自的资源 facade 调 ResourceText，共享产品显示读 SharedResources；Framework 注册器从贡献 attribute 指定的 Type 查找，设置页从贡献指定的 Type 查找，不反向引用具体 owner 程序集。

```text
模块声明 typeof(Owner) + nameof(Owner.Key)
  → Framework 注册器 / Settings 模型
  → Core.Resource.ResourceText
  → Owner 全名对应、Owner.Assembly 内的资源
```

例如 SharedResources 的中性资源名是 `DigitalWorkstation.Core.Resource.SharedResources.resources`，主程序集为 `DigitalWorkstation.Core.Resource.dll`，英文卫星为 `en-US/DigitalWorkstation.Core.Resource.resources.dll`。Workstation 对应资源名为 `DigitalWorkstation.Workstation.Resources.WorkstationResources.resources`，英文卫星为 `en-US/DigitalWorkstation.Workstation.resources.dll`。其他 owner 同理，构建命名规则来自 Build/Base.props。

MenuTreeBuilder 只接收稳定 Path 和已解析 PathTitle，不依赖 ResourceText。资源文件分布见 file-list.md，新增文本配方见 common.md。
