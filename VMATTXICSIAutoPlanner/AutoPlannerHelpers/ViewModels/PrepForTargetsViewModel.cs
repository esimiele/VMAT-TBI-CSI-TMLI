using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class PrepForTargetsViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<RequestedTSStructureModel> RequestedTuningStructures { get; set; }

        #region fields
        private List<RequestedTSStructureModel> _originalRequestedTargets;
        #endregion

        #region commands
        public ICommand DisplayInfoCommand { get; set; }
        public ICommand AddDefaultTSStructuresCommand { get; set; }
        public ICommand RemoveAllTSStructuresCommand { get; set; }
        public RelayCommand<RequestedTSStructureModel> ClearRowCommand { get; set; }
        public ICommand RunPrepForTargetsCommand { get; set; }
        private ICommand _notifyMainVMExecuted;
        #endregion

        public PrepForTargetsViewModel(ICommand notifyMainVM)
        {
            _notifyMainVMExecuted = notifyMainVM;
            _originalRequestedTargets = new List<RequestedTSStructureModel> { };
            RequestedTuningStructures = new ObservableCollectionPropertyNotify<RequestedTSStructureModel> { };
            DisplayInfoCommand = new RelayCommand(DisplayPrepForTargetsInfo);
            AddDefaultTSStructuresCommand = new RelayCommand(AddDefaultTSStructures);
            RemoveAllTSStructuresCommand = new RelayCommand(RemoveAllTSStructures);
            ClearRowCommand = new RelayCommand<RequestedTSStructureModel>(ClearRow);
            RunPrepForTargetsCommand = new RelayCommand(RunPrepForTargets);
        }

        public void UpdateRequestedTargetStructures(List<RequestedTSStructureModel> targets)
        {
            RequestedTuningStructures.Clear();
            _originalRequestedTargets = new List<RequestedTSStructureModel>(targets);
            foreach (RequestedTSStructureModel itr in _originalRequestedTargets) RequestedTuningStructures.Add(itr);
        }

        private void DisplayPrepForTargetsInfo()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("This tab will prepare the structure set for contouring of the final targets that will be used for planning. Specifically, it will ensure the brain and spinal cord are default resolution.");
            MessageBox.Show(message.ToString());
        }

        public void AddDefaultTSStructures()
        {
            RequestedTuningStructures.Clear();
            foreach (RequestedTSStructureModel itr in _originalRequestedTargets) RequestedTuningStructures.Add(itr);
        }

        public void ClearRow(RequestedTSStructureModel o)
        {
            RequestedTuningStructures.Remove(o);
        }

        private void RemoveAllTSStructures()
        {
            RequestedTuningStructures.Clear();
        }

        private void RunPrepForTargets()
        {
            _notifyMainVMExecuted.Execute(null);
        }
    }
}
