namespace AutoPlannerHelpers.Models
{
    public class SpecialOptimizationStructureModel
    {
        public string DICOMType { get; set; } = string.Empty;
        public string StructureId { get; set; } = string.Empty;

        public SpecialOptimizationStructureModel(string dICOMType, string structureId)
        {
            DICOMType = dICOMType;
            StructureId = structureId;
        }
    }
}
