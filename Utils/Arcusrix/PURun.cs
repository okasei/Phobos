using System;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Utils.Arcusrix
{
    public class PURun
    {
        private static readonly Lazy<PURun> _lazyInstance = new Lazy<PURun>(() => new PURun());
        private PURun() { }
        public static PURun Instance => _lazyInstance.Value;
    }
}
