using Phobos.Shared.Class;

namespace Phobos.Components.Arcusrix.Desktop.Components
{
    /// <summary>
    /// 桌面本地化资源
    /// </summary>
    public static partial class DesktopLocalization
    {
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
        public static void RegisterAll()
        {
            var lm = LocalizationManager.Instance;

            // 桌面标题
            lm.Register(Desktop_Title, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Phobos Desktop" },
                { "zh-CN", "Phobos 桌面" },
                { "zh-TW", "Phobos 桌面" },
                { "ja-JP", "Phobos デスクトップ" },
                { "ko-KR", "Phobos 데스크톱" }
            }));

            // 搜索占位符
            lm.Register(Desktop_Search_Placeholder, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Search plugins..." },
                { "zh-CN", "搜索插件..." },
                { "zh-TW", "搜尋插件..." },
                { "ja-JP", "プラグインを検索..." },
                { "ko-KR", "플러그인 검색..." }
            }));

            // 插件菜单
            lm.Register(Menu_Plugin_Open, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Open" },
                { "zh-CN", "打开" },
                { "zh-TW", "開啟" },
                { "ja-JP", "開く" },
                { "ko-KR", "열기" }
            }));

            lm.Register(Menu_Plugin_Info, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Info" },
                { "zh-CN", "信息" },
                { "zh-TW", "資訊" },
                { "ja-JP", "情報" },
                { "ko-KR", "정보" }
            }));

            lm.Register(Menu_Plugin_Settings, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Settings" },
                { "zh-CN", "设置" },
                { "zh-TW", "設定" },
                { "ja-JP", "設定" },
                { "ko-KR", "설정" }
            }));

            lm.Register(Menu_Plugin_Uninstall, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Uninstall" },
                { "zh-CN", "卸载" },
                { "zh-TW", "解除安裝" },
                { "ja-JP", "アンインストール" },
                { "ko-KR", "제거" }
            }));

            // 文件夹菜单
            lm.Register(Menu_Folder_Open, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Open" },
                { "zh-CN", "打开" },
                { "zh-TW", "開啟" },
                { "ja-JP", "開く" },
                { "ko-KR", "열기" }
            }));

            lm.Register(Menu_Folder_Rename, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Rename" },
                { "zh-CN", "重命名" },
                { "zh-TW", "重新命名" },
                { "ja-JP", "名前を変更" },
                { "ko-KR", "이름 바꾸기" }
            }));

            lm.Register(Menu_Folder_Delete, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Delete" },
                { "zh-CN", "删除" },
                { "zh-TW", "刪除" },
                { "ja-JP", "削除" },
                { "ko-KR", "삭제" }
            }));

            // 桌面菜单
            lm.Register(Menu_Desktop_Fullscreen, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Enter Fullscreen" },
                { "zh-CN", "进入全屏" },
                { "zh-TW", "進入全螢幕" },
                { "ja-JP", "フルスクリーンに入る" },
                { "ko-KR", "전체 화면 시작" }
            }));

            lm.Register(Menu_Desktop_ExitFullscreen, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Exit Fullscreen" },
                { "zh-CN", "退出全屏" },
                { "zh-TW", "退出全螢幕" },
                { "ja-JP", "フルスクリーンを終了" },
                { "ko-KR", "전체 화면 종료" }
            }));

            lm.Register(Menu_Desktop_Settings, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Settings" },
                { "zh-CN", "设置" },
                { "zh-TW", "設定" },
                { "ja-JP", "設定" },
                { "ko-KR", "설정" }
            }));

            lm.Register(Menu_Desktop_NewFolder, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "New Folder" },
                { "zh-CN", "新建文件夹" },
                { "zh-TW", "新增資料夾" },
                { "ja-JP", "新しいフォルダー" },
                { "ko-KR", "새 폴더" }
            }));

            lm.Register(Menu_Desktop_NewShortcut, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "New Shortcut" },
                { "zh-CN", "新建快捷方式" },
                { "zh-TW", "新增捷徑" },
                { "ja-JP", "新しいショートカット" },
                { "ko-KR", "새 바로 가기" }
            }));

            // 快捷方式
            lm.Register(Shortcut_NewTitle, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "New Shortcut" },
                { "zh-CN", "新建快捷方式" },
                { "zh-TW", "新增捷徑" },
                { "ja-JP", "新しいショートカット" },
                { "ko-KR", "새 바로 가기" }
            }));

            lm.Register(Shortcut_EditTitle, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Edit Shortcut" },
                { "zh-CN", "编辑快捷方式" },
                { "zh-TW", "編輯捷徑" },
                { "ja-JP", "ショートカットを編集" },
                { "ko-KR", "바로 가기 편집" }
            }));

            lm.Register(Shortcut_Name, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Shortcut Name" },
                { "zh-CN", "快捷方式名称" },
                { "zh-TW", "捷徑名稱" },
                { "ja-JP", "ショートカット名" },
                { "ko-KR", "바로 가기 이름" }
            }));

            lm.Register(Shortcut_TargetPlugin, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Target Plugin" },
                { "zh-CN", "目标插件" },
                { "zh-TW", "目標插件" },
                { "ja-JP", "対象プラグイン" },
                { "ko-KR", "대상 플러그인" }
            }));

            lm.Register(Shortcut_OrInputPackageName, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Or input package name manually:" },
                { "zh-CN", "或手动输入包名：" },
                { "zh-TW", "或手動輸入套件名稱：" },
                { "ja-JP", "またはパッケージ名を手動入力：" },
                { "ko-KR", "또는 패키지 이름을 직접 입력:" }
            }));

            lm.Register(Shortcut_Arguments, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Arguments (comma separated, use quotes for values with commas)" },
                { "zh-CN", "参数（逗号分隔，包含逗号的值请用双引号包裹）" },
                { "zh-TW", "參數（逗號分隔，包含逗號的值請用雙引號包裹）" },
                { "ja-JP", "引数（カンマ区切り、カンマを含む値は引用符で囲む）" },
                { "ko-KR", "인수 (쉼표로 구분, 쉼표가 포함된 값은 따옴표로 묶음)" }
            }));

            lm.Register(Shortcut_CustomIcon, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Custom Icon (optional, defaults to plugin icon)" },
                { "zh-CN", "自定义图标（可选，默认使用插件图标）" },
                { "zh-TW", "自訂圖示（可選，預設使用插件圖示）" },
                { "ja-JP", "カスタムアイコン（オプション、デフォルトはプラグインアイコン）" },
                { "ko-KR", "사용자 지정 아이콘 (선택 사항, 기본값은 플러그인 아이콘)" }
            }));

            lm.Register(Shortcut_Cancel, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Cancel" },
                { "zh-CN", "取消" },
                { "zh-TW", "取消" },
                { "ja-JP", "キャンセル" },
                { "ko-KR", "취소" }
            }));

            lm.Register(Shortcut_Save, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Save" },
                { "zh-CN", "保存" },
                { "zh-TW", "儲存" },
                { "ja-JP", "保存" },
                { "ko-KR", "저장" }
            }));

            lm.Register(Shortcut_SelectFile, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Select File" },
                { "zh-CN", "选择文件" },
                { "zh-TW", "選擇檔案" },
                { "ja-JP", "ファイルを選択" },
                { "ko-KR", "파일 선택" }
            }));

            lm.Register(Shortcut_SelectIcon, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Select Icon" },
                { "zh-CN", "选择图标" },
                { "zh-TW", "選擇圖示" },
                { "ja-JP", "アイコンを選択" },
                { "ko-KR", "아이콘 선택" }
            }));

            lm.Register(Shortcut_NameRequired, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Please enter a name for the shortcut." },
                { "zh-CN", "请输入快捷方式名称。" },
                { "zh-TW", "請輸入捷徑名稱。" },
                { "ja-JP", "ショートカット名を入力してください。" },
                { "ko-KR", "바로 가기 이름을 입력하세요." }
            }));

            lm.Register(Shortcut_PluginRequired, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Please select a target plugin." },
                { "zh-CN", "请选择目标插件。" },
                { "zh-TW", "請選擇目標插件。" },
                { "ja-JP", "対象プラグインを選択してください。" },
                { "ko-KR", "대상 플러그인을 선택하세요." }
            }));

            lm.Register(Menu_Shortcut_Open, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Open" },
                { "zh-CN", "打开" },
                { "zh-TW", "開啟" },
                { "ja-JP", "開く" },
                { "ko-KR", "열기" }
            }));

            lm.Register(Menu_Shortcut_Edit, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Edit" },
                { "zh-CN", "编辑" },
                { "zh-TW", "編輯" },
                { "ja-JP", "編集" },
                { "ko-KR", "편집" }
            }));

            lm.Register(Menu_Shortcut_Delete, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Delete" },
                { "zh-CN", "删除" },
                { "zh-TW", "刪除" },
                { "ja-JP", "削除" },
                { "ko-KR", "삭제" }
            }));

            // 设置面板
            lm.Register(Settings_Title, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Desktop Settings" },
                { "zh-CN", "桌面设置" },
                { "zh-TW", "桌面設定" },
                { "ja-JP", "デスクトップ設定" },
                { "ko-KR", "데스크톱 설정" }
            }));

            lm.Register(Settings_BackgroundImage, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Background Image" },
                { "zh-CN", "背景图片" },
                { "zh-TW", "背景圖片" },
                { "ja-JP", "背景画像" },
                { "ko-KR", "배경 이미지" }
            }));

            lm.Register(Settings_NoBackground, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "No background image" },
                { "zh-CN", "无背景图片" },
                { "zh-TW", "無背景圖片" },
                { "ja-JP", "背景画像なし" },
                { "ko-KR", "배경 이미지 없음" }
            }));

            lm.Register(Settings_Browse, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Browse" },
                { "zh-CN", "浏览" },
                { "zh-TW", "瀏覽" },
                { "ja-JP", "参照" },
                { "ko-KR", "찾아보기" }
            }));

            lm.Register(Settings_Clear, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Clear" },
                { "zh-CN", "清除" },
                { "zh-TW", "清除" },
                { "ja-JP", "クリア" },
                { "ko-KR", "지우기" }
            }));

            lm.Register(Settings_ScalingMode, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Scaling Mode" },
                { "zh-CN", "缩放模式" },
                { "zh-TW", "縮放模式" },
                { "ja-JP", "スケーリングモード" },
                { "ko-KR", "크기 조절 모드" }
            }));

            lm.Register(Settings_Scale_Fill, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Fill (Crop to fit)" },
                { "zh-CN", "填充（裁剪适应）" },
                { "zh-TW", "填充（裁剪適應）" },
                { "ja-JP", "塗りつぶし（切り抜き）" },
                { "ko-KR", "채우기 (자르기)" }
            }));

            lm.Register(Settings_Scale_Fit, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Fit (Show entire image)" },
                { "zh-CN", "适应（显示完整图片）" },
                { "zh-TW", "適應（顯示完整圖片）" },
                { "ja-JP", "フィット（全体表示）" },
                { "ko-KR", "맞춤 (전체 이미지 표시)" }
            }));

            lm.Register(Settings_Scale_Stretch, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Stretch (Distort to fit)" },
                { "zh-CN", "拉伸（变形适应）" },
                { "zh-TW", "拉伸（變形適應）" },
                { "ja-JP", "ストレッチ（変形）" },
                { "ko-KR", "늘이기 (왜곡하여 맞춤)" }
            }));

            lm.Register(Settings_Scale_Tile, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Tile" },
                { "zh-CN", "平铺" },
                { "zh-TW", "平鋪" },
                { "ja-JP", "タイル" },
                { "ko-KR", "타일" }
            }));

            lm.Register(Settings_BackgroundOpacity, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Background Opacity" },
                { "zh-CN", "背景透明度" },
                { "zh-TW", "背景透明度" },
                { "ja-JP", "背景の不透明度" },
                { "ko-KR", "배경 불투명도" }
            }));

            lm.Register(Settings_Save, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Save Settings" },
                { "zh-CN", "保存设置" },
                { "zh-TW", "儲存設定" },
                { "ja-JP", "設定を保存" },
                { "ko-KR", "설정 저장" }
            }));

            // 插件信息对话框
            lm.Register(PluginInfo_Title, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Plugin Information" },
                { "zh-CN", "插件信息" },
                { "zh-TW", "插件資訊" },
                { "ja-JP", "プラグイン情報" },
                { "ko-KR", "플러그인 정보" }
            }));

            lm.Register(PluginInfo_PackageName, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Package Name" },
                { "zh-CN", "包名" },
                { "zh-TW", "套件名稱" },
                { "ja-JP", "パッケージ名" },
                { "ko-KR", "패키지 이름" }
            }));

            lm.Register(PluginInfo_Manufacturer, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Manufacturer" },
                { "zh-CN", "制造商" },
                { "zh-TW", "製造商" },
                { "ja-JP", "メーカー" },
                { "ko-KR", "제조업체" }
            }));

            lm.Register(PluginInfo_Description, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Description" },
                { "zh-CN", "描述" },
                { "zh-TW", "描述" },
                { "ja-JP", "説明" },
                { "ko-KR", "설명" }
            }));

            lm.Register(PluginInfo_InstallDirectory, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Install Directory" },
                { "zh-CN", "安装目录" },
                { "zh-TW", "安裝目錄" },
                { "ja-JP", "インストール先" },
                { "ko-KR", "설치 디렉터리" }
            }));

            lm.Register(PluginInfo_InstallTime, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Install Time" },
                { "zh-CN", "安装时间" },
                { "zh-TW", "安裝時間" },
                { "ja-JP", "インストール日時" },
                { "ko-KR", "설치 시간" }
            }));

            lm.Register(PluginInfo_Status, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Status" },
                { "zh-CN", "状态" },
                { "zh-TW", "狀態" },
                { "ja-JP", "状態" },
                { "ko-KR", "상태" }
            }));

            lm.Register(PluginInfo_Enabled, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Enabled" },
                { "zh-CN", "已启用" },
                { "zh-TW", "已啟用" },
                { "ja-JP", "有効" },
                { "ko-KR", "활성화됨" }
            }));

            lm.Register(PluginInfo_Disabled, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Disabled" },
                { "zh-CN", "已禁用" },
                { "zh-TW", "已停用" },
                { "ja-JP", "無効" },
                { "ko-KR", "비활성화됨" }
            }));

            lm.Register(PluginInfo_SystemPlugin, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "System Plugin" },
                { "zh-CN", "系统插件" },
                { "zh-TW", "系統插件" },
                { "ja-JP", "システムプラグイン" },
                { "ko-KR", "시스템 플러그인" }
            }));

            lm.Register(PluginInfo_Close, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Close" },
                { "zh-CN", "关闭" },
                { "zh-TW", "關閉" },
                { "ja-JP", "閉じる" },
                { "ko-KR", "닫기" }
            }));

            // 快捷键
            lm.Register(Hotkey_Title, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Set Hotkey" },
                { "zh-CN", "设置快捷键" },
                { "zh-TW", "設定快捷鍵" },
                { "ja-JP", "ホットキーを設定" },
                { "ko-KR", "단축키 설정" }
            }));

            lm.Register(Hotkey_Hint, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Press the key combination you want to use:" },
                { "zh-CN", "按下您想要使用的组合键：" },
                { "zh-TW", "按下您想要使用的組合鍵：" },
                { "ja-JP", "使用したいキーの組み合わせを押してください：" },
                { "ko-KR", "사용하려는 키 조합을 누르세요:" }
            }));

            lm.Register(Hotkey_PressKey, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Press a key..." },
                { "zh-CN", "按下按键..." },
                { "zh-TW", "按下按鍵..." },
                { "ja-JP", "キーを押してください..." },
                { "ko-KR", "키를 누르세요..." }
            }));

            lm.Register(Hotkey_Clear, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Clear Hotkey" },
                { "zh-CN", "清除快捷键" },
                { "zh-TW", "清除快捷鍵" },
                { "ja-JP", "ホットキーをクリア" },
                { "ko-KR", "단축키 지우기" }
            }));

            lm.Register(Hotkey_None, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "None" },
                { "zh-CN", "无" },
                { "zh-TW", "無" },
                { "ja-JP", "なし" },
                { "ko-KR", "없음" }
            }));

            lm.Register(Hotkey_NeedModifier, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Please use at least one modifier key (Ctrl, Alt, Shift, or Win)." },
                { "zh-CN", "请至少使用一个修饰键（Ctrl、Alt、Shift 或 Win）。" },
                { "zh-TW", "請至少使用一個修飾鍵（Ctrl、Alt、Shift 或 Win）。" },
                { "ja-JP", "少なくとも1つの修飾キー（Ctrl、Alt、Shift、またはWin）を使用してください。" },
                { "ko-KR", "수정자 키(Ctrl, Alt, Shift 또는 Win)를 하나 이상 사용하세요." }
            }));

            lm.Register(Hotkey_SetHotkey, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Hotkey" },
                { "zh-CN", "快捷键" },
                { "zh-TW", "快捷鍵" },
                { "ja-JP", "ホットキー" },
                { "ko-KR", "단축키" }
            }));

            lm.Register(Menu_Shortcut_SetHotkey, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Set Hotkey" },
                { "zh-CN", "设置快捷键" },
                { "zh-TW", "設定快捷鍵" },
                { "ja-JP", "ホットキーを設定" },
                { "ko-KR", "단축키 설정" }
            }));

            lm.Register(Menu_Plugin_SetHotkey, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Set Hotkey" },
                { "zh-CN", "设置快捷键" },
                { "zh-TW", "設定快捷鍵" },
                { "ja-JP", "ホットキーを設定" },
                { "ko-KR", "단축키 설정" }
            }));

            lm.Register(Menu_Folder_SetHotkey, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Set Hotkey" },
                { "zh-CN", "设置快捷键" },
                { "zh-TW", "設定快捷鍵" },
                { "ja-JP", "ホットキーを設定" },
                { "ko-KR", "단축키 설정" }
            }));

            // 对话框
            lm.Register(Dialog_NewFolder, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "New Folder" },
                { "zh-CN", "新建文件夹" },
                { "zh-TW", "新增資料夾" },
                { "ja-JP", "新しいフォルダー" },
                { "ko-KR", "새 폴더" }
            }));

            lm.Register(Dialog_NewFolder_Prompt, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Enter folder name:" },
                { "zh-CN", "输入文件夹名称：" },
                { "zh-TW", "輸入資料夾名稱：" },
                { "ja-JP", "フォルダー名を入力：" },
                { "ko-KR", "폴더 이름 입력:" }
            }));

            lm.Register(Dialog_RenameFolder, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Rename Folder" },
                { "zh-CN", "重命名文件夹" },
                { "zh-TW", "重新命名資料夾" },
                { "ja-JP", "フォルダーの名前を変更" },
                { "ko-KR", "폴더 이름 바꾸기" }
            }));

            lm.Register(Dialog_RenameFolder_Prompt, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Enter new folder name:" },
                { "zh-CN", "输入新的文件夹名称：" },
                { "zh-TW", "輸入新的資料夾名稱：" },
                { "ja-JP", "新しいフォルダー名を入力：" },
                { "ko-KR", "새 폴더 이름 입력:" }
            }));

            lm.Register(Dialog_ConfirmUninstall, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Confirm Uninstall" },
                { "zh-CN", "确认卸载" },
                { "zh-TW", "確認解除安裝" },
                { "ja-JP", "アンインストールの確認" },
                { "ko-KR", "제거 확인" }
            }));

            lm.Register(Dialog_ConfirmUninstall_Message, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Are you sure you want to uninstall '{0}'?\n\nThis action cannot be undone." },
                { "zh-CN", "确定要卸载 '{0}' 吗？\n\n此操作无法撤销。" },
                { "zh-TW", "確定要解除安裝 '{0}' 嗎？\n\n此操作無法復原。" },
                { "ja-JP", "'{0}' をアンインストールしますか？\n\nこの操作は元に戻せません。" },
                { "ko-KR", "'{0}'을(를) 제거하시겠습니까?\n\n이 작업은 취소할 수 없습니다." }
            }));

            lm.Register(Dialog_AlreadyInFolder, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Plugin '{0}' is already in this folder." },
                { "zh-CN", "插件 '{0}' 已在此文件夹中。" },
                { "zh-TW", "插件 '{0}' 已在此資料夾中。" },
                { "ja-JP", "プラグイン '{0}' は既にこのフォルダーにあります。" },
                { "ko-KR", "플러그인 '{0}'은(는) 이미 이 폴더에 있습니다." }
            }));

            lm.Register(Dialog_NoSettings, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Plugin '{0}' does not have a settings page." },
                { "zh-CN", "插件 '{0}' 没有设置页面。" },
                { "zh-TW", "插件 '{0}' 沒有設定頁面。" },
                { "ja-JP", "プラグイン '{0}' には設定ページがありません。" },
                { "ko-KR", "플러그인 '{0}'에는 설정 페이지가 없습니다." }
            }));

            lm.Register(Dialog_LaunchError, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Launch Error" },
                { "zh-CN", "启动错误" },
                { "zh-TW", "啟動錯誤" },
                { "ja-JP", "起動エラー" },
                { "ko-KR", "실행 오류" }
            }));

            lm.Register(Dialog_Error, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Error" },
                { "zh-CN", "错误" },
                { "zh-TW", "錯誤" },
                { "ja-JP", "エラー" },
                { "ko-KR", "오류" }
            }));

            lm.Register(Dialog_UninstallComplete, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Uninstall Complete" },
                { "zh-CN", "卸载完成" },
                { "zh-TW", "解除安裝完成" },
                { "ja-JP", "アンインストール完了" },
                { "ko-KR", "제거 완료" }
            }));

            lm.Register(Dialog_UninstallFailed, new LocalizedString(new Dictionary<string, string>
            {
                { "en-US", "Uninstall Failed" },
                { "zh-CN", "卸载失败" },
                { "zh-TW", "解除安裝失敗" },
                { "ja-JP", "アンインストール失敗" },
                { "ko-KR", "제거 실패" }
            }));
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public static string Get(string key)
        {
            return LocalizationManager.Instance.Get(key);
        }

        /// <summary>
        /// 获取格式化的本地化文本
        /// </summary>
        public static string GetFormat(string key, params object[] args)
        {
            return LocalizationManager.Instance.GetFormat(key, args);
        }
    }
}
