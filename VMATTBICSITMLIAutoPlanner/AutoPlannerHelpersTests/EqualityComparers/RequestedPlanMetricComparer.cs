using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;

namespace AutoPlannerHelpersTests.EqualityComparers
{
    public class RequestedPlanMetricComparer : IEqualityComparer<RequestedPlanMetricModel>
    {
        public string Print(RequestedPlanMetricModel x)
        {
            return $"{x.StructureId} {x.DVHMetric} {x.QueryValue} {x.QueryUnits} {x.QueryResultUnits}";
        }
        public bool Equals(RequestedPlanMetricModel x, RequestedPlanMetricModel y)
        {
            if (x == null && y == null) return true;
            else if (x == null || y == null) return false;
            else if (object.ReferenceEquals(x, y)) return true;

            return string.Equals(x.StructureId, y.StructureId)
                && x.DVHMetric == y.DVHMetric
                && ((double.IsNaN(x.QueryValue) && double.IsNaN(y.QueryValue)) || CalculationHelper.AreEqual(x.QueryValue, y.QueryValue))
                && x.QueryUnits == y.QueryUnits
                && x.QueryResultUnits == y.QueryResultUnits;
        }

        public int GetHashCode(RequestedPlanMetricModel obj)
        {
            throw new NotImplementedException();
        }
    }
}
