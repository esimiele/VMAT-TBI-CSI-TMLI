using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerOptimizationLoop.Base;
using AutoPlannerOptimizationLoop.Core;
using AutoPlannerOptimizationLoop.DataContainers;
using AutoPlannerOptimizationLoop.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;

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
        public ICommand StartOptimizationCommand { get; set; }
        private ICommand _notifyStartOptimization;
        #endregion

        #region fields
        private PlanType _planType;
        private AutoPlanTemplateBase _selectedTemplate;
        private List<string> _planIds;
        #endregion

        public OptimizationConstraintsViewModel(List<string> sIds, PlanType type, ICommand NotifyStartOptimization)
        {
            if (sIds.Any()) StructureIds = sIds;
            else StructureIds = new List<string> { "1", "2", "3" };
            _planType = type;
            AddOptimizationConstraintCommand = new RelayCommand(AddOptimizationObjective);
            GetOptConstraintsFromPlanCommand = new RelayCommand(GetOptimizationConstraintsFromPlan);
            GetOptConstraintsFromLogsCommand = new RelayCommand(GetOptimizationConstraintsFromLogs);
            ClearOptimizationConstraintListCommand = new RelayCommand(ClearOptimizationConstraints);
            StartOptimizationCommand = new RelayCommand(StartOptimization);
            PlanOptimizationConstraints = new ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> { };
            foreach (PlanOptimizationSetupModel itr in OptimizationLoopSettings.PlanPreparationOptimizationSetup)
            {
                PlanOptimizationConstraints.Add(itr);
            }
            _notifyStartOptimization = NotifyStartOptimization;
            if (EclipseContext.GetInstance().IsInitialized && EclipseContext.GetInstance().VMATPlans.Any()) _planIds = new List<string>(EclipseContext.GetInstance().VMATPlans.Select(x => x.Id));
            else _planIds = new List<string> { "1", "2"};
        }

        public void UpdateViewWithSelectedPlanTemplate(AutoPlanTemplateBase template)
        {
            if (!OptimizationLoopSettings.PlanPreparationOptimizationSetup.Any())
            {
                if (ReferenceEquals(template, null)) return;
                _selectedTemplate = template;
                PlanOptimizationConstraints.Clear();
                if (_planType == PlanType.VMAT_TBI) PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(_planIds.First(), (_selectedTemplate as TBIAutoPlanTemplate).InitialOptimizationConstraints.Where(x => _structureIds.Any(y => y.Contains(x.StructureId, StringComparison.OrdinalIgnoreCase)))));
                else if (_planType == PlanType.VMAT_CSI)
                {
                    PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(_planIds.First(), (_selectedTemplate as CSIAutoPlanTemplate).InitialOptimizationConstraints.Where(x => _structureIds.Any(y => y.Contains(x.StructureId, StringComparison.OrdinalIgnoreCase)))));
                    if ((_selectedTemplate as CSIAutoPlanTemplate).BoostRxDosePerFx != 0.1)
                    {
                        PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(_planIds.Last(), (_selectedTemplate as CSIAutoPlanTemplate).BoostOptimizationConstraints.Where(x => _structureIds.Any(y => y.Contains(x.StructureId, StringComparison.OrdinalIgnoreCase)))));
                    }
                }
                else PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(_planIds.First(), (_selectedTemplate as TMLIAutoPlanTemplate).InitialOptimizationConstraints.Where(x => _structureIds.Any(y => y.Contains(x.StructureId, StringComparison.OrdinalIgnoreCase)))));
            }
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
            if (!EclipseContext.GetInstance().IsInitialized || !EclipseContext.GetInstance().VMATPlans.Any()) return;
            PlanOptimizationConstraints.Clear();
            foreach (ExternalPlanSetup itr in EclipseContext.GetInstance().VMATPlans)
            {
                PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(itr.Id, OptimizationSetupHelper.ReadConstraintsFromPlan(itr)));
            }
        }

        public void GetOptimizationConstraintsFromLogs()
        {
            foreach (PlanOptimizationSetupModel itr in OptimizationLoopSettings.PlanPreparationOptimizationSetup)
            {
                PlanOptimizationConstraints.Add(itr);
            }
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
                PlanOptimizationConstraints.Refresh();
            }
        }

        public void StartOptimization()
        {
            _notifyStartOptimization.Execute(null);
        }
    }
}
