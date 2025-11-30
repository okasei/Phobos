using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Phobos.Shared.Class
{
    /// <summary>
    /// 文本转义工具
    /// </summary>
    public static class TextEscaper
    {
        private static readonly Dictionary<char, string> EscapeMap = new()
        {
            { '\\', @"\\" },
            { '\n', @"\n" },
            { '\r', @"\r" },
            { '\t', @"\t" },
            { '\'', @"\'" },
            { '"', @"\""" },
            { '\0', @"\0" }
        };

        private static readonly Dictionary<string, char> UnescapeMap = new()
        {
            { @"\\", '\\' },
            { @"\n", '\n' },
            { @"\r", '\r' },
            { @"\t", '\t' },
            { @"\'", '\'' },
            { @"\""", '"' },
            { @"\0", '\0' }
        };

        /// <summary>
        /// 转义文本
        /// </summary>
        public static string Escape(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length * 2);
            foreach (var c in input)
            {
                if (EscapeMap.TryGetValue(c, out var escaped))
                    sb.Append(escaped);
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 反转义文本
        /// </summary>
        public static string Unescape(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var result = input;
            foreach (var kvp in UnescapeMap)
            {
                result = result.Replace(kvp.Key, kvp.Value.ToString());
            }
            return result;
        }
    }

    /// <summary>
    /// 本地化字符串
    /// </summary>
    public class LocalizedString
    {
        private readonly Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

        public LocalizedString() { }

        public LocalizedString(string defaultValue)
        {
            _translations["en-US"] = defaultValue;
        }

        public LocalizedString(Dictionary<string, string> translations)
        {
            foreach (var kvp in translations)
            {
                _translations[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 添加翻译
        /// </summary>
        public void Add(string languageCode, string value)
        {
            _translations[languageCode] = TextEscaper.Escape(value);
        }

        /// <summary>
        /// 获取指定语言的文本
        /// </summary>
        public string Get(string languageCode)
        {
            if (_translations.TryGetValue(languageCode, out var value))
                return TextEscaper.Unescape(value);

            // 尝试获取基础语言
            var baseLang = languageCode.Split('-')[0];
            foreach (var key in _translations.Keys)
            {
                if (key.StartsWith(baseLang, StringComparison.OrdinalIgnoreCase))
                    return TextEscaper.Unescape(_translations[key]);
            }

            // 回退到英语或第一个可用翻译
            if (_translations.TryGetValue("en-US", out var englishValue))
                return TextEscaper.Unescape(englishValue);

            foreach (var v in _translations.Values)
                return TextEscaper.Unescape(v);

            return string.Empty;
        }

        /// <summary>
        /// 获取所有翻译
        /// </summary>
        public Dictionary<string, string> GetAll()
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in _translations)
            {
                result[kvp.Key] = TextEscaper.Unescape(kvp.Value);
            }
            return result;
        }

        public override string ToString() => Get(CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>
    /// 本地化资源管理器
    /// </summary>
    public class LocalizationManager
    {
        private static LocalizationManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, LocalizedString> _resources = new(StringComparer.OrdinalIgnoreCase);
        private string _currentLanguage = "en-US";

        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LocalizationManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 当前语言
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    _currentLanguage = value;
            }
        }

        /// <summary>
        /// 支持的语言列表
        /// </summary>
        public List<string> SupportedLanguages { get; } = new() { "en-US", "zh-CN", "zh-TW", "ja-JP", "ko-KR" };

        /// <summary>
        /// 注册本地化资源
        /// </summary>
        public void Register(string key, LocalizedString localizedString)
        {
            _resources[key] = localizedString;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public string Get(string key, string? languageCode = null)
        {
            var lang = languageCode ?? _currentLanguage;
            if (_resources.TryGetValue(key, out var localizedString))
                return localizedString.Get(lang);
            return key;
        }

        /// <summary>
        /// 获取本地化文本，带格式化参数
        /// </summary>
        public string GetFormat(string key, params object[] args)
        {
            var template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// 检查是否存在指定资源
        /// </summary>
        public bool Contains(string key) => _resources.ContainsKey(key);

        /// <summary>
        /// 清除所有资源
        /// </summary>
        public void Clear() => _resources.Clear();
    }
}