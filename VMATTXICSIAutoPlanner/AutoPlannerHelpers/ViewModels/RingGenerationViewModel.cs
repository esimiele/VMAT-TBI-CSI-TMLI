using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class RingGenerationViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<TSRingStructureModel> RequestedRingStructures { get; set; }

        #region properties
        private List<string> _originalStructureIdList;
        private List<string> _structureIdsPostUnion;

        public List<string> StructureIdsPostUnion
        {
            get { return _structureIdsPostUnion; }
            set { SetProperty(ref _structureIdsPostUnion, value); }
        }
        #endregion

        #region commands
        public ICommand AddRingCommand { get; set; }
        public ICommand AddDefaultRingsCommand { get; set; }
        public ICommand ClearRingListCommand { get; set; }
        public RelayCommand<TSRingStructureModel> ClearRowCommand { get; set; }
        #endregion

        #region fields
        private AutoPlanTemplateBase _selectedTemplate;
        #endregion

        public RingGenerationViewModel(List<string> ids) 
        {
            _originalStructureIdList = new List<string>(ids);
            StructureIdsPostUnion = new List<string>(ids);
            RequestedRingStructures = new ObservableCollectionPropertyNotify<TSRingStructureModel> { };
            AddRingCommand = new RelayCommand(AddRing);
            AddDefaultRingsCommand = new RelayCommand(AddDefaultRings);
            ClearRingListCommand = new RelayCommand(ClearRingList);
            ClearRowCommand = new RelayCommand<TSRingStructureModel>(ClearRow);
        }

        public void AutoPlanTemplateSelectionChanged(AutoPlanTemplateBase template, bool skipStructureCheck = false)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            StructureIdsPostUnion.RemoveAll(x => !_originalStructureIdList.Contains(x));
            UpdateViewWithAutoPlanTemplateRings(skipStructureCheck);
        }

        private void UpdateViewWithAutoPlanTemplateRings(bool skipStructureIdCheck)
        {
            RequestedRingStructures.Clear();
            List<TSRingStructureModel> templateRings;
            if (_selectedTemplate.GetType() == typeof(CSIAutoPlanTemplate)) templateRings = (_selectedTemplate as CSIAutoPlanTemplate).Rings;
            else if (_selectedTemplate.GetType() == typeof(TMLIAutoPlanTemplate)) templateRings = (_selectedTemplate as TMLIAutoPlanTemplate).Rings;
            else return;
            foreach (TSRingStructureModel itr in templateRings)
            {
                if (skipStructureIdCheck)
                {
                    if (!StructureIdsPostUnion.Any(x => string.Equals(x, itr.TargetId, StringComparison.OrdinalIgnoreCase))) StructureIdsPostUnion.Add(itr.TargetId);
                    RequestedRingStructures.Add(itr);
                }
                else if (StructureIdsPostUnion.Any(x => string.Equals(x, itr.TargetId, StringComparison.OrdinalIgnoreCase)))
                {
                    //only add it they base structure exists in the structure set
                    RequestedRingStructures.Add(itr);
                }
            }
        }

        public void AddRing()
        {
            RequestedRingStructures.Add(new TSRingStructureModel(_structureIdsPostUnion.First(), 0.0, 0.0, 0.0));
        }

        public void AddDefaultRings()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            UpdateViewWithAutoPlanTemplateRings(false);
        }

        public void ClearRingList()
        {
            RequestedRingStructures.Clear();
        }

        public void ClearRow(TSRingStructureModel item)
        {
            RequestedRingStructures.Remove(item);
        }
    }
}
