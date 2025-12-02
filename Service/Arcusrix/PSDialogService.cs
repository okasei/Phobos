using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Class;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Phobos.Service.Arcusrix
{
    /// <summary>
    /// 对话框服务
    /// 提供全局统一的对话框访问接口
    /// </summary>
    public static class PSDialogService
    {
        #region 基础对话框

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题（可选）</param>
        /// <param name="owner">父窗口（可选）</param>
        /// <returns>用户是否确认</returns>
        public static bool Confirm(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            return PCDialogPlugin.Confirm(message, title, isModal, owner);
        }

        /// <summary>
        /// 显示确认对话框（带调用者图标）
        /// </summary>
        public static bool Confirm(string message, string? title, bool isModal = true, ImageSource? callerIcon = null, Window? owner = null)
        {
            return PCDialogPlugin.Confirm(message, title, callerIcon, isModal, owner);
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public static void Info(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            PCDialogPlugin.Info(message, title, isModal, owner);
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public static void Warning(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            PCDialogPlugin.Warning(message, title, isModal, owner);
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public static void Error(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            PCDialogPlugin.Error(message, title, isModal, owner);
        }

        /// <summary>
        /// 显示是/否对话框
        /// </summary>
        /// <returns>true=是，false=否，null=取消/关闭</returns>
        public static bool? YesNo(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            return PCDialogPlugin.YesNo(message, title, isModal, owner);
        }

        /// <summary>
        /// 显示是/否/取消对话框
        /// </summary>
        /// <returns>true=是，false=否，null=取消</returns>
        public static bool? YesNoCancel(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            return PCDialogPlugin.YesNoCancel(message, title, isModal, owner);
        }

        #endregion

        #region 高级对话框

        /// <summary>
        /// 显示自定义对话框
        /// </summary>
        public static DialogCallbackResult Show(DialogConfig config)
        {
            return PCDialogPlugin.Show(config);
        }

        /// <summary>
        /// 显示自定义对话框（异步回调）
        /// </summary>
        public static void ShowAsync(DialogConfig config, Action<DialogCallbackResult>? callback = null)
        {
            PCDialogPlugin.ShowAsync(config, callback);
        }

        /// <summary>
        /// 创建对话框构建器
        /// </summary>
        public static DialogBuilder CreateDialog()
        {
            return PCDialogPlugin.CreateDialog();
        }

        #endregion

        #region 带图片的对话框

        /// <summary>
        /// 显示带图片的信息对话框
        /// </summary>
        public static void InfoWithImage(string message, ImageSource image, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = new DialogConfig
            {
                Title = title ?? "Information",
                ContentMode = DialogContentMode.ImageWithText,
                ContentText = message,
                ContentImage = image,
                ShowCancelButton = false,
                IsModal = isModal,
                Buttons = new List<DialogButton>
                {
                    new DialogButton
                    {
                        Text = "OK",
                        ButtonType = DialogButtonType.Primary,
                        Tag = "ok"
                    }
                }
            };

            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            PCDialogPlugin.Show(config);
        }

        /// <summary>
        /// 显示带图片的确认对话框
        /// </summary>
        public static bool ConfirmWithImage(string message, ImageSource image, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.Confirm(message, title);
            config.ContentMode = DialogContentMode.ImageWithText;
            config.ContentImage = image;
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = PCDialogPlugin.Show(config);
            return result.ButtonTag == "ok";
        }

        #endregion

        #region 多按钮对话框

        /// <summary>
        /// 显示多按钮对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <param name="buttons">按钮配置（从右到左排列）</param>
        /// <param name="showCancel">是否显示取消按钮</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static DialogCallbackResult ShowWithButtons(
            string message,
            string? title,
            List<DialogButton> buttons,
            bool showCancel = true,
            bool isModal = true,
            Window? owner = null)
        {
            var config = new DialogConfig
            {
                Title = title,
                ContentMode = DialogContentMode.CenteredText,
                ContentText = message,
                Buttons = buttons,
                VisibleButtonCount = buttons.Count,
                ShowCancelButton = showCancel,
                IsModal = isModal
            };

            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            return PCDialogPlugin.Show(config);
        }

        /// <summary>
        /// 显示三按钮对话框
        /// </summary>
        public static string? ShowThreeButton(
            string message,
            string? title,
            string button1Text,
            string button2Text,
            string button3Text,
            bool isModal = true,
            Window? owner = null)
        {
            var buttons = new List<DialogButton>
            {
                new DialogButton { Text = button1Text, Tag = "button1", ButtonType = DialogButtonType.Primary },
                new DialogButton { Text = button2Text, Tag = "button2", ButtonType = DialogButtonType.Secondary },
                new DialogButton { Text = button3Text, Tag = "button3", ButtonType = DialogButtonType.Secondary }
            };

            var result = ShowWithButtons(message, title, buttons, false, isModal, owner);
            return result.ButtonTag;
        }

        /// <summary>
        /// 显示四按钮对话框
        /// </summary>
        public static string? ShowFourButton(
            string message,
            string? title,
            string button1Text,
            string button2Text,
            string button3Text,
            string button4Text,
            bool isModal = true,
            Window? owner = null)
        {
            var buttons = new List<DialogButton>
            {
                new DialogButton { Text = button1Text, Tag = "button1", ButtonType = DialogButtonType.Primary },
                new DialogButton { Text = button2Text, Tag = "button2", ButtonType = DialogButtonType.Secondary },
                new DialogButton { Text = button3Text, Tag = "button3", ButtonType = DialogButtonType.Secondary },
                new DialogButton { Text = button4Text, Tag = "button4", ButtonType = DialogButtonType.Secondary }
            };

            var result = ShowWithButtons(message, title, buttons, false, isModal, owner);
            return result.ButtonTag;
        }

        #endregion

        #region 异步对话框

        /// <summary>
        /// 异步显示确认对话框
        /// </summary>
        public static Task<bool> ConfirmAsync(string message, string? title = null, Window? owner = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var result = Confirm(message, title, false, owner);
                tcs.SetResult(result);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 异步显示是/否对话框
        /// </summary>
        public static Task<bool?> YesNoAsync(string message, string? title = null, Window? owner = null)
        {
            var tcs = new TaskCompletionSource<bool?>();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var result = YesNo(message, title, false, owner);
                tcs.SetResult(result);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 异步显示自定义对话框
        /// </summary>
        public static Task<DialogCallbackResult> ShowAsync(DialogConfig config)
        {
            var tcs = new TaskCompletionSource<DialogCallbackResult>();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var result = Show(config);
                tcs.SetResult(result);
            });

            return tcs.Task;
        }

        #endregion

        #region 自定义内容对话框

        /// <summary>
        /// 显示自定义内容对话框
        /// </summary>
        public static DialogCallbackResult ShowCustomContent(
            FrameworkElement content,
            string? title = null,
            List<DialogButton>? buttons = null,
            bool showCancel = true,
            bool isModal = true,
            Window? owner = null)
        {
            var config = new DialogConfig
            {
                Title = title,
                ContentMode = DialogContentMode.Custom,
                CustomContent = content,
                IsModal = isModal,
                Buttons = buttons ?? new List<DialogButton>
                {
                    new DialogButton { Text = "OK", Tag = "ok", ButtonType = DialogButtonType.Primary }
                },
                ShowCancelButton = showCancel
            };

            if (buttons != null)
            {
                config.VisibleButtonCount = buttons.Count;
            }

            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            return PCDialogPlugin.Show(config);
        }

        /// <summary>
        /// 显示自定义按钮区域对话框
        /// </summary>
        public static DialogCallbackResult ShowCustomButtons(
            string message,
            FrameworkElement buttonArea,
            string? title = null,
            bool isModal = true,
            Window? owner = null)
        {
            var config = new DialogConfig
            {
                Title = title,
                ContentMode = DialogContentMode.CenteredText,
                ContentText = message,
                CustomButtonArea = buttonArea,
                IsModal = isModal
            };

            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            return PCDialogPlugin.Show(config);
        }

        #endregion
    }
}
