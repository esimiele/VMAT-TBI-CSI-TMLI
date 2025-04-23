using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AutoPlannerHelpers.ViewModels
{
    public class StructureCropOverlapViewModel : ObservableObject
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
        public ICommand AddCropOverlapStructureCommand { get; set; }
        public ICommand AddDefaultCropOverlapStructuresCommand { get; set; }
        public ICommand ClearCropOverlapStructureListCommand { get; set; }
        public RelayCommand<string> RemoveCropOverlapStructureCommand { get; set; }
        #endregion

        public StructureCropOverlapViewModel(List<string> structures)
        {
            StructureIdsPostUnion = new List<string>(structures);
            CropOverlapStructures = new ObservableCollectionPropertyNotify<string> { };
            AddCropOverlapStructureCommand = new RelayCommand(AddCropOverlapStructure);
            AddDefaultCropOverlapStructuresCommand = new RelayCommand(AddDefaultCropOverlapStructures);
            ClearCropOverlapStructureListCommand = new RelayCommand(ClearCropOverlapStructureList);
            RemoveCropOverlapStructureCommand = new RelayCommand<string>(RemoveCropOverlapStructure);
            WeakReferenceMessenger.Default.Register<RequestAutoPlanTemplateChangedMessage>(this, (r, m) =>
            {
                AutoPlanTemplateSelectionChanged(m.AutoPlanTemplate);
            });
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
