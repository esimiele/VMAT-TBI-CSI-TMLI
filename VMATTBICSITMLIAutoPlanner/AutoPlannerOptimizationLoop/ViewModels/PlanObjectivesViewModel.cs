using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerOptimizationLoop.Helpers;
using System.Text;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.Logging;

namespace AutoPlannerOptimizationLoop.ViewModels
{
    internal class PlanObjectivesViewModel : ObservableObject
    {
        #region Properties
        public ObservableCollectionPropertyNotify<PlanObjectiveModel> PlanObjectives { get; set; }
        private List<string> _structureIds;

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set { SetProperty(ref _structureIds, value); }
        }
        #endregion

        #region commands
        public ICommand AddPlanObjectiveCommand { get; set; }
        public ICommand ClearPlanObjectiveListCommand { get; set; }
        public RelayCommand<PlanObjectiveModel> ClearRowCommand { get; set; }
        #endregion

        public PlanObjectivesViewModel(List<string> sIds) 
        { 
            if(sIds.Any()) StructureIds = sIds;
            else StructureIds = new List<string> { "1", "2", "3"};
            AddPlanObjectiveCommand = new RelayCommand(AddPlanObjective);
            ClearPlanObjectiveListCommand = new RelayCommand(ClearPlanObjectives);
            ClearRowCommand = new RelayCommand<PlanObjectiveModel>(ClearRow);
            PlanObjectives = new ObservableCollectionPropertyNotify<PlanObjectiveModel> { };
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdatePlanObjectives>(this, (r, m) =>
            {
                ESAPIThreadContext.UIDispatcher?.BeginInvoke(() =>
                {
                    UpdateViewWithPlanObjectivesFromTemplate(m.PlanObjectives);
                });
            });
            WeakReferenceMessenger.Default.Register<RequestPlanObjectives>(this, (r, m) =>
            {
                if (VerifyPlanObjectivesIntegrity()) m.Reply(PlanObjectives.ToList());
                else m.Reply(new List<PlanObjectiveModel> { });
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                ESAPIThreadContext.UIDispatcher?.BeginInvoke(() =>
                {
                    ClearPlanObjectives();
                    StructureIds = new List<string>(m.StructureIds);
                });
            });
        }

        public void UpdateViewWithPlanObjectivesFromTemplate(List<PlanObjectiveModel> obj)
        {
            PlanObjectives.Clear();
            foreach (PlanObjectiveModel itr in obj)
            {
                if (_structureIds.Any(x => x.Equals(itr.StructureId, StringComparison.OrdinalIgnoreCase)))
                {
                    itr.StructureId = _structureIds.First(x => x.Equals(itr.StructureId, StringComparison.OrdinalIgnoreCase));
                    PlanObjectives.Add(itr);
                }
            }
        }

        public void AddPlanObjective()
        {
            if (!ReferenceEquals(PlanObjectives, null))
            {
                PlanObjectives.Add(new PlanObjectiveModel(_structureIds.First(), OptimizationObjectiveType.None, 0, Units.None, 0, Units.None));
            }
        }

        public void ClearPlanObjectives()
        {
            PlanObjectives.Clear();
        }

        public void ClearRow(object o)
        {
            PlanObjectiveModel p = o as PlanObjectiveModel;
            if (PlanObjectives.Contains(p))
            {
                PlanObjectives.Remove(p);
            }
        }

        public bool VerifyPlanObjectivesIntegrity()
        {
            if (!PlanObjectives.Any())
            {
                Logger.GetInstance().LogError("Error! No plan objectives present! Please fix and try again");
                return false;
            }
            if (PlanObjectives.Any(x => !x.IsValidObjective))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following plan objectives are not valid:");
                foreach (PlanObjectiveModel itr in PlanObjectives)
                {
                    if (!itr.IsValidObjective)
                    {
                        sb.AppendLine(itr.FriendlyName);
                    }
                }
                sb.AppendLine("");
                sb.AppendLine("Do you want to continue?");
                ConfirmPrompt CP = new ConfirmPrompt(sb.ToString());
                CP.ShowDialog();
                if (!CP.GetSelection()) return false;
            }
            return true;
        }
    }
}
