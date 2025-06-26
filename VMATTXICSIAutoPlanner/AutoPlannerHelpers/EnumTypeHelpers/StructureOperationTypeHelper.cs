using AutoPlannerHelpers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.EnumTypeHelpers
{
    public static class StructureOperationTypeHelper
    {
        public static StructureDerivationOperation GetStructureDerivationType(string operation)
        {
            operation= operation.Trim();
            if (string.Equals(operation, "intersection", StringComparison.OrdinalIgnoreCase)) return StructureDerivationOperation.Intersection;
            else if (string.Equals(operation, "union", StringComparison.OrdinalIgnoreCase)) return StructureDerivationOperation.Union;
            else if (string.Equals(operation, "crop", StringComparison.OrdinalIgnoreCase)) return StructureDerivationOperation.Crop;
            else if (string.Equals(operation, "xor", StringComparison.OrdinalIgnoreCase)) return StructureDerivationOperation.XOR;
            else if (string.Equals(operation, "cutinferiorto", StringComparison.OrdinalIgnoreCase)) return StructureDerivationOperation.CutInferiorTo;
            else if (string.Equals(operation, "cutsuperiorto", StringComparison.OrdinalIgnoreCase)) return StructureDerivationOperation.CutSuperiorTo;
            else return StructureDerivationOperation.None;
        }
    }
}
