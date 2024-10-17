using System;
using System.Collections.Generic;
using System.Linq;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Commands;
using Prism.Mvvm;

namespace AutoPlannerHelpers.ViewModels
{
    public class StructureCropOverlapViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<string> CropOverlapStructures { get; set; }
        #region properties
        private AutoPlanTemplateBase _selectedTemplate;
        private List<string> _structureIdsPostUnion;

        public List<string> StructureIdsPostUnion
        {
            get { return _structureIdsPostUnion; }
            set { SetProperty(ref _structureIdsPostUnion, value); }
        }

        #endregion

        #region commands
        public DelegateCommand AddCropOverlapStructureCommand { get; set; }
        public DelegateCommand AddDefaultCropOverlapStructuresCommand { get; set; }
        public DelegateCommand ClearCropOverlapStructureListCommand { get; set; }
        public DelegateCommand<string> RemoveCropOverlapStructureCommand { get; set; }
        #endregion

        public StructureCropOverlapViewModel(List<string> structures)
        {
            StructureIdsPostUnion = new List<string>(structures);
            CropOverlapStructures = new ObservableCollectionPropertyNotify<string> { };
            AddCropOverlapStructureCommand = new DelegateCommand(AddCropOverlapStructure);
            AddDefaultCropOverlapStructuresCommand = new DelegateCommand(AddDefaultCropOverlapStructures);
            ClearCropOverlapStructureListCommand = new DelegateCommand(ClearCropOverlapStructureList);
            RemoveCropOverlapStructureCommand = new DelegateCommand<string>(RemoveCropOverlapStructure);
        }

        public void AddCropOverlapStructure()
        {
            CropOverlapStructures.Add(_structureIdsPostUnion.FirstOrDefault());
        }

        public void AutoPlanTemplateSelectionChanged(AutoPlanTemplateBase template, bool skipStructureCheck = false)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            UpdateViewWithAutoPlanTemplateCropOverlapStructures(skipStructureCheck);
        }

        public void UpdateViewWithAutoPlanTemplateCropOverlapStructures(bool skipStructureCheck)
        {
            CropOverlapStructures.Clear();
            List<string> cropOverlap;
            if (_selectedTemplate.GetType() == typeof(CSIAutoPlanTemplate))
            {
                cropOverlap = (_selectedTemplate as CSIAutoPlanTemplate).CropAndOverlapStructures;
            }
            else return;
            foreach (string itr in cropOverlap)
            {
                if (skipStructureCheck)
                {
                    if(!StructureIdsPostUnion.Any(x => string.Equals(x, itr, StringComparison.OrdinalIgnoreCase))) StructureIdsPostUnion.Add(itr);
                    CropOverlapStructures.Add(itr);
                }
                else if (StructureIdsPostUnion.Any(x => string.Equals(x, itr, StringComparison.OrdinalIgnoreCase)))
                {
                    CropOverlapStructures.Add(itr);
                }
            }
        }

        public void AddDefaultCropOverlapStructures()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            UpdateViewWithAutoPlanTemplateCropOverlapStructures(false);
        }

        public void ClearCropOverlapStructureList()
        {
            CropOverlapStructures.Clear();
        }

        public void RemoveCropOverlapStructure(string item)
        {
            if (CropOverlapStructures.Contains(item)) CropOverlapStructures.Remove(item);
        }
    }
}
