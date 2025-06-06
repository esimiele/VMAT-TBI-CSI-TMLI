using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.EqualityComparers
{
    public class RequestedTSStructureModelComparer : IEqualityComparer<RequestedTSStructureModel>
    {
        public bool Equals(RequestedTSStructureModel x, RequestedTSStructureModel y)
        {
            if (x == null && y == null) return true;
            else if (x == null || y == null) return false;
            else if (object.ReferenceEquals(x, y)) return true;
            return string.Equals(x.StructureId, y.StructureId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(RequestedTSStructureModel obj)
        {
            return obj.GetHashCode();
        }
    }
}
