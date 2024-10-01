using System.Collections.Generic;

namespace AutoPlannerHelpers.Models
{
    public class PlanTargetsModel
    {
        public string PlanId { get; set; } = string.Empty;
        public List<TargetModel> Targets { get; set; } = new List<TargetModel>();

        public PlanTargetsModel(string plan, IEnumerable<TargetModel> tgts) 
        {
            PlanId = plan;
            Targets = new List<TargetModel>(tgts);
        }

        public PlanTargetsModel(string plan, TargetModel tgts)
        {
            PlanId = plan;
            Targets.Add(tgts);
        }

        public PlanTargetsModel(string plan, double tgtRx, string tgtId)
        {
            PlanId = plan;
            Targets = new List<TargetModel>
            {
                new TargetModel(tgtId, tgtRx)
            };
        }
    }
}
