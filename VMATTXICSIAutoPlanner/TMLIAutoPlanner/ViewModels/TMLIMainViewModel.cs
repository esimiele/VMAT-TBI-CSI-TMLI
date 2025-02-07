using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using CTStitcher.ViewModels;
using CTStitcher.Views;
using TMLIAutoPlanner.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.TPS.Common.Model.Types;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.UIHelpers;
using System.Reflection;
using TMLIAutoPlanner.Core;
using AutoPlannerHelpers.BaseViewModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using AutoPlannerHelpers.Enums;

namespace TMLIAutoPlanner.ViewModels
{
    public class TMLIMainViewModel : BaseViewModel
    {
        #region properties
        private System.Windows.Media.SolidColorBrush _prepForTargetsBackground;
        private System.Windows.Media.SolidColorBrush _setTargetsTabBackground;
        private System.Windows.Media.SolidColorBrush _stitchCTTabBackground;

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

        public System.Windows.Media.SolidColorBrush StitchCTTabBackground
        {
            get { return _stitchCTTabBackground; }
            set { SetProperty(ref _stitchCTTabBackground, value); }
        }
        #endregion

        #region view objects
        private CTStitcherViewModel _stitcherViewModel;
        private object _stitchCT;
        private PrepForTargetsViewModel _prepForTargetsVM;
        private object _prepForTargets;
        private RingGenerationViewModel _ringGenerationVM;
        private object _ringGeneration;

        public object StitchCT
        {
            get { return _stitchCT; }
            set { SetProperty(ref _stitchCT, value); }
        }

        public object PrepForTargets
        {
            get { return _prepForTargets; }
            set { SetProperty(ref _prepForTargets, value); }
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
        public ICommand PTVMarginInfoCommand { get; set; }
        private ICommand NotifyPrepForTargetsCommand;
        #endregion

        public TMLIMainViewModel(string[] args) :
            base(PlanType.VMAT_TMLI, args)
        {
            Initialize();
        }

        public void Initialize()
        {
            //try { VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication(); }
            //catch (Exception e) { MessageBox.Show(e.Message); }
            _generalConfigurationFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TMLI_config.ini";
            LoadScriptConfigurationSettings(_generalConfigurationFile);
            LoadPlanTemplates();

            _stitcherViewModel = new CTStitcherViewModel();
            StitchCT = new CTStitcherView { DataContext = _stitcherViewModel };

            NotifyPrepForTargetsCommand = new RelayCommand(PreparePreliminaryTargets);
            _prepForTargetsVM = new PrepForTargetsViewModel(NotifyPrepForTargetsCommand);
            PrepForTargets = new PrepForTargetsView { DataContext = _prepForTargetsVM };

            _ringGenerationVM = new RingGenerationViewModel(_structureIdsPostUnion);
            RingGeneration = new RingGenerationView { DataContext = _ringGenerationVM };

            if (TMLIAutoPlannerSettings.AllBeamsVMAT) _beamPlacementVM.HideRequestedNumberOfIsos();
            
            QuickStartGuideCommand = new RelayCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new RelayCommand(LaunchHelpGuide);
            PTVMarginInfoCommand = new RelayCommand(ShowPTVMarginInfo);

            //needs to be initialized after the plan templates are loaded
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
            if (EclipseContext.GetInstance().IsInitialized && !ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                PatientMRN = EclipseContext.GetInstance().Patient.Id;
                StructureSetId = EclipseContext.GetInstance().StructureSet.Id;
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
        private void PreparePreliminaryTargets()
        {
            if (!_prepForTargetsVM.RequestedTuningStructures.Any()) return;
            List<RequestedTSManipulationModel> targetCropOperations = new List<RequestedTSManipulationModel> { };
            if (!ReferenceEquals(_selectedTemplate, null)) targetCropOperations.AddRange(_selectedTemplate.TSManipulations.Where(x => x.ManipulationType == TSManipulationType.CropTargetFromStructure));
            GeneratePreliminaryTargets_TMLI generateTargets = new GeneratePreliminaryTargets_TMLI(_prepForTargetsVM.RequestedTuningStructures, 
                                                                                                  targetCropOperations);
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
        protected override bool VerifyTargetsIntegrity(List<PlanTargetsModel> parsedTargets)
        {
            //verify selected targets are APPROVED
            //for tbi, we only want to make there is one plan (not configured for sequential boosts)
            if (!parsedTargets.Any()) return true;
            if (parsedTargets.Select(x => x.PlanId).Distinct().Count() > 1)
            {
                Logger.GetInstance().LogError($"Error! Multiple plan Ids entered! This script is only configured to auto-plan one TBI plan!");
                return true;
            }
            foreach (TargetModel target in parsedTargets.SelectMany(x => x.Targets))
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
            List<RequestedTSManipulationModel> tsManipulations = _tsManipulationVM.RequestedTSManipulations.ToList();
            List<TSRingStructureModel> rings = _ringGenerationVM.RequestedRingStructures.ToList();
            TSGenerationManipulation_TMLI generateTS = new TSGenerationManipulation_TMLI(tsGeneration,
                                                                                       tsManipulations,
                                                                                       rings,
                                                                                       _prescriptions);

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

            _beamPlacementVM.PopulateBeamPlacementUI(_planIsocenters, TMLIAutoPlannerSettings.AvailableLinacs, TMLIAutoPlannerSettings.AvailableEnergies);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithRings(generateTS.AddedRings, _planOptimizationSetup);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithTSTargets(generateTS.PlanTargets, _planOptimizationSetup);

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
            GeneratePlansAndPlaceBeams_TMLI placeBeams = new GeneratePlansAndPlaceBeams_TMLI(_planIsocenters,
                                                                                           _prescriptions,
                                                                                           _beamPlacementVM.SelectedLinac,
                                                                                           _beamPlacementVM.SelectedEnergy,
                                                                                           _beamPlacementVM.ContourFieldOverlapChecked,
                                                                                           _beamPlacementVM.FieldOverlapMargin);
            bool failed = placeBeams.Execute();
            Logger.GetInstance().AppendLogOutput("Generate plans and place beams output:", placeBeams.GetLogOutput());
            if (failed) return;
            if (placeBeams.VMATPlans.Any()) EclipseContext.GetInstance().VMATPlans = placeBeams.VMATPlans;
            _planOptimizationSetup = UpdateOptimizationConstraintsWithTSJunctions(placeBeams.FieldJunctions, _planOptimizationSetup);
            _optimizationSetupVM.UpdateUIWithPlanOptimizationSetupList(_planOptimizationSetup);

            BeamPlacementTabBackground = System.Windows.Media.Brushes.ForestGreen;
            OptimizationSetupTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
        }
        #endregion

        #region prepare for treatment
        protected override void PreparePlanForTreatment()
        {
            //ExternalPlanSetup thePlan = PlanPrepHelper.RetrieveVMATPlan(EclipseContext.GetInstance().Patient, Logger.GetInstance().LogPath, TMLIAutoPlannerSettings.CourseId);
            //if (ReferenceEquals(thePlan, null)) return;
            //EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { thePlan };

            //if (GenerateShiftNote()) return;
            //if (SeparatePlans()) return;
            //Logger.GetInstance().OpType = ScriptOperationType.PlanPrep;
            //_planPrepVM.UpdateUIAllPrepItemsCompleted();
        }

        public bool GenerateShiftNote()
        {
            //List<ExternalPlanSetup> appaPlans = new List<ExternalPlanSetup> { };
            //if (EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("legs")))
            //{
            //    appaPlans = EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("legs")).ToList();
            //    if (appaPlans.Any(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine))
            //    {
            //        StringBuilder sb = new StringBuilder();
            //        sb.AppendLine($"The AP/PA plan {appaPlans.First(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine).Id} is NOT in the FFS orientation!");
            //        sb.AppendLine("THE COUCH SHIFTS FOR THESE PLANS WILL NOT BE ACCURATE! Please fix and try again!");
            //        Logger.GetInstance().LogError(sb.ToString());
            //        return true;
            //    }
            //}

            //Clipboard.SetText(PlanPrepHelper.GetTBIShiftNote(EclipseContext.GetInstance().VMATPlans.First(), appaPlans).ToString());
            return false;
        }
        public bool SeparatePlans()
        {
            ////The shift note has to be retrieved first! Otherwise, we don't have instances of the plan objects
            //if (!EclipseContext.GetInstance().VMATPlans.Any() || EclipseContext.GetInstance().VMATPlans.Count > 1)
            //{
            //    Logger.GetInstance().LogError("Please generate the shift note before separating the plans!");
            //    return true;
            //}
            //ExternalPlanSetup thePlan = EclipseContext.GetInstance().VMATPlans.First();

            //if (!thePlan.Beams.Any(x => x.IsSetupField))
            //{
            //    ConfirmPrompt CUI = new ConfirmPrompt($"I didn't find any setup fields in the {thePlan.Id}." + Environment.NewLine + Environment.NewLine + "Are you sure you want to continue?!");
            //    CUI.ShowDialog();
            //    if (!CUI.GetSelection()) return true;
            //}

            ////separate the plans
            //EclipseContext.GetInstance().Patient.BeginModifications();
            //PreparePlansForTreatment_TMLI planPrep = new PreparePlansForTreatment_TMLI();
            //bool result = planPrep.Execute();
            //Logger.GetInstance().AppendLogOutput("Plan preparation:", planPrep.GetLogOutput());
            //if (result) return true;

            ////inform the user it's done
            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine("Original plan(s) have been separated!");
            //sb.AppendLine("Be sure to set the target volume and primary reference point!");
            //if (thePlan.Beams.Any(x => x.IsSetupField))
            //{
            //    sb.AppendLine("Also reset the isocenter position of the setup fields!");
            //}
            //sb.AppendLine("");
            //sb.AppendLine("Isocenter shifts have been copied to the clipboard!");
            //sb.AppendLine("Paste them into the journal note!");
            //MessageBox.Show(sb.ToString());

            return false;
        }
        #endregion

        protected override void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;

            InitialDosePerFraction = (_selectedTemplate as TMLIAutoPlanTemplate).InitialRxDosePerFx;
            InitialNumberOfFractions = (_selectedTemplate as TMLIAutoPlanTemplate).InitialRxNumberOfFractions;
            _prepForTargetsVM.UpdateRequestedTargetStructures((_selectedTemplate as TMLIAutoPlanTemplate).RequestedPreliminaryTargets);
            _setTargetsVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
            _tsGenerationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
            _ringGenerationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
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
                                else if (parameter == "max Y-jaw field extent") TMLIAutoPlannerSettings.MaxFieldYExtent = double.Parse(value);
                                else if (parameter == "minimum field overlap") TMLIAutoPlannerSettings.MinFieldOverlap = double.Parse(value);
                                else if (parameter == "all beams VMAT") TMLIAutoPlannerSettings.AllBeamsVMAT = bool.Parse(value);
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

        protected override StringBuilder BuildScriptConfigurationInfo()
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
    }
}
