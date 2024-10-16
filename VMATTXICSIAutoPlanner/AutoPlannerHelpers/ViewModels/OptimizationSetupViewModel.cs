using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.Prompts;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.ViewModels
{
    public class OptimizationSetupViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> PlanOptimizationConstraints { get; set; }

        #region properties
        private List<string> _structureIds;

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set { SetProperty(ref _structureIds, value); }
        }
        #endregion

        #region fields
        private AutoPlanTemplateBase _selectedTemplate;
        private List<PrescriptionModel> _prescriptions;
        #endregion

        #region commands
        private DelegateCommand _notifyMainVMExecuted;
        public DelegateCommand AddOptimizationConstraintCommand { get; set; }
        public DelegateCommand AddDefualtOptimizationConstraintsCommand { get; set; }
        public DelegateCommand ClearOptimizationConstraintListCommand { get; set; }
        public DelegateCommand<OptimizationConstraintModel> ClearRowCommand { get; set; }
        public DelegateCommand AssignOptimizationConstraintsCommand { get; set; }
        #endregion

        public OptimizationSetupViewModel(List<string> sIds, DelegateCommand notifyMainVMExecuted)
        {
            AddOptimizationConstraintCommand = new DelegateCommand(AddOptimizationObjective);
            AddDefualtOptimizationConstraintsCommand = new DelegateCommand(AddDefualtOptimizationConstraints);
            ClearOptimizationConstraintListCommand = new DelegateCommand(ClearOptimizationConstraints);
            ClearRowCommand = new DelegateCommand<OptimizationConstraintModel>(ClearRow);
            AssignOptimizationConstraintsCommand = new DelegateCommand(AssignOptimizationConstraints);
            if(sIds.Any()) StructureIds = new List<string>(sIds);
            else StructureIds = new List<string> { "1", "2", "3"};
            PlanOptimizationConstraints = new ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> { };
            _notifyMainVMExecuted = notifyMainVMExecuted;
        }

        public void UpdatePrescriptionList(List<PrescriptionModel> prescriptions)
        {
            _prescriptions = new List<PrescriptionModel>(prescriptions);
        }

        public void UpdateUIWithSelectedPlanTemplate(AutoPlanTemplateBase template)
        {
            if (ReferenceEquals(template, null) || ReferenceEquals(_prescriptions, null) || !_prescriptions.Any()) return;
            _selectedTemplate = template;
            AddDefualtOptimizationConstraints();
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
            return new OptimizationConstraintModel(_structureIds.First(), OptimizationObjectiveType.None, 0.0, Units.None, 0.0, 0);
        }

        private void AddDefualtOptimizationConstraints()
        {
            if (ReferenceEquals(_selectedTemplate, null) || ReferenceEquals(_prescriptions, null) || !_prescriptions.Any()) return;

            PlanOptimizationConstraints.Clear();
            List<PlanOptimizationSetupModel> constraints = OptimizationSetupHelper.RetrieveOptConstraintsFromTemplate(_selectedTemplate, _prescriptions);
            foreach (PlanOptimizationSetupModel itr in  constraints) PlanOptimizationConstraints.Add(itr);
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
            if (!EclipseContext.GetInstance().VMATPlans.Any()) return;
            bool constraintsAssigned = false;
            foreach (PlanOptimizationSetupModel itr in PlanOptimizationConstraints)
            {
                //additional check if the plan was not found in the list of VMATplans
                if (EclipseContext.GetInstance().VMATPlans.Any(x => string.Equals(x.Id, itr.PlanId)))
                {
                    ExternalPlanSetup plan = EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, itr.PlanId));
                    if (plan.OptimizationSetup.Objectives.Any())
                    {
                        foreach (OptimizationObjective o in plan.OptimizationSetup.Objectives) plan.OptimizationSetup.RemoveObjective(o);
                    }
                    OptimizationSetupHelper.AssignOptConstraints(itr.OptimizationConstraints, plan, false, 0.0);
                    constraintsAssigned = true;
                }
                else Logger.GetInstance().LogError($"{itr.PlanId} not found!");
            }
            if (constraintsAssigned)
            {
                string message = "Optimization objectives have been successfully set!" + Environment.NewLine + Environment.NewLine + "Please review the generated structures, placed isocenters, placed beams, and optimization parameters!";
                MessageBox.Show(message);
                Logger.GetInstance().OptimizationConstraints = PlanOptimizationConstraints.ToList();
                _notifyMainVMExecuted.Execute();
            }
            else Logger.GetInstance().LogError("Error! No optimization constraints assigned!");
            _notifyMainVMExecuted.Execute();
        }
    }
}
