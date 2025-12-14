using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Utils.Arcusrix
{
    public class PULocalization
    {
        private static readonly Lazy<PULocalization> _lazyInstance = new Lazy<PULocalization>(() => new PULocalization());
        private PULocalization() { }
        public static PULocalization Instance => _lazyInstance.Value;

        internal async Task SetLocalizationForPlugins(PluginCallerContext callerContext, string langCode = "system", string? directory = null, bool isSystem = false)
        {
            await SetLocalizationForPlugins(callerContext.PackageName, langCode, directory, isSystem);
        }

        internal async Task SetLocalizationForPlugins(string packageName, string langCode = "system", string? directory = null, bool isSystem = false)
        {
            LocalizationManager.Instance.RegisterPlugin(packageName, directory ?? await PMPlugin.Instance.GetPluginFolder(packageName), langCode);
            LocalizationManager.Instance.SetPluginLanguage(packageName, langCode);
        }
    }
}
