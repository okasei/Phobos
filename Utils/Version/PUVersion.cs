using System;
using System.Text.RegularExpressions;

namespace Phobos.Utils.Version
{
    /// <summary>
    /// Version comparison result
    /// </summary>
    public enum VersionCompareResult
    {
        /// <summary>
        /// First version is greater
        /// </summary>
        Greater = 1,

        /// <summary>
        /// Versions are equal
        /// </summary>
        Equal = 0,

        /// <summary>
        /// First version is less
        /// </summary>
        Less = -1,

        /// <summary>
        /// Versions are incompatible (different pre-release tags)
        /// </summary>
        Incompatible = -2
    }

    /// <summary>
    /// Parsed version information
    /// </summary>
    public class ParsedVersion
    {
        /// <summary>
        /// Major version number
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// Minor version number
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// Patch version number
        /// </summary>
        public int Patch { get; set; }

        /// <summary>
        /// Pre-release tag (e.g., "alpha", "beta", "rc")
        /// </summary>
        public string? PreReleaseTag { get; set; }

        /// <summary>
        /// Pre-release number (e.g., 01, 02)
        /// </summary>
        public int PreReleaseNumber { get; set; }

        /// <summary>
        /// Whether this is a pre-release version
        /// </summary>
        public bool IsPreRelease => !string.IsNullOrEmpty(PreReleaseTag);

        /// <summary>
        /// Original version string
        /// </summary>
        public string OriginalString { get; set; } = string.Empty;
    }

    /// <summary>
    /// Version comparison utility
    ///
    /// Version format: Major.Minor.Patch[-PreReleaseTag PreReleaseNumber]
    ///
    /// Comparison rules:
    /// - 1.0.0-alpha02 > 1.0.0-alpha01 > 1.0.0 > 0.9.9-beta99 > 0.9.9
    /// - If pre-release tags are different, versions are considered incompatible
    /// - Pre-release versions (e.g., 1.0.0-alpha01) are greater than release versions with same base (1.0.0)
    /// </summary>
    public static class PUVersion
    {
        // Regex pattern: Major.Minor.Patch[-TagNumber]
        // Examples: 1.0.0, 1.0.0-alpha01, 1.0.0-beta02, 1.0.0-rc1
        private static readonly Regex VersionPattern = new(
            @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z]+)(\d+))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// Parse a version string into its components
        /// </summary>
        /// <param name="version">Version string to parse</param>
        /// <returns>Parsed version or null if invalid</returns>
        public static ParsedVersion? Parse(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return null;

            var match = VersionPattern.Match(version.Trim());
            if (!match.Success)
                return null;

            var result = new ParsedVersion
            {
                OriginalString = version.Trim(),
                Major = int.Parse(match.Groups[1].Value),
                Minor = int.Parse(match.Groups[2].Value),
                Patch = int.Parse(match.Groups[3].Value)
            };

            if (match.Groups[4].Success)
            {
                result.PreReleaseTag = match.Groups[4].Value.ToLowerInvariant();
                result.PreReleaseNumber = match.Groups[5].Success
                    ? int.Parse(match.Groups[5].Value)
                    : 0;
            }

            return result;
        }

        /// <summary>
        /// Check if a version string is valid
        /// </summary>
        public static bool IsValid(string? version)
        {
            return Parse(version) != null;
        }

        /// <summary>
        /// Compare two version strings
        /// </summary>
        /// <param name="version1">First version</param>
        /// <param name="version2">Second version</param>
        /// <returns>
        /// Greater (1): version1 > version2
        /// Equal (0): version1 == version2
        /// Less (-1): version1 < version2
        /// Incompatible (-2): Different pre-release tags
        /// </returns>
        public static VersionCompareResult Compare(string? version1, string? version2)
        {
            var v1 = Parse(version1);
            var v2 = Parse(version2);

            // Handle null/invalid versions
            if (v1 == null && v2 == null) return VersionCompareResult.Equal;
            if (v1 == null) return VersionCompareResult.Less;
            if (v2 == null) return VersionCompareResult.Greater;

            return Compare(v1, v2);
        }

        /// <summary>
        /// Compare two parsed versions
        /// </summary>
        public static VersionCompareResult Compare(ParsedVersion v1, ParsedVersion v2)
        {
            // Compare base version (Major.Minor.Patch)
            var baseComparison = CompareBase(v1, v2);
            if (baseComparison != 0)
            {
                return baseComparison > 0 ? VersionCompareResult.Greater : VersionCompareResult.Less;
            }

            // Base versions are equal, compare pre-release info
            // Case 1: Both are release versions (no pre-release tag)
            if (!v1.IsPreRelease && !v2.IsPreRelease)
            {
                return VersionCompareResult.Equal;
            }

            // Case 2: v1 is pre-release, v2 is release
            // Pre-release (1.0.0-alpha01) > Release (1.0.0)
            if (v1.IsPreRelease && !v2.IsPreRelease)
            {
                return VersionCompareResult.Greater;
            }

            // Case 3: v1 is release, v2 is pre-release
            if (!v1.IsPreRelease && v2.IsPreRelease)
            {
                return VersionCompareResult.Less;
            }

            // Case 4: Both are pre-release
            // If tags are different, versions are incompatible
            if (!string.Equals(v1.PreReleaseTag, v2.PreReleaseTag, StringComparison.OrdinalIgnoreCase))
            {
                return VersionCompareResult.Incompatible;
            }

            // Same tag, compare pre-release numbers
            if (v1.PreReleaseNumber > v2.PreReleaseNumber)
                return VersionCompareResult.Greater;
            if (v1.PreReleaseNumber < v2.PreReleaseNumber)
                return VersionCompareResult.Less;

            return VersionCompareResult.Equal;
        }

        /// <summary>
        /// Compare base version numbers (Major.Minor.Patch)
        /// </summary>
        private static int CompareBase(ParsedVersion v1, ParsedVersion v2)
        {
            if (v1.Major != v2.Major)
                return v1.Major.CompareTo(v2.Major);

            if (v1.Minor != v2.Minor)
                return v1.Minor.CompareTo(v2.Minor);

            return v1.Patch.CompareTo(v2.Patch);
        }

        /// <summary>
        /// Check if version1 is greater than version2
        /// </summary>
        public static bool IsGreater(string? version1, string? version2)
        {
            return Compare(version1, version2) == VersionCompareResult.Greater;
        }

        /// <summary>
        /// Check if version1 is greater than or equal to version2
        /// </summary>
        public static bool IsGreaterOrEqual(string? version1, string? version2)
        {
            var result = Compare(version1, version2);
            return result == VersionCompareResult.Greater || result == VersionCompareResult.Equal;
        }

        /// <summary>
        /// Check if version1 is less than version2
        /// </summary>
        public static bool IsLess(string? version1, string? version2)
        {
            return Compare(version1, version2) == VersionCompareResult.Less;
        }

        /// <summary>
        /// Check if two versions are compatible for upgrade
        /// Returns false if pre-release tags are different
        /// </summary>
        public static bool IsCompatible(string? version1, string? version2)
        {
            return Compare(version1, version2) != VersionCompareResult.Incompatible;
        }

        /// <summary>
        /// Check if installing newVersion over existingVersion is allowed
        /// </summary>
        /// <param name="newVersion">The version to be installed</param>
        /// <param name="existingVersion">The currently installed version</param>
        /// <returns>True if installation is allowed, false otherwise</returns>
        public static bool CanInstallOver(string? newVersion, string? existingVersion)
        {
            var result = Compare(newVersion, existingVersion);

            // Allow if new version is greater or equal
            if (result == VersionCompareResult.Greater || result == VersionCompareResult.Equal)
                return true;

            // Don't allow if incompatible (different pre-release tags) or less
            return false;
        }

        /// <summary>
        /// Get a human-readable comparison description
        /// </summary>
        public static string GetComparisonDescription(string? version1, string? version2, string langCode = "en-US")
        {
            var result = Compare(version1, version2);

            return (result, langCode) switch
            {
                (VersionCompareResult.Greater, "zh-CN") => $"{version1} 高于 {version2}",
                (VersionCompareResult.Greater, "zh-TW") => $"{version1} 高於 {version2}",
                (VersionCompareResult.Greater, "ja-JP") => $"{version1} は {version2} より高い",
                (VersionCompareResult.Greater, _) => $"{version1} is greater than {version2}",

                (VersionCompareResult.Equal, "zh-CN") => $"{version1} 等于 {version2}",
                (VersionCompareResult.Equal, "zh-TW") => $"{version1} 等於 {version2}",
                (VersionCompareResult.Equal, "ja-JP") => $"{version1} は {version2} と同じ",
                (VersionCompareResult.Equal, _) => $"{version1} equals {version2}",

                (VersionCompareResult.Less, "zh-CN") => $"{version1} 低于 {version2}",
                (VersionCompareResult.Less, "zh-TW") => $"{version1} 低於 {version2}",
                (VersionCompareResult.Less, "ja-JP") => $"{version1} は {version2} より低い",
                (VersionCompareResult.Less, _) => $"{version1} is less than {version2}",

                (VersionCompareResult.Incompatible, "zh-CN") => $"{version1} 与 {version2} 不兼容（预发布标签不同）",
                (VersionCompareResult.Incompatible, "zh-TW") => $"{version1} 與 {version2} 不相容（預發布標籤不同）",
                (VersionCompareResult.Incompatible, "ja-JP") => $"{version1} は {version2} と互換性がありません（プレリリースタグが異なります）",
                (VersionCompareResult.Incompatible, _) => $"{version1} is incompatible with {version2} (different pre-release tags)",

                _ => $"Cannot compare {version1} and {version2}"
            };
        }
    }
}
