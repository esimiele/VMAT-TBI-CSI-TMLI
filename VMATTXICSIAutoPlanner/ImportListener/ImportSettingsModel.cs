using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImportListener
{
    public class ImportSettingsModel
    {
        public bool IsValid { get => !string.IsNullOrEmpty(ImportPath) && !string.IsNullOrEmpty(MRN) && !string.IsNullOrEmpty(AriaDBAET) && !string.IsNullOrEmpty(AriaDBIP) && AriaDBPort != -1 && !string.IsNullOrEmpty(LocalAET) && LocalPort != -1; }
        public string ImportPath { get; set; } = string.Empty;
        public string MRN { get; set; } = string.Empty;
        public string AriaDBAET { get; set; } = string.Empty;
        public string AriaDBIP { get; set; } = string.Empty;
        public int AriaDBPort { get; set; } = -1;
        public string LocalAET { get; set; } = string.Empty;
        public int LocalPort { get; set; } = -1;
        public double TimeoutSec { get; set; } = 30 & 60;

        public ImportSettingsModel() { }
    }
}
