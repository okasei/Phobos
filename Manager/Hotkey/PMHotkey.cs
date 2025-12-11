using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Phobos.Class.Plugin.BuiltIn;

namespace Phobos.Manager.Hotkey
{
    /// <summary>
    /// 热键信息
    /// </summary>
    public class HotkeyInfo
    {
        /// <summary>
        /// 热键唯一标识（通常是 DesktopItem.Id）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 修饰键（Ctrl, Alt, Shift, Win）
        /// </summary>
        public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;

        /// <summary>
        /// 主键
        /// </summary>
        public Key Key { get; set; } = Key.None;

        /// <summary>
        /// 热键触发时的回调
        /// </summary>
        public Action? Callback { get; set; }

        /// <summary>
        /// 系统分配的热键ID（用于注销）
        /// </summary>
        internal int SystemHotkeyId { get; set; }

        /// <summary>
        /// 获取显示字符串
        /// </summary>
        public string GetDisplayString()
        {
            if (Key == Key.None)
                return string.Empty;

            var parts = new List<string>();

            if ((Modifiers & ModifierKeys.Control) != 0)
                parts.Add("Ctrl");
            if ((Modifiers & ModifierKeys.Alt) != 0)
                parts.Add("Alt");
            if ((Modifiers & ModifierKeys.Shift) != 0)
                parts.Add("Shift");
            if ((Modifiers & ModifierKeys.Windows) != 0)
                parts.Add("Win");

            parts.Add(GetKeyDisplayName(Key));

            return string.Join(" + ", parts);
        }

        /// <summary>
        /// 获取按键显示名称
        /// </summary>
        private string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                Key.NumPad0 => "Num 0",
                Key.NumPad1 => "Num 1",
                Key.NumPad2 => "Num 2",
                Key.NumPad3 => "Num 3",
                Key.NumPad4 => "Num 4",
                Key.NumPad5 => "Num 5",
                Key.NumPad6 => "Num 6",
                Key.NumPad7 => "Num 7",
                Key.NumPad8 => "Num 8",
                Key.NumPad9 => "Num 9",
                Key.OemMinus => "-",
                Key.OemPlus => "+",
                Key.OemOpenBrackets => "[",
                Key.OemCloseBrackets => "]",
                Key.OemPipe => "\\",
                Key.OemSemicolon => ";",
                Key.OemQuotes => "'",
                Key.OemComma => ",",
                Key.OemPeriod => ".",
                Key.OemQuestion => "/",
                Key.OemTilde => "`",
                _ => key.ToString()
            };
        }

        /// <summary>
        /// 从字符串解析热键
        /// </summary>
        public static HotkeyInfo? Parse(string hotkeyString)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString))
                return null;

            try
            {
                var info = new HotkeyInfo();
                var parts = hotkeyString.Split('+');

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    switch (trimmed.ToLower())
                    {
                        case "ctrl":
                        case "control":
                            info.Modifiers |= ModifierKeys.Control;
                            break;
                        case "alt":
                            info.Modifiers |= ModifierKeys.Alt;
                            break;
                        case "shift":
                            info.Modifiers |= ModifierKeys.Shift;
                            break;
                        case "win":
                        case "windows":
                            info.Modifiers |= ModifierKeys.Windows;
                            break;
                        default:
                            // 尝试解析为Key
                            if (Enum.TryParse<Key>(trimmed, true, out var key))
                            {
                                info.Key = key;
                            }
                            else if (trimmed.Length == 1 && char.IsLetterOrDigit(trimmed[0]))
                            {
                                // 单个字符
                                if (char.IsDigit(trimmed[0]))
                                {
                                    info.Key = (Key)(Key.D0 + (trimmed[0] - '0'));
                                }
                                else
                                {
                                    info.Key = (Key)(Key.A + (char.ToUpper(trimmed[0]) - 'A'));
                                }
                            }
                            break;
                    }
                }

                return info.Key == Key.None ? null : info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 转换为存储字符串
        /// </summary>
        public string ToStorageString()
        {
            if (Key == Key.None)
                return string.Empty;

            var parts = new List<string>();

            if ((Modifiers & ModifierKeys.Control) != 0)
                parts.Add("Ctrl");
            if ((Modifiers & ModifierKeys.Alt) != 0)
                parts.Add("Alt");
            if ((Modifiers & ModifierKeys.Shift) != 0)
                parts.Add("Shift");
            if ((Modifiers & ModifierKeys.Windows) != 0)
                parts.Add("Win");

            parts.Add(Key.ToString());

            return string.Join("+", parts);
        }
    }

    /// <summary>
    /// 全局热键管理器 - 管理应用程序中的全局快捷键注册和反注册
    /// </summary>
    public class PMHotkey : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        // 修饰键常量
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        #endregion

        private static PMHotkey? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<int, HotkeyInfo> _registeredHotkeys = new();
        private readonly Dictionary<string, int> _idToSystemId = new();
        private int _nextHotkeyId = 0x0001;
        private IntPtr _windowHandle = IntPtr.Zero;
        private HwndSource? _hwndSource;
        private bool _isDisposed;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static PMHotkey Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMHotkey();
                    }
                }
                return _instance;
            }
        }

        private PMHotkey() { }

        /// <summary>
        /// 初始化热键管理器（必须在主窗口加载后调用）
        /// </summary>
        public void Initialize(Window window)
        {
            if (_windowHandle != IntPtr.Zero)
            {
                PCLoggerPlugin.Info("PMHotkey", "[PMHotkey] Already initialized");
                return;
            }

            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;

            if (_windowHandle == IntPtr.Zero)
            {
                PCLoggerPlugin.Error("PMHotkey", "[PMHotkey] Failed to get window handle");
                return;
            }

            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(WndProc);

            PCLoggerPlugin.Info("PMHotkey", $"[PMHotkey] Initialized with window handle: {_windowHandle}");
        }

        /// <summary>
        /// 窗口消息处理
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();

                if (_registeredHotkeys.TryGetValue(hotkeyId, out var hotkeyInfo))
                {
                    PCLoggerPlugin.Info("PMHotkey", $"[PMHotkey] Hotkey triggered: {hotkeyInfo.Id} ({hotkeyInfo.GetDisplayString()})");

                    try
                    {
                        hotkeyInfo.Callback?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        PCLoggerPlugin.Error("PMHotkey", $"[PMHotkey] Hotkey callback error: {ex.Message}");
                    }

                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        public bool Register(HotkeyInfo hotkeyInfo)
        {
            if (_windowHandle == IntPtr.Zero)
            {
                PCLoggerPlugin.Error("PMHotkey", "[PMHotkey] Not initialized. Call Initialize() first.");
                return false;
            }

            if (hotkeyInfo.Key == Key.None)
            {
                PCLoggerPlugin.Warning("PMHotkey", $"[PMHotkey] Invalid hotkey for {hotkeyInfo.Id}");
                return false;
            }

            // 如果已存在，先注销
            if (_idToSystemId.ContainsKey(hotkeyInfo.Id))
            {
                Unregister(hotkeyInfo.Id);
            }

            int systemId = _nextHotkeyId++;
            uint modifiers = ConvertModifiers(hotkeyInfo.Modifiers) | MOD_NOREPEAT;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(hotkeyInfo.Key);

            if (RegisterHotKey(_windowHandle, systemId, modifiers, vk))
            {
                hotkeyInfo.SystemHotkeyId = systemId;
                _registeredHotkeys[systemId] = hotkeyInfo;
                _idToSystemId[hotkeyInfo.Id] = systemId;

                PCLoggerPlugin.Info("PMHotkey", $"[PMHotkey] Registered: {hotkeyInfo.Id} -> {hotkeyInfo.GetDisplayString()} (SystemId: {systemId})");
                return true;
            }
            else
            {
                PCLoggerPlugin.Warning("PMHotkey", $"[PMHotkey] Failed to register: {hotkeyInfo.Id} -> {hotkeyInfo.GetDisplayString()}. The hotkey may already be in use by another application.");
                return false;
            }
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        public bool Unregister(string id)
        {
            if (!_idToSystemId.TryGetValue(id, out var systemId))
            {
                return false;
            }

            if (_windowHandle != IntPtr.Zero && UnregisterHotKey(_windowHandle, systemId))
            {
                _registeredHotkeys.Remove(systemId);
                _idToSystemId.Remove(id);

                PCLoggerPlugin.Info("PMHotkey", $"[PMHotkey] Unregistered: {id} (SystemId: {systemId})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 注销所有热键
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var systemId in _registeredHotkeys.Keys.ToArray())
            {
                if (_windowHandle != IntPtr.Zero)
                {
                    UnregisterHotKey(_windowHandle, systemId);
                }
            }

            _registeredHotkeys.Clear();
            _idToSystemId.Clear();

            PCLoggerPlugin.Info("PMHotkey", "[PMHotkey] Unregistered all hotkeys");
        }

        /// <summary>
        /// 检查热键是否已注册
        /// </summary>
        public bool IsRegistered(string id)
        {
            return _idToSystemId.ContainsKey(id);
        }

        /// <summary>
        /// 获取已注册的热键信息
        /// </summary>
        public HotkeyInfo? GetHotkey(string id)
        {
            if (_idToSystemId.TryGetValue(id, out var systemId))
            {
                return _registeredHotkeys.GetValueOrDefault(systemId);
            }
            return null;
        }

        /// <summary>
        /// 获取所有已注册的热键
        /// </summary>
        public IReadOnlyCollection<HotkeyInfo> GetAllHotkeys()
        {
            return _registeredHotkeys.Values;
        }

        /// <summary>
        /// 转换修饰键
        /// </summary>
        private static uint ConvertModifiers(ModifierKeys modifiers)
        {
            uint result = MOD_NONE;

            if ((modifiers & ModifierKeys.Alt) != 0)
                result |= MOD_ALT;
            if ((modifiers & ModifierKeys.Control) != 0)
                result |= MOD_CONTROL;
            if ((modifiers & ModifierKeys.Shift) != 0)
                result |= MOD_SHIFT;
            if ((modifiers & ModifierKeys.Windows) != 0)
                result |= MOD_WIN;

            return result;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            UnregisterAll();

            _hwndSource?.RemoveHook(WndProc);
            _hwndSource = null;
            _windowHandle = IntPtr.Zero;
            _isDisposed = true;

            PCLoggerPlugin.Info("PMHotkey", "[PMHotkey] Disposed");
        }
    }
}
