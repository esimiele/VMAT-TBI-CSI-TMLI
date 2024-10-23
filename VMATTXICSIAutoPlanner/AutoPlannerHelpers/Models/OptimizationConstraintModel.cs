using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Interfaces;

namespace AutoPlannerHelpers.Models
{
    public class OptimizationConstraintModel : IPlanConstraint
    {
        public bool IsValidConstraint { get => !string.IsNullOrEmpty(StructureId) && ConstraintType != OptimizationObjectiveType.None && !double.IsNaN(QueryDose) && QueryDoseUnits == Units.cGy && !double.IsNaN(QueryVolume) && QueryVolumeUnits != Units.None && Priority > 0; }
        public string StructureId { get; set; } = string.Empty;
        public OptimizationObjectiveType ConstraintType { get; set; } = OptimizationObjectiveType.None;
        public double QueryDose { get; set; } = double.NaN;
        public Units QueryDoseUnits { get; set; } = Units.None;
        public double QueryVolume { get; set; } = double.NaN;
        public Units QueryVolumeUnits { get; set; } = Units.None;
        public int Priority { get; set; } = -1;

        public OptimizationConstraintModel(string structureId, OptimizationObjectiveType constraintType, double queryDose, Units queryDoseUnits, double queryVolume, int priority, Units queryVolumeUnits = Units.Percent)
        {
            StructureId = structureId;
            ConstraintType = constraintType;
            QueryDose = queryDose;
            QueryDoseUnits = queryDoseUnits;
            QueryVolume = queryVolume;
            Priority = priority;
            QueryVolumeUnits = queryVolumeUnits;
        }

        public OptimizationConstraintModel(OptimizationConstraintModel model)
        {
            StructureId = model.StructureId;
            ConstraintType = model.ConstraintType;
            QueryDose = model.QueryDose;
            QueryDoseUnits = model.QueryDoseUnits;
            QueryVolume = model.QueryVolume;
            Priority = model.Priority;
            QueryVolumeUnits = model.QueryVolumeUnits;
        }
    }
}
