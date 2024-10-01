using AutoPlannerHelpers.Enums;

namespace AutoPlannerHelpers.Interfaces
{
    public interface IPlanConstraint
    {
        string StructureId { get; set; }
        OptimizationObjectiveType ConstraintType { get; set; }
        double QueryDose { get; set; }
        Units QueryDoseUnits { get; set; }
        double QueryVolume { get; set; }
        Units QueryVolumeUnits { get; set; }
    }
}
