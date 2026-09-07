# CONTEXT.md

项目领域词汇表。只收录领域含义，不含实现细节。

**面板对齐（Panel Alignment）**：BottomPanel 在窗口底部的水平跨度，类比文本对齐。四档：左对齐（面板贴左，横跨 SideBar 与 MainContent 下方，AuxiliaryPanel 通高到底）、右对齐（贴右，横跨 MainContent 与 AuxiliaryPanel 下方，SideBar 通高到底）、居中（仅占 MainContent 下方，默认；两侧栏均通高）、两端对齐（横跨三列全宽）。侧栏隐藏时对应列宽归零，面板自然伸缩。

_避免_：面板位置（Panel Position）——指把面板整体搬到窗口左侧/右侧/底部，是另一回事，与面板对齐无关。
