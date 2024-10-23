using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;

namespace AutoPlannerHelpersTests.EqualityComparers
{
    public class TSManipulationComparer : IEqualityComparer<RequestedTSManipulationModel>
    {
        public string Print(RequestedTSManipulationModel x)
        {
            return $"{x.StructureId} {x.ManipulationType} {x.MarginInCM}";
        }
        public bool Equals(RequestedTSManipulationModel x, RequestedTSManipulationModel y)
        {
            if (x == null && y == null) return true;
            else if (x == null || y == null) return false;
            else if (object.ReferenceEquals(x, y)) return true;

            return string.Equals(x.StructureId, y.StructureId)
                && x.ManipulationType == y.ManipulationType
                && CalculationHelper.AreEqual(x.MarginInCM, y.MarginInCM);
        }

        public int GetHashCode(RequestedTSManipulationModel obj)
        {
            throw new NotImplementedException();
        }
    }
}
