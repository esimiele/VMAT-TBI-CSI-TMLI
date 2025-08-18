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
                UpdateViewWithPlanObjectivesFromTemplate(m.PlanObjectives);
            });
            WeakReferenceMessenger.Default.Register<RequestPlanObjectives>(this, (r, m) =>
            {
                m.Reply(PlanObjectives.ToList());
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                ESAPIThreadContext.UIDispatcher.BeginInvoke(() =>
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
    }
}
