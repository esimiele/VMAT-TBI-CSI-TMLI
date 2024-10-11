using System.Collections.Generic;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Models
{
    public class PlanFieldJunctionModel
    {
        public ExternalPlanSetup PlanSetup { get; set; } = null;
        public List<FieldJunctionModel> FieldJunctions { get; set; } = new List<FieldJunctionModel> { };
        public PlanFieldJunctionModel(ExternalPlanSetup p, IEnumerable<FieldJunctionModel> junctions)
        {
            PlanSetup = p;
            FieldJunctions = new List<FieldJunctionModel>(junctions);
        }
    }
}
