using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Phobos.Utils.General
{
    /// <summary>
    /// 文本处理工具类
    /// </summary>
    public static class PUText
    {
        /// <summary>
        /// 转义特殊字符
        /// </summary>
        public static string Escape(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length * 2);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\t': sb.Append(@"\t"); break;
                    case '\'': sb.Append(@"\'"); break;
                    case '"': sb.Append(@"\"""); break;
                    case '\0': sb.Append(@"\0"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 反转义特殊字符
        /// </summary>
        public static string Unescape(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input
                .Replace(@"\\", "\x00BACKSLASH\x00")
                .Replace(@"\n", "\n")
                .Replace(@"\r", "\r")
                .Replace(@"\t", "\t")
                .Replace(@"\'", "'")
                .Replace(@"\""", "\"")
                .Replace(@"\0", "\0")
                .Replace("\x00BACKSLASH\x00", "\\");
        }

        /// <summary>
        /// 安全截取字符串
        /// </summary>
        public static string SafeSubstring(string? input, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (startIndex < 0)
                startIndex = 0;

            if (startIndex >= input.Length)
                return string.Empty;

            var maxLength = input.Length - startIndex;
            if (length > maxLength)
                length = maxLength;

            return input.Substring(startIndex, length);
        }

        /// <summary>
        /// 是否为有效的包名格式 (com.xxx.xxx)
        /// </summary>
        public static bool IsValidPackageName(string? packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return false;

            var pattern = @"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$";
            return Regex.IsMatch(packageName, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 是否为有效的版本号格式 (x.x.x)
        /// </summary>
        public static bool IsValidVersion(string? version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            var pattern = @"^\d+(\.\d+)*$";
            return Regex.IsMatch(version, pattern);
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        /// <returns>1: v1 > v2, 0: v1 == v2, -1: v1 < v2</returns>
        public static int CompareVersion(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');
            var maxLength = Math.Max(parts1.Length, parts2.Length);

            for (int i = 0; i < maxLength; i++)
            {
                var num1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                var num2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;

                if (num1 > num2) return 1;
                if (num1 < num2) return -1;
            }

            return 0;
        }

        /// <summary>
        /// 生成唯一标识符
        /// </summary>
        public static string GenerateUID()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 从协议链接中提取协议名
        /// </summary>
        public static string ExtractProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            var colonIndex = url.IndexOf(':');
            if (colonIndex > 0)
                return url[..colonIndex].ToLowerInvariant();

            return string.Empty;
        }

        /// <summary>
        /// 从协议链接中提取不含协议头的部分
        /// </summary>
        public static string ExtractWithoutProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            var colonIndex = url.IndexOf(':');
            if (colonIndex > 0 && colonIndex < url.Length - 1)
            {
                var result = url[(colonIndex + 1)..];
                // 去除前导的 //
                if (result.StartsWith("//"))
                    result = result[2..];
                return result;
            }

            return url;
        }
    }
}