using Phobos.Shared.Class;
using System;
using System.Globalization;
using System.IO;

namespace Phobos.Components.Arcusrix.ThemeManager.Helpers
{
    /// <summary>
    /// 主题管理器本地化资源
    /// </summary>
    public static class ThemeManagerLocalization
    {
        private const string PackageName = "com.phobos.thememanager";
        private static PluginLocalizationContext? _context;
        private static string _currentLanguage = "en-US";

        /// <summary>
        /// 获取当前语言
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        // 资源键常量
        public const string Title = "tm.title";
        public const string Subtitle = "tm.subtitle";

        // 按钮
        public const string Button_Refresh = "button.refresh";
        public const string Button_Import = "button.import";
        public const string Button_Create = "button.create";
        public const string Button_Apply = "button.apply";
        public const string Button_Preview = "button.preview";
        public const string Button_Export = "button.export";
        public const string Button_Save = "button.save";

        // 列表
        public const string List_Search = "list.search";
        public const string List_Current = "list.current";
        public const string List_Select = "list.select";

        // 预览
        public const string Preview_Title = "preview.title";
        public const string Preview_Buttons = "preview.buttons";
        public const string Preview_Input = "preview.input";
        public const string Preview_Selection = "preview.selection";
        public const string Preview_Toggle = "preview.toggle";
        public const string Preview_Progress = "preview.progress";
        public const string Preview_List = "preview.list";

        // 编辑器
        public const string Editor_Title = "editor.title";
        public const string Editor_Primary = "editor.primary";
        public const string Editor_Background = "editor.background";
        public const string Editor_Foreground = "editor.foreground";
        public const string Editor_Status = "editor.status";
        public const string Editor_Border = "editor.border";
        public const string Editor_Readonly = "editor.readonly";

        // 状态
        public const string Status_Ready = "status.ready";
        public const string Status_Loading = "status.loading";
        public const string Status_Found = "status.found";
        public const string Status_Selected = "status.selected";
        public const string Status_Applied = "status.applied";
        public const string Status_Exported = "status.exported";
        public const string Status_Saved = "status.saved";
        public const string Status_Created = "status.created";
        public const string Status_Imported = "status.imported";
        public const string Status_Importing = "status.importing";
        public const string Status_ApplyRestart = "status.apply_restart";
        public const string Status_NoChanges = "status.no_changes";
        public const string Status_ImportCancelled = "status.import_cancelled";
        public const string Status_ImportFailed = "status.import_failed";
        public const string Status_ApplyFailed = "status.apply_failed";

        // 对话框
        public const string Dialog_SelectTheme = "dialog.select_theme";
        public const string Dialog_ExportOnlyFile = "dialog.export_only_file";
        public const string Dialog_NoEditable = "dialog.no_editable";
        public const string Dialog_FileExists = "dialog.file_exists";
        public const string Dialog_LoadFailed = "dialog.load_failed";
        public const string Dialog_Duplicate = "dialog.duplicate";
        public const string Dialog_ExportFailed = "dialog.export_failed";
        public const string Dialog_ImportFailed = "dialog.import_failed";
        public const string Dialog_SaveFailed = "dialog.save_failed";

        // 菜单
        public const string Menu_Expand = "menu.expand";
        public const string Menu_Collapse = "menu.collapse";

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
            var localizationDir = Path.Combine(baseDir, "Assets", "Localization", "ThemeManager");

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
