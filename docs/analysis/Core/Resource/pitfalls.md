# Core/Resource — 不变量与陷阱

1. **身份不是文案。** 菜单路径 `shell.file`/`shell.view`/`shell.help`、设置分组 `framework.general` 不随资源键重命名或翻译改变；不同路径即使使用同一标题键也不会合并。
2. **owner 全名就是资源基名。** owner 的命名空间、目录与同名 resx 必须一致；移动类型或文件时同步迁移其两份资源。模块 facade 必须 public，供跨程序集注册查询与 XAML 使用。
3. **键仅在 owner 内唯一。** `nameof` 检查 C# 属性，却不检查 XML 键存在；neutral/en-US 两份键集必须相同。保留用途 comment，不因翻译相同合并不同职责的键。
4. **缺键和缺清单不同。** 前者返回键，后者抛异常；不增加 catch 把坏 manifest 伪装成普通漏翻译。
5. **缓存不固定语言。** manager 按 Type 缓存，文字每次按 CurrentUICulture 查询。已存入贡献/视图模型的标题不自动重算，当前应用语言设置需重启。
6. **Core 依赖不能随资源下移删除。** 模块 facade 仍调用 Core/Resource 的 ResourceText；产品名仍由 SharedResources 共享。
7. **不要为所有私有文案建立共享别名。** 菜单、主页与关于标题属于 Workstation；启动进度属于 DashBoard；Settings 页只拥有重启提示，框架常规设置名与枚举项属于 Framework。
