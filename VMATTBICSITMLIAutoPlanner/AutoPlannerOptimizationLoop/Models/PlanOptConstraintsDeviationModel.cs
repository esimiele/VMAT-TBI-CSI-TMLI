using AutoPlannerOptimizationLoop.Interfaces;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerOptimizationLoop.Models
{
    public class PlanOptConstraintsDeviationModel : IPlanQualityEvaluation
    {
        public Structure Structure { get; set; } = null;
        public double DoseDifferenceSquared { get; set; } = double.NaN;
        public double OptimizationCost { get; set; } = double.NaN;
        public double DoseConstraint { get; set; } = double.NaN;
        public int Prioirty { get; set; } = -1;

        public PlanOptConstraintsDeviationModel() { }

        public PlanOptConstraintsDeviationModel(Structure s, double constraint, double doseDiff, double cost, int prioirty)
        {
            Structure = s;
            DoseDifferenceSquared = doseDiff;
            OptimizationCost = cost;
            DoseConstraint = constraint;
            Prioirty = prioirty;
        }
    }
}
