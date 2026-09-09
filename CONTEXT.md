# CONTEXT.md

项目领域词汇表。只收录领域含义，不含实现细节。

**面板对齐（Panel Alignment）**：BottomPanel 在窗口底部的水平跨度，类比文本对齐。四档：左对齐（面板贴左，横跨 SideBar 与 MainContent 下方，AuxiliaryPanel 通高到底）、右对齐（贴右，横跨 MainContent 与 AuxiliaryPanel 下方，SideBar 通高到底）、居中（仅占 MainContent 下方，默认；两侧栏均通高）、两端对齐（横跨三列全宽）。侧栏隐藏时对应列宽归零，面板自然伸缩。

_避免_：面板位置（Panel Position）——指把面板整体搬到窗口左侧/右侧/底部，是另一回事，与面板对齐无关。

**菜单路径（Menu Path）**：菜单项在菜单栏中的位置，以 "/" 分隔的多级名称（如 `File/Export`）。首段是顶层菜单；中间各段是子菜单节点；路径不限深度。

**菜单组（Menu Group）**：同一父菜单内、由分隔线（Separator）隔开的命名分区。不同组之间自动插入分隔线；组按 GroupOrder 排序，组内条目按 Order 排序。顶层菜单（菜单栏本身）不分组、不插分隔线，只按 Order 排序。未指定组的条目归入无名默认组，默认组排在所有命名组之前；一个菜单只有默认组时不插任何分隔线。

_避免_：菜单分类（Menu Category）——常被错当成菜单组；分类是语义标签，菜单组是带排序权重和分隔线行为的布局概念。

**工具视图（Tool View）**：带图标与标题的可停靠界面单元，内容是一个视图。可栖身于三处 Bar：ActivityBar（内容显示在 SideBar）、AuxiliaryPanel、BottomPanel；用户可在 Bar 之间拖拽迁移，排列与布局状态被记住，重启后恢复。

_避免_：面板 tab（Panel Tab）／导航项（Navigation Item）——统一前的旧称；统一后二者都是工具视图，只是栖身的 Bar 不同。

**钉住项（Pinned Item）**：声明为不可拖拽的工具视图，固定在 ActivityBar 底部段（如设置）。底部段是钉住区，不接受拖放。
