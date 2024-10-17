using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        #endregion

        #region commands
        public DelegateCommand WindowClosingCommand { get; set; }
        protected DelegateCommand NotifySetTargetsCommand;
        protected DelegateCommand NotifyGenerateManipulateTuningStructuresCommand;
        protected DelegateCommand NotifyBeamsPlacedCommand;
        protected DelegateCommand NotifyAssignOptimizationConstraintsCommand;
        #endregion

        #region fields
        protected List<PrescriptionModel> _prescriptions = new List<PrescriptionModel> { };
        protected List<PlanIsocenterModel> _planIsocenters = new List<PlanIsocenterModel> { };
        protected List<string> _structureIdsPostUnion;
        protected string _generalConfigurationFile = string.Empty;
        #endregion

        public BaseViewModel(PlanType type)
        {
            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase>() { };

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
            _optimizationSetupVM = new OptimizationSetupViewModel(_structureIdsPostUnion, NotifyAssignOptimizationConstraintsCommand);
            OptimizationSetup = new OptimizationSetupView { DataContext = _optimizationSetupVM };

            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };

            WindowClosingCommand = new DelegateCommand(WindowClosing);
        }

        protected abstract void PerformTSStructureGenerationManipulation();

        protected abstract void GeneratePlansAndPlaceBeams();

        protected void AssignOptimizationConstraints()
        {
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.ForestGreen;
        }

        protected abstract StringBuilder BuildScriptConfigurationInfo();

        protected virtual void SetTargets()
        {
            if (VerifyTargetsIntegrity(_setTargetsVM.PlanTargets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(_setTargetsVM.PlanTargets, _initialDosePerFraction, _initialNumberOfFractions, _initialPlanTotalDose);
            if (!_prescriptions.Any()) return;
            _optimizationSetupVM.UpdatePrescriptionList(_prescriptions);
            if (!ReferenceEquals(_selectedTemplate, null)) _optimizationSetupVM.UpdateUIWithSelectedPlanTemplate(_selectedTemplate);
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.ForestGreen;
            StructureTuningTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            TSManipulationTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }

        protected abstract bool VerifyTargetsIntegrity(List<PlanTargetsModel> parsedTargets);

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
