using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AutoPlannerHelpers.ViewModels
{
    public class PrepForTargetsViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<RequestedTSStructureModel> RequestedTuningStructures { get; set; }

        #region fields
        private List<RequestedTSStructureModel> _originalRequestedTargets;
        #endregion

        #region commands
        public DelegateCommand DisplayInfoCommand { get; set; }
        public DelegateCommand AddDefaultTSStructuresCommand { get; set; }
        public DelegateCommand RemoveAllTSStructuresCommand { get; set; }
        public DelegateCommand<RequestedTSStructureModel> ClearRowCommand { get; set; }
        private DelegateCommand RunPrepForTargetsCommand { get; set; }
        private DelegateCommand _notifyMainVMExecuted;
        #endregion

        public PrepForTargetsViewModel(DelegateCommand notifyMainVM, List<RequestedTSStructureModel> requestedTargets)
        {
            _notifyMainVMExecuted = notifyMainVM;
            _originalRequestedTargets = requestedTargets;
            RequestedTuningStructures = new ObservableCollectionPropertyNotify<RequestedTSStructureModel> { };
            foreach(RequestedTSStructureModel itr in requestedTargets) RequestedTuningStructures.Add(itr);
            DisplayInfoCommand = new DelegateCommand(DisplayPrepForTargetsInfo);
            AddDefaultTSStructuresCommand = new DelegateCommand(AddDefaultTSStructures);
            RemoveAllTSStructuresCommand = new DelegateCommand(RemoveAllTSStructures);
            ClearRowCommand = new DelegateCommand<RequestedTSStructureModel>(ClearRow);
            RunPrepForTargetsCommand = new DelegateCommand(RunPrepForTargets);
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
            _notifyMainVMExecuted.Execute();
        }
    }
}
