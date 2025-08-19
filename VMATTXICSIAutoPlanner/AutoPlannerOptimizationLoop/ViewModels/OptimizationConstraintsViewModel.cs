using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerOptimizationLoop.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AutoPlannerOptimizationLoop.Helpers;

namespace AutoPlannerOptimizationLoop.ViewModels
{
    public class OptimizationConstraintsViewModel : ObservableObject
    {
        #region properties
        public ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> PlanOptimizationConstraints { get; set; }
        private List<string> _structureIds;

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set { SetProperty(ref _structureIds, value); }
        }
        #endregion

        #region commands
        public ICommand AddOptimizationConstraintCommand { get; set; }
        public ICommand GetOptConstraintsFromPlanCommand { get; set; }
        public ICommand GetOptConstraintsFromLogsCommand { get; set; }
        public ICommand ClearOptimizationConstraintListCommand { get; set; }
        public ICommand ClearRowCommand { get; set; }
        public ICommand StartOptimizationCommand { get; set; }
        #endregion

        #region fields
        private List<string> _planIds;
        private List<PlanOptimizationSetupModel> _tmpPlanOptSetup;
        #endregion

        public OptimizationConstraintsViewModel(List<string> sIds, List<string> pids, PlanType type)
        {
            if (sIds.Any()) StructureIds = sIds;
            else StructureIds = new List<string> { "1", "2", "3" };
            AddOptimizationConstraintCommand = new RelayCommand(AddOptimizationObjective);
            GetOptConstraintsFromPlanCommand = new RelayCommand(GetOptimizationConstraintsFromPlan);
            GetOptConstraintsFromLogsCommand = new RelayCommand(GetOptimizationConstraintsFromLogs);
            ClearOptimizationConstraintListCommand = new RelayCommand(ClearOptimizationConstraints);
            ClearRowCommand = new RelayCommand<OptimizationConstraintModel>(ClearRow);
            StartOptimizationCommand = new RelayCommand(StartOptimization);
            PlanOptimizationConstraints = new ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> { };
            foreach (PlanOptimizationSetupModel itr in OptimizationLoopSettings.PlanPreparationOptimizationSetup)
            {
                PlanOptimizationConstraints.Add(itr);
            }
            _planIds = new List<string>(pids);
            _tmpPlanOptSetup = new List<PlanOptimizationSetupModel>();
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestPlanSelectionChanged>(this, (r, m) =>
            {
                ESAPIThreadContext.UIDispatcher?.BeginInvoke(() =>
                {
                    GetOptimizationConstraintsFromPlan();
                });
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                ESAPIThreadContext.UIDispatcher?.BeginInvoke(() =>
                {
                    ClearOptimizationConstraints();
                    StructureIds = new List<string>(m.StructureIds);
                });
            });
        }

        public void AddOptimizationObjective()
        {
            if (PlanOptimizationConstraints.Count() > 1)
            {
                //logic for multiple plans
                SelectItemPrompt SIP = new SelectItemPrompt("Please selct a plan to add a constraint!", new List<string>(PlanOptimizationConstraints.Select(x => x.PlanId)));
                SIP.ShowDialog();
                if (!SIP.GetSelection()) return;
                PlanOptimizationSetupModel planOptSetupModel = PlanOptimizationConstraints.First(x => string.Equals(x.PlanId, SIP.GetSelectedItem()));
                List<OptimizationConstraintModel> constraints = planOptSetupModel.OptimizationConstraints;
                constraints.Add(GenerateNewEmptyOptimizationConstraint());
                PlanOptimizationConstraints.Refresh();
            }
            else if (!PlanOptimizationConstraints.Any())
            {
                PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel("1", GenerateNewEmptyOptimizationConstraint()));
            }
            else
            {
                PlanOptimizationConstraints.First().OptimizationConstraints.Add(GenerateNewEmptyOptimizationConstraint());
                PlanOptimizationConstraints.Refresh();
            }
        }

        private OptimizationConstraintModel GenerateNewEmptyOptimizationConstraint()
        {
            return new OptimizationConstraintModel(_structureIds.First(), OptimizationObjectiveType.None, 0.0, Units.None, 0.0, 0);
        }

        public void GetOptimizationConstraintsFromPlan()
        {
            _tmpPlanOptSetup = WeakReferenceMessenger.Default.Send(new RequestOptimizationConstraintsFromPlan());
            if (!_tmpPlanOptSetup.Any()) return;
            _planIds.Clear();
            _planIds = new List<string>(_tmpPlanOptSetup.Select(x => x.PlanId));
            AddOptimizationConstraintList(_tmpPlanOptSetup);
        }

        public void AddOptimizationConstraintList(IEnumerable<PlanOptimizationSetupModel> planOptSetup)
        {
            ClearOptimizationConstraints();
            foreach (PlanOptimizationSetupModel itr in planOptSetup.ToList())
            {
                PlanOptimizationConstraints.Add(itr);
            }
        }

        public void GetOptimizationConstraintsFromLogs()
        {
            if(!OptimizationLoopSettings.PlanPreparationOptimizationSetup.Any())
            {
                Logger.GetInstance().LogError("Warning! No optimization constraints found in log file! Skipping!");
                return;
            }
            
            _tmpPlanOptSetup.Clear();
            foreach (PlanOptimizationSetupModel itr in OptimizationLoopSettings.PlanPreparationOptimizationSetup)
            {
                if (_planIds.Any(x => string.Equals(x, itr.PlanId, StringComparison.OrdinalIgnoreCase)))
                {
                    _tmpPlanOptSetup.Add(new PlanOptimizationSetupModel(itr.PlanId, itr.OptimizationConstraints));
                }
            }

            AddOptimizationConstraintList(_tmpPlanOptSetup);
        }

        public void ClearOptimizationConstraints()
        {
            PlanOptimizationConstraints.Clear();
        }

        public void ClearRow(OptimizationConstraintModel opt)
        {
            if (PlanOptimizationConstraints.SelectMany(x => x.OptimizationConstraints).Contains(opt))
            {
                PlanOptimizationSetupModel planOptSetupModel = PlanOptimizationConstraints.First(x => x.OptimizationConstraints.Contains(opt));
                List<OptimizationConstraintModel> constraints = planOptSetupModel.OptimizationConstraints;
                constraints.Remove(opt);
                if (!constraints.Any()) PlanOptimizationConstraints.Remove(planOptSetupModel);
                PlanOptimizationConstraints.Refresh();
            }
        }

        public void StartOptimization()
        {
            WeakReferenceMessenger.Default.Send(new RequestSetOptimizationConstraintsMessage(PlanOptimizationConstraints));
        }
    }
}
