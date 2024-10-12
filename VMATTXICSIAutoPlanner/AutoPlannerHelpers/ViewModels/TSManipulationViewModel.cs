using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;

namespace AutoPlannerHelpers.ViewModels
{
    public class TSManipulationViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<RequestedTSManipulationModel> RequestedTSManipulations { get; set; }

        #region properties
        private AutoPlanTemplateBase _selectedTemplate;

        private List<string> _structureIdsPostUnion;

        public List<string> StructureIdsPostUnion
        {
            get { return _structureIdsPostUnion; }
            set { _structureIdsPostUnion = value; }
        }

        #endregion

        #region commands
        private DelegateCommand _notifyMainVMExecuted;
        public DelegateCommand AddTSManipulationCommand { get; set; }
        public DelegateCommand AddDefaultTSManipulationsCommand { get; set; }
        public DelegateCommand RemoveAllTSManipulationsCommand { get; set; }
        public DelegateCommand PerformTSGenerationManipulationCommand { get; set; }
        public DelegateCommand<RequestedTSManipulationModel> ClearRowCommand { get; set; } 
        #endregion

        public TSManipulationViewModel(DelegateCommand NotifyMainVMExecuted, List<string> structureIds)
        {
            _notifyMainVMExecuted = NotifyMainVMExecuted;
            AddTSManipulationCommand = new DelegateCommand(AddTSManipulation);
            AddDefaultTSManipulationsCommand = new DelegateCommand(AddDefaultTSManipulations);
            PerformTSGenerationManipulationCommand = new DelegateCommand(PerformTSGenerationManipulation);
            RemoveAllTSManipulationsCommand = new DelegateCommand(RemoveAllTSManipulations);
            ClearRowCommand = new DelegateCommand<RequestedTSManipulationModel>(ClearRow);
            StructureIdsPostUnion = new List<string>(structureIds);
            RequestedTSManipulations = new ObservableCollectionPropertyNotify<RequestedTSManipulationModel> { };
        }

        public void AutoPlanTemplateSelectionChaged(AutoPlanTemplateBase template)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            UpdateViewWithAutoPlanTemplateTSManipulations();
        }

        private void UpdateViewWithAutoPlanTemplateTSManipulations(bool skipStructureIdCheck = false)
        {
            RequestedTSManipulations.Clear();
            foreach (RequestedTSManipulationModel itr in _selectedTemplate.TSManipulations)
            {
                if (skipStructureIdCheck) RequestedTSManipulations.Add(itr);
                else if (StructureIdsPostUnion.Any(x => string.Equals(x, itr.StructureId, System.StringComparison.OrdinalIgnoreCase)))
                {
                    //only add it they base structure exists in the structure set
                    RequestedTSManipulations.Add(itr);
                }
            }
        }

        private void AddTSManipulation()
        {
            RequestedTSManipulations.Add(new RequestedTSManipulationModel(StructureIdsPostUnion.First(), Enums.TSManipulationType.None, 0.0));
        }

        private void AddDefaultTSManipulations()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            UpdateViewWithAutoPlanTemplateTSManipulations();
        }

        private void RemoveAllTSManipulations()
        {
            RequestedTSManipulations.Clear();
        }

        private void ClearRow(RequestedTSManipulationModel item)
        {
            RequestedTSManipulations.Remove(item);
        }

        public void PerformTSGenerationManipulation()
        {
            _notifyMainVMExecuted.Execute();
        }
    }
}
