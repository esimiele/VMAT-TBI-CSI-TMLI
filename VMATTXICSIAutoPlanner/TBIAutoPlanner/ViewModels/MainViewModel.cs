using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Mvvm;
using Prism.Commands;
using AutoPlannerHelpers.Models;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Helpers;

namespace TBIAutoPlanner.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public ObservableCollection<TBIAutoPlanTemplate> PlanTemplates { get; set; }

        #region properties
        private string _patientMRN;
        private List<string> _structureSetIds;
        private double _dosePerFraction;
        private int _numberOfFractions;
        private double _planTotalDose;
        private TBIAutoPlanTemplate _selectedTemplate;
        private bool _useFlash;
        private Visibility _flashMarginVisible;
        private double _flashMargin;
        private double _ptvMarginFromBody;

        public string PatientMRN
        {
            get { return _patientMRN; }
            set { SetProperty(ref _patientMRN, value); }
        }

        public List<string> StructureSetIds
        {
            get { return _structureSetIds; }
            set { SetProperty(ref _structureSetIds, value); }
        }

        public double DosePerFraction
        {
            get { return _dosePerFraction; }
            set { SetProperty(ref _dosePerFraction, value); ResetRxDose(); }
        }

        public int NumberOfFractions
        {
            get { return _numberOfFractions; }
            set { SetProperty(ref _numberOfFractions, value); ResetRxDose(); }
        }

        public double PlanTotalDose
        {
            get { return _planTotalDose; }
            set { SetProperty(ref _planTotalDose, value); }
        }

        public TBIAutoPlanTemplate SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { SetProperty(ref _selectedTemplate, value); UpdateUIWithSelectedPlanTemplate(); }
        }

        public bool UseFlash
        {
            get { return _useFlash; }
            set { SetProperty(ref _useFlash, value); UpdateUseFlash(); }
        }

        public Visibility FlashMarginVisible
        {
            get { return _flashMarginVisible; }
            set { SetProperty(ref _flashMarginVisible, value); }
        }

        public double FlashMargin
        {
            get { return _flashMargin; }
            set { SetProperty(ref _flashMargin, value); }
        }

        public double PTVMarginFromBody
        {
            get { return _ptvMarginFromBody; }
            set { SetProperty(ref _ptvMarginFromBody, value); }
        }

        private System.Windows.Media.SolidColorBrush _specifyTargetsTabBackground;

        public System.Windows.Media.SolidColorBrush SpecifyTargetsTabBackground
        {
            get { return _specifyTargetsTabBackground; }
            set { SetProperty(ref _specifyTargetsTabBackground, value); }
        }

        private System.Windows.Media.SolidColorBrush _structureTuningTabBackground;

        public System.Windows.Media.SolidColorBrush StructureTuningTabBackground
        {
            get { return _structureTuningTabBackground; }
            set { SetProperty(ref _structureTuningTabBackground, value); }
        }

        private System.Windows.Media.SolidColorBrush _tsManipulationTabBackground;

        public System.Windows.Media.SolidColorBrush TSManipulationTabBackground
        {
            get { return _tsManipulationTabBackground; }
            set { SetProperty(ref _tsManipulationTabBackground, value); }
        }

        #endregion

        #region view objects
        private SetTargetsViewModel _setTargetsVM;
        private object _specifyTargets;
        private TSGenerationViewModel _tsGenerationVM;
        private object _tsGeneration;
        private object _tsManipulation;
        private object _optimizationSetup;
        private object _planPreparation;
        private object _scriptConfiguration;
        private object _beamPlacement;

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

        public object PlanPreparation
        {
            get { return _planPreparation; }
            set { SetProperty(ref _planPreparation, value); }
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
        public DelegateCommand QuickStartGuideCommand { get; set; }
        public DelegateCommand HelpGuideCommand { get; set; }
        public DelegateCommand PTVMarginInfoCommand { get; set; }
        private DelegateCommand NotifySetTargetsCommand;
        private DelegateCommand NotifyGenerateTuningStructuresCommand;
        #endregion

        #region fields
        private List<PlanTargetsModel> _planTargets = new List<PlanTargetsModel> { };
        List<PrescriptionModel> _prescriptions = new List<PrescriptionModel> { };
        #endregion

        public MainViewModel(List<string> args)
        {
            Initialize();
        }

        public void Initialize()
        {
            FlashMarginVisible = Visibility.Hidden;
            NotifySetTargetsCommand = new DelegateCommand(SetTargets);
            _setTargetsVM = new SetTargetsViewModel(NotifySetTargetsCommand);
            SpecifyTargets = new SpecifyTargetsView { DataContext = _setTargetsVM };
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            _tsGenerationVM = new TSGenerationViewModel();
            TSGeneration = new TSGenerationView { DataContext = _tsGenerationVM };
            StructureTuningTabBackground = System.Windows.Media.Brushes.LightGray;
            TSManipulation = new TSManipulationView { DataContext = new TSManipulationView() };
            TSManipulationTabBackground = System.Windows.Media.Brushes.LightGray;
            BeamPlacement = new BeamPlacementView { DataContext = new BeamPlacementViewModel(PlanType.VMAT_TBI) };
            OptimizationSetup = new OptimizationSetupView { DataContext = new OptimizationSetupViewModel() };
            PlanPreparation = new PlanPreparationView { DataContext = new PlanPreparationViewModel() };
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel() };
            QuickStartGuideCommand = new DelegateCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new DelegateCommand(LaunchHelpGuide);
            PTVMarginInfoCommand = new DelegateCommand(ShowPTVMarginInfo);
            PlanTemplates = new ObservableCollection<TBIAutoPlanTemplate>() { new TBIAutoPlanTemplate("--select--") };
            LoadPlanTemplates();
        }

        private void SetTargets()
        {
            if(VerifyTargetsIntegrity(_setTargetsVM.PlanTargets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(_setTargetsVM.PlanTargets, _dosePerFraction, _numberOfFractions, _planTotalDose);
            if(!_prescriptions.Any()) return;
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.ForestGreen;
            StructureTuningTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            TSManipulationTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }

        private bool VerifyTargetsIntegrity(List<PlanTargetsModel> parsedTargets)
        {
            //verify selected targets are APPROVED
            //for tbi, we only want to make there is one plan (not configured for sequential boosts)
            if (!parsedTargets.Any()) return true;
            if (parsedTargets.Select(x => x.PlanId).Distinct().Count() > 1)
            {
                Logger.GetInstance().LogError($"Error! Multiple plan Ids entered! This script is only configured to auto-plan one TBI plan!");
                return true;
            }
            return false;
        }

        private void LaunchQuickStartGuide()
        {
            MessageBox.Show("test");
        }

        private void LaunchHelpGuide()
        {
            MessageBox.Show("test");
        }

        private void ShowPTVMarginInfo()
        {
            MessageBox.Show("test");
        }

        private void ResetRxDose()
        {
            if (NumberOfFractions > 0 && DosePerFraction > 0)
            {
                //double priorTotalDose = PlanTotalDose;
                PlanTotalDose = DosePerFraction * NumberOfFractions;
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

        private void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;

            DosePerFraction = SelectedTemplate.InitialRxDosePerFx;
            NumberOfFractions = SelectedTemplate.InitialRxNumberOfFractions;
            _setTargetsVM.AutoPlanTemplateSelectionChaged(_selectedTemplate);
            _tsGenerationVM.AutoPlanTemplateSelectionChaged(_selectedTemplate);
            //PlanObjectives.Clear();
            //foreach (PlanObjectiveModel itr in template.PlanObjectives)
            //{
            //    PlanObjectives.Add(new PlanObjectiveModel(itr));
            //}
            //OptimizationConstraints.Clear();
            //foreach (OptimizationConstraintModel itr in template.InitialOptimizationConstraints)
            //{
            //    OptimizationConstraints.Add(new OptimizationConstraintModel(itr));
            //}
        }

        private void UpdateUseFlash()
        {
            if (UseFlash) FlashMarginVisible = Visibility.Visible;
            else FlashMarginVisible = Visibility.Hidden;
        }

        private bool LoadPlanTemplates()
        {
            int count = 1;
            try
            {
                foreach (string itr in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\templates\\TBI\\", "*.ini").OrderBy(x => x))
                {
                    PlanTemplates.Add(ConfigurationHelper.ReadTBITemplatePlan(itr, count++));
                }

            }
            catch (Exception e)
            {
                Logger.GetInstance().LogError($"Error could not load plan template file because: {e.Message}");
                Logger.GetInstance().LogError(e.StackTrace, true);
                return true;
            }
            return false;
        }
    }
}
