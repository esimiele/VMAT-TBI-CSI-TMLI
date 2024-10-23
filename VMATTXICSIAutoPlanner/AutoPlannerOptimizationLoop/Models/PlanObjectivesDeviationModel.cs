using AutoPlannerOptimizationLoop.Interfaces;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerOptimizationLoop.Models
{
    public class PlanObjectivesDeviationModel : IPlanQualityEvaluation
    {
        public Structure Structure { get; set; } = null;
        public double DoseDifferenceSquared { get; set; } = double.NaN;
        public bool ObjectiveMet { get; set; } = false;

        public PlanObjectivesDeviationModel() { }
        public PlanObjectivesDeviationModel(Structure structure, double doseDifferenceSquared, bool met)
        {
            Structure = structure;
            DoseDifferenceSquared = doseDifferenceSquared;
            ObjectiveMet = met;
        }
    }
}
