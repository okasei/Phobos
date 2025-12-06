# Plugin Installer 重构说明

## 概述

本次重构将插件安装器升级为支持两种模式的交互界面：

1. **用户主动打开模式 (UserOpen)** - 显示文件选择界面，支持返回操作
2. **调用模式 (Invoked)** - 通过 URI 或代码调用时直接显示插件详情

## 文件变更

需要替换以下文件：

| 文件 | 位置 |
|------|------|
| PCOInstaller.xaml | `Components/Arcusrix/Installer/` |
| PCOInstaller.xaml.cs | `Components/Arcusrix/Installer/` |
| PCPluginInstaller.cs | `Class/Plugin/BuiltIn/` |

## 新功能

### 1. 双模式支持

#### 用户主动打开模式
- 显示拖放区域和文件选择按钮
- 选择文件后进入详情页
- 详情页显示"返回"按钮，可返回文件选择界面

#### 调用模式
- 通过 URI 或代码调用时直接进入详情页
- 不显示"返回"按钮
- 安装完成后自动触发退出事件

### 2. 插件详情显示

- **图标** - 从插件元数据加载
- **名称** - 支持本地化
- **版本** - 显示版本号
- **制造商** - 显示插件制造商
- **包名** - 显示完整包名
- **介绍** - 支持本地化的插件描述

### 3. 依赖检查与显示

依赖分为三类，分别用不同颜色和图标区分：

| 类型 | 颜色 | 图标 | 说明 |
|------|------|------|------|
| 缺少必需依赖 | 红色 | ❌ | 无法安装，必须先安装依赖 |
| 缺少可选依赖 | 橙色 | ⚡ | 可以安装，但部分功能可能受限 |
| 已满足依赖 | 默认 | ✓ | 依赖已安装 |

### 4. URI 协议支持

支持以下 URI 格式：

```
# 通过查询参数指定路径
pi://install?path=C:\path\to\plugin.dll

# 通过路径指定
pi://install/C:/path/to/plugin.dll
```

### 5. 代码调用接口

```csharp
// 方式1: 直接安装（无 UI 确认）
await PMPlugin.Instance.Install("C:\\path\\to\\plugin.dll");

// 方式2: 通过安装器 UI 安装
var installer = PMPlugin.Instance.GetPlugin("com.phobos.plugin.installer");
await installer.Run("open", "C:\\path\\to\\plugin.dll");

// 方式3: 启动时带参数
await PMPlugin.Instance.Launch("com.phobos.plugin.installer", "C:\\path\\to\\plugin.dll");
```

## UI 结构

```
PCOInstaller (UserControl)
├── FileSelectView (Grid) ─────────────── 文件选择视图
│   ├── 标题
│   ├── DropZone (Border) ─────────────── 拖放区域
│   │   ├── 图标
│   │   ├── 提示文字
│   │   └── BrowseButton ──────────────── 浏览按钮
│   └── FileSelectStatus ──────────────── 状态栏
│
├── PluginDetailView (Grid) ───────────── 插件详情视图
│   ├── BackButton ────────────────────── 返回按钮 (仅 UserOpen 模式)
│   ├── 插件基本信息
│   │   ├── PluginIcon
│   │   ├── PluginNameText
│   │   ├── PluginVersionText
│   │   ├── PluginManufacturerText
│   │   └── PluginPackageNameText
│   ├── 内容区域 (ScrollViewer)
│   │   ├── 介绍 (PluginDescriptionText)
│   │   └── DependencySection
│   │       ├── MissingRequiredPanel
│   │       ├── MissingOptionalPanel
│   │       └── SatisfiedDependencyPanel
│   └── 底部按钮区域
│       ├── InstallStatus
│       ├── ExitButton
│       └── InstallButton
│
└── LoadingOverlay (Border) ───────────── 加载遮罩
```

## 事件

| 事件 | 说明 |
|------|------|
| `InstallCompleted` | 安装完成时触发，包含成功状态和消息 |
| `ExitRequested` | 用户点击退出或安装完成后触发 |

## 主题集成

使用以下动态资源键：

- `PrimaryBrush` / `PrimaryHoverBrush` - 主按钮颜色
- `Background2Brush` / `Background3Brush` - 背景色
- `Foreground1Brush` ~ `Foreground4Brush` - 文字颜色
- `BorderBrush` - 边框颜色

## 注意事项

1. 需要添加 `System.Web` 引用以支持 URI 查询字符串解析
2. `PluginAssemblyLoadContext` 需要从 `Manager/Plugin/PMPlugin.cs` 中导入
3. 确保主题资源字典中定义了所有需要的 Brush 资源

## 迁移指南

1. 备份原有的 `PCOInstaller.xaml` 和 `PCOInstaller.xaml.cs`
2. 替换为新文件
3. 替换 `PCPluginInstaller.cs`
4. 编译并测试两种模式