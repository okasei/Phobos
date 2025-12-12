# PCOPhobosDesktop 窗口

Android 风格的插件桌面管理器，支持图标网格布局、文件夹、拖拽和布局持久化。

## 功能特性

### 窗口设计
- **自定义标题栏**：包含窗口图标、标题、最小化、最大化/还原、关闭按钮
- **无边框设计**：WindowStyle="None"，支持透明背景和圆角边框
- **可拖动**：点击标题栏可拖动窗口，双击标题栏可最大化/还原
- **可调整大小**：支持窗口大小调整，最小尺寸 800x600
- **主题集成**：完全使用 DynamicResource，支持主题切换

### 桌面布局
- **Android 风格图标网格**：类似 Android 桌面的图标排列方式
- **自适应网格**：根据窗口大小自动调整行列数（动态计算合理的 x×y 布局）
- **图标缩放动画**：鼠标悬停时图标放大（1.1x）并带有平滑过渡动画
- **文件夹支持**：可创建文件夹，将多个插件分组管理
- **布局持久化**：布局配置保存在 `%AppData%\Roaming\Phobos\Plugins\com.phobos.desktop\Layout\desktop_layout.json`
- **启动加载**：窗口启动时自动加载上次保存的布局

### 插件管理
- **数据库驱动**：从 Phobos_Plugin 表加载所有插件数据
- **内建插件识别**：自动识别 Directory 为 "builtin" 或 "Built-In" 的插件并标记为系统插件
- **图标显示**：64x64 圆角容器，48x48 图标尺寸
- **插件名称**：自动换行，最大宽度 80px

### 右键菜单

#### 插件图标右键菜单
- **打开 (Open)**：启动插件
- **信息 (Info)**：查看插件详细信息
- **设置 (Settings)**：打开插件设置页面
- **卸载 (Uninstall)**：卸载插件（系统插件不显示此选项）

#### 文件夹图标右键菜单
- **打开 (Open)**：打开文件夹查看内部插件
- **重命名 (Rename)**：重命名文件夹
- **删除 (Delete)**：删除文件夹（插件不会被删除）

#### 桌面空白区域右键菜单
- **进入/退出全屏 (Enter/Exit Fullscreen)**：切换全屏和窗口模式
- **设置 (Settings)**：打开桌面设置
- **新建文件夹 (New Folder)**：创建新文件夹

### 文件夹功能
- **弹出窗口**：点击文件夹后以模态遮罩方式显示文件夹内容
- **关闭方式**：点击遮罩层或窗口外部关闭文件夹
- **内容显示**：文件夹内的插件以网格形式展示
- **滚动支持**：文件夹内容过多时自动显示滚动条

## 使用主题资源

窗口使用以下 DynamicResource 键值：

### 颜色资源
- `Background1Brush` - 主背景色（窗口背景）
- `Background2Brush` - 次级背景色（标题栏、文件夹面板、上下文菜单）
- `Background3Brush` - 三级背景色（图标容器、悬停效果）
- `Background4Brush` - 四级背景色（边框）
- `Foreground1Brush` - 主前景色（插件名称、菜单项）
- `Foreground2Brush` - 次级前景色（窗口控制按钮）
- `Foreground3Brush` - 三级前景色
- `Foreground4Brush` - 四级前景色
- `BorderBrush` - 边框颜色
- `DangerBrush` - 危险色（关闭按钮悬停、卸载/删除菜单项）
- `Accent2Brush` - 备选色2（文件夹图标背景）

### 字体资源
- `FontSizeSm` - 小字号（插件名称）
- `FontSizeLg` - 大字号（文件夹标题）
- `FontWeightSemibold` - 半粗体（文件夹标题）

### 尺寸资源
- `BorderRadius` - 圆角半径

## 数据结构

### DesktopLayout（布局配置）
```csharp
public class DesktopLayout
{
    public int Version { get; set; } = 1;          // 布局版本
    public int Columns { get; set; } = 6;          // 网格列数
    public int Rows { get; set; } = 4;             // 网格行数
    public bool IsFullscreen { get; set; } = false; // 是否全屏模式
    public List<DesktopItem> Items { get; set; }   // 桌面项列表
    public List<FolderDesktopItem> Folders { get; set; } // 文件夹列表
}
```

### DesktopItem（桌面项基类）
```csharp
public class DesktopItem
{
    public string Id { get; set; }                 // 唯一标识
    public DesktopItemType Type { get; set; }      // 类型（Plugin/Folder）
    public int GridX { get; set; }                 // 网格 X 坐标
    public int GridY { get; set; }                 // 网格 Y 坐标
}
```

### PluginDesktopItem（插件桌面项）
```csharp
public class PluginDesktopItem : DesktopItem
{
    public string PackageName { get; set; }        // 插件包名
}
```

### FolderDesktopItem（文件夹桌面项）
```csharp
public class FolderDesktopItem : DesktopItem
{
    public string Name { get; set; }               // 文件夹名称
    public List<string> PluginPackageNames { get; set; } // 包含的插件包名列表
}
```

## 布局持久化

### 存储位置
```
%AppData%\Roaming\Phobos\Plugins\com.phobos.desktop\Layout\desktop_layout.json
```

### JSON 格式示例
```json
{
  "Version": 1,
  "Columns": 6,
  "Rows": 4,
  "IsFullscreen": false,
  "Items": [
    {
      "Id": "com.phobos.calculator",
      "Type": 0,
      "GridX": 0,
      "GridY": 0,
      "PackageName": "com.phobos.calculator"
    },
    {
      "Id": "folder-123",
      "Type": 1,
      "GridX": 1,
      "GridY": 0,
      "Name": "Tools",
      "PluginPackageNames": [
        "com.phobos.editor",
        "com.phobos.terminal"
      ]
    }
  ],
  "Folders": [
    {
      "Id": "folder-123",
      "Type": 1,
      "GridX": 1,
      "GridY": 0,
      "Name": "Tools",
      "PluginPackageNames": [
        "com.phobos.editor",
        "com.phobos.terminal"
      ]
    }
  ]
}
```

### 保存时机
- 窗口大小改变时
- 全屏/窗口模式切换时
- 创建/删除文件夹时
- 布局变更时

### 加载时机
- 窗口启动时自动加载
- 如果文件不存在，创建默认布局（所有插件按顺序排列）

## 使用示例

```csharp
using Phobos.Components.Arcusrix;
using Phobos.Class.Database;

// 创建窗口
var desktop = new PCOPhobosDesktop();

// 设置数据库（可选，会自动从 PMPlugin 获取）
var database = new PCSqliteDatabase("path/to/Phobos.db");
await database.Connect();
desktop.SetDatabase(database);

// 设置窗口属性
desktop.SetTitle("Phobos Desktop");
desktop.SetWindowIcon("path/to/icon.png");

// 订阅插件点击事件
desktop.PluginClicked += async (sender, packageName) =>
{
    // 处理插件点击事件
    Console.WriteLine($"Clicked plugin: {packageName}");

    // 例如：启动插件
    await PMPlugin.Instance.Launch(packageName);
};

// 显示窗口
desktop.Show();
```

## 公开 API

### 方法
- `SetDatabase(PCSqliteDatabase database)` - 设置数据库实例
- `SetTitle(string title)` - 设置窗口标题
- `SetWindowIcon(ImageSource iconSource)` - 设置窗口图标
- `SetWindowIcon(string iconPath)` - 从路径设置窗口图标

### 事件
- `PluginClicked(object sender, string packageName)` - 插件被点击时触发

## 注意事项

1. 窗口会在加载时自动尝试从 `PMPlugin.Instance` 获取数据库实例
2. 如果需要，可以通过 `SetDatabase()` 方法显式设置数据库
3. 所有主题资源必须在应用程序资源字典中定义
4. 内建插件（Directory = "builtin" 或 "Built-In"）会自动标记为系统插件
5. 插件图标路径支持绝对路径和相对路径
6. 窗口双击标题栏可最大化/还原
7. 窗口大小改变时会自动调整网格布局并保存
8. 布局配置使用 JSON 格式序列化存储
9. 系统插件不显示"卸载"选项
10. 图标悬停动画使用 CubicEase 缓动函数

## 网格布局算法

### 自适应列数和行数
```csharp
const double iconSize = 100; // 图标大小 + 边距
int columns = Math.Max(3, (int)(availableWidth / iconSize));
int rows = Math.Max(2, (int)(availableHeight / iconSize));
```

### 默认布局生成
- 将所有插件按顺序填充到网格中
- 从左到右、从上到下排列
- 超出网格范围的插件不显示（可通过调整窗口大小查看）

## 动画效果

### 图标缩放动画
- **触发条件**：鼠标悬停
- **放大比例**：1.1x
- **动画时长**：150ms
- **缓动函数**：CubicEase EaseOut
- **变换中心**：图标中心点 (0.5, 0.5)

## 对话框组件

### PCOInputDialog（输入对话框）
用于获取用户文本输入，支持以下功能：
- 自定义标题和提示文本
- 默认值设置
- 回车键确认，Esc 键取消
- 主题集成

**使用示例**：
```csharp
var result = PCOInputDialog.Show(this, "New Folder", "Enter folder name:", "New Folder");
if (!string.IsNullOrWhiteSpace(result))
{
    // 用户输入了有效值
}
```

### PCOPluginInfoDialog（插件信息对话框）
显示插件详细信息，包括：
- 插件图标（96x96）
- 名称、版本、包名
- 制造商、描述
- 安装目录、安装时间
- 启用状态、系统插件标记

**使用示例**：
```csharp
await PCOPluginInfoDialog.ShowAsync(this, packageName, database);
```

### PCODesktopSettingsDialog（桌面设置对话框）
配置桌面布局参数：
- 调整网格列数（3-20）
- 调整网格行数（2-20）
- 重置布局到默认状态

**使用示例**：
```csharp
var (saved, columns, rows, resetLayout) = PCODesktopSettingsDialog.Show(this, currentColumns, currentRows);
if (saved && resetLayout)
{
    // 重置布局
}
```

## 已实现功能

### 文件夹管理
- ✅ **创建文件夹**：通过输入对话框设置文件夹名称，自动查找空闲位置
- ✅ **重命名文件夹**：通过输入对话框修改文件夹名称
- ✅ **删除文件夹**：删除文件夹（不影响其中的插件）

### 插件操作
- ✅ **查看信息**：显示插件详细信息对话框
- ✅ **打开设置**：如果插件有 SettingUri，则打开设置页面
- ✅ **卸载插件**：显示确认对话框，卸载后自动更新布局

### 桌面设置
- ✅ **调整网格大小**：动态调整列数和行数
- ✅ **重置布局**：恢复到默认布局（所有插件按顺序排列）

## 键盘快捷键

- **回车键**：在输入对话框中确认输入
- **Esc 键**：在输入对话框中取消输入

## 性能优化

- **图标懒加载**：仅加载当前可见的图标
- **布局缓存**：避免重复渲染
- **动画性能**：使用硬件加速的 WPF 动画
- **图标缓存**：BitmapCacheOption.OnLoad 确保图标缓存到内存

## 已知限制

1. 拖拽功能尚未实现（图标位置通过网格系统管理）
2. 不支持自定义图标大小（固定 64x64 容器，48x48 图标）
3. 不支持多页桌面（所有插件在一个页面）
4. 布局迁移功能尚未实现（版本升级时可能需要重新生成布局）
5. 文件夹不支持拖拽添加插件（需要手动编辑布局文件或通过代码添加）

## 创建的文件列表

1. **PCODesktopLayoutModels.cs** - 布局数据模型
   - `DesktopItem` - 桌面项基类
   - `PluginDesktopItem` - 插件桌面项
   - `FolderDesktopItem` - 文件夹桌面项
   - `DesktopLayout` - 布局配置

2. **PCOInputDialog.xaml / .cs** - 通用输入对话框

3. **PCOPluginInfoDialog.xaml / .cs** - 插件信息对话框

4. **PCODesktopSettingsDialog.xaml / .cs** - 桌面设置对话框

5. **PCOPhobosDesktop.xaml / .cs** - 主桌面窗口（已更新）

6. **PCOPhobosDesktop_README.md** - 本文档