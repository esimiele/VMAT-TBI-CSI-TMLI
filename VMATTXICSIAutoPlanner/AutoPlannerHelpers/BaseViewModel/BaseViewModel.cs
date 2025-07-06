using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using AutoPlannerHelpers.BaseCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerHelpers.Messengers;
using VMS.TPS.Common.Model.API;
using StructureApprovalStatus = VMS.TPS.Common.Model.Types.StructureApprovalStatus;

namespace AutoPlannerHelpers.BaseViewModel
{
    public abstract class BaseViewModel : ObservableObject
    {
        public ObservableCollection<AutoPlanTemplateBase> PlanTemplates { get; set; }

        #region properties
        protected string _patientMRN;
        protected string _structureSetId;
        protected AutoPlanTemplateBase _selectedTemplate;

        protected double _initialDosePerFraction;
        protected int _initialNumberOfFractions;
        protected double _initialPlanTotalDose;
        private System.Windows.Media.SolidColorBrush _specifyTargetsTabBackground;
        private System.Windows.Media.SolidColorBrush _structureTuningTabBackground;
        private System.Windows.Media.SolidColorBrush _optimizationStructureDerivationBackground;
        private System.Windows.Media.SolidColorBrush _beamPlacementTabBackground;
        private System.Windows.Media.SolidColorBrush _optimizationSetupTabBackground;

        public string PatientMRN
        {
            get { return _patientMRN; }
            set { SetProperty(ref _patientMRN, value); }
        }

        public string StructureSetId
        {
            get { return _structureSetId; }
            set { SetProperty(ref _structureSetId, value); }
        }

        public AutoPlanTemplateBase SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { SetProperty(ref _selectedTemplate, value); UpdateUIWithSelectedPlanTemplate(); }
        }

        public double InitialDosePerFraction
        {
            get { return _initialDosePerFraction; }
            set { SetProperty(ref _initialDosePerFraction, value); ResetInitialRxDose(); }
        }

        public int InitialNumberOfFractions
        {
            get { return _initialNumberOfFractions; }
            set { SetProperty(ref _initialNumberOfFractions, value); ResetInitialRxDose(); }
        }

        public double InitialPlanTotalDose
        {
            get { return _initialPlanTotalDose; }
            set { SetProperty(ref _initialPlanTotalDose, value); }
        }

        public System.Windows.Media.SolidColorBrush SpecifyTargetsTabBackground
        {
            get { return _specifyTargetsTabBackground; }
            set { SetProperty(ref _specifyTargetsTabBackground, value); }
        }

        public System.Windows.Media.SolidColorBrush StructureTuningTabBackground
        {
            get { return _structureTuningTabBackground; }
            set { SetProperty(ref _structureTuningTabBackground, value); }
        }

        public System.Windows.Media.SolidColorBrush OptimizationStructureDerivationBackground
        {
            get { return _optimizationStructureDerivationBackground; }
            set { SetProperty(ref _optimizationStructureDerivationBackground, value); }
        }

        public System.Windows.Media.SolidColorBrush BeamPlacementTabBackground
        {
            get { return _beamPlacementTabBackground; }
            set { SetProperty(ref _beamPlacementTabBackground, value); }
        }

        public System.Windows.Media.SolidColorBrush OptimizationSetupTabBackground
        {
            get { return _optimizationSetupTabBackground; }
            set { SetProperty(ref _optimizationSetupTabBackground, value); }
        }
        #endregion

        #region view objects
        private object _specifyTargets;
        private object _optimizationSetup;
        private object _specialOptimizationStructures;
        private object _optimizationStructureDerivations;
        private object _beamPlacement;
        private object _planPreparation;
        private object _scriptConfiguration;

        public object SpecifyTargets
        {
            get { return _specifyTargets; }
            set { SetProperty(ref _specifyTargets, value); }
        }

        public object SpecialOptimizationStructures
        {
            get { return _specialOptimizationStructures; }
            set { SetProperty(ref _specialOptimizationStructures, value); }
        }

        public object OptimizationStructureDerivations
        {
            get { return _optimizationStructureDerivations; }
            set { SetProperty(ref _optimizationStructureDerivations, value); }
        }

        public object OptimizationSetup
        {
            get { return _optimizationSetup; }
            set { SetProperty(ref _optimizationSetup, value); }
        }

        public object ScriptConfiguration
        {
            get { return _scriptConfiguration; }
            set { SetProperty(ref _scriptConfiguration, value); }
        }

        public object BeamPlacement
        {
            get { return _beamPlacement; }
            set { SetProperty(ref _beamPlacement, value); }
        }

        public object PlanPreparation
        {
            get { return _planPreparation; }
            set { SetProperty(ref _planPreparation, value); }
        }
        #endregion

        #region commands
        public ICommand WindowClosingCommand { get; set; }
        #endregion

        #region fields
        protected List<PrescriptionModel> _prescriptions = new List<PrescriptionModel> { };
        protected List<PlanOptimizationSetupModel> _planOptimizationSetup = new List<PlanOptimizationSetupModel> { };
        protected List<PlanIsocenterModel> _planIsocenters = new List<PlanIsocenterModel> { };
        protected List<string> _structureIdsPostUnion;
        protected string _generalConfigurationFile = string.Empty;
        protected PlanPreparationBase _planPrep = null;
        private PlanType _planType;
        #endregion

        public BaseViewModel(PlanType type, string[] args)
        {
            _planType = type;
            Logger.GetInstance().PlanType = _planType;
            if (args.Any()) EclipseContextHelper.GenerateEclipseContext(args.ToList());
            if (EclipseContext.GetInstance().IsInitialized && !ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                _structureIdsPostUnion = StructureTuningHelper.GenerateStructureIdListPostUnion(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList());
            }
            else
            {
                _structureIdsPostUnion = new List<string> { "lung_l", "lung_r", "kidney_l", "kidney_r", "PTV^Body", "OpticChiasm", "Brainstem" };
            }

            SpecifyTargets = new SpecifyTargetsView { DataContext = new SetTargetsViewModel() };
            SpecialOptimizationStructures = new SpecialOptimizationStructuresView { DataContext = new SpecialOptimizationStructuresViewModel() };
            OptimizationStructureDerivations = new StructureDerivationsView { DataContext = new StructureDerivationsViewModel(_structureIdsPostUnion, false) };
            BeamPlacement = new BeamPlacementView { DataContext = new BeamPlacementViewModel(type) };
            OptimizationSetup = new OptimizationSetupView { DataContext = new OptimizationSetupViewModel(_structureIdsPostUnion, type) };
            PlanPreparation = new PlanPreparationView { DataContext = new PlanPreparationViewModel() };

            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase>() { };
            WindowClosingCommand = new RelayCommand(WindowClosing);

            StructureTuningTabBackground = System.Windows.Media.Brushes.LightGray;
            OptimizationStructureDerivationBackground = System.Windows.Media.Brushes.LightGray;
            BeamPlacementTabBackground = System.Windows.Media.Brushes.LightGray;
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.LightGray;
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestSetTargetsMessage>(this, (r, m) =>
            {
                SetTargets(m.PlanTargets);
            });
            WeakReferenceMessenger.Default.Register<RequestPerformOptimizationStructureDerivations>(this, (r, m) =>
            {
                PerformOptimizationStructureDerivations(m.StructureOperations);
            });
            WeakReferenceMessenger.Default.Register<RequestGenerateAndPlaceBeams>(this, (r, m) =>
            {
                GeneratePlansAndPlaceBeams(m.SelectedLinac, m.SelectedEnergy, m.ContourOverlap, m.ContourOverlapMargin, m.PlanIsocenters);
            });
            WeakReferenceMessenger.Default.Register<RequestSetOptimizationConstraintsMessage>(this, (r, m) =>
            {
                AssignOptimizationConstraints(m.PlanOptimizationSetup);
            });
            WeakReferenceMessenger.Default.Register<RequestGenerateShiftNoteMessage>(this, (r, m) =>
            {
                m.Reply(GenerateShiftNote());
            });
            WeakReferenceMessenger.Default.Register<RequestSeparatePlanMessage>(this, (r, m) =>
            {
                m.Reply(SeparatePlans());
            });
            WeakReferenceMessenger.Default.Register<RequestDoSeparatedPlansRequireDoseRecalculation>(this, (r, m) =>
            {
                m.Reply(_planPrep.DoseRecalcNeeded);
            });
            WeakReferenceMessenger.Default.Register<RequestRecalculateDoseForSeparatedPlans>(this, (r, m) =>
            {
                m.Reply(RecalculateDoseForSeparatePlans());
            });
        }

        protected virtual void SetTargets(List<PlanTargetsModel> targets)
        {
            if (VerifyTargetsIntegrity(targets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(targets, _initialDosePerFraction, _initialNumberOfFractions, _initialPlanTotalDose);
            if (!_prescriptions.Any()) return;
            Logger.GetInstance().Prescriptions = _prescriptions;
            _planOptimizationSetup = BuildPlanOptimizationSetupList();
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.ForestGreen;
            StructureTuningTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            OptimizationStructureDerivationBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }

        protected bool AreRequestedPrescriptionTargetsApproved(IEnumerable<TargetModel> targets)
        {
            foreach (TargetModel target in targets)
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(target.TargetId, true))
                {
                    Logger.GetInstance().LogError($"Error! {target.TargetId} is either NOT present in structure set or is not contoured!");
                    return false;
                }
                else
                {
                    //structure is present and contoured
                    StructureApprovalStatus approvalStatus = StructureTuningHelper.GetStructureFromId(target.TargetId).ApprovalHistory.First().ApprovalStatus;
                    if (approvalStatus != StructureApprovalStatus.Approved)
                    {
                        Logger.GetInstance().LogError($"Error! {target.TargetId} is NOT approved!" + Environment.NewLine + $"{target.TargetId} approval status: {approvalStatus}");
                        return false;
                    }
                }
            }
            return true;
        }

        public List<PlanOptimizationSetupModel> BuildPlanOptimizationSetupList()
        {
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                return OptimizationSetupHelper.RetrieveOptConstraintsFromTemplate(_selectedTemplate, _prescriptions, _planType);
            }
            else
            {
                List<PlanOptimizationSetupModel> result = new List<PlanOptimizationSetupModel> { };
                foreach (PlanTargetsModel itr in TargetsHelper.GroupPrescriptionsByPlanIdAndOrderByTargetRx(_prescriptions))
                {
                    List<OptimizationConstraintModel> constraints = new List<OptimizationConstraintModel>();
                    foreach (TargetModel target in itr.Targets)
                    {
                        constraints.Add(new OptimizationConstraintModel(target.TargetId, OptimizationObjectiveType.Lower, target.TargetRxDose, Units.cGy, 100.0, 100));
                        constraints.Add(new OptimizationConstraintModel(target.TargetId, OptimizationObjectiveType.Upper, 1.02 * target.TargetRxDose, Units.cGy, 0.0, 100));
                    }
                    result.Add(new PlanOptimizationSetupModel(itr.PlanId, constraints));
                }
                return result;
            }
        }

        protected abstract bool VerifyTargetsIntegrity(List<PlanTargetsModel> parsedTargets);

        protected abstract void PerformOptimizationStructureDerivations(List<StructureOperationModel> operations);

        protected abstract void GeneratePlansAndPlaceBeams(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters);

        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithTSTargets(List<PlanTargetsModel> planTargets, List<PlanOptimizationSetupModel> planConstraints)
        {
            //update optimization constraint list to replace target constraints with ts targets
            foreach (PlanTargetsModel itr in planTargets)
            {
                if(planConstraints.Any(x => string.Equals(x.PlanId, itr.PlanId)))
                {
                    List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(x.PlanId, itr.PlanId)).OptimizationConstraints;
                    foreach (TargetModel target in itr.Targets)
                    {
                        if (constraints.Any(x => string.Equals(x.StructureId, target.TargetId)))
                        {
                            foreach (OptimizationConstraintModel matchingTargetConstraint in constraints.Where(x => string.Equals(x.StructureId, target.TargetId)))
                            {
                                matchingTargetConstraint.StructureId = target.TsTargetId;
                            }
                        }
                    }
                }
            }
            return planConstraints;
        }

        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithRings(List<TSRingStructureModel> rings, List<PlanOptimizationSetupModel> planConstraints, int ringPrioity = 80)
        {
            foreach (TSRingStructureModel itr in rings)
            {
                if(_prescriptions.Any(x => string.Equals(itr.TargetId, x.TargetId)))
                {
                    //grab the plan that contains this specific target
                    string planId = _prescriptions.First(x => string.Equals(itr.TargetId, x.TargetId)).PlanId;
                    //grab the optimization constraints that belong to this plan
                    List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(planId, x.PlanId)).OptimizationConstraints;
                    //insert the ts ring constraint
                    constraints.Insert(0, new OptimizationConstraintModel(itr.RingId, OptimizationObjectiveType.Upper, itr.DoseLevel, Units.cGy, 0.0, ringPrioity));
                }
            }
            return planConstraints;
        }

        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithCropOverlapStructures(List<TSTargetCropOverlapModel> manipulations, List<PlanOptimizationSetupModel> planConstraints)
        {
            foreach (TSTargetCropOverlapModel itr in manipulations)
            {
                List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(x.PlanId, itr.PlanId)).OptimizationConstraints;
                foreach (OptimizationConstraintModel model in constraints.Where(x => string.Equals(x.StructureId, itr.TargetId)))
                {
                    model.StructureId = itr.ManipulationTargetId;
                }
            }
            return planConstraints;
        }

        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithTSJunctions(List<PlanFieldJunctionModel> junctions, List<PlanOptimizationSetupModel> planConstraints)
        {
            //update optimization constraint list to replace target constraints with ts targets
            foreach (PlanFieldJunctionModel itr in junctions)
            {
                //get the dose for the last prescription item that has the same plan id as the current plan field junction model (should already be sorted by cumulative dose to targets)
                double dose = _prescriptions.Last(x => string.Equals(itr.PlanId, x.PlanId)).CumulativeDoseToTarget;
                List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(x.PlanId, itr.PlanId)).OptimizationConstraints;
                foreach (FieldJunctionModel jnx in itr.FieldJunctions)
                {
                    constraints.Insert(0, new OptimizationConstraintModel(jnx.JunctionStructureId, OptimizationObjectiveType.Lower, dose, Units.cGy, 100.0, 100));
                    constraints.Insert(1, new OptimizationConstraintModel(jnx.JunctionStructureId, OptimizationObjectiveType.Upper, 1.02 * dose, Units.cGy, 0.0, 100));
                }
            }
            return planConstraints;
        }

        protected void AssignOptimizationConstraints(List<PlanOptimizationSetupModel> PlanOptimizationConstraints)
        {
            if (!EclipseContext.GetInstance().VMATPlans.Any())
            {
                Logger.GetInstance().LogError("Error! No vmat plans generated from beam placement! No plans to assign optimization constraints to!");
                return;
            }
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
                OptimizationSetupTabBackground = System.Windows.Media.Brushes.ForestGreen;
            }
            else Logger.GetInstance().LogError("Error! No optimization constraints assigned!");
        }

        protected abstract void LoadScriptConfigurationSettings(string file);
        protected abstract StringBuilder BuildScriptConfigurationInfo();
        protected abstract bool GenerateShiftNote();
        protected abstract bool SeparatePlans();
        protected abstract bool RecalculateDoseForSeparatePlans();
        protected abstract void UpdateUIWithSelectedPlanTemplate();

        public void ResetInitialRxDose()
        {
            if (InitialNumberOfFractions > 0 && InitialDosePerFraction > 0)
            {
                //double priorTotalDose = PlanTotalDose;
                InitialPlanTotalDose = Math.Round(InitialDosePerFraction * InitialNumberOfFractions, 1);
                //if (PlanTotalDose != priorTotalDose)
                //{
                //    foreach (PlanObjectiveModel itr in PlanObjectives)
                //    {
                //        if (itr.QueryDoseUnits == Units.cGy)
                //        {
                //            itr.QueryDose = Math.Round(itr.QueryDose * PlanTotalDose / priorTotalDose, 1);
                //        }
                //    }
                //    PlanObjectives.Refresh();
                //    foreach (OptimizationConstraintModel itr in OptimizationConstraints)
                //    {
                //        if (itr.QueryDoseUnits == Units.cGy)
                //        {
                //            itr.QueryDose = Math.Round(itr.QueryDose * PlanTotalDose / priorTotalDose, 1);
                //        }
                //    }
                //    OptimizationConstraints.Refresh();
                //}
            }
        }

        public void WindowClosing()
        {
            if (EclipseContext.GetInstance().IsInitialized)
            {
                ScriptClosingHelper.CloseApplication(false);
            }
        }
    }
}
