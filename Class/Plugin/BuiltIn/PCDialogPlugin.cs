using Phobos.Class.Plugin;
using Phobos.Components.Arcusrix.Dialog;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Phobos 对话框插件
    /// 提供统一的对话框显示功能
    /// </summary>
    public class PCDialogPlugin : PCPluginBase
    {
        private static PCDialogPlugin? _instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static PCDialogPlugin Instance => _instance ??= new PCDialogPlugin();

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Phobos Dialog",
            PackageName = "com.phobos.dialog",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_dialog_dfj912ls0f92",
            DatabaseKey = "PDialog",
            Icon = "Assets/dialog-icon.png",
            IsSystemPlugin = true,
            SettingUri = "dialog://settings",
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Phobos Dialog is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Phobos Dialog is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "Phobos 对话框是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "Phobos 對話框是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "Phobos ダイアログは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Phobos Dialog" },
                { "zh-CN", "Phobos 对话框" },
                { "zh-TW", "Phobos 對話框" },
                { "ja-JP", "Phobos ダイアログ" },
                { "ko-KR", "Phobos 대화상자" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "System dialog component for Phobos" },
                { "zh-CN", "Phobos 系统对话框组件" },
                { "zh-TW", "Phobos 系統對話框元件" },
                { "ja-JP", "Phobos のシステムダイアログコンポーネント" },
                { "ko-KR", "Phobos 시스템 대화상자 구성요소" }
            }
        };

        public override FrameworkElement? ContentArea => null;

        public PCDialogPlugin()
        {
            _instance = this;
        }

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            await Link(new LinkAssociation
            {
                Protocol = "dialog:",
                Name = "PhobosDialogHandler",
                Description = "Phobos Dialog Protocol Handler",
                Command = "phobos://plugin/com.phobos.dialog?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "phobostest:",
                Name = "PhobosDialogHandler",
                Description = "Phobos Dialog Protocol Handler",
                Command = "phobos://plugin/com.phobos.dialog?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "Phobos.Dialog:",
                Name = "PhobosDialogHandler",
                Description = "Phobos Dialog Protocol Handler",
                Command = "phobos://plugin/com.phobos.dialog?action=%0"
            });

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> Run(params object[] args)
        {
            try
            {
                if (args.Length == 0)
                    return new RequestResult { Success = false, Message = "No command specified" };

                var command = args[0]?.ToString()?.ToLowerInvariant() ?? string.Empty;

                switch (command)
                {
                    case "show":
                        return await HandleShowAsync(args);

                    case "confirm":
                        return await HandleConfirmAsync(args);

                    case "info":
                        return await HandleInfoAsync(args);

                    case "warning":
                        return await HandleWarningAsync(args);

                    case "error":
                        return await HandleErrorAsync(args);

                    case "yesno":
                        return await HandleYesNoAsync(args);

                    case "yesnocancel":
                        return await HandleYesNoCancelAsync(args);

                    case "custom":
                        return await HandleCustomAsync(args);

                    default:
                        return new RequestResult { Success = false, Message = $"Unknown command: {command}" };
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Dialog", $"Command failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        #region Async Command Handlers

        private async Task<RequestResult> HandleShowAsync(object[] args)
        {
            if (args.Length < 2 || args[1] is not DialogConfig config)
            {
                return new RequestResult
                {
                    Success = false,
                    Message = "DialogConfig required"
                };
            }

            var result = await PCOPhobosDialog.ShowAsync(config);

            return new RequestResult
            {
                Success = true,
                Message = "Dialog closed",
                Data = new List<object> { result }
            };
        }

        private async Task<RequestResult> HandleConfirmAsync(object[] args)
        {
            var message = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
            var title = args.Length > 2 ? args[2]?.ToString() : null;
            var owner = args.Length > 3 ? args[3] as Window : null;

            var confirmed = await PCOPhobosDialog.ConfirmAsync(message, title, owner);

            return new RequestResult
            {
                Success = true,
                Message = confirmed ? "Confirmed" : "Cancelled",
                Data = new List<object> { confirmed }
            };
        }

        private async Task<RequestResult> HandleInfoAsync(object[] args)
        {
            var message = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
            var title = args.Length > 2 ? args[2]?.ToString() : null;
            var owner = args.Length > 3 ? args[3] as Window : null;

            await PCOPhobosDialog.InfoAsync(message, title, owner);

            return new RequestResult
            {
                Success = true,
                Message = "Info dialog shown"
            };
        }

        private async Task<RequestResult> HandleWarningAsync(object[] args)
        {
            var message = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
            var title = args.Length > 2 ? args[2]?.ToString() : null;
            var owner = args.Length > 3 ? args[3] as Window : null;

            await PCOPhobosDialog.WarningAsync(message, title, owner);

            return new RequestResult
            {
                Success = true,
                Message = "Warning dialog shown"
            };
        }

        private async Task<RequestResult> HandleErrorAsync(object[] args)
        {
            var message = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
            var title = args.Length > 2 ? args[2]?.ToString() : null;
            var owner = args.Length > 3 ? args[3] as Window : null;

            await PCOPhobosDialog.ErrorAsync(message, title, owner);

            return new RequestResult
            {
                Success = true,
                Message = "Error dialog shown"
            };
        }

        private async Task<RequestResult> HandleYesNoAsync(object[] args)
        {
            var message = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
            var title = args.Length > 2 ? args[2]?.ToString() : null;
            var owner = args.Length > 3 ? args[3] as Window : null;

            var result = await PCOPhobosDialog.YesNoAsync(message, title, owner);

            return new RequestResult
            {
                Success = true,
                Message = result switch
                {
                    true => "Yes",
                    false => "No",
                    _ => "Cancelled"
                },
                Data = result.HasValue ? new List<object> { result.Value } : new List<object>()
            };
        }

        private async Task<RequestResult> HandleYesNoCancelAsync(object[] args)
        {
            var message = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
            var title = args.Length > 2 ? args[2]?.ToString() : null;
            var owner = args.Length > 3 ? args[3] as Window : null;

            var result = await PCOPhobosDialog.YesNoCancelAsync(message, title, owner);

            return new RequestResult
            {
                Success = true,
                Message = result switch
                {
                    true => "Yes",
                    false => "No",
                    _ => "Cancelled"
                },
                Data = result.HasValue ? new List<object> { result.Value } : new List<object>()
            };
        }

        private async Task<RequestResult> HandleCustomAsync(object[] args)
        {
            if (args.Length < 2 || args[1] is not DialogConfig config)
            {
                return new RequestResult
                {
                    Success = false,
                    Message = "DialogConfig required"
                };
            }

            // 设置自定义内容模式
            config.ContentMode = DialogContentMode.Custom;

            var result = await PCOPhobosDialog.ShowAsync(config);

            return new RequestResult
            {
                Success = true,
                Message = "Custom dialog closed",
                Data = new List<object> { result }
            };
        }

        #endregion

        #region 静态异步方法（推荐使用）

        /// <summary>
        /// 异步显示对话框（推荐）
        /// </summary>
        public static Task<DialogCallbackResult> ShowDialogAsync(DialogConfig config)
        {
            return PCOPhobosDialog.ShowAsync(config);
        }

        /// <summary>
        /// 异步确认对话框（推荐）
        /// </summary>
        public static Task<bool> ConfirmDialogAsync(string message, string? title = null, Window? owner = null)
        {
            return PCOPhobosDialog.ConfirmAsync(message, title, owner);
        }

        /// <summary>
        /// 异步信息对话框（推荐）
        /// </summary>
        public static Task InfoDialogAsync(string message, string? title = null, Window? owner = null)
        {
            return PCOPhobosDialog.InfoAsync(message, title, owner);
        }

        /// <summary>
        /// 异步警告对话框（推荐）
        /// </summary>
        public static Task WarningDialogAsync(string message, string? title = null, Window? owner = null)
        {
            return PCOPhobosDialog.WarningAsync(message, title, owner);
        }

        /// <summary>
        /// 异步错误对话框（推荐）
        /// </summary>
        public static Task ErrorDialogAsync(string message, string? title = null, Window? owner = null)
        {
            return PCOPhobosDialog.ErrorAsync(message, title, owner);
        }

        /// <summary>
        /// 异步是/否对话框（推荐）
        /// </summary>
        public static Task<bool?> YesNoDialogAsync(string message, string? title = null, Window? owner = null)
        {
            return PCOPhobosDialog.YesNoAsync(message, title, owner);
        }

        /// <summary>
        /// 异步是/否/取消对话框（推荐）
        /// </summary>
        public static Task<bool?> YesNoCancelDialogAsync(string message, string? title = null, Window? owner = null)
        {
            return PCOPhobosDialog.YesNoCancelAsync(message, title, owner);
        }

        #endregion

        #region 静态便捷方法（供其他插件直接调用，保留向后兼容）

        /// <summary>
        /// [已弃用] 显示对话框，建议使用 ShowDialogAsync
        /// </summary>
        [Obsolete("Use ShowDialogAsync for better multi-dialog support")]
        public static DialogCallbackResult Show(DialogConfig config)
        {
#pragma warning disable CS0618
            return PCOPhobosDialog.Show(config);
#pragma warning restore CS0618
        }

        /// <summary>
        /// 显示对话框（异步回调）
        /// </summary>
        public static void ShowWithCallback(DialogConfig config, Action<DialogCallbackResult>? callback = null)
        {
            PCOPhobosDialog.ShowWithCallback(config, callback);
        }

        /// <summary>
        /// 显示对话框（异步回调，别名）
        /// </summary>
        public static void ShowAsync(DialogConfig config, Action<DialogCallbackResult>? callback = null)
        {
            PCOPhobosDialog.ShowWithCallback(config, callback);
        }

        /// <summary>
        /// [已弃用] 显示确认对话框，建议使用 ConfirmDialogAsync
        /// </summary>
        [Obsolete("Use ConfirmDialogAsync for better multi-dialog support")]
        public static bool Confirm(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
#pragma warning disable CS0618
            return PCOPhobosDialog.Confirm(message, title, isModal, owner);
#pragma warning restore CS0618
        }

        /// <summary>
        /// [已弃用] 显示确认对话框（带图标），建议使用 ConfirmDialogAsync
        /// </summary>
        [Obsolete("Use ConfirmDialogAsync for better multi-dialog support")]
        public static bool Confirm(string message, string? title, ImageSource? callerIcon, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.Confirm(message, title);
            config.CallerIcon = callerIcon;
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }
#pragma warning disable CS0618
            var result = PCOPhobosDialog.Show(config);
#pragma warning restore CS0618
            return result.ButtonTag == "ok";
        }

        /// <summary>
        /// [已弃用] 显示信息对话框，建议使用 InfoDialogAsync
        /// </summary>
        [Obsolete("Use InfoDialogAsync for better multi-dialog support")]
        public static void Info(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
#pragma warning disable CS0618
            PCOPhobosDialog.Info(message, title, isModal, owner);
#pragma warning restore CS0618
        }

        /// <summary>
        /// [已弃用] 显示警告对话框，建议使用 WarningDialogAsync
        /// </summary>
        [Obsolete("Use WarningDialogAsync for better multi-dialog support")]
        public static void Warning(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
#pragma warning disable CS0618
            PCOPhobosDialog.Warning(message, title, isModal, owner);
#pragma warning restore CS0618
        }

        /// <summary>
        /// [已弃用] 显示错误对话框，建议使用 ErrorDialogAsync
        /// </summary>
        [Obsolete("Use ErrorDialogAsync for better multi-dialog support")]
        public static void Error(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
#pragma warning disable CS0618
            PCOPhobosDialog.Error(message, title, isModal, owner);
#pragma warning restore CS0618
        }

        /// <summary>
        /// [已弃用] 显示是/否对话框，建议使用 YesNoDialogAsync
        /// </summary>
        [Obsolete("Use YesNoDialogAsync for better multi-dialog support")]
        public static bool? YesNo(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
#pragma warning disable CS0618
            return PCOPhobosDialog.YesNo(message, title, isModal, owner);
#pragma warning restore CS0618
        }

        /// <summary>
        /// [已弃用] 显示是/否/取消对话框，建议使用 YesNoCancelDialogAsync
        /// </summary>
        [Obsolete("Use YesNoCancelDialogAsync for better multi-dialog support")]
        public static bool? YesNoCancel(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
#pragma warning disable CS0618
            return PCOPhobosDialog.YesNoCancel(message, title, isModal, owner);
#pragma warning restore CS0618
        }

        /// <summary>
        /// 创建自定义对话框构建器
        /// </summary>
        public static DialogBuilder CreateDialog()
        {
            return new DialogBuilder();
        }

        #endregion
    }

    /// <summary>
    /// 对话框构建器 - 链式调用
    /// </summary>
    public class DialogBuilder
    {
        private readonly DialogConfig _config = new();

        /// <summary>
        /// 设置标题
        /// </summary>
        public DialogBuilder WithTitle(string title)
        {
            _config.Title = title;
            return this;
        }

        /// <summary>
        /// 设置本地化标题
        /// </summary>
        public DialogBuilder WithLocalizedTitle(Dictionary<string, string> titles)
        {
            _config.LocalizedTitles = titles;
            return this;
        }

        /// <summary>
        /// 设置调用者图标
        /// </summary>
        public DialogBuilder WithCallerIcon(ImageSource icon)
        {
            _config.CallerIcon = icon;
            return this;
        }

        /// <summary>
        /// 设置调用者图标路径
        /// </summary>
        public DialogBuilder WithCallerIconPath(string path)
        {
            _config.CallerIconPath = path;
            return this;
        }

        /// <summary>
        /// 设置居中图片+文字模式
        /// </summary>
        public DialogBuilder WithImageAndText(string text, ImageSource image)
        {
            _config.ContentMode = DialogContentMode.ImageWithText;
            _config.ContentText = text;
            _config.ContentImage = image;
            return this;
        }

        /// <summary>
        /// 设置居中图片+文字模式（使用路径）
        /// </summary>
        public DialogBuilder WithImageAndText(string text, string imagePath)
        {
            _config.ContentMode = DialogContentMode.ImageWithText;
            _config.ContentText = text;
            _config.ContentImagePath = imagePath;
            return this;
        }

        /// <summary>
        /// 设置居中图片+注释模式
        /// </summary>
        public DialogBuilder WithImageAndCaption(string text, ImageSource image, string caption)
        {
            _config.ContentMode = DialogContentMode.ImageWithCaption;
            _config.ContentText = text;
            _config.ContentImage = image;
            _config.ContentImageCaption = caption;
            return this;
        }

        /// <summary>
        /// 设置居中图片+注释模式（使用路径）
        /// </summary>
        public DialogBuilder WithImageAndCaption(string text, string imagePath, string caption)
        {
            _config.ContentMode = DialogContentMode.ImageWithCaption;
            _config.ContentText = text;
            _config.ContentImagePath = imagePath;
            _config.ContentImageCaption = caption;
            return this;
        }

        /// <summary>
        /// 设置居中文字模式
        /// </summary>
        public DialogBuilder WithCenteredText(string text)
        {
            _config.ContentMode = DialogContentMode.CenteredText;
            _config.ContentText = text;
            return this;
        }

        /// <summary>
        /// 设置左对齐文字模式
        /// </summary>
        public DialogBuilder WithLeftAlignedText(string text)
        {
            _config.ContentMode = DialogContentMode.LeftAlignedText;
            _config.ContentText = text;
            return this;
        }

        /// <summary>
        /// 设置自定义内容
        /// </summary>
        public DialogBuilder WithCustomContent(FrameworkElement content)
        {
            _config.ContentMode = DialogContentMode.Custom;
            _config.CustomContent = content;
            return this;
        }

        /// <summary>
        /// 设置自定义按钮区域
        /// </summary>
        public DialogBuilder WithCustomButtonArea(FrameworkElement buttonArea)
        {
            _config.CustomButtonArea = buttonArea;
            return this;
        }

        /// <summary>
        /// 添加主按钮（最右侧）
        /// </summary>
        public DialogBuilder WithPrimaryButton(string text, string tag = "", Action<DialogButton>? onClick = null)
        {
            _config.Buttons.Insert(0, new DialogButton
            {
                Text = text,
                Tag = tag,
                ButtonType = DialogButtonType.Primary,
                OnClick = onClick
            });
            return this;
        }

        /// <summary>
        /// 添加次要按钮
        /// </summary>
        public DialogBuilder WithSecondaryButton(string text, string tag = "", Action<DialogButton>? onClick = null)
        {
            _config.Buttons.Add(new DialogButton
            {
                Text = text,
                Tag = tag,
                ButtonType = DialogButtonType.Secondary,
                OnClick = onClick
            });
            return this;
        }

        /// <summary>
        /// 添加按钮
        /// </summary>
        public DialogBuilder WithButton(DialogButton button)
        {
            _config.Buttons.Add(button);
            return this;
        }

        /// <summary>
        /// 设置显示按钮数量（1-5）
        /// </summary>
        public DialogBuilder WithButtonCount(int count)
        {
            _config.VisibleButtonCount = Math.Max(1, Math.Min(5, count));
            return this;
        }

        /// <summary>
        /// 显示取消按钮
        /// </summary>
        public DialogBuilder WithCancelButton(string text = "Cancel")
        {
            _config.ShowCancelButton = true;
            _config.CancelButtonText = text;
            return this;
        }

        /// <summary>
        /// 隐藏取消按钮
        /// </summary>
        public DialogBuilder WithoutCancelButton()
        {
            _config.ShowCancelButton = false;
            return this;
        }

        /// <summary>
        /// 设置对话框大小
        /// </summary>
        public DialogBuilder WithSize(double width, double minHeight = 200, double maxHeight = 600)
        {
            _config.Width = width;
            _config.MinHeight = minHeight;
            _config.MaxHeight = maxHeight;
            return this;
        }

        /// <summary>
        /// 居中于屏幕
        /// </summary>
        public DialogBuilder CenterOnScreen()
        {
            _config.PositionMode = DialogPositionMode.CenterScreen;
            return this;
        }

        /// <summary>
        /// 居中于父窗口
        /// </summary>
        public DialogBuilder CenterOnOwner(Window owner)
        {
            _config.PositionMode = DialogPositionMode.CenterOwner;
            _config.OwnerWindow = owner;
            return this;
        }

        /// <summary>
        /// 设置自定义位置
        /// </summary>
        public DialogBuilder AtPosition(double x, double y)
        {
            _config.PositionMode = DialogPositionMode.Custom;
            _config.CustomPosition = new Point(x, y);
            return this;
        }

        /// <summary>
        /// 设置位置偏移
        /// </summary>
        public DialogBuilder WithOffset(double x, double y)
        {
            _config.CustomOffset = new Point(x, y);
            return this;
        }

        /// <summary>
        /// 设置为模态对话框
        /// </summary>
        public DialogBuilder AsModal()
        {
            _config.IsModal = true;
            return this;
        }

        /// <summary>
        /// 设置为非模态对话框
        /// </summary>
        public DialogBuilder AsModeless()
        {
            _config.IsModal = false;
            return this;
        }

        /// <summary>
        /// 允许拖动
        /// </summary>
        public DialogBuilder Draggable()
        {
            _config.IsDraggable = true;
            return this;
        }

        /// <summary>
        /// 显示关闭按钮
        /// </summary>
        public DialogBuilder WithCloseButton()
        {
            _config.ShowCloseButton = true;
            return this;
        }

        /// <summary>
        /// 按 ESC 关闭
        /// </summary>
        public DialogBuilder CloseOnEscape(bool value = true)
        {
            _config.CloseOnEscape = value;
            return this;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        public DialogConfig Build()
        {
            return _config;
        }

        /// <summary>
        /// 异步显示对话框（推荐）
        /// </summary>
        public Task<DialogCallbackResult> ShowAsync()
        {
            return PCOPhobosDialog.ShowAsync(_config);
        }

        /// <summary>
        /// 显示对话框（异步回调）
        /// </summary>
        public void ShowWithCallback(Action<DialogCallbackResult>? callback = null)
        {
            PCOPhobosDialog.ShowWithCallback(_config, callback);
        }

        /// <summary>
        /// [已弃用] 同步显示对话框，建议使用 ShowAsync
        /// </summary>
        [Obsolete("Use ShowAsync for better multi-dialog support")]
        public DialogCallbackResult Show()
        {
#pragma warning disable CS0618
            return PCOPhobosDialog.Show(_config);
#pragma warning restore CS0618
        }
    }
}