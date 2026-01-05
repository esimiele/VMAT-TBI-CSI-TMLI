using System.Collections.Generic;
using System.Windows;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
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
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.BaseViewModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.Messaging;
using ExternalPlanSetup = VMS.TPS.Common.Model.API.ExternalPlanSetup;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.BaseCore;

namespace TBIAutoPlanner.ViewModels
{
    public class TBIMainViewModel : BaseViewModel
    {
        #region properties
        private bool _useFlash;
        private Visibility _flashMarginVisible;
        private double _flashMargin;
        private double _ptvMarginFromBody;

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
            set { UpdatePTVMarginFromBodyInTargetDerivations(_ptvMarginFromBody, value); SetProperty(ref _ptvMarginFromBody, value);   }
        }
        #endregion

        #region commands
        public ICommand PTVMarginInfoCommand { get; set; }
        #endregion

        public TBIMainViewModel(string[] args) :
            base(PlanType.VMAT_TBI, args)
        {
        }

        protected override void PerformPlanTypeSpecificInitialization()
        {
            _generalConfigurationFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TBI_config.ini";
            LoadScriptConfigurationSettings(_generalConfigurationFile);
            LoadPlanTemplates();

            PTVMarginFromBody = TBIAutoPlannerSettings.PTVInnerMarginFromBodyInCM;
            UseFlash = TBIAutoPlannerSettings.UseFlash;
            if (!TBIAutoPlannerSettings.UseFlash) FlashMarginVisible = Visibility.Hidden;
            FlashMargin = TBIAutoPlannerSettings.FlashMarginInCM;

            PTVMarginInfoCommand = new RelayCommand(ShowPTVMarginInfo);

            if (TBIAutoPlannerSettings.AllBeamsVMAT) WeakReferenceMessenger.Default.Send(new RequestHideNumberOfVMATIsocenters());
            WeakReferenceMessenger.Default.Send(new RequestUpdateBeamPlacementDefaultSettings(TBIAutoPlannerSettings.AvailableLinacs,
                                                                                              TBIAutoPlannerSettings.AvailableEnergies,
                                                                                              TBIAutoPlannerSettings.ContourFieldOverlap,
                                                                                              TBIAutoPlannerSettings.ContourFieldOverlapMarginInCM,
                                                                                              TBIAutoPlannerSettings.BeamsPerIsocenter));

            PTVMarginInfoCommand = new RelayCommand(ShowPTVMarginInfo);
        }

        #region messengers
        protected override void InitializePlanTypeSpecificMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestAreSeparatedPlansAutomaticallyRecalculated>(this, (r, m) =>
            {
                m.Reply(TBIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep);
            });
        }
        #endregion

        #region information and help guides
        protected override void LaunchQuickStartGuide()
        {
            MessageBox.Show("test");
        }

        protected override void LaunchHelpGuide()
        {
            MessageBox.Show("test");
        }

        private void ShowPTVMarginInfo()
        {
            MessageBox.Show("test");
        }
        #endregion

        #region specify targets
        protected override GeneratePreliminaryTargetsBase GetTargetDerivationClassInstanceForPlanType(List<StructureOperationModel> preliminaryTargets)
        {
            return new GeneratePreliminaryTargets_TBI(preliminaryTargets);
        }

        protected override bool PhysicianTargetApprovalRequired()
        {
            return TBIAutoPlannerSettings.PhysicianTargetApprovalRequired;
        }

        protected override List<PrescriptionModel> BuildPlanTypeSpecificPrescriptionList(List<PlanTargetsModel> planTargets)
        {
            return TargetsHelper.BuildPrescriptionList(planTargets,
                                                    _initialDosePerFraction,
                                                    _initialNumberOfFractions,
                                                    _initialPlanTotalDose);
        }

        protected override void UpdatePlanTypeSpecificStructureOperationViews() { }

        private void UpdatePTVMarginFromBodyInTargetDerivations(double oldMargin, double newMargin)
        {
            List<StructureOperationModel> targetDerivations = WeakReferenceMessenger.Default.Send(new RequestTargetStructureDerivations());
            if (!targetDerivations.Any() || CalculationHelper.AreEqual(oldMargin, newMargin)) return;
            if(targetDerivations.Any(x => x.Operation == StructureDerivationOperation.CopyContractExpand && 
                                          string.Equals(x.StructureA, "body", StringComparison.OrdinalIgnoreCase) && 
                                          string.Equals(x.OutputStructure, "ptv_body", StringComparison.OrdinalIgnoreCase) && 
                                          x.MarginA.MarginType == StructureMarginType.Uniform && 
                                          x.MarginA.GeometryType == MarginGeometryType.Inner &&
                                          CalculationHelper.AreEqual(x.MarginA.x1, oldMargin)))
            {
                StructureOperationModel ptvDerivation = targetDerivations.First(x => x.Operation == StructureDerivationOperation.CopyContractExpand &&
                                                                                      string.Equals(x.StructureA, "body", StringComparison.OrdinalIgnoreCase) &&
                                                                                      string.Equals(x.OutputStructure, "ptv_body", StringComparison.OrdinalIgnoreCase) &&
                                                                                      x.MarginA.MarginType == StructureMarginType.Uniform &&
                                                                                      x.MarginA.GeometryType == MarginGeometryType.Inner &&
                                                                                      CalculationHelper.AreEqual(x.MarginA.x1, oldMargin));
                ptvDerivation.MarginA.UpdateMargin(new StructureMarginModel(-newMargin));
                WeakReferenceMessenger.Default.Send(new RequestUpdateTargetDerivationOperations(targetDerivations));
            }
        }
        #endregion

        #region TS generation and manipulation
        protected override TSGenerationManipulationBase GetOptStructureDerivationClassInstanceForPlanType(List<StructureOperationModel> operations, List<SpecialOptimizationStructureModel> specialOps)
        {
            return new TSGenerationManipulation_TBI(specialOps,
                                                    operations,
                                                    _prescriptions,
                                                    _useFlash,
                                                    _flashMargin,
                                                    _ptvMarginFromBody);
        }
        #endregion

        #region beam placement
        protected override GeneratePlansAndPlaceBeamsBase GetBeamPlacementClassInstanceForPlanType(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            return new GeneratePlansAndPlaceBeams_TBI(_planIsocenters,
                                                      _prescriptions,
                                                      linac,
                                                      energy,
                                                      PTVMarginFromBody,
                                                      contourOverlap,
                                                      overlapMargin);
        }
        #endregion

        #region prepare for treatment
        protected override bool GenerateShiftNote()
        {
            if(!EclipseContext.GetInstance().VMATPlans.Any())
            {
                ExternalPlanSetup plan = PlanPrepHelper.RetrieveVMATPlan(!string.IsNullOrEmpty(TBIAutoPlannerSettings.CourseId) ? TBIAutoPlannerSettings.CourseId : "VMAT-TBI", PlanType.VMAT_TBI);
                if (!ReferenceEquals(plan, null)) EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { plan };
                else return true;
            }

            if (EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("leg") && x.ApprovalStatus != PlanSetupApprovalStatus.Rejected))
            {
                if (EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("leg") && x.ApprovalStatus != PlanSetupApprovalStatus.Rejected).Any(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"The AP/PA plan {EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("leg") && x.ApprovalStatus != PlanSetupApprovalStatus.Rejected).ToList().First(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine).Id} is NOT in the FFS orientation!");
                    sb.AppendLine("THE COUCH SHIFTS FOR THESE PLANS WILL NOT BE ACCURATE! Please fix and try again!");
                    Logger.GetInstance().LogError(sb.ToString());
                    return true;
                }
            }

            Clipboard.SetText(PlanPrepHelper.GetTBIShiftNote(EclipseContext.GetInstance().VMATPlans.First(), EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("leg") && x.ApprovalStatus != PlanSetupApprovalStatus.Rejected).ToList()).ToString());
            return false;
        }

        protected override bool SeparatePlans()
        {
            bool removeFlash = false;
            //check if flash was used in the plan. If so, ask the user if they want to remove these structures as part of cleanup
            if (PlanPrepHelper.CheckForFlash(EclipseContext.GetInstance().StructureSet))
            {
                StringBuilder flashSB = new StringBuilder();
                flashSB.AppendLine("I found some structures in the structure set for generating flash.");
                flashSB.AppendLine("Should I remove them?");
                flashSB.AppendLine("(NOTE: this will require dose recalculation for all plans using this structure set!)");
                ConfirmPrompt CP = new ConfirmPrompt(flashSB.ToString(), "YES", "NO");
                CP.ShowDialog();
                if (CP.GetSelection()) removeFlash = true;
            }

            //separate the plans
            EclipseContext.GetInstance().Patient.BeginModifications();
            _planPrep = new PreparePlansForTreatment_TBI(removeFlash);
            bool result = _planPrep.Execute();
            Logger.GetInstance().AppendLogOutput("Plan preparation:", _planPrep.LogOutput);
            if (result) return true;
            return false;
        }

        protected override bool RecalculateDoseForSeparatePlans()
        {
            _planPrep.RecalculateDoseOnly = true;
            bool result = _planPrep.Execute();
            Logger.GetInstance().AppendLogOutput("Plan prep dose recalculation:", _planPrep.LogOutput);
            if (result) return true;
            return false;
        }
        #endregion

        protected override void UpdatePlanTypeSpecificUIWithPlanTemplate() { }

        private void UpdateUseFlash()
        {
            if (_useFlash) FlashMarginVisible = Visibility.Visible;
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