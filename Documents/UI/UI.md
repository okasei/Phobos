# Phobos UI 样式系统

现代化的 WPF UI 样式系统，支持主题切换和动画效果。

## 特性

- 🎨 **主题感知** - 使用 DynamicResource 绑定主题颜色，支持运行时主题切换
- 🛡️ **安全回退** - 包含回退颜色定义，主题加载前不会崩溃
- ✨ **现代动画** - 按钮缩放、勾选弹跳、菜单淡入等流畅动画
- 📐 **统一圆角** - 所有控件使用一致的圆角半径
- 🖱️ **智能滚动条** - 悬停显示，超时自动隐藏

## 快速开始

### 1. 在 App.xaml 中引用

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Phobos;component/UI/Arcusrix/PhobosStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 2. 使用样式

```xml
<!-- 按钮 -->
<Button Style="{StaticResource PhobosButton}" Content="主按钮"/>
<Button Style="{StaticResource PhobosButtonSecondary}" Content="次按钮"/>
<Button Style="{StaticResource PhobosButtonDanger}" Content="危险按钮"/>
<Button Style="{StaticResource PhobosButtonGhost}" Content="文本按钮"/>

<!-- 文本框 -->
<TextBox Style="{StaticResource PhobosTextBox}" Tag="占位符文本"/>
<PasswordBox Style="{StaticResource PhobosPasswordBox}"/>
<TextBox Style="{StaticResource PhobosSearchBox}" Tag="搜索..."/>

<!-- 选择控件 -->
<ComboBox Style="{StaticResource PhobosComboBox}"/>
<RadioButton Style="{StaticResource PhobosRadioButton}" Content="选项"/>
<CheckBox Style="{StaticResource PhobosCheckBox}" Content="复选框"/>
<CheckBox Style="{StaticResource PhobosToggleSwitch}" Content="开关"/>

<!-- 滚动视图 (自动隐藏滚动条) -->
<ScrollViewer Style="{StaticResource PhobosScrollViewer}">
    ...
</ScrollViewer>

<!-- 进度条 -->
<ProgressBar Style="{StaticResource PhobosProgressBar}" Value="50"/>
<Slider Style="{StaticResource PhobosSlider}"/>

<!-- 菜单 -->
<ContextMenu Style="{StaticResource PhobosContextMenu}">
    <MenuItem Style="{StaticResource PhobosMenuItem}" Header="菜单项"/>
    <Separator Style="{StaticResource PhobosMenuSeparator}"/>
    <MenuItem Style="{StaticResource PhobosMenuItemDanger}" Header="删除"/>
</ContextMenu>

<!-- 选项卡 -->
<TabControl Style="{StaticResource PhobosTabControl}">
    <TabItem Style="{StaticResource PhobosTabItem}" Header="标签1"/>
</TabControl>

<!-- 下划线选项卡 -->
<TabControl Style="{StaticResource PhobosTabControlUnderline}">
    <TabItem Style="{StaticResource PhobosTabItemUnderline}" Header="标签1"/>
</TabControl>
```

## 样式列表

### 按钮 (PhobosButtonStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosButton` | 主按钮（蓝色背景） |
| `PhobosButtonSecondary` | 次按钮（边框样式） |
| `PhobosButtonGhost` | 文本按钮（无背景） |
| `PhobosButtonDanger` | 危险按钮（红色背景） |
| `PhobosButtonSuccess` | 成功按钮（绿色背景） |
| `PhobosButtonIcon` | 图标按钮 |
| `PhobosButtonSmall` | 小型按钮 |
| `PhobosButtonLarge` | 大型按钮 |

### 输入控件 (PhobosInputStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosTextBox` | 标准文本框 |
| `PhobosTextBoxSmall` | 小型文本框 |
| `PhobosTextBoxLarge` | 大型文本框 |
| `PhobosTextBoxMultiline` | 多行文本框 |
| `PhobosPasswordBox` | 密码框 |
| `PhobosSearchBox` | 搜索框（带图标） |

### 选择控件 (PhobosSelectStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosComboBox` | 下拉选择框 |
| `PhobosComboBoxItem` | 下拉选项 |
| `PhobosRadioButton` | 单选按钮 |
| `PhobosCheckBox` | 复选框 |
| `PhobosToggleSwitch` | 开关按钮 |

### 滚动条 (PhobosScrollBarStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosScrollBar` | 通用滚动条 |
| `PhobosScrollViewer` | 滚动视图（悬停显示，超时隐藏） |
| `PhobosScrollViewerAlwaysVisible` | 滚动视图（始终显示） |

### 进度条 (PhobosProgressStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosProgressBar` | 标准进度条 |
| `PhobosProgressBarLarge` | 大型进度条 |
| `PhobosProgressBarSuccess` | 成功进度条（绿色） |
| `PhobosProgressBarWarning` | 警告进度条（黄色） |
| `PhobosProgressBarDanger` | 危险进度条（红色） |
| `PhobosSlider` | 滑块 |

### 菜单 (PhobosMenuStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosContextMenu` | 右键菜单 |
| `PhobosMenuItem` | 菜单项 |
| `PhobosMenuItemDanger` | 危险菜单项（红色） |
| `PhobosMenuSeparator` | 菜单分隔符 |
| `PhobosMenu` | 顶部菜单栏 |
| `PhobosTopLevelMenuItem` | 顶部菜单项 |

### 选项卡 (PhobosTabStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosTabControl` | 选项卡控件（药丸样式） |
| `PhobosTabItem` | 选项卡项 |
| `PhobosTabControlUnderline` | 下划线选项卡控件 |
| `PhobosTabItemUnderline` | 下划线选项卡项 |

### 其他控件 (PhobosCommonStyles.xaml)

| 样式名 | 描述 |
|-------|------|
| `PhobosLabel` | 标签 |
| `PhobosLabelSecondary` | 次要标签 |
| `PhobosTextBlock` | 文本块 |
| `PhobosTextBlockHeading` | 标题文本 |
| `PhobosTextBlockCaption` | 说明文本 |
| `PhobosToolTip` | 工具提示 |
| `PhobosGroupBox` | 分组框 |
| `PhobosExpander` | 展开器 |
| `PhobosListBox` | 列表框 |
| `PhobosListBoxItem` | 列表项 |
| `PhobosSeparatorHorizontal` | 水平分隔线 |
| `PhobosSeparatorVertical` | 垂直分隔线 |
| `PhobosCard` | 卡片 |
| `PhobosCardHoverable` | 可悬停卡片 |

## 主题颜色键值

样式使用以下主题颜色键值（通过 DynamicResource 绑定）：

### 前景色 (5级)
- `Foreground1Brush` - 主要文字
- `Foreground2Brush` - 次要文字
- `Foreground3Brush` - 辅助文字
- `Foreground4Brush` - 占位符
- `Foreground5Brush` - 禁用文字

### 背景色 (5级)
- `Background1Brush` - 主背景
- `Background2Brush` - 次级背景/卡片
- `Background3Brush` - 悬停背景
- `Background4Brush` - 活跃背景
- `Background5Brush` - 边框背景

### 主题色
- `PrimaryBrush` - 主题色
- `PrimaryHoverBrush` - 主题色悬停
- `PrimaryPressedBrush` - 主题色按下
- `PrimaryDisabledBrush` - 主题色禁用

### 状态色
- `SuccessBrush` - 成功
- `WarningBrush` - 警告
- `DangerBrush` - 危险
- `InfoBrush` - 信息

### 边框色
- `BorderBrush` - 边框
- `BorderLightBrush` - 浅边框
- `BorderFocusBrush` - 聚焦边框

### 其他
- `ScrollbarBrush` - 滚动条
- `ScrollbarHoverBrush` - 滚动条悬停

## 动画效果

### 按钮
- 悬停时轻微放大 (1.02x)
- 按下时缩小 (0.98x)
- 使用 CubicEase 缓动

### 复选框/单选按钮
- 勾选标记弹跳动画
- 使用 ElasticEase 缓动

### 菜单
- 打开时淡入 + 向下滑动
- 使用 CubicEase 缓动

### 滚动条
- 悬停时淡入
- 离开后 1.5 秒淡出

### 开关
- 滑块平滑移动
- 背景颜色渐变

### 选项卡
- 下划线指示器伸展动画
- 箭头旋转动画

## 圆角规范

| 变量 | 值 | 用途 |
|-----|-----|-----|
| `CornerRadiusXs` | 2px | 小元素 |
| `CornerRadiusSm` | 4px | 按钮内部、列表项 |
| `CornerRadiusMd` | 6px | 按钮、输入框 |
| `CornerRadiusLg` | 8px | 卡片、菜单 |
| `CornerRadiusXl` | 12px | 对话框、大卡片 |

## 注意事项

1. **主题加载前的安全性**: `PhobosBaseStyles.xaml` 包含所有颜色的回退值，确保在主题加载前程序不会崩溃。

2. **DynamicResource vs StaticResource**: 所有颜色引用使用 `DynamicResource`，支持运行时主题切换。样式之间的引用使用 `StaticResource`。

3. **占位符文本**: TextBox 的占位符通过 `Tag` 属性设置：
   ```xml
   <TextBox Style="{StaticResource PhobosTextBox}" Tag="请输入..."/>
   ```

4. **ComboBox 项样式**: ComboBox 的项需要单独设置样式：
   ```xml
   <ComboBox Style="{StaticResource PhobosComboBox}"
             ItemContainerStyle="{StaticResource PhobosComboBoxItem}"/>
   ```

5. **ListBox 项样式**: ListBox 的项需要单独设置样式：
   ```xml
   <ListBox Style="{StaticResource PhobosListBox}"
            ItemContainerStyle="{StaticResource PhobosListBoxItem}"/>
   ```