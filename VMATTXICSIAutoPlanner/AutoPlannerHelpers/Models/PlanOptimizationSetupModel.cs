using System.Collections.Generic;

namespace AutoPlannerHelpers.Models
{
    public class PlanOptimizationSetupModel
    {
        public string PlanId { get; set; } = string.Empty;
        public List<OptimizationConstraintModel> OptimizationConstraints { get; set; } = new List<OptimizationConstraintModel>();
        public PlanOptimizationSetupModel() { }
        public PlanOptimizationSetupModel(string id, IEnumerable<OptimizationConstraintModel> constraints)
        {
            PlanId = id;
            OptimizationConstraints = new List<OptimizationConstraintModel>(constraints);
        }
    }
}
