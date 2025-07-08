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
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Media;
using ExternalPlanSetup = VMS.TPS.Common.Model.API.ExternalPlanSetup;
using AutoPlannerHelpers.BaseCore;

namespace CSIAutoPlanner.ViewModels
{
    internal class CSIMainViewModel : BaseViewModel
    {
        #region properties
        private double _boostDosePerFraction;
        private int _boostNumberOfFractions;
        private double _boostPlanTotalDose;
        
        public double BoostDosePerFraction
        {
            get { return _boostDosePerFraction; }
            set { SetProperty(ref _boostDosePerFraction, value); ResetBoostRxDose(); }
        }

        public int BoostNumberOfFractions
        {
            get { return _boostNumberOfFractions; }
            set { SetProperty(ref _boostNumberOfFractions, value); ResetBoostRxDose(); }
        }

        public double BoostPlanTotalDose
        {
            get { return _boostPlanTotalDose; }
            set { SetProperty(ref _boostPlanTotalDose, value); }
        }
        #endregion

        #region view objects
        private object _exportCT;
        private object _importSS;
        private object _structureCropOverlap;
        private object _ringGeneration;

        public object ExportCT
        {
            get { return _exportCT; }
            set { SetProperty(ref _exportCT, value); }
        }

        public object ImportSS
        {
            get { return _importSS; }
            set { SetProperty(ref _importSS, value); }
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
            
            ExportCT = new CTExportView { DataContext = new CTExportViewModel() };
            ImportSS = new ImportSSView { DataContext = new ImportSSViewModel(CSIAutoPlannerSettings.ImportExportData, PlanType.VMAT_CSI, (!ReferenceEquals(EclipseContext.GetInstance().Patient, null) ? EclipseContext.GetInstance().Patient.Id : "")) };
            WeakReferenceMessenger.Default.Send(new RequestUpdateTargetStructures(CSIAutoPlannerSettings.RequestedPreliminaryTargets));

            RingGeneration = new RingGenerationView { DataContext = new RingGenerationViewModel(_structureIdsPostUnion) };
            StructureCropOverlap = new StructureCropOverlapView { DataContext = new StructureCropOverlapViewModel(_structureIdsPostUnion) };

            WeakReferenceMessenger.Default.Send(new RequestUpdateBeamPlacementDefaultSettings(CSIAutoPlannerSettings.AvailableLinacs, 
                                                                                              CSIAutoPlannerSettings.AvailableEnergies, 
                                                                                              CSIAutoPlannerSettings.ContourFieldOverlap, 
                                                                                              CSIAutoPlannerSettings.ContourFieldOverlapMarginInCM, 
                                                                                              CSIAutoPlannerSettings.BeamsPerIsocenter));

            //needs to be initialized after the plan templates are loaded
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
            if(!EclipseContext.GetInstance().IsInitialized) WeakReferenceMessenger.Default.Send(new RequestUpdateStructureIds(PlanTemplates.SelectMany(x => x.GenerateStructureIdList()).Distinct()));
            InitializeCSIMessengers();
        }

        #region messengers
        private void InitializeCSIMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestExportCT>(this, (r, m) =>
            {
                ExportCTImage(m.SelectedCTImage);
            }); 
            WeakReferenceMessenger.Default.Register<RequestAreSeparatedPlansAutomaticallyRecalculated>(this, (r, m) =>
            {
                m.Reply(CSIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep);
            });
        }
        #endregion

        #region CT export
        public void ExportCTImage(ExportCTModel selectedImage)
        {
            if (ReferenceEquals(selectedImage, null) || !EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().Patient, null) || !EclipseContext.GetInstance().CTImages.Any()) return;
            CTImageExport imageExport = new CTImageExport(EclipseContext.GetInstance().CTImages.First(x => string.Equals(x.Id, selectedImage.CTId)),
                                                          EclipseContext.GetInstance().Patient.Id,
                                                          CSIAutoPlannerSettings.ImportExportData,
                                                          PlanType.VMAT_CSI,
                                                          CSIAutoPlannerSettings.CloseProgressWindowOnFinish);
            bool result = imageExport.Execute();
            Logger.GetInstance().AppendLogOutput("Export CT data:", imageExport.GetLogOutput());
            Logger.GetInstance().OpType = ScriptOperationType.ExportCT;
            if (result) return;
            Application.Current.MainWindow.Close();
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
        #endregion

        #region specify targets
        protected override GeneratePreliminaryTargetsBase GetTargetDerivationClassInstanceForPlanType(List<StructureOperationModel> preliminaryTargets)
        {
            return new GeneratePreliminaryTargets_CSI(preliminaryTargets);
        }

        protected override void SetTargets(List<PlanTargetsModel> planTargets)
        {
            if (!planTargets.Any()) return;
            if (VerifyPlansIntegrity(planTargets)) return;
            if (VerifyTargetsIntegrity(planTargets.SelectMany(x => x.Targets))) return;
            _prescriptions = TargetsHelper.BuildPrescriptionList(planTargets, 
                                                                 _initialDosePerFraction, 
                                                                 _initialNumberOfFractions, 
                                                                 _initialPlanTotalDose,
                                                                 _boostDosePerFraction,
                                                                 _boostNumberOfFractions,
                                                                 _boostPlanTotalDose);
            if (!_prescriptions.Any()) return;
            Logger.GetInstance().Prescriptions = _prescriptions;
            _planOptimizationSetup = BuildPlanOptimizationSetupList();
            SpecifyTargetsTabBackground = Brushes.ForestGreen;
            StructureTuningTabBackground = Brushes.PaleVioletRed;
            OptimizationStructureDerivationBackground = Brushes.PaleVioletRed;
        }
        #endregion

        #region TS generation and manipulation
        protected override TSGenerationManipulationBase GetOptStructureDerivationClassInstanceForPlanType(List<StructureOperationModel> operations, List<SpecialOptimizationStructureModel> specialOps)
        {
            List<TSRingStructureModel> rings = WeakReferenceMessenger.Default.Send(new RequestRingStructures());
            List<string> cropOverlapStructures = WeakReferenceMessenger.Default.Send(new RequestCropOverlapStructures());
            return new TSGenerationManipulation_CSI(specialOps,
                                                    operations,
                                                    rings,
                                                    _prescriptions,
                                                    cropOverlapStructures);
        }

        protected override void UpdateOptimizationSetup(TSGenerationManipulationBase generateTS)
        {
            base.UpdateOptimizationSetup(generateTS);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithRings((generateTS as TSGenerationManipulation_CSI).AddedRings, _planOptimizationSetup);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithCropOverlapStructures((generateTS as TSGenerationManipulation_CSI).TargetCropOverlapManipulations, _planOptimizationSetup);
        }
        #endregion

        #region beam placement
        protected override GeneratePlansAndPlaceBeamsBase GetBeamPlacementClassInstanceForPlanType(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            return new GeneratePlansAndPlaceBeams_CSI(_planIsocenters,
                                                      _prescriptions,
                                                      linac,
                                                      energy,
                                                      contourOverlap,
                                                      overlapMargin);
        }
        #endregion

        #region prepare for treatment
        protected override bool GenerateShiftNote()
        {
            ExternalPlanSetup plan = PlanPrepHelper.RetrieveVMATPlan(!string.IsNullOrEmpty(CSIAutoPlannerSettings.CourseId) ? CSIAutoPlannerSettings.CourseId : "VMAT-CSI", PlanType.VMAT_CSI);
            if (!ReferenceEquals(plan, null)) EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { plan };
            else return true;

            Clipboard.SetText(PlanPrepHelper.GetCSIShiftNote(EclipseContext.GetInstance().VMATPlans.First()).ToString());
            return false;
        }

        protected override bool SeparatePlans()
        {
            //separate the plans
            EclipseContext.GetInstance().Patient.BeginModifications();
            _planPrep = new PreparePlansForTreatment_CSI();
            bool result = _planPrep.Execute();
            Logger.GetInstance().AppendLogOutput("Plan preparation:", _planPrep.GetLogOutput());
            if (result) return true;
            return false;
        }

        protected override bool RecalculateDoseForSeparatePlans()
        {
            _planPrep.RecalculateDoseOnly = true;
            bool result = _planPrep.Execute();
            Logger.GetInstance().AppendLogOutput("Plan prep dose recalculation:", _planPrep.GetLogOutput());
            if (result) return true;
            return false;
        }
        #endregion

        private void ResetBoostRxDose()
        {
            if(BoostNumberOfFractions > 0 && BoostDosePerFraction > 0)
            {
                BoostPlanTotalDose = Math.Round(BoostDosePerFraction * BoostNumberOfFractions, 1);
            }
        }

        protected override void UpdatePlanTypeSpecificUIWithPlanTemplate()
        {
            BoostDosePerFraction = (_selectedTemplate as CSIAutoPlanTemplate).BoostRxDosePerFx;
            BoostNumberOfFractions = (_selectedTemplate as CSIAutoPlanTemplate).BoostRxNumberOfFractions;
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
                                else if (parameter == "auto dose recalculation") CSIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep = bool.Parse(value);
                            }
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

            sb.AppendLine($"Import/export settings:");
            sb.AppendLine($"Image export path: {CSIAutoPlannerSettings.ImportExportData.WriteLocation}");
            sb.AppendLine($"RT structure set import path: {CSIAutoPlannerSettings.ImportExportData.ImportLocation}");
            sb.AppendLine($"Image export format: {CSIAutoPlannerSettings.ImportExportData.ExportFormat}");

            if (CSIAutoPlannerSettings.ImportExportData.AriaDBDaemon.IsInitialized)
            {
                sb.AppendLine("Aria database daemon:");
                sb.AppendLine($"AE Title: {CSIAutoPlannerSettings.ImportExportData.AriaDBDaemon.AETitle}");
                sb.AppendLine($"IP: {CSIAutoPlannerSettings.ImportExportData.AriaDBDaemon.IP}");
                sb.AppendLine($"Port: {CSIAutoPlannerSettings.ImportExportData.AriaDBDaemon.Port}");
            }
            if (CSIAutoPlannerSettings.ImportExportData.VMSFileDaemon.IsInitialized)
            {
                sb.AppendLine("Aria VMS File daemon:");
                sb.AppendLine($"AE Title: {CSIAutoPlannerSettings.ImportExportData.VMSFileDaemon.AETitle}");
                sb.AppendLine($"IP: {CSIAutoPlannerSettings.ImportExportData.VMSFileDaemon.IP}");
                sb.AppendLine($"Port: {CSIAutoPlannerSettings.ImportExportData.VMSFileDaemon.Port}");
            }
            if (CSIAutoPlannerSettings.ImportExportData.LocalDaemon.IsInitialized)
            {
                sb.AppendLine("Local daemon:");
                sb.AppendLine($"AE Title: {CSIAutoPlannerSettings.ImportExportData.LocalDaemon.AETitle}");
                sb.AppendLine($"Port: {CSIAutoPlannerSettings.ImportExportData.LocalDaemon.Port}");
            }
            sb.AppendLine();

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
