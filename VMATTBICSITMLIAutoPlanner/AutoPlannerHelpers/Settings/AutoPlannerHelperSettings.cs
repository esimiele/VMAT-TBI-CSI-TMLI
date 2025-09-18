using AutoPlannerHelpers.Enums;
using System.Collections.Generic;

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
