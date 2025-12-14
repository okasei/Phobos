using Phobos.Shared.Class;
using System;
using System.Globalization;
using System.IO;

namespace Phobos.Components.Arcusrix.Installer.Helpers
{
    /// <summary>
    /// 插件安装器本地化资源
    /// </summary>
    public static class InstallerLocalization
    {
        private const string PackageName = "com.phobos.plugin.installer";
        private static PluginLocalizationContext? _context;
        private static string _currentLanguage = "en-US";

        /// <summary>
        /// 获取当前语言
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        // 标题
        public const string Title = "Installer.Title";

        // 按钮
        public const string Button_Browse = "Button.Browse";
        public const string Button_Back = "Button.Back";
        public const string Button_PreviousStep = "Button.PreviousStep";
        public const string Button_Install = "Button.Install";
        public const string Button_InstallAll = "Button.InstallAll";
        public const string Button_Installed = "Button.Installed";
        public const string Button_Retry = "Button.Retry";

        // 文件选择视图
        public const string FileSelect_DropHint = "FileSelect.DropHint";
        public const string FileSelect_Or = "FileSelect.Or";
        public const string FileSelect_SupportedFiles = "FileSelect.SupportedFiles";
        public const string FileSelect_Ready = "FileSelect.Ready";
        public const string FileSelect_FileNotExist = "FileSelect.FileNotExist";
        public const string FileSelect_InvalidPlugin = "FileSelect.InvalidPlugin";
        public const string FileSelect_NoPluginTypes = "FileSelect.NoPluginTypes";
        public const string FileSelect_CannotCreateInstance = "FileSelect.CannotCreateInstance";
        public const string FileSelect_PluginNotFound = "FileSelect.PluginNotFound";
        public const string FileSelect_LoadFailed = "FileSelect.LoadFailed";
        public const string FileSelect_DropSingleDll = "FileSelect.DropSingleDll";

        // 多插件视图
        public const string MultiPlugin_Title = "MultiPlugin.Title";
        public const string MultiPlugin_Subtitle = "MultiPlugin.Subtitle";

        // 插件详情视图
        public const string Detail_Introduction = "Detail.Introduction";
        public const string Detail_NoDescription = "Detail.NoDescription";
        public const string Detail_Dependencies = "Detail.Dependencies";
        public const string Detail_MissingRequired = "Detail.MissingRequired";
        public const string Detail_MissingRequiredHint = "Detail.MissingRequiredHint";
        public const string Detail_MissingOptional = "Detail.MissingOptional";
        public const string Detail_MissingOptionalHint = "Detail.MissingOptionalHint";
        public const string Detail_SatisfiedDependencies = "Detail.SatisfiedDependencies";
        public const string Detail_CannotInstall = "Detail.CannotInstall";
        public const string Detail_HasMissingRequired = "Detail.HasMissingRequired";
        public const string Detail_HasMissingOptional = "Detail.HasMissingOptional";

        // 安装状态
        public const string Status_Installing = "Status.Installing";
        public const string Status_InstallingMultiple = "Status.InstallingMultiple";
        public const string Status_ReadingPlugin = "Status.ReadingPlugin";
        public const string Status_Processing = "Status.Processing";
        public const string Status_InstallSuccess = "Status.InstallSuccess";
        public const string Status_InstallFailed = "Status.InstallFailed";
        public const string Status_InstallError = "Status.InstallError";

        // 对话框
        public const string Dialog_OptionalDepsTitle = "Dialog.OptionalDepsTitle";
        public const string Dialog_OptionalDepsMessage = "Dialog.OptionalDepsMessage";

        // 文件对话框
        public const string FileDialog_Filter = "FileDialog.Filter";
        public const string FileDialog_Title = "FileDialog.Title";

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
            var localizationDir = Path.Combine(baseDir, "Assets", "Localization", "Installer");

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
