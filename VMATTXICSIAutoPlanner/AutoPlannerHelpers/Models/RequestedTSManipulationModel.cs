using AutoPlannerHelpers.Enums;

namespace AutoPlannerHelpers.Models
{
    public class RequestedTSManipulationModel
    {
        public string TargetId { get; set; } = string.Empty;
        public string StructureId { get; set; } = string.Empty;
        public TSManipulationType ManipulationType { get; set; } = TSManipulationType.None;
        public double MarginInCM { get; set; } = double.NaN;

        public RequestedTSManipulationModel(string structureId, TSManipulationType manipulationType, double marginInCM)
        {
            StructureId = structureId;
            ManipulationType = manipulationType;
            MarginInCM = marginInCM;
        }

        public RequestedTSManipulationModel(string targetId, string structureId, TSManipulationType manipulationType, double marginInCM)
        {
            TargetId = targetId;
            StructureId = structureId;
            ManipulationType = manipulationType;
            MarginInCM = marginInCM;
        }
    }
}
