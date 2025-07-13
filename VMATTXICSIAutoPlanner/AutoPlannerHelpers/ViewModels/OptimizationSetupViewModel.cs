using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class OptimizationSetupViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> PlanOptimizationConstraints { get; set; }
        public ObservableCollectionPropertyNotify<string> StructureIds { get; set; }


        #region fields
        private List<PlanOptimizationSetupModel> _defaultPlanOptSetup = new List<PlanOptimizationSetupModel>();
        private PlanType _planType;
        #endregion

        #region commands
        public ICommand AddOptimizationConstraintCommand { get; set; }
        public ICommand AddDefaultOptimizationConstraintsCommand { get; set; }
        public ICommand ClearOptimizationConstraintListCommand { get; set; }
        public RelayCommand<OptimizationConstraintModel> ClearRowCommand { get; set; }
        public ICommand AssignOptimizationConstraintsCommand { get; set; }
        #endregion

        public OptimizationSetupViewModel(PlanType planType)
        {
            AddOptimizationConstraintCommand = new RelayCommand(AddOptimizationObjective);
            AddDefaultOptimizationConstraintsCommand = new RelayCommand(AddDefaultOptimizationConstraints);
            ClearOptimizationConstraintListCommand = new RelayCommand(ClearOptimizationConstraints);
            ClearRowCommand = new RelayCommand<OptimizationConstraintModel>(ClearRow);
            AssignOptimizationConstraintsCommand = new RelayCommand(AssignOptimizationConstraints);
            StructureIds = new ObservableCollectionPropertyNotify<string> { };
            PlanOptimizationConstraints = new ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> { };
            _planType = planType;
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdateOptimizationConstraintsMessage>(this, (r, m) =>
            {
                UpdateUIWithPlanOptimizationSetupList(m.PlanOptimizationSetup);
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                StructureIds.Clear();
                StructureIds.AddRange(m.StructureIds);
                AddDefaultOptimizationConstraints();
            });
        }

        public void UpdateUIWithPlanOptimizationSetupList(List<PlanOptimizationSetupModel> planOptSetup)
        {
            _defaultPlanOptSetup = planOptSetup;
            AddDefaultOptimizationConstraints();
        }

        private void AddOptimizationObjective()
        {
            if (PlanOptimizationConstraints.Count() == 1)
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
            return new OptimizationConstraintModel(StructureIds.First(), OptimizationObjectiveType.None, 0.0, Units.None, 0.0, 0);
        }

        private void AddDefaultOptimizationConstraints()
        {
            if (!_defaultPlanOptSetup.Any()) return;
            PlanOptimizationConstraints.Clear();
            foreach (PlanOptimizationSetupModel itr in _defaultPlanOptSetup)
            {
                List<OptimizationConstraintModel> constraints = new List<OptimizationConstraintModel>();
                foreach(OptimizationConstraintModel optModel in itr.OptimizationConstraints)
                {
                    if (StructureIds.Any(x => string.Equals(x, optModel.StructureId, StringComparison.OrdinalIgnoreCase)))
                    {
                        optModel.StructureId = StructureIds.First(x => string.Equals(x, optModel.StructureId, StringComparison.OrdinalIgnoreCase));
                        constraints.Add(new OptimizationConstraintModel(optModel));
                    }
                }
                PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(itr.PlanId, constraints));
            }
        }

        private void ClearOptimizationConstraints()
        {
            PlanOptimizationConstraints.Clear();
        }

        private void ClearRow(OptimizationConstraintModel o)
        {
            if(PlanOptimizationConstraints.SelectMany(x => x.OptimizationConstraints).Contains(o))
            {
                PlanOptimizationSetupModel planOptSetupModel = PlanOptimizationConstraints.First(x => x.OptimizationConstraints.Contains(o));
                List<OptimizationConstraintModel> constraints = planOptSetupModel.OptimizationConstraints;
                constraints.Remove(o);
                PlanOptimizationConstraints.Refresh();
            }
        }

        public void AssignOptimizationConstraints()
        {
            WeakReferenceMessenger.Default.Send(new RequestSetOptimizationConstraintsMessage(PlanOptimizationConstraints));
        }
    }
}
