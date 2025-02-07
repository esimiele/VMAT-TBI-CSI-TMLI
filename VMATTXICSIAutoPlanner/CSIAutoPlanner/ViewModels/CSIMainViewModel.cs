using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using AutoPlannerHelpers.Models;
using CSIAutoPlanner.Core;
using AutoPlannerHelpers.Context;
using CSIAutoPlanner.Settings;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.PlanTemplateModels;
using System.Windows;
using System.IO;
using VMS.TPS.Common.Model.Types;
using AutoPlannerHelpers.Helpers;
using System.Reflection;
using AutoPlannerHelpers.UIHelpers;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.EnumTypeHelpers;
using AutoPlannerHelpers.BaseViewModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace CSIAutoPlanner.ViewModels
{
    internal class CSIMainViewModel : BaseViewModel
    {
        #region properties
        private double _boostDosePerFraction;
        private int _boostNumberOfFractions;
        private double _boostPlanTotalDose;
        private System.Windows.Media.SolidColorBrush _prepForTargetsBackground;
        private System.Windows.Media.SolidColorBrush _setTargetsTabBackground;
        
        public double BoostDosePerFraction
        {
            get { return _boostDosePerFraction; }
            set { SetProperty(ref _boostDosePerFraction, value); ResetRxDose(); }
        }

        public int BoostNumberOfFractions
        {
            get { return _boostNumberOfFractions; }
            set { SetProperty(ref _boostNumberOfFractions, value); ResetRxDose(); }
        }

        public double BoostPlanTotalDose
        {
            get { return _boostPlanTotalDose; }
            set { SetProperty(ref _boostPlanTotalDose, value); }
        }

        public System.Windows.Media.SolidColorBrush PrepForTargetsBackground
        {
            get { return _prepForTargetsBackground; }
            set { SetProperty(ref _prepForTargetsBackground, value); }
        }

        public System.Windows.Media.SolidColorBrush SetTargetsTabBackground
        {
            get { return _setTargetsTabBackground; }
            set { SetProperty(ref _setTargetsTabBackground, value); }
        }
        #endregion

        #region view objects
        private CTExportViewModel _ctExportViewModel;
        private object _exportCT;
        private PrepForTargetsViewModel _prepForTargetsVM;
        private object _prepForTargets;
        private StructureCropOverlapViewModel _structureCropOverlapVM;
        private object _structureCropOverlap;
        private RingGenerationViewModel _ringGenerationVM;
        private object _ringGeneration;

        public object ExportCT
        {
            get { return _exportCT; }
            set { SetProperty(ref _exportCT, value); }
        }

        public object PrepForTargets
        {
            get { return _prepForTargets; }
            set { SetProperty(ref _prepForTargets, value); }
        }

        public object StructureCropOverlap
        {
            get { return _structureCropOverlap; }
            set { SetProperty(ref _structureCropOverlap, value); }
        }

        public object RingGeneration
        {
            get { return _ringGeneration; }
            set { SetProperty(ref _ringGeneration, value); }
        }
        #endregion

        #region commands
        public ICommand QuickStartGuideCommand { get; set; }
        public ICommand HelpGuideCommand { get; set; }
        private ICommand NotifyExportCTCommand;
        private ICommand NotifyPrepForTargetsCommand;
        #endregion

        public CSIMainViewModel(string[] args) :
            base(PlanType.VMAT_CSI, args)
        {
            Initialize();
        }

        public void Initialize()
        {
            _generalConfigurationFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_CSI_config.ini";
            LoadScriptConfigurationSettings(_generalConfigurationFile);
            LoadPlanTemplates();

            List<ExportCTModel> models = new List<ExportCTModel>
            {
                new ExportCTModel("1", "CT 1", 100, DateTime.Now.ToString("yyyy-mm-dd")),
                new ExportCTModel("2", "CT 2", 200, "2019-01-01"),
                new ExportCTModel("3", "CT 3", 300, "2020-10-10"),
            };
            NotifyExportCTCommand = new RelayCommand(ExportCTImage);
            _ctExportViewModel = new CTExportViewModel(models, NotifyExportCTCommand);
            ExportCT = new CTExportView { DataContext = _ctExportViewModel };

            NotifyPrepForTargetsCommand = new RelayCommand(PreparePreliminaryTargets);
            _prepForTargetsVM = new PrepForTargetsViewModel(NotifyPrepForTargetsCommand);
            _prepForTargetsVM.UpdateRequestedTargetStructures(CSIAutoPlannerSettings.RequestedPreliminaryTargets);
            PrepForTargets = new PrepForTargetsView { DataContext = _prepForTargetsVM };

            _ringGenerationVM = new RingGenerationViewModel(_structureIdsPostUnion);
            RingGeneration = new RingGenerationView { DataContext = _ringGenerationVM };

            _structureCropOverlapVM = new StructureCropOverlapViewModel(_structureIdsPostUnion);
            StructureCropOverlap = new StructureCropOverlapView { DataContext = _structureCropOverlapVM };

            QuickStartGuideCommand = new RelayCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new RelayCommand(LaunchHelpGuide);

            //needs to be initialized after the plan templates are loaded
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };

            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            if (EclipseContext.GetInstance().IsInitialized && ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                if (EclipseContext.GetInstance().StructureSet.Structures.Any(x => x.ApprovalHistory.Last().ApprovalStatus == StructureApprovalStatus.Approved && x.Id.ToLower().Contains("ptv")))
                {
                    SetTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
                    PrepForTargetsBackground = System.Windows.Media.Brushes.LightGray;
                }
                else
                {
                    PrepForTargetsBackground = System.Windows.Media.Brushes.PaleVioletRed;
                    SetTargetsTabBackground = System.Windows.Media.Brushes.LightGray;
                }
            }
            else
            {
                PrepForTargetsBackground = System.Windows.Media.Brushes.LightGray;
                SetTargetsTabBackground = System.Windows.Media.Brushes.LightGray;
            }
        }

        public void ExportCTImage()
        {
            if (!ReferenceEquals(_ctExportViewModel.SelectedCTImage, null)) return;
            CTImageExport imageExport = new CTImageExport(EclipseContext.GetInstance().CTImages.First(x => string.Equals(x.Id, _ctExportViewModel.SelectedCTImage.CTId)),
                                                          EclipseContext.GetInstance().Patient.Id,
                                                          CSIAutoPlannerSettings.ImportExportData,
                                                          CSIAutoPlannerSettings.CloseProgressWindowOnFinish);
            bool result = imageExport.Execute();
            Logger.GetInstance().AppendLogOutput("Export CT data:", imageExport.GetLogOutput());
            Logger.GetInstance().OpType = ScriptOperationType.ExportCT;
            if (result) return;
            Application.Current.MainWindow.Close();
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
        #endregion

        #region specify targets
        private void PreparePreliminaryTargets()
        {
            if (!_prepForTargetsVM.RequestedTuningStructures.Any()) return;
            GeneratePreliminaryTargets_CSI generateTargets = new GeneratePreliminaryTargets_CSI(_prepForTargetsVM.RequestedTuningStructures);
            EclipseContext.GetInstance().Patient.BeginModifications();
            bool result = generateTargets.Execute();
            //grab the log output regardless if it passes or fails
            Logger.GetInstance().AppendLogOutput("Preliminary target generation output:", generateTargets.GetLogOutput());
            Logger.GetInstance().OpType = ScriptOperationType.GeneratePrelimTargets;
            if (result) return;
            Logger.GetInstance().AddedPrelimTargetsStructures = generateTargets.GetAddedTargetStructures();
            PrepForTargetsBackground = System.Windows.Media.Brushes.ForestGreen;
            MessageBox.Show("Structure set is prepared and ready for physician to review targets!");
        }

        protected override void SetTargets()
        {
            if (VerifyTargetsIntegrity(_setTargetsVM.PlanTargets)) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(_setTargetsVM.PlanTargets, 
                                                                 _initialDosePerFraction, 
                                                                 _initialNumberOfFractions, 
                                                                 _initialPlanTotalDose,
                                                                 _boostDosePerFraction,
                                                                 _boostNumberOfFractions,
                                                                 _boostPlanTotalDose);
            if (!_prescriptions.Any()) return;
            _planOptimizationSetup = BuildPlanOptimizationSetupList();

            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.ForestGreen;
            StructureTuningTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            TSManipulationTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }

        protected override bool VerifyTargetsIntegrity(List<PlanTargetsModel> parsedTargets)
        {
            //verify selected targets are APPROVED
            //for CSI, we only want to make there is one plan (not configured for sequential boosts)
            if (!parsedTargets.Any()) return true;
            if (parsedTargets.Select(x => x.PlanId).Distinct().Count() > 2)
            {
                Logger.GetInstance().LogError($"Error! More than 2 plan Ids entered! This script is only configured to auto-plan two or less CSI plans!");
                return true;
            }
            foreach (TargetModel target in parsedTargets.SelectMany(x =>x.Targets))
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(target.TargetId, EclipseContext.GetInstance().StructureSet, true))
                {
                    Logger.GetInstance().LogError($"Error! {target.TargetId} is either NOT present in structure set or is not contoured!");
                    return true;
                }
                else
                {
                    //structure is present and contoured
                    StructureApprovalStatus approvalStatus = StructureTuningHelper.GetStructureFromId(target.TargetId, EclipseContext.GetInstance().StructureSet).ApprovalHistory.First().ApprovalStatus;
                    if (approvalStatus != StructureApprovalStatus.Approved)
                    {
                        Logger.GetInstance().LogError($"Error! {target.TargetId} is NOT approved!" + Environment.NewLine + $"{target.TargetId} approval status: {approvalStatus}");
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region TS generation and manipulation
        protected override void PerformTSStructureGenerationManipulation()
        {
            List<RequestedTSStructureModel> tsGeneration = _tsGenerationVM.RequestedTuningStructures.ToList();
            List<TSRingStructureModel> rings = _ringGenerationVM.RequestedRingStructures.ToList();
            List<string> cropOverlapStructures = _structureCropOverlapVM.CropOverlapStructures.ToList();
            List<RequestedTSManipulationModel> tsManipulations = _tsManipulationVM.RequestedTSManipulations.ToList();
            TSGenerationManipulation_CSI generateTS = new TSGenerationManipulation_CSI(tsGeneration, 
                                                                                       tsManipulations, 
                                                                                       rings, 
                                                                                       _prescriptions, 
                                                                                       cropOverlapStructures);

            EclipseContext.GetInstance().Patient.BeginModifications();
            bool failed = generateTS.Execute();
            Logger.GetInstance().AppendLogOutput("TS Generation and manipulation output:", generateTS.LogOutput);
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

            _beamPlacementVM.PopulateBeamPlacementUI(_planIsocenters, CSIAutoPlannerSettings.AvailableLinacs, CSIAutoPlannerSettings.AvailableEnergies);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithTSTargets(generateTS.PlanTargets, _planOptimizationSetup);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithRings(generateTS.AddedRings, _planOptimizationSetup);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithCropOverlapStructures(generateTS.TargetCropOverlapManipulations, _planOptimizationSetup);

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
        #endregion

        #region beam placement
        protected override void GeneratePlansAndPlaceBeams()
        {
            _planIsocenters = _beamPlacementVM.PlanIsocenterList.ToList();
            GeneratePlansAndPlaceBeams_CSI placeBeams = new GeneratePlansAndPlaceBeams_CSI(_planIsocenters,
                                                                                           _prescriptions,
                                                                                           _beamPlacementVM.SelectedLinac,
                                                                                           _beamPlacementVM.SelectedEnergy,
                                                                                           _beamPlacementVM.ContourFieldOverlapChecked,
                                                                                           _beamPlacementVM.FieldOverlapMargin);
            bool failed = placeBeams.Execute();
            Logger.GetInstance().AppendLogOutput("Generate plans and place beams output:", placeBeams.GetLogOutput());
            if (failed) return;
            if (!placeBeams.VMATPlans.Any()) return;
            EclipseContext.GetInstance().VMATPlans = placeBeams.VMATPlans;
            Logger.GetInstance().PlanUIDs = placeBeams.VMATPlans.OrderBy(x => x.CreationDateTime).Select(x => x.UID).ToList();
            _planOptimizationSetup = UpdateOptimizationConstraintsWithTSJunctions(placeBeams.FieldJunctions, _planOptimizationSetup);
            _optimizationSetupVM.UpdateUIWithPlanOptimizationSetupList(_planOptimizationSetup);

            BeamPlacementTabBackground = System.Windows.Media.Brushes.ForestGreen;
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }
        #endregion

        #region prepare for treatment
        protected override void PreparePlanForTreatment()
        {

        }

        public bool GenerateShiftNote()
        {
            return false;
        }
        public bool SeparatePlans()
        {
            return false;
        }
        #endregion

        private void ResetRxDose()
        {
            if(BoostNumberOfFractions > 0 && BoostDosePerFraction > 0)
            {
                BoostPlanTotalDose = BoostDosePerFraction * BoostNumberOfFractions;
            }
        }

        protected override void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            InitialDosePerFraction = (_selectedTemplate as CSIAutoPlanTemplate).InitialRxDosePerFx;
            InitialNumberOfFractions = (_selectedTemplate as CSIAutoPlanTemplate).InitialRxNumberOfFractions;
            BoostDosePerFraction = (_selectedTemplate as CSIAutoPlanTemplate).BoostRxDosePerFx;
            BoostNumberOfFractions = (_selectedTemplate as CSIAutoPlanTemplate).BoostRxNumberOfFractions;
            _setTargetsVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
            _tsGenerationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
            _ringGenerationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate, true);
            _structureCropOverlapVM.AutoPlanTemplateSelectionChanged(_selectedTemplate, true);
            _tsManipulationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
        }

        #region script configuration
        protected override void LoadScriptConfigurationSettings(string file)
        {
            try
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    //setup temporary vectors to hold the parsed data
                    string line;
                    List<string> linac_temp = new List<string> { };
                    List<string> energy_temp = new List<string> { };
                    List<VRect<double>> jawPos_temp = new List<VRect<double>> { };

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
                                    if (!string.IsNullOrEmpty(value)) CSIAutoPlannerSettings.CloseProgressWindowOnFinish = bool.Parse(value);
                                }
                                else if (parameter == "img export location")
                                {
                                    string result = ConfigurationHelper.VerifyPathIntegrity(value);
                                    if (!string.IsNullOrEmpty(result)) CSIAutoPlannerSettings.ImportExportData.WriteLocation = result;
                                    else Logger.GetInstance().LogError($"Warning! {value} does NOT exist!");
                                }
                                else if (parameter == "RTStruct import location")
                                {
                                    string result = ConfigurationHelper.VerifyPathIntegrity(value);
                                    if (!string.IsNullOrEmpty(result)) CSIAutoPlannerSettings.ImportExportData.ImportLocation = result;
                                    else Logger.GetInstance().LogError($"Warning! {value} does NOT exist!");
                                }
                                else if (parameter == "img export format")
                                {
                                    if (string.Equals(value, "dcm") || string.Equals(value, "png")) CSIAutoPlannerSettings.ImportExportData.ExportFormat = ExportFormatTypeHelper.GetExportFormatType(value);
                                    else Logger.GetInstance().LogError("Only png and dcm image formats are supported for export!");
                                }
                                else if (parameter.Contains("daemon"))
                                {
                                    //CONTINUE HERE 070523!
                                    DaemonModel result = ConfigurationHelper.ParseDaemonSettings(line);
                                    if (result.Port != -1)
                                    {
                                        if (parameter.ToLower().Contains("aria")) CSIAutoPlannerSettings.ImportExportData.AriaDBDaemon = result;
                                        else if (parameter.ToLower().Contains("vms file")) CSIAutoPlannerSettings.ImportExportData.VMSFileDaemon = result;
                                        else if (parameter.ToLower().Contains("local")) CSIAutoPlannerSettings.ImportExportData.LocalDaemon = result;
                                        else
                                        {
                                            Logger.GetInstance().LogError($"Error! Daemon type {parameter} not recognized! Skipping!");
                                        }
                                    }
                                    else Logger.GetInstance().LogError($"Error! Daemon configuration settings for {line} not parsed successfully! Skipping!");
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
                                    CSIAutoPlannerSettings.BeamsPerIsocenter.Clear();
                                    CSIAutoPlannerSettings.BeamsPerIsocenter.AddRange(b);
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
                                    CSIAutoPlannerSettings.CollimatorRotations.Clear();
                                    CSIAutoPlannerSettings.CollimatorRotations.AddRange(c);
                                }
                                else if (parameter == "check couch collision")
                                {
                                    if (!string.IsNullOrEmpty(value)) CSIAutoPlannerSettings.CheckTTCollision = bool.Parse(value);
                                }
                                else if (parameter == "course Id") CSIAutoPlannerSettings.CourseId = value;
                                else if (parameter == "use GPU for dose calculation") CSIAutoPlannerSettings.UseGPUForDosecalculation = bool.Parse(value);
                                else if (parameter == "use GPU for optimization") CSIAutoPlannerSettings.UseGPUForOptimization = bool.Parse(value);
                                else if (parameter == "MR level restart") CSIAutoPlannerSettings.MRLevelRestart = value;
                                //other parameters that should be updated
                                else if (parameter == "calculation model") { if (value != "") CSIAutoPlannerSettings.DoseCalculationAlgorithm = value; }
                                else if (parameter == "optimization model") { if (value != "") CSIAutoPlannerSettings.OptimizationAlorithm = value; }
                                else if (parameter == "contour field overlap") { if (value != "") CSIAutoPlannerSettings.ContourFieldOverlap = bool.Parse(value); }
                                else if (parameter == "contour field overlap margin") { if (value != "") CSIAutoPlannerSettings.ContourFieldOverlapMarginInCM = double.Parse(value); }
                            }
                            else if (line.Contains("create preliminary target")) CSIAutoPlannerSettings.RequestedPreliminaryTargets.Add(ConfigurationHelper.ParseCreateTS(line));
                            else if (line.Contains("add linac"))
                            {
                                //parse the linacs that should be added. One entry per line
                                line = ConfigurationHelper.CropLine(line, "{");
                                CSIAutoPlannerSettings.AvailableLinacs.Add(line.Substring(0, line.IndexOf("}")));
                            }
                            else if (line.Contains("add beam energy"))
                            {
                                //parse the photon energies that should be added. One entry per line
                                line = ConfigurationHelper.CropLine(line, "{");
                                CSIAutoPlannerSettings.AvailableEnergies.Add(line.Substring(0, line.IndexOf("}")));
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
                        CSIAutoPlannerSettings.JawPositions.Clear();
                        CSIAutoPlannerSettings.JawPositions = new List<VRect<double>>(jawPos_temp);
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
                foreach (string itr in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\templates\\CSI\\", "*.ini").OrderBy(x => x))
                {
                    PlanTemplates.Add(ConfigurationHelper.ReadCSITemplatePlan(itr, count++));
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

        protected override StringBuilder BuildScriptConfigurationInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{DateTime.Now}");
            if (!string.IsNullOrEmpty(_generalConfigurationFile)) sb.AppendLine($"Configuration file: {_generalConfigurationFile}");
            else sb.AppendLine("Configuration file: none");
            sb.AppendLine($"Documentation path: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\documentation\\"}");
            sb.AppendLine($"Log file path: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs\\"}");
            sb.AppendLine($"Close progress windows on finish: {CSIAutoPlannerSettings.CloseProgressWindowOnFinish}");
            sb.AppendLine("Default parameters:");
            sb.AppendLine($"Course Id: {CSIAutoPlannerSettings.CourseId}");
            sb.AppendLine($"Check for potential couch collision: {CSIAutoPlannerSettings.CheckTTCollision}");
            sb.AppendLine($"Contour field ovelap: {CSIAutoPlannerSettings.ContourFieldOverlap}");
            sb.AppendLine($"Contour field overlap margin: {CSIAutoPlannerSettings.ContourFieldOverlapMarginInCM} cm");
            sb.AppendLine("Available linacs:");
            foreach (string l in CSIAutoPlannerSettings.AvailableLinacs) sb.AppendLine($"    {l}");
            sb.AppendLine("Available photon energies:");
            foreach (string e in CSIAutoPlannerSettings.AvailableEnergies) sb.AppendLine($"    {e}");
            sb.AppendLine($"Beams per isocenter: ");
            for (int i = 0; i < CSIAutoPlannerSettings.BeamsPerIsocenter.Count; i++)
            {
                sb.Append($"{CSIAutoPlannerSettings.BeamsPerIsocenter.ElementAt(i)}");
                if (i != CSIAutoPlannerSettings.BeamsPerIsocenter.Count - 1) sb.Append(", ");
            }
            sb.AppendLine("");
            sb.AppendLine("Collimator rotation (deg) order: ");
            for (int i = 0; i < CSIAutoPlannerSettings.CollimatorRotations.Count; i++)
            {
                sb.Append($"{CSIAutoPlannerSettings.CollimatorRotations.ElementAt(i):0.0}");
                if (i != CSIAutoPlannerSettings.CollimatorRotations.Count - 1) sb.Append(", ");
            }

            sb.AppendLine("");
            sb.AppendLine("Field jaw position (cm) order: ");
            sb.AppendLine(" (x1,y1,x2,y2)");
            foreach (VRect<double> j in CSIAutoPlannerSettings.JawPositions) sb.AppendLine($"({j.X1 / 10:0.0},{j.Y1 / 10:0.0},{j.X2 / 10:0.0},{j.Y2 / 10:0.0})");
            sb.AppendLine($"Photon dose calculation model: {CSIAutoPlannerSettings.DoseCalculationAlgorithm}");
            sb.AppendLine($"Use GPU for dose calculation: {CSIAutoPlannerSettings.UseGPUForDosecalculation}");
            sb.AppendLine($"Photon optimization model: {CSIAutoPlannerSettings.OptimizationAlorithm}");
            sb.AppendLine($"Use GPU for optimization: {CSIAutoPlannerSettings.UseGPUForOptimization}");
            sb.AppendLine($"MR level restart at: {CSIAutoPlannerSettings.MRLevelRestart}");

            if (PlanTemplates.Any()) sb.Append(ConfigurationUIHelper.PrintCSIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
            return sb;
        }
        #endregion
    }
}
