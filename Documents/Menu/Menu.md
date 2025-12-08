# Phobos Menu 组件使用文档

## 概述

Phobos Menu 系统提供了一套简洁、可定制的上下文菜单组件，包含：

- **PMMenu**: 菜单管理器（单例模式）
- **PCOMenu**: 菜单 UI 组件
- **PhobosMenuItem**: 菜单项数据模型

## 快速开始

### 1. 基本用法

```csharp
using Phobos.Manager.Arcusrix;
using Phobos.Components.Arcusrix.Menu;

// 创建菜单项
var items = new List<PhobosMenuItem>
{
    PhobosMenuItem.Create("open", "打开", "📂", () => OpenFile()),
    PhobosMenuItem.Create("edit", "编辑", "✏", () => EditFile()),
    PhobosMenuItem.Separator(),
    PhobosMenuItem.Create("delete", "删除", "🗑", () => DeleteFile(), isDanger: true)
};

// 在鼠标位置显示菜单
PMMenu.Instance.ShowAtMouse(ContainerPanel, items, selectedItem =>
{
    Console.WriteLine($"选中了: {selectedItem.Id}");
});
```

### 2. 使用 PMMenu 管理器

PMMenu 是一个单例管理器，提供了便捷的菜单创建和显示方法：

```csharp
var menu = PMMenu.Instance;

// 创建菜单项
var item1 = menu.CreateItem("id1", "文本", "图标", () => { /* 点击回调 */ });
var separator = menu.CreateSeparator();
var dangerItem = menu.CreateItem("delete", "删除", "🗑", () => { }, isDanger: true);

// 显示菜单
menu.ShowAt(container, items, new Point(100, 100), OnItemSelected);
menu.ShowNear(container, items, targetElement, OnItemSelected);
menu.ShowAtMouse(container, items, OnItemSelected);
```

### 3. 本地化支持

```csharp
var localizedItem = PMMenu.Instance.CreateLocalizedItem(
    "settings",
    new Dictionary<string, string>
    {
        { "en-US", "Settings" },
        { "zh-CN", "设置" },
        { "ja-JP", "設定" }
    },
    "⚙",
    () => OpenSettings()
);
```

## API 参考

### PhobosMenuItem 类

菜单项数据模型。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 菜单项唯一标识 |
| `Text` | `string` | 显示文本 |
| `Icon` | `string` | 图标（支持 Emoji 或字符） |
| `IsSeparator` | `bool` | 是否为分隔符 |
| `IsDanger` | `bool` | 是否为危险操作（显示为红色） |
| `IsEnabled` | `bool` | 是否启用 |
| `Tag` | `object?` | 自定义数据 |
| `OnClick` | `Action?` | 点击回调 |
| `Children` | `List<PhobosMenuItem>?` | 子菜单项 |
| `LocalizedTexts` | `Dictionary<string, string>?` | 本地化文本 |

#### 静态方法

```csharp
// 创建分隔符
PhobosMenuItem.Separator();

// 快速创建菜单项
PhobosMenuItem.Create(string id, string text, string icon = "", Action? onClick = null, bool isDanger = false);
```

### PMMenu 类

菜单管理器（单例模式）。

#### 属性

```csharp
PMMenu.Instance  // 获取单例实例
```

#### 方法

| 方法 | 说明 |
|------|------|
| `CreateItem()` | 创建菜单项 |
| `CreateLocalizedItem()` | 创建带本地化的菜单项 |
| `CreateSeparator()` | 创建分隔符 |
| `CreateSubmenuItem()` | 创建带子菜单的菜单项 |
| `ShowAt()` | 在指定位置显示菜单 |
| `ShowNear()` | 在元素旁边显示菜单 |
| `ShowAtMouse()` | 在鼠标位置显示菜单 |
| `CloseMenusIn()` | 关闭容器中的所有菜单 |
| `ShowConfirmMenu()` | 显示确认菜单 |
| `ShowYesNoMenu()` | 显示是/否菜单 |

### PCOMenu 类

菜单 UI 组件。

#### 事件

| 事件 | 说明 |
|------|------|
| `MenuClosed` | 菜单关闭时触发 |
| `ItemSelected` | 菜单项被选中时触发 |

#### 属性

| 属性 | 说明 |
|------|------|
| `IsOpen` | 菜单是否打开 |

#### 方法

| 方法 | 说明 |
|------|------|
| `Show()` | 在指定位置显示菜单 |
| `ShowAt()` | 在元素旁边显示菜单 |
| `Close()` | 关闭菜单 |

## 使用示例

### 示例 1: 右键菜单

```csharp
private void Element_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
{
    var items = new List<PhobosMenuItem>
    {
        PhobosMenuItem.Create("copy", "复制", "📋", () => Copy()),
        PhobosMenuItem.Create("paste", "粘贴", "📄", () => Paste()),
        PhobosMenuItem.Separator(),
        PhobosMenuItem.Create("delete", "删除", "🗑", () => Delete(), isDanger: true)
    };

    var position = e.GetPosition(MainGrid);
    PMMenu.Instance.ShowAt(MainGrid, items, position);
}
```

### 示例 2: 下拉菜单

```csharp
private void DropdownButton_Click(object sender, RoutedEventArgs e)
{
    var items = new List<PhobosMenuItem>
    {
        PhobosMenuItem.Create("option1", "选项 1", "", () => SelectOption(1)),
        PhobosMenuItem.Create("option2", "选项 2", "", () => SelectOption(2)),
        PhobosMenuItem.Create("option3", "选项 3", "", () => SelectOption(3))
    };

    PMMenu.Instance.ShowNear(MainGrid, items, (FrameworkElement)sender);
}
```

### 示例 3: 带回调处理

```csharp
PMMenu.Instance.ShowAt(ContainerPanel, items, position, selectedItem =>
{
    switch (selectedItem.Id)
    {
        case "open":
            OpenFile();
            break;
        case "save":
            SaveFile();
            break;
        case "delete":
            if (ConfirmDelete())
                DeleteFile();
            break;
    }
});
```

### 示例 4: 动态菜单

```csharp
private void ShowDynamicMenu(Point position, FileInfo file)
{
    var items = new List<PhobosMenuItem>
    {
        PhobosMenuItem.Create("open", "打开", "📂", () => OpenFile(file))
    };

    // 根据文件类型添加不同选项
    if (file.Extension == ".txt")
    {
        items.Add(PhobosMenuItem.Create("edit", "编辑", "✏", () => EditText(file)));
    }
    else if (file.Extension == ".jpg" || file.Extension == ".png")
    {
        items.Add(PhobosMenuItem.Create("preview", "预览", "👁", () => PreviewImage(file)));
    }

    items.Add(PhobosMenuItem.Separator());
    items.Add(PhobosMenuItem.Create("delete", "删除", "🗑", () => DeleteFile(file), isDanger: true));

    PMMenu.Instance.ShowAt(MainGrid, items, position);
}
```

## 主题集成

PCOMenu 组件自动使用 Phobos 主题系统的颜色资源：

- `Background2Brush`: 菜单背景色
- `Background3Brush`: 悬停背景色
- `Background4Brush`: 边框和分隔线颜色
- `Foreground1Brush`: 主文字颜色
- `Foreground3Brush`: 次要文字颜色
- `DangerBrush`: 危险操作颜色

确保在应用程序资源字典中定义这些资源，或使用 PMTheme 主题管理器。

## 注意事项

1. **容器选择**: 菜单需要添加到一个 Panel 容器中（如 Grid、Canvas）
2. **自动关闭**: 点击菜单外部或窗口失去焦点时，菜单会自动关闭
3. **内存管理**: PMMenu.CloseMenusIn() 会自动从容器中移除菜单控件
4. **动画**: 菜单显示和关闭时有平滑的动画效果

## 更新日志

### v1.0.0
- 初始版本
- 支持基本菜单项、分隔符、危险项
- 支持本地化
- 支持动画效果
- PMMenu 单例管理器