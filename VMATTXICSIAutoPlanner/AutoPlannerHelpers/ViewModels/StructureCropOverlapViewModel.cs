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
        public ObservableCollectionPropertyNotify<string> StructureIdsPostUnion { get; set; }
        
        #region properties
        private AutoPlanTemplateBase _selectedTemplate;

        #endregion

        #region fields
        private bool _skipStructureIdCheck = false;
        #endregion

        #region commands
        public ICommand AddCropOverlapStructureCommand { get; set; }
        public ICommand AddDefaultCropOverlapStructuresCommand { get; set; }
        public ICommand ClearCropOverlapStructureListCommand { get; set; }
        public RelayCommand<string> RemoveCropOverlapStructureCommand { get; set; }
        #endregion

        public StructureCropOverlapViewModel()
        {
            StructureIdsPostUnion = new ObservableCollectionPropertyNotify<string> { };
            CropOverlapStructures = new ObservableCollectionPropertyNotify<string> { };
            AddCropOverlapStructureCommand = new RelayCommand(AddCropOverlapStructure);
            AddDefaultCropOverlapStructuresCommand = new RelayCommand(AddDefaultCropOverlapStructures);
            ClearCropOverlapStructureListCommand = new RelayCommand(ClearCropOverlapStructureList);
            RemoveCropOverlapStructureCommand = new RelayCommand<string>(RemoveCropOverlapStructure);
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestAutoPlanTemplateChangedMessage>(this, (r, m) =>
            {
                AutoPlanTemplateSelectionChanged(m.AutoPlanTemplate, m.SkipStructureIdCheck);
            });
            WeakReferenceMessenger.Default.Register<RequestCropOverlapStructures>(this, (r, m) =>
            {
                m.Reply(this.CropOverlapStructures.ToList());
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                StructureIdsPostUnion.Clear();
                StructureIdsPostUnion.AddRange(m.StructureIds);
            });
        }

        public void AddCropOverlapStructure()
        {
            CropOverlapStructures.Add(StructureIdsPostUnion.FirstOrDefault());
        }

        public void AutoPlanTemplateSelectionChanged(AutoPlanTemplateBase template, bool skipStructureCheck)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            _skipStructureIdCheck = skipStructureCheck;
            UpdateViewWithAutoPlanTemplateCropOverlapStructures();
        }

        public void UpdateViewWithAutoPlanTemplateCropOverlapStructures()
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
                if (_skipStructureIdCheck)
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
            UpdateViewWithAutoPlanTemplateCropOverlapStructures();
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
