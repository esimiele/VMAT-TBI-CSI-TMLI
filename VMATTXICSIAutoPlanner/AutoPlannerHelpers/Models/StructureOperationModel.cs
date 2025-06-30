using AutoPlannerHelpers.Enums;
using System.Collections.Generic;

namespace AutoPlannerHelpers.Models
{
    public class StructureOperationModel
    {
        public bool IsValidOperation { get => !string.IsNullOrEmpty(StructureA) &&
                                              !string.IsNullOrEmpty(StructureB) &&
                                              !string.IsNullOrEmpty(OutputStructure) &&
                                              Operation != StructureDerivationOperation.None &&
                                              MarginA.IsValidMargin &&
                                              MarginB.IsValidMargin; }

        public List<string> StructureIdList { get => new List<string> { StructureA, StructureB, OutputStructure }; }
        public string FriendlyName { get => $"{StructureA} ({MarginA.AxisAlignedMargins.ToString()}) {Operation} {StructureB} ({MarginB.AxisAlignedMargins.ToString()}) -> {OutputStructure} (Is temp = {IsTemporary})"; }

        #region properties
        public string StructureA { get; set; } = string.Empty;
        public StructureMarginModel MarginA { get; set; } = new StructureMarginModel();
        public StructureDerivationOperation Operation { get; set; } = StructureDerivationOperation.None;
        public string StructureB { get; set; } = string.Empty;
        public string OutputStructure { get ; set; } = string.Empty;
        public StructureMarginModel MarginB { get; set; } = new StructureMarginModel();
        public bool IsTemporary { get; set; } = false;
        #endregion

        public StructureOperationModel(string a, StructureDerivationOperation op, string b, string outStructure, StructureMarginModel marginA, StructureMarginModel marginB, bool isTemp = false)
        {
            StructureA = a;
            MarginA = marginA;
            Operation = op;
            StructureB = b;
            MarginB = marginB;
            OutputStructure = outStructure;
            IsTemporary = isTemp;
        }

        public StructureOperationModel(string a, StructureDerivationOperation op, string b, string outStructure, bool isTemp = false)
        {
            StructureA = a;
            MarginA = new StructureMarginModel(0.0);
            Operation = op;
            StructureB = b;
            MarginB = new StructureMarginModel(0.0);
            OutputStructure = outStructure;
            IsTemporary = isTemp;
        }

        public void UpdateStructureIds(string newId)
        {
            if(string.Equals(newId, StructureA,System.StringComparison.OrdinalIgnoreCase)) StructureA = newId;
            if(string.Equals(newId, StructureB,System.StringComparison.OrdinalIgnoreCase)) StructureB = newId;
            if(string.Equals(newId, OutputStructure,System.StringComparison.OrdinalIgnoreCase)) OutputStructure = newId;
        }
    }
}
