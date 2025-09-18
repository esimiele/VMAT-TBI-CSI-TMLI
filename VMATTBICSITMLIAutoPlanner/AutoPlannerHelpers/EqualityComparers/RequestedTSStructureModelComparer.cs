using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.EqualityComparers
{
    public class RequestedTSStructureModelComparer : IEqualityComparer<SpecialOptimizationStructureModel>
    {
        public bool Equals(SpecialOptimizationStructureModel x, SpecialOptimizationStructureModel y)
        {
            if (x == null && y == null) return true;
            else if (x == null || y == null) return false;
            else if (object.ReferenceEquals(x, y)) return true;
            return string.Equals(x.StructureId, y.StructureId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(SpecialOptimizationStructureModel obj)
        {
            return obj.GetHashCode();
        }
    }
}
