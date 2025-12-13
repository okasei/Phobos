using Phobos.Shared.Class;
using System;
using System.Globalization;
using System.IO;

namespace Phobos.Components.Arcusrix.TaskManager.Helpers
{
    /// <summary>
    /// 任务管理器本地化资源
    /// </summary>
    public static class TaskManagerLocalization
    {
        private const string PackageName = "com.phobos.taskmanager";
        private static PluginLocalizationContext? _context;
        private static string _currentLanguage = "en-US";

        /// <summary>
        /// 获取当前语言
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        // 资源键常量
        public const string Title = "TaskManager.Title";

        // 选项卡
        public const string Tab_RunningPlugins = "Tab.RunningPlugins";
        public const string Tab_StartupItems = "Tab.StartupItems";
        public const string Startup_Subtitle = "Startup.Subtitle";

        // 运行中插件数量
        public const string Running_Count = "Running.Count";

        // 按钮
        public const string Button_Refresh = "Button.Refresh";
        public const string Button_EndTask = "Button.EndTask";
        public const string Button_Add = "Button.Add";
        public const string Button_Edit = "Button.Edit";
        public const string Button_Delete = "Button.Delete";
        public const string Button_Cancel = "Button.Cancel";
        public const string Button_Save = "Button.Save";
        public const string Button_Enable = "Button.Enable";
        public const string Button_Disable = "Button.Disable";

        // 表头
        public const string Header_PluginName = "Header.PluginName";
        public const string Header_PackageName = "Header.PackageName";
        public const string Header_Status = "Header.Status";
        public const string Header_Memory = "Header.Memory";
        public const string Header_Enabled = "Header.Enabled";
        public const string Header_Command = "Header.Command";
        public const string Header_Priority = "Header.Priority";
        public const string Header_Actions = "Header.Actions";

        // 状态
        public const string Status_Running = "Status.Running";
        public const string Status_Suspended = "Status.Suspended";
        public const string Status_Stopped = "Status.Stopped";

        // 空状态
        public const string Empty_NoRunningPlugins = "Empty.NoRunningPlugins";
        public const string Empty_NoStartupItems = "Empty.NoStartupItems";

        // 对话框
        public const string Dialog_AddTitle = "Dialog.AddTitle";
        public const string Dialog_EditTitle = "Dialog.EditTitle";
        public const string Dialog_PackageName = "Dialog.PackageName";
        public const string Dialog_Command = "Dialog.Command";
        public const string Dialog_Priority = "Dialog.Priority";
        public const string Dialog_PriorityHint = "Dialog.PriorityHint";

        // 消息
        public const string Message_EndTaskConfirm = "Message.EndTaskConfirm";
        public const string Message_DeleteConfirm = "Message.DeleteConfirm";
        public const string Message_CannotEndSystemPlugin = "Message.CannotEndSystemPlugin";

        // 工具提示
        public const string Tooltip_ToggleMenu = "Tooltip.ToggleMenu";
        public const string Tooltip_Toggle = "Tooltip.Toggle";

        /// <summary>
        /// 注册所有本地化资源
        /// </summary>
        /// <param name="languageSetting">语言设置，"system" 表示跟随系统</param>
        public static void RegisterAll(string languageSetting = "system")
        {
            // 确定实际语言
            if (languageSetting == "system")
            {
                _currentLanguage = CultureInfo.CurrentUICulture.Name;
            }
            else
            {
                _currentLanguage = languageSetting;
            }

            // 获取本地化目录路径
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var localizationDir = Path.Combine(baseDir, "Assets", "Localization", "TaskManager");

            // 使用 LocalizationManager 注册插件本地化
            _context = LocalizationManager.Instance.RegisterPlugin(PackageName, localizationDir, languageSetting);
        }

        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="languageSetting">"system" 或具体语言代码如 "en-US"</param>
        public static void SetLanguage(string languageSetting)
        {
            if (languageSetting == "system")
            {
                _currentLanguage = CultureInfo.CurrentUICulture.Name;
            }
            else
            {
                _currentLanguage = languageSetting;
            }

            if (_context != null)
            {
                _context.LanguageSetting = languageSetting;
            }
            else
            {
                LocalizationManager.Instance.SetPluginLanguage(PackageName, languageSetting);
            }
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public static string Get(string key)
        {
            return LocalizationManager.Instance.GetPluginText(PackageName, key);
        }

        /// <summary>
        /// 获取格式化的本地化文本
        /// </summary>
        public static string GetFormat(string key, params object[] args)
        {
            return LocalizationManager.Instance.GetPluginTextFormat(PackageName, key, args);
        }
    }
}
