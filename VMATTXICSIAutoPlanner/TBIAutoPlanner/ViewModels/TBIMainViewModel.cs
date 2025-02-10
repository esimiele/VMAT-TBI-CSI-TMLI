using System.Collections.Generic;
using System.Windows;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using AutoPlannerHelpers.PlanTemplateModels;
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
using AutoPlannerHelpers.BaseViewModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace TBIAutoPlanner.ViewModels
{
    public class TBIMainViewModel : BaseViewModel
    {
        #region properties
        private bool _useFlash;
        private Visibility _flashMarginVisible;
        private double _flashMargin;
        private double _ptvMarginFromBody;
        private Visibility _stitchCTTabVisible;
        private System.Windows.Media.SolidColorBrush _stitchCTTabBackground;
        private int _initialTabSelected;

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

        public System.Windows.Media.SolidColorBrush StitchCTTabBackground
        {
            get { return _stitchCTTabBackground; }
            set { SetProperty(ref _stitchCTTabBackground, value); }
        }

        public Visibility StitchCTTabVisible
        {
            get { return _stitchCTTabVisible; }
            set { SetProperty(ref _stitchCTTabVisible, value); }
        }

        public int InitialTabSelected
        {
            get { return _initialTabSelected; }
            set { SetProperty(ref _initialTabSelected, value); }
        }

        #endregion

        #region view objects
        private CTStitcherViewModel _stitcherViewModel;
        private object _stitchCT;

        public object StitchCT
        {
            get { return _stitchCT; }
            set { SetProperty(ref _stitchCT, value); }
        }
        #endregion

        #region commands
        public ICommand QuickStartGuideCommand { get; set; }
        public ICommand HelpGuideCommand { get; set; }
        public ICommand PTVMarginInfoCommand { get; set; }
        #endregion

        #region fields
        #endregion

        public TBIMainViewModel(string[] args) :
            base(PlanType.VMAT_TBI, args)
        {
            Initialize();
        }

        public void Initialize()
        {
            _generalConfigurationFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TBI_config.ini";
            LoadScriptConfigurationSettings(_generalConfigurationFile);
            LoadPlanTemplates();

            _stitcherViewModel = new CTStitcherViewModel();
            StitchCT = new CTStitcherView { DataContext = _stitcherViewModel };
            StitchCTTabBackground = Brushes.LightGray;

            if (!TBIAutoPlannerSettings.ShowStitchCTTab)
            {
                StitchCTTabVisible = Visibility.Collapsed;
                InitialTabSelected = 1;
            }

            PTVMarginFromBody = TBIAutoPlannerSettings.PTVInnerMarginFromBodyInCM;
            UseFlash = TBIAutoPlannerSettings.UseFlash;
            if (!TBIAutoPlannerSettings.UseFlash) FlashMarginVisible = Visibility.Hidden;
            FlashMargin = TBIAutoPlannerSettings.FlashMarginInCM;

            QuickStartGuideCommand = new RelayCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new RelayCommand(LaunchHelpGuide);
            PTVMarginInfoCommand = new RelayCommand(ShowPTVMarginInfo);

            _beamPlacementVM.UpdateBeamsPerIso(TBIAutoPlannerSettings.BeamsPerIsocenter);

            //needs to be initialized after the plan templates are loaded
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
            SpecifyTargetsTabBackground = System.Windows.Media.Brushes.PaleVioletRed;
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
            return false;
        }
        #endregion

        #region TS generation and manipulation
        protected override void PerformTSStructureGenerationManipulation()
        {
            List<RequestedTSStructureModel> tsGeneration = _tsGenerationVM.RequestedTuningStructures.ToList();
            List<RequestedTSManipulationModel> tsManipulations = _tsManipulationVM.RequestedTSManipulations.ToList();
            TSGenerationManipulation_TBI generateTS = new TSGenerationManipulation_TBI(tsGeneration,
                                                                                       tsManipulations,
                                                                                       _prescriptions,
                                                                                       UseFlash,
                                                                                       FlashMargin,
                                                                                       PTVMarginFromBody);

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

            _beamPlacementVM.PopulateBeamPlacementUI(_planIsocenters, TBIAutoPlannerSettings.AvailableLinacs, TBIAutoPlannerSettings.AvailableEnergies, TBIAutoPlannerSettings.ContourFieldOverlap, TBIAutoPlannerSettings.ContourFieldOverlapMarginInCM);
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
            GeneratePlansAndPlaceBeams_TBI placeBeams = new GeneratePlansAndPlaceBeams_TBI(_planIsocenters,
                                                                                           _prescriptions,
                                                                                           _beamPlacementVM.SelectedLinac,
                                                                                           _beamPlacementVM.SelectedEnergy,
                                                                                           PTVMarginFromBody,
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
            //ExternalPlanSetup thePlan = PlanPrepHelper.RetrieveVMATPlan(EclipseContext.GetInstance().Patient, Logger.GetInstance().LogPath, TBIAutoPlannerSettings.CourseId);
            //if (ReferenceEquals(thePlan, null)) return;
            //EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { thePlan };

            //if (GenerateShiftNote()) return;
            //if(SeparatePlans()) return;
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

            //bool removeFlash = false;
            //StringBuilder sb = new StringBuilder();
            ////check if flash was used in the plan. If so, ask the user if they want to remove these structures as part of cleanup
            //if (PlanPrepHelper.CheckForFlash(thePlan.StructureSet))
            //{
            //    sb.AppendLine("I found some structures in the structure set for generating flash.");
            //    sb.AppendLine("Should I remove them?");
            //    sb.AppendLine("(NOTE: this will require dose recalculation for all plans using this structure set!)");
            //    ConfirmPrompt CP = new ConfirmPrompt(sb.ToString(), "YES", "NO");
            //    CP.ShowDialog();
            //    if (CP.GetSelection()) removeFlash = true;
            //}

            ////separate the plans
            //EclipseContext.GetInstance().Patient.BeginModifications();
            //PreparePlansForTreatment_TBI planPrep = new PreparePlansForTreatment_TBI(removeFlash);
            //bool result = planPrep.Execute();
            //Logger.GetInstance().AppendLogOutput("Plan preparation:", planPrep.GetLogOutput());
            //if (result) return true;

            ////inform the user it's done
            //sb.Clear();
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

            InitialDosePerFraction = (_selectedTemplate as TBIAutoPlanTemplate).InitialRxDosePerFx;
            InitialNumberOfFractions = (_selectedTemplate as TBIAutoPlanTemplate).InitialRxNumberOfFractions;
            _setTargetsVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
            _tsGenerationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
            _tsManipulationVM.AutoPlanTemplateSelectionChanged(_selectedTemplate);
        }

        private void UpdateUseFlash()
        {
            if (UseFlash) FlashMarginVisible = Visibility.Visible;
            else FlashMarginVisible = Visibility.Hidden;
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
                                if (double.TryParse(value, out double result))
                                {
                                    if (parameter == "default flash margin") TBIAutoPlannerSettings.FlashMarginInCM = result;
                                    else if (parameter == "default target margin") TBIAutoPlannerSettings.PTVInnerMarginFromBodyInCM = result;
                                }
                                else if (parameter == "close progress windows on finish")
                                {
                                    if (!string.IsNullOrEmpty(value)) TBIAutoPlannerSettings.CloseProgressWindowOnFinish = bool.Parse(value);
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
                                    TBIAutoPlannerSettings.BeamsPerIsocenter.Clear();
                                    TBIAutoPlannerSettings.BeamsPerIsocenter.AddRange(b);
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
                                    TBIAutoPlannerSettings.CollimatorRotations.Clear();
                                    TBIAutoPlannerSettings.CollimatorRotations.AddRange(c);
                                }
                                else if (parameter == "check couch collision")
                                {
                                    if (!string.IsNullOrEmpty(value)) TBIAutoPlannerSettings.CheckTTCollision = bool.Parse(value);
                                }
                                else if (parameter == "show CT stitcher tab") TBIAutoPlannerSettings.ShowStitchCTTab = bool.Parse(value);
                                else if (parameter == "course Id") TBIAutoPlannerSettings.CourseId = value;
                                else if (parameter == "use GPU for dose calculation") TBIAutoPlannerSettings.UseGPUForDosecalculation = bool.Parse(value);
                                else if (parameter == "use GPU for optimization") TBIAutoPlannerSettings.UseGPUForOptimization = bool.Parse(value);
                                else if (parameter == "MR level restart") TBIAutoPlannerSettings.MRLevelRestart = value;
                                //other parameters that should be updated
                                else if (parameter == "use flash by default") TBIAutoPlannerSettings.UseFlash = bool.Parse(value);
                                else if (parameter == "calculation model") { if (value != "") TBIAutoPlannerSettings.DoseCalculationAlgorithm = value; }
                                else if (parameter == "optimization model") { if (value != "") TBIAutoPlannerSettings.OptimizationAlorithm = value; }
                                else if (parameter == "contour field overlap") { if (value != "") TBIAutoPlannerSettings.ContourFieldOverlap = bool.Parse(value); }
                                else if (parameter == "contour field overlap margin") { if (value != "") TBIAutoPlannerSettings.ContourFieldOverlapMarginInCM = double.Parse(value); }
                                else if (parameter == "max Y-jaw field extent") TBIAutoPlannerSettings.MaxFieldYExtent = double.Parse(value);
                                else if (parameter == "minimum field overlap") TBIAutoPlannerSettings.MinFieldOverlap = double.Parse(value);
                                else if (parameter == "all beams VMAT") TBIAutoPlannerSettings.AllBeamsVMAT = bool.Parse(value);
                            }
                            else if (line.Contains("add linac"))
                            {
                                //parse the linacs that should be added. One entry per line
                                line = ConfigurationHelper.CropLine(line, "{");
                                TBIAutoPlannerSettings.AvailableLinacs.Add(line.Substring(0, line.IndexOf("}")));
                            }
                            else if (line.Contains("add beam energy"))
                            {
                                //parse the photon energies that should be added. One entry per line
                                line = ConfigurationHelper.CropLine(line, "{");
                                TBIAutoPlannerSettings.AvailableEnergies.Add(line.Substring(0, line.IndexOf("}")));
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
                        TBIAutoPlannerSettings.JawPositions.Clear();
                        TBIAutoPlannerSettings.JawPositions = new List<VRect<double>>(jawPos_temp);
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

        protected override StringBuilder BuildScriptConfigurationInfo()
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
            for (int i = 0; i < TBIAutoPlannerSettings.BeamsPerIsocenter.Count; i++)
            {
                sb.Append($"{TBIAutoPlannerSettings.BeamsPerIsocenter.ElementAt(i)}");
                if (i != TBIAutoPlannerSettings.BeamsPerIsocenter.Count - 1) sb.Append(", ");
            }
            sb.AppendLine("");
            sb.AppendLine("Collimator rotation (deg) order: ");
            for (int i = 0; i < TBIAutoPlannerSettings.CollimatorRotations.Count; i++)
            {
                sb.Append($"{TBIAutoPlannerSettings.CollimatorRotations.ElementAt(i):0.0}");
                if (i != TBIAutoPlannerSettings.CollimatorRotations.Count - 1) sb.Append(", ");
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
    }
}