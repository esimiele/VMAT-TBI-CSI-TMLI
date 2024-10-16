using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using CTStitcher.ViewModels;
using CTStitcher.Views;
using TMLIAutoPlanner.Settings;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.UIHelpers;
using System.Reflection;
using AutoPlannerHelpers.Enums;
using TMLIAutoPlanner.Core;

namespace TMLIAutoPlanner.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public ObservableCollection<TMLIAutoPlanTemplate> PlanTemplates { get; set; }

        #region properties
        private string _patientMRN;
        private string _structureSetId;
        private double _dosePerFraction;
        private int _numberOfFractions;
        private double _planTotalDose;
        private TMLIAutoPlanTemplate _selectedTemplate;
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

        public TMLIAutoPlanTemplate SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { SetProperty(ref _selectedTemplate, value); UpdateUIWithSelectedPlanTemplate(); }
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
        private RingGenerationViewModel _ringGenerationVM;
        private object _ringGeneration;
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

        public object RingGeneration
        {
            get { return _ringGeneration; }
            set { SetProperty(ref _ringGeneration, value); }
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
        private List<string> _structureIdsPostUnion;
        private string _generalConfigurationFile = string.Empty;
        #endregion

        public MainViewModel(string[] args)
        {
            if (args.Any()) EclipseContextHelper.GenerateEclipseContext(args.ToList());
            InitializeUI();
        }

        public void InitializeUI()
        {
            //_structureIdsPostUnion = StructureTuningHelper.GenerateStructureIdListPostUnion();

            _structureIdsPostUnion = new List<string> { "PTV_Body", "PTV_TMLI"};
            _stitcherViewModel = new CTStitcherViewModel();
            StitchCT = new CTStitcherView { DataContext = _stitcherViewModel };

            NotifySetTargetsCommand = new DelegateCommand(SetTargets);
            _setTargetsVM = new SetTargetsViewModel(NotifySetTargetsCommand);
            SpecifyTargets = new SpecifyTargetsView { DataContext = _setTargetsVM };
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;

            _tsGenerationVM = new TSGenerationViewModel();
            TSGeneration = new TSGenerationView { DataContext = _tsGenerationVM };
            StructureTuningTabBackground = System.Windows.Media.Brushes.LightGray;

            _ringGenerationVM = new RingGenerationViewModel(_structureIdsPostUnion);
            RingGeneration = new RingGenerationView { DataContext = _ringGenerationVM };

            NotifyGenerateManipulateTuningStructuresCommand = new DelegateCommand(PerformTSStructureGenerationManipulation);
            _tsManipulationVM = new TSManipulationViewModel(NotifyGenerateManipulateTuningStructuresCommand, _structureIdsPostUnion);
            TSManipulation = new TSManipulationView { DataContext = _tsManipulationVM };
            TSManipulationTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyBeamsPlacedCommand = new DelegateCommand(GeneratePlansAndPlaceBeams);
            _beamPlacementVM = new BeamPlacementViewModel(NotifyBeamsPlacedCommand, PlanType.VMAT_TMLI, TMLIAutoPlannerSettings.AvailableLinacs, TMLIAutoPlannerSettings.AvailableEnergies);
            BeamPlacement = new BeamPlacementView { DataContext = _beamPlacementVM };
            BeamPlacementTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyAssignOptimizationConstraintsCommand = new DelegateCommand(AssignOptimizationConstraints);
            _optimizationSetupVM = new OptimizationSetupViewModel(_structureIdsPostUnion, NotifyAssignOptimizationConstraintsCommand);
            OptimizationSetup = new OptimizationSetupView { DataContext = _optimizationSetupVM };
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.LightGray;

            NotifyPreparePlanForTreatmentCommand = new DelegateCommand(PreparePlanForTreatment);
            _planPrepVM = new PlanPreparationViewModel(NotifyPreparePlanForTreatmentCommand);
            PlanPreparation = new PlanPreparationView { DataContext = _planPrepVM };

            QuickStartGuideCommand = new DelegateCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new DelegateCommand(LaunchHelpGuide);
            PTVMarginInfoCommand = new DelegateCommand(ShowPTVMarginInfo);

            PlanTemplates = new ObservableCollection<TMLIAutoPlanTemplate>() { new TMLIAutoPlanTemplate("--select--") };
            LoadPlanTemplates();

            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
            WindowClosingCommand = new DelegateCommand(WindowClosing);
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
            if (VerifyTargetsIntegrity(_setTargetsVM.PlanTargets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(_setTargetsVM.PlanTargets, _dosePerFraction, _numberOfFractions, _planTotalDose);
            if (!_prescriptions.Any()) return;
            _optimizationSetupVM.UpdatePrescriptionList(_prescriptions);
            if (!ReferenceEquals(_selectedTemplate, null)) _optimizationSetupVM.UpdateUIWithSelectedPlanTemplate(_selectedTemplate);
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
            List<TSRingStructureModel> rings = _ringGenerationVM.RequestedRingStructures.ToList();
            TSGenerationManipulation_TMLI generateTS = new TSGenerationManipulation_TMLI(tsGeneration,
                                                                                       tsManipulations,
                                                                                       rings,
                                                                                       _prescriptions);

            EclipseContext.GetInstance().Patient.BeginModifications();
            bool failed = generateTS.Execute();
            Logger.GetInstance().AppendLogOutput("TS Generation and manipulation output:", generateTS.GetLogOutput());
            if (failed) return;

            //does the structure sparing list need to be updated? This occurs when structures the user elected to spare with option of 'Mean Dose < Rx Dose' are high resolution. Since Eclipse can't perform
            //boolean operations on structures of two different resolutions, code was added to the generateTS class to automatically convert these structures to low resolution with the name of
            // '<original structure Id>_lowRes'. When these structures are converted to low resolution, the updateSparingList flag in the generateTS class is set to true to tell this class that the 
            //structure sparing list needs to be updated with the new low resolution structures.
            if (generateTS.DoesTSManipulationListRequireUpdating)
            {
                _tsManipulationVM.UpdateTSManipulationList(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id), generateTS.TSManipulationList);
            }
            _planIsocenters = generateTS.PlanIsocentersList;

            _beamPlacementVM.PopulatePlanIsocenterList(_planIsocenters);
            UpdateOptimizationConstraintsWithRings(rings);
            UpdateOptimizationConstraintsWithTSTargets(generateTS.PlanTargets);

            StructureTuningTabBackground = System.Windows.Media.Brushes.ForestGreen;
            TSManipulationTabBackground = System.Windows.Media.Brushes.ForestGreen;
            BeamPlacementTabBackground = System.Windows.Media.Brushes.ForestGreen;

            Logger.GetInstance().AddedStructures = generateTS.AddedStructureIds;
            Logger.GetInstance().StructureManipulations = tsManipulations;
            Logger.GetInstance().TSTargets = generateTS.PlanTargets.SelectMany(x => x.Targets).ToDictionary(x => x.TargetId, x => x.TsTargetId);
            Logger.GetInstance().NormalizationVolumes = generateTS.NormalizationVolumes;
            Logger.GetInstance().PlanIsocenters = generateTS.PlanIsocentersList;

            //_planIsocenters.Add(new PlanIsocenterModel("test", new List<IsocenterModel> { new IsocenterModel("1", 2, BeamType.VMAT), new IsocenterModel("2", 3, BeamType.VMAT), new IsocenterModel("3", 4, BeamType.VMAT) }));
            //_planIsocenters.Add(new PlanIsocenterModel("doubleTest", new List<IsocenterModel> { new IsocenterModel("4", 2, BeamType.APPA) }));
        }

        public void UpdateOptimizationConstraintsWithTSTargets(List<PlanTargetsModel> planTargets)
        {
            //update optimization constraint list to replace target constraints with ts targets
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                foreach (PlanTargetsModel itr in planTargets)
                {
                    foreach (TargetModel target in itr.Targets)
                    {
                        if (_selectedTemplate.InitialOptimizationConstraints.Any(x => string.Equals(x.StructureId, target.TargetId)))
                        {
                            _selectedTemplate.InitialOptimizationConstraints.First(x => string.Equals(x.StructureId, target.TargetId)).StructureId = target.TsTargetId;
                        }
                    }
                }
            }
        }

        public void UpdateOptimizationConstraintsWithRings(List<TSRingStructureModel> rings)
        {
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                foreach (TSRingStructureModel itr in rings)
                {
                    if (_prescriptions.Any(x => string.Equals(x.TargetId, itr.TargetId)))
                    {
                        _selectedTemplate.InitialOptimizationConstraints.Insert(0, new OptimizationConstraintModel(itr.RingId, OptimizationObjectiveType.Upper, itr.DoseLevel, Units.cGy, 0.0, 80));
                    }
                }
            }
        }
        #endregion

        #region beam placement
        private void GeneratePlansAndPlaceBeams()
        {
            _planIsocenters = _beamPlacementVM.PlanIsocenterList.ToList();
            GeneratePlansAndPlaceBeams_TMLI placeBeams = new GeneratePlansAndPlaceBeams_TMLI();
            bool failed = placeBeams.Execute();
            Logger.GetInstance().AppendLogOutput("Generate plans and place beams output:", placeBeams.GetLogOutput());
            if (failed) return;
            if (placeBeams.VMATPlans.Any()) EclipseContext.GetInstance().VMATPlans = placeBeams.VMATPlans;
            UpdateOptimizationConstraintsWithTSJunctions(placeBeams.FieldJunctions);
            if (!ReferenceEquals(_selectedTemplate, null)) _optimizationSetupVM.UpdateUIWithSelectedPlanTemplate(_selectedTemplate);
            BeamPlacementTabBackground = System.Windows.Media.Brushes.ForestGreen;
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }

        public void UpdateOptimizationConstraintsWithTSJunctions(List<PlanFieldJunctionModel> junctions)
        {
            //update optimization constraint list to replace target constraints with ts targets
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                foreach (PlanFieldJunctionModel itr in junctions)
                {
                    double dose = _prescriptions.Last().CumulativeDoseToTarget;
                    foreach (FieldJunctionModel jnx in itr.FieldJunctions)
                    {
                        _selectedTemplate.InitialOptimizationConstraints.Insert(0, new OptimizationConstraintModel(jnx.JunctionStructure.Id, OptimizationObjectiveType.Lower, dose, Units.cGy, 100.0, 100));
                        _selectedTemplate.InitialOptimizationConstraints.Insert(1, new OptimizationConstraintModel(jnx.JunctionStructure.Id, OptimizationObjectiveType.Upper, 1.02 * dose, Units.cGy, 0.0, 100));
                    }
                }
            }
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
            ExternalPlanSetup thePlan = PlanPrepHelper.RetrieveVMATPlan(EclipseContext.GetInstance().Patient, Logger.GetInstance().LogPath, TMLIAutoPlannerSettings.CourseId);
            if (ReferenceEquals(thePlan, null)) return;
            EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { thePlan };

            if (GenerateShiftNote()) return;
            if (SeparatePlans()) return;
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

            //separate the plans
            EclipseContext.GetInstance().Patient.BeginModifications();
            PreparePlansForTreatment_TMLI planPrep = new PreparePlansForTreatment_TMLI();
            bool result = planPrep.Execute();
            Logger.GetInstance().AppendLogOutput("Plan preparation:", planPrep.GetLogOutput());
            if (result) return true;

            //inform the user it's done
            StringBuilder sb = new StringBuilder();
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
            _ringGenerationVM.AutoPlanTemplateSelectionChaged(_selectedTemplate);
            _tsManipulationVM.AutoPlanTemplateSelectionChaged(_selectedTemplate);
        }

        #region script configuration
        private void LoadScriptConfigurationSettings(string file)
        {
            //encapsulate everything in a try-catch statment so I can be a bit lazier about data checking of the configuration settings (i.e., if a parameter or value is bad the script won't crash)
            try
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    //setup temporary vectors to hold the parsed data
                    string line;
                    List<string> linac_temp = new List<string> { };
                    List<string> energy_temp = new List<string> { };
                    List<VRect<double>> jawPos_temp = new List<VRect<double>> { };
                    List<RequestedTSManipulationModel> defaultTSManipulations_temp = new List<RequestedTSManipulationModel> { };
                    List<RequestedTSStructureModel> defaultTSstructures_temp = new List<RequestedTSStructureModel> { };

                    while ((line = reader.ReadLine()) != null)
                    {
                        //this line contains useful information (i.e., it is not a comment)
                        if (!string.IsNullOrEmpty(line) && line.Substring(0, 1) != "%")
                        {
                            //useful info on this line in the format of parameter=value
                            //parse parameter and value separately using '=' as the delimeter
                            if (line.Contains("="))
                            {
                                //default configuration parameters
                                string parameter = line.Substring(0, line.IndexOf("="));
                                string value = line.Substring(line.IndexOf("=") + 1, line.Length - line.IndexOf("=") - 1);
                                //check if it's a double value
                                if (parameter == "close progress windows on finish")
                                {
                                    if (!string.IsNullOrEmpty(value)) TMLIAutoPlannerSettings.CloseProgressWindowOnFinish = bool.Parse(value);
                                }
                                else if (parameter == "beams per iso")
                                {
                                    //parse the default requested number of beams per isocenter
                                    line = ConfigurationHelper.CropLine(line, "{");
                                    List<int> b = new List<int> { };
                                    //second character should not be the end brace (indicates the last element in the array)
                                    while (line.Substring(1, 1) != "}")
                                    {
                                        b.Add(int.Parse(line.Substring(0, line.IndexOf(","))));
                                        line = ConfigurationHelper.CropLine(line, ",");
                                    }
                                    b.Add(int.Parse(line.Substring(0, line.IndexOf("}"))));
                                    TMLIAutoPlannerSettings.BeamsPerIsocenter.Clear();
                                    TMLIAutoPlannerSettings.BeamsPerIsocenter.AddRange(b);
                                }
                                else if (parameter == "collimator rotations")
                                {
                                    //parse the default requested number of beams per isocenter
                                    line = ConfigurationHelper.CropLine(line, "{");
                                    List<double> c = new List<double> { };
                                    //second character should not be the end brace (indicates the last element in the array)
                                    while (line.Contains(","))
                                    {
                                        c.Add(double.Parse(line.Substring(0, line.IndexOf(","))));
                                        line = ConfigurationHelper.CropLine(line, ",");
                                    }
                                    c.Add(double.Parse(line.Substring(0, line.IndexOf("}"))));
                                    TMLIAutoPlannerSettings.CollimatorRotations.Clear();
                                    TMLIAutoPlannerSettings.CollimatorRotations.AddRange(c);
                                }
                                else if (parameter == "check couch collision")
                                {
                                    if (!string.IsNullOrEmpty(value)) TMLIAutoPlannerSettings.CheckTTCollision = bool.Parse(value);
                                }
                                else if (parameter == "show CT stitcher tab") TMLIAutoPlannerSettings.ShowStitchCTTab = bool.Parse(value);
                                else if (parameter == "course Id") TMLIAutoPlannerSettings.CourseId = value;
                                else if (parameter == "use GPU for dose calculation") TMLIAutoPlannerSettings.UseGPUForDosecalculation = bool.Parse(value);
                                else if (parameter == "use GPU for optimization") TMLIAutoPlannerSettings.UseGPUForOptimization = bool.Parse(value);
                                else if (parameter == "MR level restart") TMLIAutoPlannerSettings.MRLevelRestart = value;
                                //other parameters that should be updated
                                else if (parameter == "calculation model") { if (value != "") TMLIAutoPlannerSettings.DoseCalculationAlgorithm = value; }
                                else if (parameter == "optimization model") { if (value != "") TMLIAutoPlannerSettings.OptimizationAlorithm = value; }
                                else if (parameter == "contour field overlap") { if (value != "") TMLIAutoPlannerSettings.ContourFieldOverlap = bool.Parse(value); }
                                else if (parameter == "contour field overlap margin") { if (value != "") TMLIAutoPlannerSettings.ContourFieldOverlapMarginInCM = double.Parse(value); }
                            }
                            else if (line.Contains("add default TS manipulation")) defaultTSManipulations_temp.Add(ConfigurationHelper.ParseTSManipulation(line));
                            else if (line.Contains("create default TS")) defaultTSstructures_temp.Add(ConfigurationHelper.ParseCreateTS(line));
                            else if (line.Contains("add linac"))
                            {
                                //parse the linacs that should be added. One entry per line
                                line = ConfigurationHelper.CropLine(line, "{");
                                TMLIAutoPlannerSettings.AvailableLinacs.Add(line.Substring(0, line.IndexOf("}")));
                            }
                            else if (line.Contains("add beam energy"))
                            {
                                //parse the photon energies that should be added. One entry per line
                                line = ConfigurationHelper.CropLine(line, "{");
                                TMLIAutoPlannerSettings.AvailableEnergies.Add(line.Substring(0, line.IndexOf("}")));
                            }
                            else if (line.Contains("add jaw position"))
                            {
                                //parse the default requested number of beams per isocenter
                                VRect<double> parsedPositions = ConfigurationHelper.ParseJawPositions(line);
                                if (parsedPositions.X1 != parsedPositions.X2) jawPos_temp.Add(parsedPositions);
                            }
                        }
                    }
                    //anything that is an array needs to be updated AFTER the while loop.
                    if (jawPos_temp.Count == 4)
                    {
                        TMLIAutoPlannerSettings.JawPositions.Clear();
                        TMLIAutoPlannerSettings.JawPositions = new List<VRect<double>>(jawPos_temp);
                    }
                }
            }
            //let the user know if the data parsing failed
            catch (Exception e)
            {
                Logger.GetInstance().LogError($"Error could not load configuration file because: {e.Message}\n\nAssuming default parameters");
                Logger.GetInstance().LogError(e.StackTrace, true);
                return;
            }
        }

        private bool LoadPlanTemplates()
        {
            int count = 1;
            try
            {
                foreach (string itr in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\templates\\TMLI\\", "*.ini").OrderBy(x => x))
                {
                    PlanTemplates.Add(ConfigurationHelper.ReadTMLITemplatePlan(itr, count++));
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
            sb.AppendLine($"Close progress windows on finish: {TMLIAutoPlannerSettings.CloseProgressWindowOnFinish}");
            sb.AppendLine("Default parameters:");
            sb.AppendLine($"Course Id: {TMLIAutoPlannerSettings.CourseId}");
            sb.AppendLine($"Check for potential couch collision: {TMLIAutoPlannerSettings.CheckTTCollision}");
            sb.AppendLine($"Contour field ovelap: {TMLIAutoPlannerSettings.ContourFieldOverlap}");
            sb.AppendLine($"Contour field overlap margin: {TMLIAutoPlannerSettings.ContourFieldOverlapMarginInCM} cm");
            sb.AppendLine("Available linacs:");
            foreach (string l in TMLIAutoPlannerSettings.AvailableLinacs) sb.AppendLine($"    {l}");
            sb.AppendLine("Available photon energies:");
            foreach (string e in TMLIAutoPlannerSettings.AvailableEnergies) sb.AppendLine($"    {e}");
            sb.AppendLine($"Beams per isocenter: ");
            for (int i = 0; i < TMLIAutoPlannerSettings.BeamsPerIsocenter.Count; i++)
            {
                sb.Append($"{TMLIAutoPlannerSettings.BeamsPerIsocenter.ElementAt(i)}");
                if (i != TMLIAutoPlannerSettings.BeamsPerIsocenter.Count - 1) sb.Append(", ");
            }
            sb.AppendLine("");
            sb.AppendLine("Collimator rotation (deg) order: ");
            for (int i = 0; i < TMLIAutoPlannerSettings.CollimatorRotations.Count; i++)
            {
                sb.Append($"{TMLIAutoPlannerSettings.CollimatorRotations.ElementAt(i):0.0}");
                if (i != TMLIAutoPlannerSettings.CollimatorRotations.Count - 1) sb.Append(", ");
            }

            sb.AppendLine("");
            sb.AppendLine("Field jaw position (cm) order: ");
            sb.AppendLine(" (x1,y1,x2,y2)");
            foreach (VRect<double> j in TMLIAutoPlannerSettings.JawPositions) sb.AppendLine($"({j.X1 / 10:0.0},{j.Y1 / 10:0.0},{j.X2 / 10:0.0},{j.Y2 / 10:0.0})");
            sb.AppendLine($"Photon dose calculation model: {TMLIAutoPlannerSettings.DoseCalculationAlgorithm}");
            sb.AppendLine($"Use GPU for dose calculation: {TMLIAutoPlannerSettings.UseGPUForDosecalculation}");
            sb.AppendLine($"Photon optimization model: {TMLIAutoPlannerSettings.OptimizationAlorithm}");
            sb.AppendLine($"Use GPU for optimization: {TMLIAutoPlannerSettings.UseGPUForOptimization}");
            sb.AppendLine($"MR level restart at: {TMLIAutoPlannerSettings.MRLevelRestart}");

            if (PlanTemplates.Any()) sb.Append(ConfigurationUIHelper.PrintTMLIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
            return sb;
        }
        #endregion

        public void WindowClosing()
        {
            if (EclipseContext.GetInstance().IsInitialized)
            {
                ScriptClosingHelper.CloseApplication(false);
            }
        }
    }
}
