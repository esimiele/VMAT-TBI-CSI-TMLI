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
using TBIAutoPlanner.Core;
using AutoPlannerHelpers.Context;
using System.Text;
using AutoPlannerHelpers.UIHelpers;
using VMS.TPS.Common.Model.Types;
using TBIAutoPlanner.Settings;
using CTStitcher.Views;
using CTStitcher.ViewModels;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using VMS.TPS.Common.Model.API;
using AutoPlannerHelpers.Prompts;

namespace TBIAutoPlanner.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public ObservableCollection<TBIAutoPlanTemplate> PlanTemplates { get; set; }

        #region properties
        private string _patientMRN;
        private string _structureSetId;
        private double _dosePerFraction;
        private int _numberOfFractions;
        private double _planTotalDose;
        private TBIAutoPlanTemplate _selectedTemplate;
        private bool _useFlash;
        private Visibility _flashMarginVisible;
        private double _flashMargin;
        private double _ptvMarginFromBody;
        private System.Windows.Media.SolidColorBrush _specifyTargetsTabBackground;
        private System.Windows.Media.SolidColorBrush _structureTuningTabBackground;
        private System.Windows.Media.SolidColorBrush _tsManipulationTabBackground;

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

        private System.Windows.Media.SolidColorBrush _beamPlacementTabBackground;

        public System.Windows.Media.SolidColorBrush BeamPlacementTabBackground
        {
            get { return _beamPlacementTabBackground; }
            set { SetProperty(ref _beamPlacementTabBackground, value); }
        }

        private System.Windows.Media.SolidColorBrush _optimizationSetupTabBackground;

        public System.Windows.Media.SolidColorBrush OptimizationSetupTabBackground
        {
            get { return _optimizationSetupTabBackground; }
            set { SetProperty(ref _optimizationSetupTabBackground, value); }
        }

        #endregion

        #region view objects
        private CTStitcherViewModel _stitcherViewModel;
        private object _stitchCT;
        private SetTargetsViewModel _setTargetsVM;
        private object _specifyTargets;
        private TSGenerationViewModel _tsGenerationVM;
        private object _tsGeneration;
        private TSManipulationViewModel _tsManipulationVM;
        private object _tsManipulation;
        private BeamPlacementViewModel _beamPlacementVM;
        private object _beamPlacement;
        private OptimizationSetupViewModel _optimizationSetupVM;
        private object _optimizationSetup;
        private PlanPreparationViewModel _planPrepVM;
        private object _planPreparation;
        private object _scriptConfiguration;

        public object StitchCT
        {
            get { return _stitchCT; }
            set { SetProperty(ref _stitchCT, value); }
        }

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
        private DelegateCommand NotifyGenerateManipulateTuningStructuresCommand;
        private DelegateCommand NotifyBeamsPlacedCommand;
        private DelegateCommand NotifyAssignOptimizationConstraintsCommand;
        private DelegateCommand NotifyPreparePlanForTreatmentCommand;
        public DelegateCommand WindowClosingCommand { get; set; }
        #endregion

        #region fields
        private List<PrescriptionModel> _prescriptions = new List<PrescriptionModel> { };
        private List<PlanIsocenterModel> _planIsocenters = new List<PlanIsocenterModel> { };
        private string _generalConfigurationFile = string.Empty;
        #endregion

        public MainViewModel(List<string> args)
        {
            Initialize();
        }

        public void Initialize()
        {
            FlashMarginVisible = Visibility.Hidden;

            _stitcherViewModel = new CTStitcherViewModel();
            StitchCT = new CTStitcherView { DataContext = _stitcherViewModel };

            NotifySetTargetsCommand = new DelegateCommand(SetTargets);
            _setTargetsVM = new SetTargetsViewModel(NotifySetTargetsCommand);
            SpecifyTargets = new SpecifyTargetsView { DataContext = _setTargetsVM };
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;

            _tsGenerationVM = new TSGenerationViewModel();
            TSGeneration = new TSGenerationView { DataContext = _tsGenerationVM };
            StructureTuningTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyGenerateManipulateTuningStructuresCommand = new DelegateCommand(PerformTSStructureGenerationManipulation);
            _tsManipulationVM = new TSManipulationViewModel(NotifyGenerateManipulateTuningStructuresCommand, new List<string> { "Lungs", "Liver", "Kidneys"});
            TSManipulation = new TSManipulationView { DataContext = _tsManipulationVM };
            TSManipulationTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyBeamsPlacedCommand = new DelegateCommand(GeneratePlansAndPlaceBeams);
            _beamPlacementVM = new BeamPlacementViewModel(NotifyBeamsPlacedCommand, PlanType.VMAT_TBI, new List<string> { "LA16"}, new List<string> { "6X", "10X"});
            BeamPlacement = new BeamPlacementView { DataContext = _beamPlacementVM };
            BeamPlacementTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyAssignOptimizationConstraintsCommand = new DelegateCommand(AssignOptimizationConstraints);
            _optimizationSetupVM = new OptimizationSetupViewModel(new List<string> { "Lungs", "Liver", "Kidneys" }, NotifyAssignOptimizationConstraintsCommand);
            OptimizationSetup = new OptimizationSetupView { DataContext = _optimizationSetupVM };
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyPreparePlanForTreatmentCommand = new DelegateCommand(PreparePlanForTreatment);
            _planPrepVM = new PlanPreparationViewModel(NotifyPreparePlanForTreatmentCommand);
            PlanPreparation = new PlanPreparationView { DataContext = _planPrepVM };

            QuickStartGuideCommand = new DelegateCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new DelegateCommand(LaunchHelpGuide);
            PTVMarginInfoCommand = new DelegateCommand(ShowPTVMarginInfo);

            PlanTemplates = new ObservableCollection<TBIAutoPlanTemplate>() { new TBIAutoPlanTemplate("--select--") };
            LoadPlanTemplates();

            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
            WindowClosingCommand = new DelegateCommand(WindowClosing);

            _planIsocenters.Add(new PlanIsocenterModel("test", new List<IsocenterModel> { new IsocenterModel("1", 2, BeamType.VMAT), new IsocenterModel("2", 3, BeamType.VMAT), new IsocenterModel("3", 4, BeamType.VMAT) }));
            _planIsocenters.Add(new PlanIsocenterModel("doubleTest", new List<IsocenterModel> { new IsocenterModel("4", 2, BeamType.APPA) }));
            _beamPlacementVM.PopulatePlanIsocenterList(_planIsocenters);
        }

        #region information and help guides
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
        #endregion

        #region specify targets
        private void SetTargets()
        {
            if(VerifyTargetsIntegrity(_setTargetsVM.PlanTargets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(_setTargetsVM.PlanTargets, _dosePerFraction, _numberOfFractions, _planTotalDose);
            if(!_prescriptions.Any()) return;
            _optimizationSetupVM.UpdatePrescriptionList(_prescriptions);
            if(!ReferenceEquals(_selectedTemplate, null)) _optimizationSetupVM.UpdateUIWithSelectedPlanTemplate(_selectedTemplate);
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
        #endregion

        #region TS generation and manipulation
        private void PerformTSStructureGenerationManipulation()
        {
            List<RequestedTSStructureModel> tsGeneration = _tsGenerationVM.RequestedTuningStructures.ToList();
            List<RequestedTSManipulationModel> tsManipulations = _tsManipulationVM.RequestedTSManipulations.ToList();
            TSGenerationManipulation_TBI generateTS = new TSGenerationManipulation_TBI(tsGeneration, 
                                                                                       tsManipulations, 
                                                                                       _prescriptions);

            EclipseContext.GetInstance().Patient.BeginModifications();
            bool failed = generateTS.Execute();
            Logger.GetInstance().AppendLogOutput("TS Generation and manipulation output:", generateTS.GetLogOutput());

            if (failed) return;
            StructureTuningTabBackground = System.Windows.Media.Brushes.ForestGreen;
            TSManipulationTabBackground = System.Windows.Media.Brushes.ForestGreen;
            BeamPlacementTabBackground = System.Windows.Media.Brushes.ForestGreen;

            Logger.GetInstance().AddedStructures = generateTS.AddedStructureIds;
            Logger.GetInstance().StructureManipulations = tsManipulations;
            Logger.GetInstance().TSTargets = generateTS.PlanTargets.SelectMany(x => x.Targets).ToDictionary(x => x.TargetId, x => x.TsTargetId);
            Logger.GetInstance().NormalizationVolumes = generateTS.NormalizationVolumes;
            Logger.GetInstance().PlanIsocenters = generateTS.PlanIsocentersList;

            _planIsocenters.Add(new PlanIsocenterModel("test", new List<IsocenterModel> { new IsocenterModel("1", 2, BeamType.VMAT), new IsocenterModel("2", 3, BeamType.VMAT), new IsocenterModel("3", 4, BeamType.VMAT) }));
            _planIsocenters.Add(new PlanIsocenterModel("doubleTest", new List<IsocenterModel> { new IsocenterModel("4", 2, BeamType.APPA) }));
            _beamPlacementVM.PopulatePlanIsocenterList(_planIsocenters);
        }
        #endregion

        #region beam placement
        private void GeneratePlansAndPlaceBeams()
        {
            _planIsocenters = _beamPlacementVM.PlanIsocenterList.ToList();
            return;
            GeneratePlansAndPlaceBeams_TBI placeBeams = new GeneratePlansAndPlaceBeams_TBI();
            bool failed = placeBeams.Execute();
            Logger.GetInstance().AppendLogOutput("Generate plans and place beams output:", placeBeams.GetLogOutput());
            if (failed) return;
            if (placeBeams.VMATPlans.Any()) EclipseContext.GetInstance().VMATPlans = placeBeams.VMATPlans;
            BeamPlacementTabBackground = System.Windows.Media.Brushes.ForestGreen;
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }
        #endregion

        #region opimization parameters
        public void AssignOptimizationConstraints()
        {
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.ForestGreen;
        }
        #endregion

        #region prepare for treatment
        public void PreparePlanForTreatment()
        {
            ExternalPlanSetup thePlan = PlanPrepHelper.RetrieveVMATPlan(EclipseContext.GetInstance().Patient, Logger.GetInstance().LogPath, TBIAutoPlannerSettings.CourseId);
            if (ReferenceEquals(thePlan, null)) return;
            EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { thePlan };

            if (GenerateShiftNote()) return;
            if(SeparatePlans()) return;
            Logger.GetInstance().OpType = ScriptOperationType.PlanPrep;
            _planPrepVM.UpdateUIAllPrepItemsCompleted();
        }

        public bool GenerateShiftNote()
        {
            List<ExternalPlanSetup> appaPlans = new List<ExternalPlanSetup> { };
            if (EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("legs")))
            {
                appaPlans = EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("legs")).ToList();
                if (appaPlans.Any(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"The AP/PA plan {appaPlans.First(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine).Id} is NOT in the FFS orientation!");
                    sb.AppendLine("THE COUCH SHIFTS FOR THESE PLANS WILL NOT BE ACCURATE! Please fix and try again!");
                    Logger.GetInstance().LogError(sb.ToString());
                    return true;
                }
            }

            Clipboard.SetText(PlanPrepHelper.GetTBIShiftNote(EclipseContext.GetInstance().VMATPlans.First(), appaPlans).ToString());
            return false;
        }
        public bool SeparatePlans()
        {
            //The shift note has to be retrieved first! Otherwise, we don't have instances of the plan objects
            if (!EclipseContext.GetInstance().VMATPlans.Any() || EclipseContext.GetInstance().VMATPlans.Count > 1)
            {
                Logger.GetInstance().LogError("Please generate the shift note before separating the plans!");
                return true;
            }
            ExternalPlanSetup thePlan = EclipseContext.GetInstance().VMATPlans.First();

            if (!thePlan.Beams.Any(x => x.IsSetupField))
            {
                ConfirmPrompt CUI = new ConfirmPrompt($"I didn't find any setup fields in the {thePlan.Id}." + Environment.NewLine + Environment.NewLine + "Are you sure you want to continue?!");
                CUI.ShowDialog();
                if (!CUI.GetSelection()) return true;
            }

            bool removeFlash = false;
            StringBuilder sb = new StringBuilder();
            //check if flash was used in the plan. If so, ask the user if they want to remove these structures as part of cleanup
            if (PlanPrepHelper.CheckForFlash(thePlan.StructureSet))
            {
                sb.AppendLine("I found some structures in the structure set for generating flash.");
                sb.AppendLine("Should I remove them?");
                sb.AppendLine("(NOTE: this will require dose recalculation for all plans using this structure set!)");
                ConfirmPrompt CP = new ConfirmPrompt(sb.ToString(), "YES", "NO");
                CP.ShowDialog();
                if (CP.GetSelection()) removeFlash = true;
            }

            //separate the plans
            EclipseContext.GetInstance().Patient.BeginModifications();
            PreparePlansForTreatment_TBI planPrep = new PreparePlansForTreatment_TBI(removeFlash);
            bool result = planPrep.Execute();
            Logger.GetInstance().AppendLogOutput("Plan preparation:", planPrep.GetLogOutput());
            if (result) return true;

            //inform the user it's done
            sb.Clear();
            sb.AppendLine("Original plan(s) have been separated!");
            sb.AppendLine("Be sure to set the target volume and primary reference point!");
            if (thePlan.Beams.Any(x => x.IsSetupField))
            {
                sb.AppendLine("Also reset the isocenter position of the setup fields!");
            }
            sb.AppendLine("");
            sb.AppendLine("Isocenter shifts have been copied to the clipboard!");
            sb.AppendLine("Paste them into the journal note!");
            MessageBox.Show(sb.ToString());

            return false;
        }
        #endregion

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
            _tsManipulationVM.AutoPlanTemplateSelectionChaged(_selectedTemplate);
            _optimizationSetupVM.UpdateUIWithSelectedPlanTemplate(_selectedTemplate);
        }

        private void UpdateUseFlash()
        {
            if (UseFlash) FlashMarginVisible = Visibility.Visible;
            else FlashMarginVisible = Visibility.Hidden;
        }

        #region script configuration
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

        private StringBuilder BuildScriptConfigurationInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{DateTime.Now}");
            if (!string.IsNullOrEmpty(_generalConfigurationFile)) sb.AppendLine($"Configuration file: {_generalConfigurationFile}");
            else sb.AppendLine("Configuration file: none");
            sb.AppendLine($"Documentation path: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\documentation\\"}");
            sb.AppendLine($"Log file path: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs\\"}");
            sb.AppendLine($"Close progress windows on finish: {TBIAutoPlannerSettings.CloseProgressWindowOnFinish}");
            sb.AppendLine("Default parameters:");
            sb.AppendLine($"Course Id: {TBIAutoPlannerSettings.CourseId}");
            sb.AppendLine($"Check for potential couch collision: {TBIAutoPlannerSettings.CheckTTCollision}");
            sb.AppendLine($"Contour field ovelap: {TBIAutoPlannerSettings.ContourFieldOverlap}");
            sb.AppendLine($"Contour field overlap margin: {TBIAutoPlannerSettings.ContourFieldOverlapMarginInCM} cm");
            sb.AppendLine("Available linacs:");
            foreach (string l in TBIAutoPlannerSettings.AvailableLinacs) sb.AppendLine($"    {l}");
            sb.AppendLine("Available photon energies:");
            foreach (string e in TBIAutoPlannerSettings.AvailableEnergies) sb.AppendLine($"    {e}");
            sb.AppendLine($"Beams per isocenter: ");
            for (int i = 0; i < TBIAutoPlannerSettings.BeamsPerIsocenter.Length; i++)
            {
                sb.Append($"{TBIAutoPlannerSettings.BeamsPerIsocenter.ElementAt(i)}");
                if (i != TBIAutoPlannerSettings.BeamsPerIsocenter.Length - 1) sb.Append(", ");
            }
            sb.AppendLine("");
            sb.AppendLine("Collimator rotation (deg) order: ");
            for (int i = 0; i < TBIAutoPlannerSettings.CollimatorRotations.Length; i++)
            {
                sb.Append($"{TBIAutoPlannerSettings.CollimatorRotations.ElementAt(i):0.0}");
                if (i != TBIAutoPlannerSettings.CollimatorRotations.Length - 1) sb.Append(", ");
            }
            sb.AppendLine("");
            sb.AppendLine($"Include flash by default: {TBIAutoPlannerSettings.UseFlash}");
            sb.AppendLine($"Flash margin: {TBIAutoPlannerSettings.FlashMarginInCM} cm");
            sb.AppendLine($"Target inner margin: {TBIAutoPlannerSettings.PTVInnerMarginFromBodyInCM} cm");

            sb.AppendLine("");
            sb.AppendLine("Field jaw position (cm) order: ");
            sb.AppendLine(" (x1,y1,x2,y2)");
            foreach (VRect<double> j in TBIAutoPlannerSettings.JawPositions) sb.AppendLine($"({j.X1 / 10:0.0},{j.Y1 / 10:0.0},{j.X2 / 10:0.0},{j.Y2 / 10:0.0})");
            sb.AppendLine($"Photon dose calculation model: {TBIAutoPlannerSettings.DoseCalculationAlgorithm}");
            sb.AppendLine($"Use GPU for dose calculation: {TBIAutoPlannerSettings.UseGPUForDosecalculation}");
            sb.AppendLine($"Photon optimization model: {TBIAutoPlannerSettings.OptimizationAlorithm}");
            sb.AppendLine($"Use GPU for optimization: {TBIAutoPlannerSettings.UseGPUForOptimization}");
            sb.AppendLine($"MR level restart at: {TBIAutoPlannerSettings.MRLevelRestart}");

            if (PlanTemplates.Any()) sb.Append(ConfigurationUIHelper.PrintTBIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
            return sb;
        }
        #endregion

        public void WindowClosing()
        {
            if(EclipseContext.GetInstance().IsInitialized)
            {
                ScriptClosingHelper.CloseApplication(false);
            }
        }
    }
}
