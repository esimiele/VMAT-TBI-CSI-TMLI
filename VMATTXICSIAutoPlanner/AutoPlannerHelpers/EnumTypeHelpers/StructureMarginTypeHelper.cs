using AutoPlannerHelpers.Enums;
using System;

namespace AutoPlannerHelpers.EnumTypeHelpers
{
    public static class StructureMarginTypeHelper
    {
        public static StructureMarginType GetStructureMarginType(string type)
        {
            if (string.Equals(type, "asymmetric", StringComparison.OrdinalIgnoreCase)) return StructureMarginType.Asymmetric;
            else return StructureMarginType.Uniform;
        }

        public static MarginGeometryType GetStructureMarginGeometryType(string type)
        {
            if (string.Equals(type, "inner", StringComparison.OrdinalIgnoreCase)) return MarginGeometryType.Inner;
            else return MarginGeometryType.Outer;
        }
    }
}
