# Unity 红点系统 (RedDotSystem)

一个功能完整的Unity红点管理系统，支持层级结构、自动传播和可视化编辑器工具。

## 功能特性

- 🎯 **层级结构支持**: 支持无限层级的红点路径配置
- 🔄 **自动传播**: 子节点状态变化自动向上传播到父节点
- 🎨 **可视化编辑器**: 提供友好的编辑器工具进行配置管理
- 📊 **实时预览**: 在编辑器中实时预览红点状态
- 🔧 **易于集成**: 简单的API设计，快速集成到现有项目
- 📝 **配置验证**: 自动验证配置的有效性和完整性

## 系统架构

```
RedDotManager (单例)
├── RedDotNode (树节点)
├── RedDotSetting (配置数据)
├── RedDotView (UI组件)
└── Editor Tools (编辑器工具)
```

## 快速开始

### 1. 创建配置文件

1. 在Project窗口中右键 → Create → RedDotSystem → Red Dot Setting
2. 使用编辑器工具添加红点路径

### 2. 初始化系统

```csharp
using RedDotSystem;

// 从Resources文件夹加载配置
RedDotManager.Instance.InitializeFromResources("RedDotSetting");

// 或者直接传入配置对象
RedDotManager.Instance.Initialize(redDotSetting);
```

### 3. 设置红点状态

```csharp
// 设置叶子节点的红点状态
RedDotManager.Instance.SetRedDot("Mail/System", true);
RedDotManager.Instance.SetRedDot("Shop/NewItem", false);

// 增加/减少红点数量
RedDotManager.Instance.AddRedDot("Mail/System", 1);
RedDotManager.Instance.RemoveRedDot("Mail/System", 1);
```

### 4. 在UI中使用

1. 将 `RedDotView` 组件添加到UI GameObject
2. 设置红点路径和目标显示对象
3. 系统会自动监听状态变化并更新UI

## 核心组件

### RedDotManager

红点系统的核心管理器，负责：
- 初始化红点树结构
- 管理红点状态
- 处理事件注册和通知

**主要方法：**
- `Initialize(RedDotSetting setting)`: 初始化系统
- `SetRedDot(string path, bool isActive)`: 设置红点状态
- `AddRedDot(string path, int count)`: 增加红点数量
- `RemoveRedDot(string path, int count)`: 减少红点数量
- `Register(string path, Action<bool> callback)`: 注册状态变化回调
- `Unregister(string path, Action<bool> callback)`: 注销回调

### RedDotSetting

ScriptableObject配置文件，存储所有红点路径定义。

**配置项：**
- `paths`: 红点路径列表
- `debugInfo`: 调试信息

### RedDotView

MonoBehaviour组件，用于在UI中显示红点。

**配置项：**
- `redDotPath`: 监听的红点路径
- `redDotObject`: 红点显示对象
- `config`: 红点配置文件

### RedDotNode

红点树的节点类，支持层级结构。

## 编辑器工具

### RedDotSettingEditor

提供可视化的红点路径配置工具：
- 添加/删除红点路径
- 配置父子关系
- 验证配置有效性
- 实时预览路径结构

### RedDotViewEditor

提供红点视图组件的编辑器工具：
- 路径选择器
- 配置自动查找
- 实时状态预览
- 快速设置工具

## 配置示例

```csharp
// 红点路径示例
"Mail/System"      // 邮件系统红点
"Mail/Personal"    // 个人邮件红点
"Shop/NewItem"     // 商店新物品红点
"Shop/Discount"    // 商店折扣红点
"Task/Daily"       // 日常任务红点
"Task/Weekly"      // 周常任务红点
```

## 使用最佳实践

1. **路径命名规范**: 使用 `模块/子模块` 的格式
2. **层级设计**: 合理设计层级结构，便于管理
3. **配置验证**: 定期使用编辑器工具验证配置
4. **性能优化**: 避免频繁的状态变化
5. **错误处理**: 在关键位置添加错误检查

## API 参考

### RedDotManager

```csharp
public class RedDotManager
{
    // 单例实例
    public static RedDotManager Instance { get; }
    
    // 初始化方法
    public void Initialize(RedDotSetting setting);
    public void InitializeFromResources(string resourcePath = "RedDotSetting");
    
    // 状态控制
    public void SetRedDot(string path, bool isActive);
    public void AddRedDot(string path, int count);
    public void RemoveRedDot(string path, int count);
    public void ClearRedDot(string path);
    
    // 事件管理
    public void Register(string path, Action<bool> callback);
    public void Unregister(string path, Action<bool> callback);
    
    // 查询方法
    public bool IsRedDotActive(string path);
    public int GetRedDotCount(string path);
    public RedDotNode GetNode(string path);
}
```

### RedDotSetting

```csharp
public class RedDotSetting : ScriptableObject
{
    public List<RedDotPathData> paths;
    
    public List<string> GetAllLeafPaths();
    public List<RedDotPathData> GetAllPaths();
    public bool HasPath(string path);
    public RedDotPathData GetPathData(string path);
    public bool ValidateConfiguration(out List<string> errors);
}
```

## 版本信息

- **Unity版本**: 2020.3 LTS 或更高
- **命名空间**: `RedDotSystem`
- **依赖**: 仅依赖Unity核心模块

## 许可证

MIT License

## 贡献

欢迎提交Issue和Pull Request来改进这个系统。

## 更新日志

### v1.0.0
- 初始版本发布
- 支持基础红点功能
- 提供编辑器工具
- 完整的API文档
