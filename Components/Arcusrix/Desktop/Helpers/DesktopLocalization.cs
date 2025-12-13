using Phobos.Shared.Class;
using System;
using System.IO;
using System.Reflection;

namespace Phobos.Components.Arcusrix.Desktop.Components
{
    /// <summary>
    /// 桌面本地化资源
    /// </summary>
    public static partial class DesktopLocalization
    {
        private const string PackageName = "com.phobos.desktop";
        private static PluginLocalizationContext? _context;

        // 资源键常量
        public const string Desktop_Title = "Desktop.Title";
        public const string Desktop_Search_Placeholder = "Desktop.Search.Placeholder";

        // 插件菜单
        public const string Menu_Plugin_Open = "Menu.Plugin.Open";
        public const string Menu_Plugin_Info = "Menu.Plugin.Info";
        public const string Menu_Plugin_Settings = "Menu.Plugin.Settings";
        public const string Menu_Plugin_Uninstall = "Menu.Plugin.Uninstall";

        // 文件夹菜单
        public const string Menu_Folder_Open = "Menu.Folder.Open";
        public const string Menu_Folder_Rename = "Menu.Folder.Rename";
        public const string Menu_Folder_Delete = "Menu.Folder.Delete";

        // 桌面菜单
        public const string Menu_Desktop_Fullscreen = "Menu.Desktop.Fullscreen";
        public const string Menu_Desktop_ExitFullscreen = "Menu.Desktop.ExitFullscreen";
        public const string Menu_Desktop_Settings = "Menu.Desktop.Settings";
        public const string Menu_Desktop_NewFolder = "Menu.Desktop.NewFolder";
        public const string Menu_Desktop_NewShortcut = "Menu.Desktop.NewShortcut";

        // 快捷方式
        public const string Shortcut_NewTitle = "Shortcut.NewTitle";
        public const string Shortcut_EditTitle = "Shortcut.EditTitle";
        public const string Shortcut_Name = "Shortcut.Name";
        public const string Shortcut_TargetPlugin = "Shortcut.TargetPlugin";
        public const string Shortcut_OrInputPackageName = "Shortcut.OrInputPackageName";
        public const string Shortcut_Arguments = "Shortcut.Arguments";
        public const string Shortcut_CustomIcon = "Shortcut.CustomIcon";
        public const string Shortcut_Cancel = "Shortcut.Cancel";
        public const string Shortcut_Save = "Shortcut.Save";
        public const string Shortcut_SelectFile = "Shortcut.SelectFile";
        public const string Shortcut_SelectIcon = "Shortcut.SelectIcon";
        public const string Shortcut_NameRequired = "Shortcut.NameRequired";
        public const string Shortcut_PluginRequired = "Shortcut.PluginRequired";
        public const string Menu_Shortcut_Open = "Menu.Shortcut.Open";
        public const string Menu_Shortcut_Edit = "Menu.Shortcut.Edit";
        public const string Menu_Shortcut_Delete = "Menu.Shortcut.Delete";

        // 设置面板
        public const string Settings_Title = "Settings.Title";
        public const string Settings_BackgroundImage = "Settings.BackgroundImage";
        public const string Settings_NoBackground = "Settings.NoBackground";
        public const string Settings_Browse = "Settings.Browse";
        public const string Settings_Clear = "Settings.Clear";
        public const string Settings_ScalingMode = "Settings.ScalingMode";
        public const string Settings_Scale_Fill = "Settings.Scale.Fill";
        public const string Settings_Scale_Fit = "Settings.Scale.Fit";
        public const string Settings_Scale_Stretch = "Settings.Scale.Stretch";
        public const string Settings_Scale_Tile = "Settings.Scale.Tile";
        public const string Settings_BackgroundOpacity = "Settings.BackgroundOpacity";
        public const string Settings_Save = "Settings.Save";

        // 插件信息对话框
        public const string PluginInfo_Title = "PluginInfo.Title";
        public const string PluginInfo_PackageName = "PluginInfo.PackageName";
        public const string PluginInfo_Manufacturer = "PluginInfo.Manufacturer";
        public const string PluginInfo_Description = "PluginInfo.Description";
        public const string PluginInfo_InstallDirectory = "PluginInfo.InstallDirectory";
        public const string PluginInfo_InstallTime = "PluginInfo.InstallTime";
        public const string PluginInfo_Status = "PluginInfo.Status";
        public const string PluginInfo_Enabled = "PluginInfo.Enabled";
        public const string PluginInfo_Disabled = "PluginInfo.Disabled";
        public const string PluginInfo_SystemPlugin = "PluginInfo.SystemPlugin";
        public const string PluginInfo_Close = "PluginInfo.Close";

        // 快捷键
        public const string Hotkey_Title = "Hotkey.Title";
        public const string Hotkey_Hint = "Hotkey.Hint";
        public const string Hotkey_PressKey = "Hotkey.PressKey";
        public const string Hotkey_Clear = "Hotkey.Clear";
        public const string Hotkey_None = "Hotkey.None";
        public const string Hotkey_NeedModifier = "Hotkey.NeedModifier";
        public const string Hotkey_SetHotkey = "Hotkey.SetHotkey";
        public const string Menu_Shortcut_SetHotkey = "Menu.Shortcut.SetHotkey";
        public const string Menu_Plugin_SetHotkey = "Menu.Plugin.SetHotkey";
        public const string Menu_Folder_SetHotkey = "Menu.Folder.SetHotkey";

        // 对话框
        public const string Dialog_NewFolder = "Dialog.NewFolder";
        public const string Dialog_NewFolder_Prompt = "Dialog.NewFolder.Prompt";
        public const string Dialog_RenameFolder = "Dialog.RenameFolder";
        public const string Dialog_RenameFolder_Prompt = "Dialog.RenameFolder.Prompt";
        public const string Dialog_ConfirmUninstall = "Dialog.ConfirmUninstall";
        public const string Dialog_ConfirmUninstall_Message = "Dialog.ConfirmUninstall.Message";
        public const string Dialog_AlreadyInFolder = "Dialog.AlreadyInFolder";
        public const string Dialog_NoSettings = "Dialog.NoSettings";
        public const string Dialog_LaunchError = "Dialog.LaunchError";
        public const string Dialog_Error = "Dialog.Error";
        public const string Dialog_UninstallComplete = "Dialog.UninstallComplete";
        public const string Dialog_UninstallFailed = "Dialog.UninstallFailed";

        /// <summary>
        /// 注册所有桌面本地化资源
        /// </summary>
        /// <param name="languageSetting">语言设置，"system" 表示跟随系统</param>
        public static void RegisterAll(string languageSetting = "system")
        {
            // 获取本地化目录路径
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var localizationDir = Path.Combine(baseDir, "Assets", "Localization", "Desktop");

            // 使用 LocalizationManager 注册插件本地化
            _context = LocalizationManager.Instance.RegisterPlugin(PackageName, localizationDir, languageSetting);
        }

        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="languageSetting">"system" 或具体语言代码如 "en-US"</param>
        public static void SetLanguage(string languageSetting)
        {
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
