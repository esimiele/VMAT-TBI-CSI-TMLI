using AutoPlannerHelpers.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace AutoPlannerHelpers.Models
{
    public class StructureOperationModel : ObservableObject
    {
        public bool IsValidOperation { get => !string.IsNullOrEmpty(StructureA) &&
                                              !string.IsNullOrEmpty(StructureB) &&
                                              !string.IsNullOrEmpty(OutputStructure) &&
                                              Operation != StructureDerivationOperation.None &&
                                              MarginA.IsValidMargin &&
                                              MarginB.IsValidMargin; }

        public List<string> StructureIdList { get => new List<string> { StructureA, StructureB, OutputStructure }; }
        public string FriendlyName { get => IsValidOperation ? $"{StructureA} {Operation} {StructureB} -> {OutputStructure} (Is temp = {IsTemporary})" : "none"; }

        #region properties

        private string _structureA = string.Empty;

        public string StructureA
        {
            get { return _structureA; }
            set { SetProperty(ref _structureA, value); OnPropertyChanged(nameof(FriendlyName)); }
        }

        private StructureMarginModel _marginA = new StructureMarginModel();

        public StructureMarginModel MarginA
        {
            get { return _marginA; }
            set { SetProperty(ref _marginA, value); OnPropertyChanged(nameof(FriendlyName)); }
        }

        private StructureDerivationOperation _operation = StructureDerivationOperation.None;

        public StructureDerivationOperation Operation
        {
            get { return _operation; }
            set { SetProperty(ref _operation, value); OnPropertyChanged(nameof(FriendlyName)); }
        }

        private string _structureB = string.Empty;

        public string StructureB
        {
            get { return _structureB; }
            set { SetProperty(ref _structureB, value); OnPropertyChanged(nameof(FriendlyName)); }
        }

        private StructureMarginModel _marginB = new StructureMarginModel();

        public StructureMarginModel MarginB
        {
            get { return _marginB; }
            set { SetProperty(ref _marginB, value); OnPropertyChanged(nameof(FriendlyName)); }
        }

        private string _outputStructure = string.Empty;

        public string OutputStructure
        {
            get { return _outputStructure; }
            set { SetProperty(ref _outputStructure, value); OnPropertyChanged(nameof(FriendlyName)); }
        }
        public bool IsTemporary { get; set; } = false;
        #endregion

        public StructureOperationModel() { }

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

        public StructureOperationModel(StructureOperationModel model)
        {
            UpdateStructureOperation(model);
        }

        public void UpdateStructureIds(string newId)
        {
            if(string.Equals(newId, StructureA,System.StringComparison.OrdinalIgnoreCase)) StructureA = newId;
            if(string.Equals(newId, StructureB,System.StringComparison.OrdinalIgnoreCase)) StructureB = newId;
            if(string.Equals(newId, OutputStructure,System.StringComparison.OrdinalIgnoreCase)) OutputStructure = newId;
        }

        public void UpdateStructureOperation(StructureOperationModel model)
        {
            StructureA = model.StructureA;
            MarginA = model.MarginA;
            Operation = model.Operation;
            StructureB = model.StructureB;
            MarginB = model.MarginB;
            OutputStructure = model.OutputStructure;
            IsTemporary = model.IsTemporary;
        }
    }
}
