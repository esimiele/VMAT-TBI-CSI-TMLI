using System.Linq;

namespace AutoPlannerHelpers.Models
{
    public class UnstructuredTargetModel
    {
        public string PlanId { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public double TargetRxDose { get; set; } = double.NaN;

        public UnstructuredTargetModel(string planId, string target, double dose)
        {
            PlanId = planId;
            TargetId = target;
            TargetRxDose = dose;
        }

        public UnstructuredTargetModel(PlanTargetsModel model)
        {
            PlanId = model.PlanId;
            TargetId = model.Targets.First().TargetId;
            TargetRxDose = model.Targets.First().TargetRxDose;
        }
    }
}
