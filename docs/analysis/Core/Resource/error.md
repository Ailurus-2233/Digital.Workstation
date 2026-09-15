# Core/Resource — 异常与排查

| 症状/异常 | 实际行为与排查入口 |
|---|---|
| 显示完整资源键 | owner 的中性资源也无此键；检查 attribute.ResourceType、nameof 与该 owner resx 的 data name（区分大小写）。不会搜索其他 owner |
| en-US 显示中文 | 英文条目或卫星程序集缺失，ResourceManager 回退中文；检查 owner 的 `.en-US.resx` 与输出中的 `en-US/<owner程序集>.resources.dll` |
| `MissingManifestResourceException` | owner 全名与资源基名不匹配，或整个中性资源清单未嵌入；首次查找时异常传播，不转换成键名。检查 namespace、同名 resx、RootNamespace、嵌入资源逻辑名 |
| `InvalidOperationException` | `GetString` 命中了非字符串资源；只维护字符串 value |
| null 参数 | 非法调用遵循 ConcurrentDictionary/ResourceManager 的参数异常，不是缺键回退；公共契约要求非 null Type/key |
| 设置组显示稳定 Id | 未声明分组，collector 创建隐式组；补齐同 Id 的 SettingGroup 声明，不把 Id 换成资源键 |

模块不记录日志、不吞异常、不切换 UI 区域性。`ResourceText.Get` 在每次读取时使用 CurrentUICulture，但注册时已解析的菜单/命令/工具视图标题不会自动刷新；语言设置下次启动生效。
