using Phobos.Manager.Plugin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Phobos.Class.Config
{
    public class PCSysConfig
    {
        private static PCSysConfig? _instance;
        private static readonly object _lock = new();

        public static PCSysConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PCSysConfig();
                    }
                }
                return _instance;
            }
        }

        public string langCode { get; set; } = CultureInfo.CurrentCulture.IetfLanguageTag;
    }
}
