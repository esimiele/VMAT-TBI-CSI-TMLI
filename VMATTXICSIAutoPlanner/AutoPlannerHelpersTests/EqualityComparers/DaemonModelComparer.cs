using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;

namespace AutoPlannerHelpersTests.EqualityComparers
{
    public class DaemonModelComparer : IEqualityComparer<DaemonModel>
    {
        public bool Equals(DaemonModel x, DaemonModel y)
        {
            if (x == null && y == null) return true;
            else if (x == null || y == null) return false;
            else if (object.ReferenceEquals(x, y)) return true;

            return string.Equals(x.AETitle, y.AETitle)
                && string.Equals(x.IP, y.IP)
                && x.Port == y.Port;
        }

        public int GetHashCode(DaemonModel obj)
        {
            throw new NotImplementedException();
        }
    }
}
