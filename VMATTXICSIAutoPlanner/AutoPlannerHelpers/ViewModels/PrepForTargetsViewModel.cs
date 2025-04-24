using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class PrepForTargetsViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<RequestedTSStructureModel> RequestedPreliminaryTargets { get; set; }

        #region fields
        private List<RequestedTSStructureModel> _originalRequestedTargets;
        #endregion

        #region commands
        public ICommand DisplayInfoCommand { get; set; }
        public ICommand AddDefaultTSStructuresCommand { get; set; }
        public ICommand RemoveAllTSStructuresCommand { get; set; }
        public RelayCommand<RequestedTSStructureModel> ClearRowCommand { get; set; }
        public ICommand RunPrepForTargetsCommand { get; set; }
        #endregion

        public PrepForTargetsViewModel()
        {
            _originalRequestedTargets = new List<RequestedTSStructureModel> { };
            RequestedPreliminaryTargets = new ObservableCollectionPropertyNotify<RequestedTSStructureModel> { };
            DisplayInfoCommand = new RelayCommand(DisplayPrepForTargetsInfo);
            AddDefaultTSStructuresCommand = new RelayCommand(AddDefaultTSStructures);
            RemoveAllTSStructuresCommand = new RelayCommand(RemoveAllTSStructures);
            ClearRowCommand = new RelayCommand<RequestedTSStructureModel>(ClearRow);
            RunPrepForTargetsCommand = new RelayCommand(RunPrepForTargets);
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdateTargetStructures>(this, (r, m) =>
            {
                UpdateRequestedTargetStructures(m.Structures);
            });
        }

        public void UpdateRequestedTargetStructures(List<RequestedTSStructureModel> targets)
        {
            RequestedPreliminaryTargets.Clear();
            _originalRequestedTargets = new List<RequestedTSStructureModel>(targets);
            foreach (RequestedTSStructureModel itr in _originalRequestedTargets) RequestedPreliminaryTargets.Add(itr);
        }

        private void DisplayPrepForTargetsInfo()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("This tab will prepare the structure set for contouring of the final targets that will be used for planning. Specifically, it will ensure the brain and spinal cord are default resolution.");
            MessageBox.Show(message.ToString());
        }

        public void AddDefaultTSStructures()
        {
            RequestedPreliminaryTargets.Clear();
            foreach (RequestedTSStructureModel itr in _originalRequestedTargets) RequestedPreliminaryTargets.Add(itr);
        }

        public void ClearRow(RequestedTSStructureModel o)
        {
            RequestedPreliminaryTargets.Remove(o);
        }

        private void RemoveAllTSStructures()
        {
            RequestedPreliminaryTargets.Clear();
        }

        private void RunPrepForTargets()
        {
            WeakReferenceMessenger.Default.Send(new RequestGeneratePreliminaryTargets(RequestedPreliminaryTargets.ToList()));
        }
    }
}
