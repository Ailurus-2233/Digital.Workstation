# Core/Resource — 术语表

| 术语 | 定义 |
|---|---|
| 资源所有者（resource owner） | 与同名 resx 同位的公开静态类型；其全名+程序集唯一定位资源族 |
| ResourceText | 跨程序集共享查找机制，不拥有模块私有文本 |
| SharedResources | 共享产品语义，目前只有 ProductName |
| 中性资源 | 不带区域性后缀的 owner.resx，中文最终回退 |
| 卫星资源 | owner.en-US.resx 编译进入 owner 所属程序集的 en-US 卫星程序集 |
| 资源键 | owner 内 data name，与 facade 属性同名；不是全局 Id |
| 稳定 Id | 菜单路径段/设置组等的结构身份，不参与翻译 |
| 缺键回退 | 同 owner 文化回退链都无键时返回原键；不覆盖 manifest 错误 |

Workstation 的 StatusReadyTitle 与 DashBoard 的 SplashPhaseReady 用途不同，中文同为「就绪」也不合并。图标属于 Core/UIPackage，不属于本模块。
