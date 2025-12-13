using Microsoft.Win32;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Phobos.Service.Arcusrix
{
    /// <summary>
    /// 系统通知类型
    /// </summary>
    public enum SystemNotificationType
    {
        /// <summary>
        /// 设备连接
        /// </summary>
        DeviceConnected,

        /// <summary>
        /// 设备断开
        /// </summary>
        DeviceDisconnected,

        /// <summary>
        /// 电量状态变化
        /// </summary>
        BatteryStatus,

        /// <summary>
        /// 省电模式变化
        /// </summary>
        PowerSaverMode,

        /// <summary>
        /// 充电状态变化
        /// </summary>
        ChargingStatus,

        /// <summary>
        /// 网络状态变化
        /// </summary>
        NetworkStatus
    }

    /// <summary>
    /// 系统通知事件参数
    /// </summary>
    public class SystemNotificationEventArgs : EventArgs
    {
        public SystemNotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? IconPath { get; set; }
        public List<NotificationAction> Actions { get; set; } = new();
        public object? Data { get; set; }
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DeviceClass { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public bool CanOpen { get; set; }
        public string? OpenCommand { get; set; }
    }

    /// <summary>
    /// 系统监控服务
    /// 监控设备连接、电池状态、网络状态等
    /// </summary>
    public class PSSystemMonitor : IDisposable
    {
        #region P/Invoke for Battery Status

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        // ACLineStatus values
        private const byte AC_LINE_OFFLINE = 0;
        private const byte AC_LINE_ONLINE = 1;
        private const byte AC_LINE_UNKNOWN = 255;

        // BatteryFlag values
        private const byte BATTERY_FLAG_HIGH = 1;
        private const byte BATTERY_FLAG_LOW = 2;
        private const byte BATTERY_FLAG_CRITICAL = 4;
        private const byte BATTERY_FLAG_CHARGING = 8;
        private const byte BATTERY_FLAG_NO_BATTERY = 128;

        #endregion

        private bool _isRunning;
        private bool _disposed;

        // WMI 监控器
        private ManagementEventWatcher? _deviceInsertWatcher;
        private ManagementEventWatcher? _deviceRemoveWatcher;

        // 电池状态
        private int _lastBatteryPercent = -1;
        private bool _lastIsCharging;
        private bool _lastIsPowerSaver;
        private byte _lastACLineStatus = AC_LINE_UNKNOWN;
        private readonly HashSet<int> _notifiedBatteryLevels = new();

        // 网络状态
        private bool _lastNetworkAvailable;

        // 媒体状态已移至 Phobos.WinRT.PCSmtcPlugin

        // 定时检查
        private System.Threading.Timer? _batteryCheckTimer;
        private const int BatteryCheckIntervalMs = 30000; // 30秒检查一次

        /// <summary>
        /// 系统通知事件
        /// </summary>
        public event EventHandler<SystemNotificationEventArgs>? SystemNotificationReceived;

        public PSSystemMonitor()
        {
            _lastNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
        }

        /// <summary>
        /// 启动监控
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning) return;
            _isRunning = true;

            try
            {
                // 初始化电池状态
                InitializeBatteryStatus();

                // 启动设备监控
                StartDeviceMonitoring();

                // 启动电池监控
                StartBatteryMonitoring();

                // 启动网络监控
                StartNetworkMonitoring();

                // 启动系统事件监控
                StartSystemEventMonitoring();

                // 媒体监控已移至 Phobos.WinRT.PCSmtcPlugin

                PCLoggerPlugin.Info("SystemMonitor", "System monitor service started");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Failed to start: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning) return;
            _isRunning = false;

            try
            {
                // 停止定时器
                _batteryCheckTimer?.Dispose();
                _batteryCheckTimer = null;

                // 停止 WMI 监控
                StopDeviceMonitoring();

                // 停止系统事件监控
                StopSystemEventMonitoring();

                // 停止网络监控
                StopNetworkMonitoring();

                // 媒体监控已移至 Phobos.WinRT.PCSmtcPlugin

                PCLoggerPlugin.Info("SystemMonitor", "System monitor service stopped");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Failed to stop: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        #region 电池监控

        private void InitializeBatteryStatus()
        {
            if (GetSystemPowerStatus(out var status))
            {
                _lastBatteryPercent = status.BatteryLifePercent == 255 ? -1 : status.BatteryLifePercent;
                _lastIsCharging = (status.BatteryFlag & BATTERY_FLAG_CHARGING) != 0;
                _lastACLineStatus = status.ACLineStatus;
            }

            // 检查省电模式（通过注册表）
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power");
                if (key != null)
                {
                    var energySaverStatus = key.GetValue("EnergySaverPolicyStatus");
                    _lastIsPowerSaver = energySaverStatus != null && Convert.ToInt32(energySaverStatus) == 1;
                }
            }
            catch
            {
                _lastIsPowerSaver = false;
            }

            _notifiedBatteryLevels.Clear();
        }

        private void StartBatteryMonitoring()
        {
            _batteryCheckTimer = new System.Threading.Timer(
                CheckBatteryStatus,
                null,
                BatteryCheckIntervalMs,
                BatteryCheckIntervalMs
            );
        }

        private void CheckBatteryStatus(object? state)
        {
            if (!_isRunning) return;

            try
            {
                if (!GetSystemPowerStatus(out var status)) return;

                // 如果没有电池，跳过
                if ((status.BatteryFlag & BATTERY_FLAG_NO_BATTERY) != 0) return;

                var currentPercent = status.BatteryLifePercent == 255 ? -1 : status.BatteryLifePercent;
                var isCharging = (status.BatteryFlag & BATTERY_FLAG_CHARGING) != 0;
                var acLineStatus = status.ACLineStatus;

                // 检查电量阈值通知 (50%, 25%, 5%)
                if (currentPercent >= 0)
                {
                    CheckBatteryThresholds(currentPercent, isCharging);
                }

                // 检查充电状态变化
                if (isCharging != _lastIsCharging)
                {
                    if (isCharging)
                    {
                        OnSystemNotification(new SystemNotificationEventArgs
                        {
                            Type = SystemNotificationType.ChargingStatus,
                            Title = "开始充电",
                            Content = $"设备正在充电，当前电量 {currentPercent}%",
                            IconPath = "pack://application:,,,/Assets/Icons/battery-charging.png"
                        });
                        // 开始充电时清除已通知的电量级别
                        _notifiedBatteryLevels.Clear();
                    }
                    _lastIsCharging = isCharging;
                }

                // 检查电源适配器连接状态
                if (acLineStatus != _lastACLineStatus)
                {
                    if (acLineStatus == AC_LINE_ONLINE)
                    {
                        OnSystemNotification(new SystemNotificationEventArgs
                        {
                            Type = SystemNotificationType.ChargingStatus,
                            Title = "已连接适配器",
                            Content = "电源适配器已连接",
                            IconPath = "pack://application:,,,/Assets/Icons/power-adapter.png"
                        });
                    }
                    else if (acLineStatus == AC_LINE_OFFLINE)
                    {
                        OnSystemNotification(new SystemNotificationEventArgs
                        {
                            Type = SystemNotificationType.ChargingStatus,
                            Title = "已断开适配器",
                            Content = $"电源适配器已断开，当前电量 {currentPercent}%",
                            IconPath = "pack://application:,,,/Assets/Icons/power-adapter-off.png"
                        });
                    }
                    _lastACLineStatus = acLineStatus;
                }

                // 检查省电模式
                CheckPowerSaverMode();

                _lastBatteryPercent = currentPercent;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Battery check failed: {ex.Message}");
            }
        }

        private void CheckBatteryThresholds(int currentPercent, bool isCharging)
        {
            if (isCharging) return; // 充电时不提醒

            var thresholds = new[] { 50, 25, 5 };

            foreach (var threshold in thresholds)
            {
                if (currentPercent <= threshold &&
                    _lastBatteryPercent > threshold &&
                    !_notifiedBatteryLevels.Contains(threshold))
                {
                    _notifiedBatteryLevels.Add(threshold);

                    var urgency = threshold switch
                    {
                        5 => "紧急",
                        25 => "较低",
                        _ => "中等"
                    };

                    var iconPath = threshold switch
                    {
                        5 => "pack://application:,,,/Assets/Icons/battery-critical.png",
                        25 => "pack://application:,,,/Assets/Icons/battery-low.png",
                        _ => "pack://application:,,,/Assets/Icons/battery-medium.png"
                    };

                    OnSystemNotification(new SystemNotificationEventArgs
                    {
                        Type = SystemNotificationType.BatteryStatus,
                        Title = $"电量{urgency}",
                        Content = $"当前电量 {currentPercent}%，请及时充电",
                        IconPath = iconPath,
                        Data = currentPercent
                    });
                    break;
                }
            }
        }

        private void CheckPowerSaverMode()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power");
                if (key != null)
                {
                    var energySaverStatus = key.GetValue("EnergySaverPolicyStatus");
                    var isPowerSaver = energySaverStatus != null && Convert.ToInt32(energySaverStatus) == 1;

                    if (isPowerSaver != _lastIsPowerSaver)
                    {
                        OnSystemNotification(new SystemNotificationEventArgs
                        {
                            Type = SystemNotificationType.PowerSaverMode,
                            Title = isPowerSaver ? "省电模式已开启" : "省电模式已关闭",
                            Content = isPowerSaver
                                ? "设备已进入省电模式，部分功能可能受限"
                                : "设备已退出省电模式",
                            IconPath = isPowerSaver
                                ? "pack://application:,,,/Assets/Icons/power-saver-on.png"
                                : "pack://application:,,,/Assets/Icons/power-saver-off.png"
                        });
                        _lastIsPowerSaver = isPowerSaver;
                    }
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Power saver check failed: {ex.Message}");
            }
        }

        #endregion

        #region 设备监控

        private void StartDeviceMonitoring()
        {
            try
            {
                // 监控设备插入
                var insertQuery = new WqlEventQuery(
                    "SELECT * FROM __InstanceCreationEvent WITHIN 2 " +
                    "WHERE TargetInstance ISA 'Win32_PnPEntity'"
                );
                _deviceInsertWatcher = new ManagementEventWatcher(insertQuery);
                _deviceInsertWatcher.EventArrived += OnDeviceInserted;
                _deviceInsertWatcher.Start();

                // 监控设备移除
                var removeQuery = new WqlEventQuery(
                    "SELECT * FROM __InstanceDeletionEvent WITHIN 2 " +
                    "WHERE TargetInstance ISA 'Win32_PnPEntity'"
                );
                _deviceRemoveWatcher = new ManagementEventWatcher(removeQuery);
                _deviceRemoveWatcher.EventArrived += OnDeviceRemoved;
                _deviceRemoveWatcher.Start();

                PCLoggerPlugin.Info("SystemMonitor", "Device monitoring started");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Failed to start device monitoring: {ex.Message}");
            }
        }

        private void StopDeviceMonitoring()
        {
            try
            {
                _deviceInsertWatcher?.Stop();
                _deviceInsertWatcher?.Dispose();
                _deviceInsertWatcher = null;

                _deviceRemoveWatcher?.Stop();
                _deviceRemoveWatcher?.Dispose();
                _deviceRemoveWatcher = null;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Failed to stop device monitoring: {ex.Message}");
            }
        }

        private void OnDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var deviceInfo = ExtractDeviceInfo(targetInstance);

                // 过滤掉不需要通知的设备（如内部设备）
                if (ShouldNotifyDevice(deviceInfo))
                {
                    var args = new SystemNotificationEventArgs
                    {
                        Type = SystemNotificationType.DeviceConnected,
                        Title = "设备已连接",
                        Content = string.IsNullOrEmpty(deviceInfo.Name)
                            ? deviceInfo.Description
                            : deviceInfo.Name,
                        IconPath = GetDeviceIconPath(deviceInfo.DeviceClass),
                        Data = deviceInfo
                    };

                    // 如果设备可以打开，添加打开按钮
                    if (deviceInfo.CanOpen && !string.IsNullOrEmpty(deviceInfo.OpenCommand))
                    {
                        args.Actions.Add(new NotificationAction
                        {
                            Text = "打开",
                            Category = NotificationActionCategory.Primary,
                            OnClick = () => OpenDevice(deviceInfo)
                        });
                    }

                    OnSystemNotification(args);
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Device insert event failed: {ex.Message}");
            }
        }

        private void OnDeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var deviceInfo = ExtractDeviceInfo(targetInstance);

                if (ShouldNotifyDevice(deviceInfo))
                {
                    OnSystemNotification(new SystemNotificationEventArgs
                    {
                        Type = SystemNotificationType.DeviceDisconnected,
                        Title = "设备已断开",
                        Content = string.IsNullOrEmpty(deviceInfo.Name)
                            ? deviceInfo.Description
                            : deviceInfo.Name,
                        IconPath = GetDeviceIconPath(deviceInfo.DeviceClass),
                        Data = deviceInfo
                    });
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Device remove event failed: {ex.Message}");
            }
        }

        private DeviceInfo ExtractDeviceInfo(ManagementBaseObject device)
        {
            var deviceId = device["DeviceID"]?.ToString() ?? string.Empty;
            var name = device["Name"]?.ToString() ?? string.Empty;
            var description = device["Description"]?.ToString() ?? string.Empty;
            var deviceClass = device["PNPClass"]?.ToString() ?? string.Empty;
            var manufacturer = device["Manufacturer"]?.ToString() ?? string.Empty;

            var info = new DeviceInfo
            {
                DeviceId = deviceId,
                Name = name,
                Description = description,
                DeviceClass = deviceClass,
                Manufacturer = manufacturer,
                CanOpen = false
            };

            // 检查设备是否可以打开
            DetermineDeviceOpenability(info);

            return info;
        }

        private void DetermineDeviceOpenability(DeviceInfo info)
        {
            var deviceClass = info.DeviceClass.ToUpperInvariant();

            // USB 存储设备（可以打开资源管理器）
            if (deviceClass.Contains("USB") || deviceClass.Contains("DISK") ||
                deviceClass.Contains("STORAGE") || deviceClass.Contains("VOLUME"))
            {
                // 尝试找到对应的驱动器盘符
                var driveLetter = FindDriveLetterForDevice(info.DeviceId);
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    info.CanOpen = true;
                    info.OpenCommand = $"explorer.exe {driveLetter}";
                }
            }
            // 蓝牙设备（可以打开蓝牙设置）
            else if (deviceClass.Contains("BLUETOOTH"))
            {
                info.CanOpen = true;
                info.OpenCommand = "ms-settings:bluetooth";
            }
            // 音频设备（可以打开声音设置）
            else if (deviceClass.Contains("AUDIO") || deviceClass.Contains("MEDIA"))
            {
                info.CanOpen = true;
                info.OpenCommand = "ms-settings:sound";
            }
            // 打印机（可以打开打印机设置）
            else if (deviceClass.Contains("PRINT"))
            {
                info.CanOpen = true;
                info.OpenCommand = "ms-settings:printers";
            }
            // 摄像头（可以打开相机隐私设置）
            else if (deviceClass.Contains("CAMERA") || deviceClass.Contains("IMAGE"))
            {
                info.CanOpen = true;
                info.OpenCommand = "ms-settings:privacy-webcam";
            }
        }

        private string? FindDriveLetterForDevice(string deviceId)
        {
            try
            {
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (drive.DriveType == System.IO.DriveType.Removable && drive.IsReady)
                    {
                        // 简单返回第一个可移动驱动器（更复杂的实现需要关联设备ID）
                        return drive.Name;
                    }
                }
            }
            catch { }
            return null;
        }

        private bool ShouldNotifyDevice(DeviceInfo info)
        {
            // 过滤掉不需要通知的设备
            var skipClasses = new[]
            {
                "SYSTEM", "PROCESSOR", "VOLUME", "DISKDRIVE",
                "HIDCLASS", "DISPLAY", "MONITOR", "FIRMWARE",
                "COMPUTER", "USB\\ROOT_HUB", "ACPI"
            };

            var upperDeviceClass = info.DeviceClass.ToUpperInvariant();
            var upperDeviceId = info.DeviceId.ToUpperInvariant();

            // 跳过系统内部设备
            foreach (var skip in skipClasses)
            {
                if (upperDeviceClass.Contains(skip) || upperDeviceId.Contains(skip))
                {
                    return false;
                }
            }

            // 跳过空名称和描述的设备
            if (string.IsNullOrWhiteSpace(info.Name) && string.IsNullOrWhiteSpace(info.Description))
            {
                return false;
            }

            return true;
        }

        private string GetDeviceIconPath(string deviceClass)
        {
            var upperClass = deviceClass.ToUpperInvariant();

            if (upperClass.Contains("USB"))
                return "pack://application:,,,/Assets/Icons/device-usb.png";
            if (upperClass.Contains("BLUETOOTH"))
                return "pack://application:,,,/Assets/Icons/device-bluetooth.png";
            if (upperClass.Contains("AUDIO") || upperClass.Contains("MEDIA"))
                return "pack://application:,,,/Assets/Icons/device-audio.png";
            if (upperClass.Contains("PRINT"))
                return "pack://application:,,,/Assets/Icons/device-printer.png";
            if (upperClass.Contains("CAMERA") || upperClass.Contains("IMAGE"))
                return "pack://application:,,,/Assets/Icons/device-camera.png";
            if (upperClass.Contains("NETWORK") || upperClass.Contains("NET"))
                return "pack://application:,,,/Assets/Icons/device-network.png";

            return "pack://application:,,,/Assets/Icons/device-generic.png";
        }

        private void OpenDevice(DeviceInfo info)
        {
            if (!info.CanOpen || string.IsNullOrEmpty(info.OpenCommand)) return;

            try
            {
                var command = info.OpenCommand;

                if (command.StartsWith("ms-settings:"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = command,
                        UseShellExecute = true
                    });
                }
                else if (command.StartsWith("explorer.exe"))
                {
                    var path = command.Replace("explorer.exe ", "").Trim();
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Failed to open device: {ex.Message}");
            }
        }

        #endregion

        #region 网络监控

        private void StartNetworkMonitoring()
        {
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            PCLoggerPlugin.Info("SystemMonitor", "Network monitoring started");
        }

        private void StopNetworkMonitoring()
        {
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            if (!_isRunning) return;

            if (e.IsAvailable != _lastNetworkAvailable)
            {
                _lastNetworkAvailable = e.IsAvailable;

                OnSystemNotification(new SystemNotificationEventArgs
                {
                    Type = SystemNotificationType.NetworkStatus,
                    Title = e.IsAvailable ? "网络已连接" : "网络已断开",
                    Content = e.IsAvailable
                        ? "设备已连接到网络"
                        : "设备已断开网络连接",
                    IconPath = e.IsAvailable
                        ? "pack://application:,,,/Assets/Icons/network-connected.png"
                        : "pack://application:,,,/Assets/Icons/network-disconnected.png",
                    Actions = e.IsAvailable ? new List<NotificationAction>() : new List<NotificationAction>
                    {
                        new NotificationAction
                        {
                            Text = "网络设置",
                            Category = NotificationActionCategory.Primary,
                            OnClick = () =>
                            {
                                try
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "ms-settings:network-status",
                                        UseShellExecute = true
                                    });
                                }
                                catch { }
                            }
                        }
                    }
                });
            }
        }

        private void OnNetworkAddressChanged(object? sender, EventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                var activeConnection = GetActiveNetworkConnection();
                if (!string.IsNullOrEmpty(activeConnection))
                {
                    OnSystemNotification(new SystemNotificationEventArgs
                    {
                        Type = SystemNotificationType.NetworkStatus,
                        Title = "网络配置已更改",
                        Content = $"当前连接: {activeConnection}",
                    });
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Network address change handling failed: {ex.Message}");
            }
        }

        private string GetActiveNetworkConnection()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                if (interfaces.Count > 0)
                {
                    var primary = interfaces.FirstOrDefault(ni =>
                        ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);

                    return primary?.Name ?? interfaces.First().Name;
                }
            }
            catch { }

            return string.Empty;
        }

        #endregion

        #region 系统事件监控

        private void StartSystemEventMonitoring()
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.SessionSwitch += OnSessionSwitch;
            PCLoggerPlugin.Info("SystemMonitor", "System event monitoring started");
        }

        private void StopSystemEventMonitoring()
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (!_isRunning) return;

            // PowerModeChanged 主要用于休眠/恢复，电池状态由定时器处理
            if (e.Mode == PowerModes.StatusChange)
            {
                // 立即检查电池状态
                CheckBatteryStatus(null);
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // 可以在这里处理用户登录/注销等事件
        }

        #endregion

        // 媒体监控 (SMTC) 已移至 Phobos.WinRT.PCSmtcPlugin

        private void OnSystemNotification(SystemNotificationEventArgs args)
        {
            try
            {
                SystemNotificationReceived?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("SystemMonitor", $"Failed to raise notification: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            StopAsync().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
