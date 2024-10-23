using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Interfaces;

namespace AutoPlannerHelpers.Models
{ 
    public class PlanObjectiveModel : IPlanConstraint
    {
        public bool IsValidObjective { get => !string.IsNullOrEmpty(StructureId) && ConstraintType != OptimizationObjectiveType.None && !double.IsNaN(QueryDose) && QueryDoseUnits != Units.None && !double.IsNaN(QueryVolume) && QueryVolumeUnits != Units.None; }
        public string StructureId { get; set; } = string.Empty;
        public OptimizationObjectiveType ConstraintType { get; set; } = OptimizationObjectiveType.None;
        public double QueryDose { get; set; } = double.NaN;
        public Units QueryDoseUnits { get; set; } = Units.None;
        public double QueryVolume { get; set; } = double.NaN;
        public Units QueryVolumeUnits { get; set; } = Units.None;

        public PlanObjectiveModel(string structureId, OptimizationObjectiveType constraintType, double queryDose, Units queryDoseUnits, double queryVolume, Units queryVolumeUnits = Units.Percent)
        {
            StructureId = structureId;
            ConstraintType = constraintType;
            QueryDose = queryDose;
            QueryDoseUnits = queryDoseUnits;
            QueryVolume = queryVolume;
            QueryVolumeUnits = queryVolumeUnits;
        }

        public PlanObjectiveModel(PlanObjectiveModel model)
        {
            StructureId = model.StructureId;
            ConstraintType = model.ConstraintType;
            QueryDose = model.QueryDose;
            QueryDoseUnits = model.QueryDoseUnits;
            QueryVolume = model.QueryVolume;
            QueryVolumeUnits = model.QueryVolumeUnits;
        }
    }
}
