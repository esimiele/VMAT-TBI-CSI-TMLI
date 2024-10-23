using System.Collections.Generic;
using System.Linq;

namespace ImportListener
{
    public static class ImportListenerHelper
    {
        /// <summary>
        /// Simple logic to parse the input string array of arguments. At least 7 arguments must be passed, the 8th is optional
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static ImportSettingsModel ParseInputArguments(List<string> args)
        {
            ImportSettingsModel model = new ImportSettingsModel();
            if (args.Count < 7) return model;
            model.ImportPath = args.ElementAt(0);
            model.MRN = args.ElementAt(1);
            model.AriaDBAET = args.ElementAt(2);
            model.AriaDBIP = args.ElementAt(3);
            model.AriaDBPort = int.Parse(args.ElementAt(4));
            model.LocalAET = args.ElementAt(5);
            model.LocalPort = int.Parse(args.ElementAt(6));
            if (args.Count() == 8) model.TimeoutSec = double.Parse(args.ElementAt(7));
            return model;
        }
    }
}
