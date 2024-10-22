using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using Prism.Commands;
using Prism.Mvvm;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.BaseViewModel
{
    public abstract class BaseViewModel : BindableBase
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
        private System.Windows.Media.SolidColorBrush _tsManipulationTabBackground;
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
            set { _structureSetId = value; }
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

        public System.Windows.Media.SolidColorBrush TSManipulationTabBackground
        {
            get { return _tsManipulationTabBackground; }
            set { SetProperty(ref _tsManipulationTabBackground, value); }
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
        protected SetTargetsViewModel _setTargetsVM;
        private object _specifyTargets;
        protected OptimizationSetupViewModel _optimizationSetupVM;
        private object _optimizationSetup;
        protected TSManipulationViewModel _tsManipulationVM;
        private object _tsManipulation;
        protected BeamPlacementViewModel _beamPlacementVM;
        private object _beamPlacement;
        protected TSGenerationViewModel _tsGenerationVM;
        private object _tsGeneration;
        private PlanPreparationViewModel _planPrepVM;
        private object _planPreparation;
        private object _scriptConfiguration;

        public object SpecifyTargets
        {
            get { return _specifyTargets; }
            set { SetProperty(ref _specifyTargets, value); }
        }

        public object TSGeneration
        {
            get { return _tsGeneration; }
            set { SetProperty(ref _tsGeneration, value); }
        }

        public object TSManipulation
        {
            get { return _tsManipulation; }
            set { SetProperty(ref _tsManipulation, value); }
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
        public DelegateCommand WindowClosingCommand { get; set; }
        protected DelegateCommand NotifySetTargetsCommand;
        protected DelegateCommand NotifyGenerateManipulateTuningStructuresCommand;
        protected DelegateCommand NotifyBeamsPlacedCommand;
        protected DelegateCommand NotifyAssignOptimizationConstraintsCommand;
        protected DelegateCommand NotifyPreparePlanForTreatmentCommand;
        #endregion

        #region fields
        protected List<PrescriptionModel> _prescriptions = new List<PrescriptionModel> { };
        protected List<PlanOptimizationSetupModel> _planOptimizationSetup = new List<PlanOptimizationSetupModel> { };
        protected List<PlanIsocenterModel> _planIsocenters = new List<PlanIsocenterModel> { };
        protected List<string> _structureIdsPostUnion;
        protected string _generalConfigurationFile = string.Empty;
        private PlanType _planType;
        #endregion

        public BaseViewModel(PlanType type, string[] args)
        {
            _planType = type;
            if (args.Any()) EclipseContextHelper.GenerateEclipseContext(args.ToList());
            if (EclipseContext.GetInstance().IsInitialized && ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                _structureIdsPostUnion = StructureTuningHelper.GenerateStructureIdListPostUnion(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList());
            }
            else
            {
                _structureIdsPostUnion = new List<string> { "lung_l", "lung_r", "kidney_l", "kidney_r", "PTV^Body", "OpticChiasm", "Brainstem" };
            }

            NotifySetTargetsCommand = new DelegateCommand(SetTargets);
            _setTargetsVM = new SetTargetsViewModel(NotifySetTargetsCommand);
            SpecifyTargets = new SpecifyTargetsView { DataContext = _setTargetsVM };

            _tsGenerationVM = new TSGenerationViewModel();
            TSGeneration = new TSGenerationView { DataContext = _tsGenerationVM };

            NotifyGenerateManipulateTuningStructuresCommand = new DelegateCommand(PerformTSStructureGenerationManipulation);
            _tsManipulationVM = new TSManipulationViewModel(NotifyGenerateManipulateTuningStructuresCommand, _structureIdsPostUnion);
            TSManipulation = new TSManipulationView { DataContext = _tsManipulationVM };

            NotifyBeamsPlacedCommand = new DelegateCommand(GeneratePlansAndPlaceBeams);
            _beamPlacementVM = new BeamPlacementViewModel(NotifyBeamsPlacedCommand, type);
            BeamPlacement = new BeamPlacementView { DataContext = _beamPlacementVM };

            NotifyAssignOptimizationConstraintsCommand = new DelegateCommand(AssignOptimizationConstraints);
            _optimizationSetupVM = new OptimizationSetupViewModel(_structureIdsPostUnion, NotifyAssignOptimizationConstraintsCommand, type);
            OptimizationSetup = new OptimizationSetupView { DataContext = _optimizationSetupVM };

            NotifyPreparePlanForTreatmentCommand = new DelegateCommand(PreparePlanForTreatment);
            _planPrepVM = new PlanPreparationViewModel(NotifyPreparePlanForTreatmentCommand);
            PlanPreparation = new PlanPreparationView { DataContext = _planPrepVM };

            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase>() { };
            WindowClosingCommand = new DelegateCommand(WindowClosing);

            StructureTuningTabBackground = System.Windows.Media.Brushes.LightGray;
            TSManipulationTabBackground = System.Windows.Media.Brushes.LightGray;
            BeamPlacementTabBackground = System.Windows.Media.Brushes.LightGray;
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.LightGray;
        }

        protected virtual void SetTargets()
        {
            if (VerifyTargetsIntegrity(_setTargetsVM.PlanTargets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(_setTargetsVM.PlanTargets, _initialDosePerFraction, _initialNumberOfFractions, _initialPlanTotalDose);
            if (!_prescriptions.Any()) return;
            _planOptimizationSetup = BuildPlanOptimizationSetupList();
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.ForestGreen;
            StructureTuningTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            TSManipulationTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }

        protected List<PlanOptimizationSetupModel> BuildPlanOptimizationSetupList()
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

        protected abstract void PerformTSStructureGenerationManipulation();

        protected abstract void GeneratePlansAndPlaceBeams();

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

        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithRings(List<TSRingStructureModel> rings, List<PlanOptimizationSetupModel> planConstraints)
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
                    constraints.Insert(0, new OptimizationConstraintModel(itr.RingId, OptimizationObjectiveType.Upper, itr.DoseLevel, Units.cGy, 0.0, 80));
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

        protected void AssignOptimizationConstraints()
        {
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.ForestGreen;
        }

        protected abstract void LoadScriptConfigurationSettings(string file);
        protected abstract StringBuilder BuildScriptConfigurationInfo();
        protected abstract void PreparePlanForTreatment();
        protected abstract void UpdateUIWithSelectedPlanTemplate();

        public void ResetInitialRxDose()
        {
            if (InitialNumberOfFractions > 0 && InitialDosePerFraction > 0)
            {
                //double priorTotalDose = PlanTotalDose;
                InitialPlanTotalDose = InitialDosePerFraction * InitialNumberOfFractions;
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
