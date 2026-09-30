# Common/Events — 手动验证

事件声明没有独立测试逻辑，不新增自动化测试。按 docs/agents/verification.md 在仓库根运行：

    dotnet run --project Launcher/Launcher.csproj -c Debug

观察启动台进度后进入主页。通过文件 > 首选项打开设置，再用文件 > 回到主页返回；用视图菜单切换面板并重置布局。更改语言后重启标记出现，改回原值后消失。存在失败模块时，启动台 Continue/Exit 仍按启动协议工作。

迁移仅改变事件的命名空间与程序集归属，负载和业务行为保持原样。运行验证尚待用户确认。