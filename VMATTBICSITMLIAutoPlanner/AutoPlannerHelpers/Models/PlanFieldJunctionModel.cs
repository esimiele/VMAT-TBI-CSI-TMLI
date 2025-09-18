using System.Collections.Generic;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Models
{
    public class PlanFieldJunctionModel
    {
        public string PlanId { get; set; } = null;
        public List<FieldJunctionModel> FieldJunctions { get; set; } = new List<FieldJunctionModel> { };
        public PlanFieldJunctionModel(string pid, IEnumerable<FieldJunctionModel> junctions)
        {
            PlanId = pid;
            FieldJunctions = new List<FieldJunctionModel>(junctions);
        }
    }
}
