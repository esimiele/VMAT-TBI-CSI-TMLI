using AutoPlannerOptimizationLoop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoPlannerHelpers.Helpers;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoopTests.EqualityComparers
{
    internal class JawPositionComparer : IEqualityComparer<VRect<double>>
    {
        public string Print(VRect<double> x)
        {
            return $"{x.X1} {x.Y1} {x.X2} {x.Y2}";
        }

        public bool Equals(VRect<double> x, VRect<double> y)
        {
            return CalculationHelper.AreEqual(x.X1, y.X1) &&
                    CalculationHelper.AreEqual(x.Y1, y.Y1) &&
                    CalculationHelper.AreEqual(x.X2, y.X2) &&
                    CalculationHelper.AreEqual(x.Y2, y.Y2);
        }

        public int GetHashCode(VRect<double> obj)
        {
            throw new NotImplementedException();
        }
    }
}
