using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Phobos.Utils.System
{
    /// <summary>
    /// 机器码工具类
    /// </summary>
    public static class PUMachineCode
    {
        private static string? _cachedMachineCode;

        /// <summary>
        /// 获取机器码（基于主板序列号）
        /// </summary>
        public static string GetMachineCode()
        {
            if (!string.IsNullOrEmpty(_cachedMachineCode))
                return _cachedMachineCode;

            try
            {
                var baseboardInfo = GetBaseboardSerialNumber();
                var processorId = GetProcessorId();
                var combinedInfo = $"{baseboardInfo}_{processorId}";

                _cachedMachineCode = ComputeHash(combinedInfo);
                return _cachedMachineCode;
            }
            catch
            {
                // 如果获取失败，使用固定的回退值
                _cachedMachineCode = "Phobos_Default_Key_2024";
                return _cachedMachineCode;
            }
        }

        private static string GetBaseboardSerialNumber()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(serial))
                        return serial;
                }
            }
            catch { }
            return "DefaultBaseboard";
        }

        private static string GetProcessorId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var id = obj["ProcessorId"]?.ToString();
                    if (!string.IsNullOrEmpty(id))
                        return id;
                }
            }
            catch { }
            return "DefaultProcessor";
        }

        private static string ComputeHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 验证机器码
        /// </summary>
        public static bool ValidateMachineCode(string code)
        {
            return code == GetMachineCode();
        }
    }
}