using AutoPlannerHelpers.Enums;

namespace AutoPlannerHelpers.Models
{
    public class StructureOperationModel
    {
        public bool IsValidOperation { get => !string.IsNullOrEmpty(StructureA) &&
                                              !string.IsNullOrEmpty(StructureB) &&
                                              !string.IsNullOrEmpty(OutputStructure) &&
                                              Operation != StructureDerivationOperation.None &&
                                              (MarginAInCM >= -5.0 && MarginAInCM <= 5.0) &&
                                              (MarginBInCM >= -5.0 && MarginBInCM <= 5.0); }
        public string StructureA { get; set; } = string.Empty;
        public double MarginAInCM { get; set; } = double.NaN;
        public StructureDerivationOperation Operation { get; set; } = StructureDerivationOperation.None;
        public string StructureB { get; set; } = string.Empty;
        public string OutputStructure { get ; set; } = string.Empty;
        public double MarginBInCM { get; set; } = double.NaN;
        public bool IsTemporary { get; set; } = false;
        public StructureOperationModel(string a, StructureDerivationOperation op, string b, string outStructure, double marginA = 0.0, double marginB = 0.0, bool isTemp = false)
        {
            StructureA = a;
            MarginAInCM = marginA;
            Operation = op;
            StructureB = b;
            MarginBInCM = marginB;
            OutputStructure = outStructure;
            IsTemporary = isTemp;
        }
    }
}
