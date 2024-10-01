using System.Collections.Generic;
using AutoPlannerHelpers.Models;

namespace AutoPlannerHelpers.PlanTemplateModels
{
    public abstract class AutoPlanTemplateBase
    {
        //this is only here for the display name data binding. All other references to the template name use the explicit get method
        public string TemplateName { get; set; } = string.Empty;
        public List<PlanTargetsModel> PlanTargets { get; set; } = new List<PlanTargetsModel>();
        public List<RequestedTSStructureModel> CreateTSStructures { get; set; } = new List<RequestedTSStructureModel> { };
        public List<RequestedTSManipulationModel> TSManipulations { get; set; } = new List<RequestedTSManipulationModel> { };

        public List<PlanObjectiveModel> PlanObjectives { get; set; } = new List<PlanObjectiveModel> { };
        public List<RequestedPlanMetricModel> RequestedPlanMetrics { get; set; } = new List<RequestedPlanMetricModel> { };
        public List<RequestedOptimizationTSStructureModel> RequestedOptimizationTSStructures { get; set; } = new List<RequestedOptimizationTSStructureModel> { };
    }
}
