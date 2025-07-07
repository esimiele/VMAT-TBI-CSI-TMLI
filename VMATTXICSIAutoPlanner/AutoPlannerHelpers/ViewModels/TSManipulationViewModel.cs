using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class TSManipulationViewModel : ObservableObject
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
        public ICommand AddTSManipulationCommand { get; set; }
        public ICommand AddDefaultTSManipulationsCommand { get; set; }
        public ICommand RemoveAllTSManipulationsCommand { get; set; }
        public ICommand PerformTSGenerationManipulationCommand { get; set; }
        public RelayCommand<RequestedTSManipulationModel> ClearRowCommand { get; set; } 
        #endregion

        public TSManipulationViewModel(List<string> structureIds)
        {
            AddTSManipulationCommand = new RelayCommand(AddTSManipulation);
            AddDefaultTSManipulationsCommand = new RelayCommand(AddDefaultTSManipulations);
            PerformTSGenerationManipulationCommand = new RelayCommand(PerformTSGenerationManipulation);
            RemoveAllTSManipulationsCommand = new RelayCommand(RemoveAllTSManipulations);
            ClearRowCommand = new RelayCommand<RequestedTSManipulationModel>(ClearRow);
            StructureIdsPostUnion = new List<string>(structureIds);
            RequestedTSManipulations = new ObservableCollectionPropertyNotify<RequestedTSManipulationModel> { };
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestAutoPlanTemplateChangedMessage>(this, (r, m) =>
            {
                AutoPlanTemplateSelectionChanged(m.AutoPlanTemplate);
            });
            //WeakReferenceMessenger.Default.Register<RequestUpdateTSManipulationList>(this, (r, m) =>
            //{
            //    UpdateTSManipulationList(m.StructureIds, m.RequestedTSManipulations);
            //});
        }

        public void UpdateTSManipulationList(IEnumerable<string> newStructureIds, List<RequestedTSManipulationModel> tsManipulations)
        {
            RequestedTSManipulations.Clear();
            StructureIdsPostUnion.Clear();
            StructureIdsPostUnion.AddRange(newStructureIds);
            foreach(RequestedTSManipulationModel itr in tsManipulations) RequestedTSManipulations.Add(itr);
        }

        public void AutoPlanTemplateSelectionChanged(AutoPlanTemplateBase template)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            UpdateViewWithAutoPlanTemplateTSManipulations();
        }

        private void UpdateViewWithAutoPlanTemplateTSManipulations(bool skipStructureIdCheck = false)
        {
            RequestedTSManipulations.Clear();
            //foreach (RequestedTSManipulationModel itr in _selectedTemplate.TSManipulations)
            //{
            //    if (skipStructureIdCheck) RequestedTSManipulations.Add(itr);
            //    else if (_structureIdsPostUnion.Any(x => string.Equals(x, itr.StructureId, System.StringComparison.OrdinalIgnoreCase)))
            //    {
            //        //only add it they base structure exists in the structure set
            //        string structureId = _structureIdsPostUnion.First(x => string.Equals(x, itr.StructureId, System.StringComparison.OrdinalIgnoreCase));
            //        RequestedTSManipulations.Add(new RequestedTSManipulationModel(structureId, itr.ManipulationType, itr.MarginInCM));
            //    }
            //}
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
            //WeakReferenceMessenger.Default.Send(new RequestGenerateManipulateTuningStructuresMessage(RequestedTSManipulations));
        }
    }
}
