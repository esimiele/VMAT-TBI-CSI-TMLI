using System.Collections.Generic;
using System.Linq;
using AutoPlannerHelpers.Models;

namespace AutoPlannerHelpers.PlanTemplateModels
{
    public abstract class AutoPlanTemplateBase
    {
        //this is only here for the display name data binding. All other references to the template name use the explicit get method
        public string TemplateName { get; set; } = string.Empty;
        public double InitialRxDosePerFx { get; set; } = 0.1;
        public int InitialRxNumberOfFractions { get; set; } = 1;
        public List<PlanTargetsModel> PlanTargets { get; set; } = new List<PlanTargetsModel>();
        public List<SpecialOptimizationStructureModel> SpecialOptimizationStructures { get; set; } = new List<SpecialOptimizationStructureModel> { };
        public List<StructureOperationModel> TargetDerivationOperations { get; set; } = new List<StructureOperationModel> { };
        public List<StructureOperationModel> OptimizationStructureDerivations { get; set; } = new List<StructureOperationModel> { };
        public List<OptimizationConstraintModel> InitialOptimizationConstraints { get; set; } = new List<OptimizationConstraintModel> { };
        public List<PlanObjectiveModel> PlanObjectives { get; set; } = new List<PlanObjectiveModel> { };
        public List<RequestedPlanMetricModel> RequestedPlanMetrics { get; set; } = new List<RequestedPlanMetricModel> { };
        public List<RequestedOptimizationTSStructureModel> RequestedOptimizationTSStructures { get; set; } = new List<RequestedOptimizationTSStructureModel> { };

        public List<string> GenerateStructureIdList()
        {
            List<string> ids = PlanTargets.SelectMany(x => x.Targets).Select(x => x.TargetId).ToList();
            ids.AddRange(TargetDerivationOperations.SelectMany(x => x.StructureIdList));
            ids.AddRange(OptimizationStructureDerivations.SelectMany(x => x.StructureIdList));
            ids.AddRange(PlanObjectives.Select(x => x.StructureId));
            return ids.Distinct().ToList();
        }
    }
}
