using AutoPlannerHelpers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.Settings
{
    public class AutoPlannerHelperSettings
    {
        public static Dictionary<string, EclipseDecodeKey> ContextKeyDictionary = new Dictionary<string, EclipseDecodeKey>
        {
            { "-m", EclipseDecodeKey.Patient },
            { "-s", EclipseDecodeKey.StructureSet },
            { "-i", EclipseDecodeKey.Image },
            { "-p", EclipseDecodeKey.Plan },
            { "-c", EclipseDecodeKey.Course }
        };
    }
}
