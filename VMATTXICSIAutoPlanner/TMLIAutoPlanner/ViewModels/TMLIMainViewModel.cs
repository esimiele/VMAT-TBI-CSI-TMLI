using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
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
using System.Windows.Media;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.EnumTypeHelpers;
using System.Windows.Media.Media3D;
using AutoPlannerHelpers.EqualityComparers;
using AutoPlannerHelpers.PlanTemplateModels;
using ExternalPlanSetup = VMS.TPS.Common.Model.API.ExternalPlanSetup;

namespace TMLIAutoPlanner.ViewModels
{
    public class TMLIMainViewModel : BaseViewModel
    {
        #region properties
        private SolidColorBrush _prepForTargetsBackground;
        private SolidColorBrush _setTargetsTabBackground;
        private SolidColorBrush _stitchCTTabBackground;
        private Visibility _stitchCTTabVisible;
        private int _initialTabSelected;
        public SolidColorBrush PrepForTargetsBackground
        {
            get { return _prepForTargetsBackground; }
            set { SetProperty(ref _prepForTargetsBackground, value); }
        }

        public SolidColorBrush SetTargetsTabBackground
        {
            get { return _setTargetsTabBackground; }
            set { SetProperty(ref _setTargetsTabBackground, value); }
        }

        public SolidColorBrush StitchCTTabBackground
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
        private object _exportCT;
        private object _importSS;
        private object _stitchCT;
        private object _prepForTargets;
        private object _ringGeneration;

        public object StitchCT
        {
            get { return _stitchCT; }
            set { SetProperty(ref _stitchCT, value); }
        }

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
        #endregion

        public TMLIMainViewModel(string[] args) :
            base(PlanType.VMAT_TMLI, args)
        {
            Initialize();
        }

        public void Initialize()
        {
            _generalConfigurationFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TMLI_config.ini";
            LoadScriptConfigurationSettings(_generalConfigurationFile);
            LoadPlanTemplates();

            StitchCT = new CTStitcherView { DataContext = new CTStitcherViewModel() };
            StitchCTTabBackground = Brushes.LightGray;
            if (!TMLIAutoPlannerSettings.ShowStitchCTTab)
            {
                StitchCTTabVisible = Visibility.Collapsed;
                InitialTabSelected = 1;
            }

            ExportCT = new CTExportView { DataContext = new CTExportViewModel() };
            ImportSS = new ImportSSView { DataContext = new ImportSSViewModel(TMLIAutoPlannerSettings.ImportExportData, PlanType.VMAT_CSI, (!ReferenceEquals(EclipseContext.GetInstance().Patient, null) ? EclipseContext.GetInstance().Patient.Id : "")) };
            //PrepForTargets = new PrepForTargetsView { DataContext = new PrepForTargetsViewModel() };
            PrepForTargets = new StructureDerivationsView { DataContext = new StructureDerivationsViewModel(_structureIdsPostUnion, true) };
            WeakReferenceMessenger.Default.Send(new RequestUpdateTargetStructures(TMLIAutoPlannerSettings.RequestedPreliminaryTargets));

            RingGeneration = new RingGenerationView { DataContext = new RingGenerationViewModel(_structureIdsPostUnion) };

            if (TMLIAutoPlannerSettings.AllBeamsVMAT) WeakReferenceMessenger.Default.Send(new RequestHideNumberOfVMATIsocenters());
            WeakReferenceMessenger.Default.Send(new RequestUpdateBeamPlacementDefaultSettings(TMLIAutoPlannerSettings.AvailableLinacs,
                                                                                              TMLIAutoPlannerSettings.AvailableEnergies,
                                                                                              TMLIAutoPlannerSettings.ContourFieldOverlap,
                                                                                              TMLIAutoPlannerSettings.ContourFieldOverlapMarginInCM,
                                                                                              TMLIAutoPlannerSettings.BeamsPerIsocenter));

            QuickStartGuideCommand = new RelayCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new RelayCommand(LaunchHelpGuide);
            PTVMarginInfoCommand = new RelayCommand(ShowPTVMarginInfo);

            //needs to be initialized after the plan templates are loaded
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
            SpecifyTargetsTabBackground = Brushes.PaleVioletRed;
            if (EclipseContext.GetInstance().IsInitialized)
            {
                if(!ReferenceEquals(EclipseContext.GetInstance().Patient, null)) PatientMRN = EclipseContext.GetInstance().Patient.Id;
                if (EclipseContext.GetInstance().CTImages.Any())
                {
                    WeakReferenceMessenger.Default.Send(new RequestUpdateCTList(EclipseContext.GetInstance().CTImages.ToList().ConvertAll(x => new ExportCTModel(x.Series.Id, x.Id, x.ZSize, x.HistoryDateTime.ToString()))));
                }
                if(!ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
                {
                    StructureSetId = EclipseContext.GetInstance().StructureSet.Id;
                    if (EclipseContext.GetInstance().StructureSet.Structures.Any(x => x.ApprovalHistory.First().ApprovalStatus == StructureApprovalStatus.Approved && x.Id.ToLower().Contains("ptv")))
                    {
                        SetTargetsTabBackground = Brushes.PaleVioletRed;
                        PrepForTargetsBackground = Brushes.LightGray;
                    }
                    else
                    {
                        PrepForTargetsBackground = Brushes.PaleVioletRed;
                        SetTargetsTabBackground = Brushes.LightGray;
                    }
                }
            }
            else
            {
                PrepForTargetsBackground = Brushes.LightGray;
                SetTargetsTabBackground = Brushes.LightGray;
                List<ExportCTModel> models = new List<ExportCTModel>
                {
                    new ExportCTModel("1", "CT 1", 100, DateTime.Now.ToString("yyyy-mm-dd")),
                    new ExportCTModel("2", "CT 2", 200, "2019-01-01"),
                    new ExportCTModel("3", "CT 3", 300, "2020-10-10"),
                };
                WeakReferenceMessenger.Default.Send(new RequestUpdateCTList(models));
            }
            InitializeTMLIMessengers();
        }

        private void InitializeTMLIMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestExportCT>(this, (r, m) =>
            {
                ExportCTImage(m.SelectedCTImage);
            });
            WeakReferenceMessenger.Default.Register<RequestGeneratePreliminaryTargets>(this, (r, m) =>
            {
                PreparePreliminaryTargets(m.Targets);
            });
            WeakReferenceMessenger.Default.Register<RequestAreSeparatedPlansAutomaticallyRecalculated>(this, (r, m) =>
            {
                m.Reply(TMLIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep);
            });
        }

        public void ExportCTImage(ExportCTModel selectedImage)
        {
            if (ReferenceEquals(selectedImage, null) || !EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().Patient,null) || !EclipseContext.GetInstance().CTImages.Any()) return;
            CTImageExport imageExport = new CTImageExport(EclipseContext.GetInstance().CTImages.First(x => string.Equals(x.Id, selectedImage.CTId)),
                                                          EclipseContext.GetInstance().Patient.Id,
                                                          TMLIAutoPlannerSettings.ImportExportData,
                                                          PlanType.VMAT_TMLI,
                                                          TMLIAutoPlannerSettings.CloseProgressWindowOnFinish);
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

        private void ShowPTVMarginInfo()
        {
            MessageBox.Show("test");
        }
        #endregion

        #region specify targets
        private void PreparePreliminaryTargets(List<RequestedTSStructureModel> preliminaryTargets)
        {
            if (!EclipseContext.GetInstance().IsInitialized || !preliminaryTargets.Any()) return;
            List<RequestedTSManipulationModel> targetCropOperations = new List<RequestedTSManipulationModel> { };
            bool includeTestesInPTV = true;
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                targetCropOperations.AddRange(_selectedTemplate.TSManipulations.Where(x => x.ManipulationType == TSManipulationType.CropTargetFromStructure));
                if(_selectedTemplate.PlanTargets.Any() && _selectedTemplate.PlanTargets.SelectMany(x => x.Targets).OrderByDescending(x => x.TargetRxDose).First().TargetRxDose <= 200) includeTestesInPTV = false;
            }
            GeneratePreliminaryTargets_TMLI generateTargets = new GeneratePreliminaryTargets_TMLI(preliminaryTargets, 
                                                                                                  targetCropOperations,
                                                                                                  includeTestesInPTV);
            EclipseContext.GetInstance().Patient.BeginModifications();
            bool result = generateTargets.Execute();
            //grab the log output regardless if it passes or fails
            Logger.GetInstance().AppendLogOutput("Preliminary target generation output:", generateTargets.LogOutput);
            Logger.GetInstance().OpType = ScriptOperationType.GeneratePrelimTargets;
            if (result) return;
            Logger.GetInstance().AddedPrelimTargetsStructures = generateTargets.GetAddedTargetStructures();
            PrepForTargetsBackground = Brushes.ForestGreen;
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
        protected override void PerformTSStructureGenerationManipulation(List<RequestedTSStructureModel> structuresToGenerate, List<RequestedTSManipulationModel> manipulations)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                Logger.GetInstance().LogError("Error! Script is not connected to aria or no structure set loaded! Cannot perform TS generation/manipulation!");
                return;
            }
            List<RequestedTSManipulationModel> tsManipulations = manipulations;
            List<TSRingStructureModel> rings = WeakReferenceMessenger.Default.Send(new RequestRingStructures());
            TSGenerationManipulation_TMLI generateTS = new TSGenerationManipulation_TMLI(structuresToGenerate,
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
                WeakReferenceMessenger.Default.Send(new RequestUpdateTSManipulationList(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id), generateTS.TSManipulationList));
            }
            _planIsocenters = generateTS.PlanIsocentersList;

            WeakReferenceMessenger.Default.Send(new RequestUpdatePlanIsocenterList(_planIsocenters));
            WeakReferenceMessenger.Default.Send(new RequestUpdateStructureIds(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id)));
            _planOptimizationSetup = UpdateOptimizationConstraintsWithRings(generateTS.AddedRings, _planOptimizationSetup, TMLIAutoPlannerSettings.DefaultRingPriority);
            _planOptimizationSetup = UpdateOptimizationConstraintsWithTSTargets(generateTS.PlanTargets, _planOptimizationSetup);

            StructureTuningTabBackground = Brushes.ForestGreen;
            TSManipulationTabBackground = Brushes.ForestGreen;
            BeamPlacementTabBackground = Brushes.PaleVioletRed;

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
        protected override void GeneratePlansAndPlaceBeams(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                Logger.GetInstance().LogError("Error! Script is not connected to aria or no structure set loaded! Cannot perform beam placement!");
                return;
            }
            _planIsocenters = PlanIsocenters;
            GeneratePlansAndPlaceBeams_TMLI placeBeams = new GeneratePlansAndPlaceBeams_TMLI(_planIsocenters,
                                                                                           _prescriptions,
                                                                                           linac,
                                                                                           energy,
                                                                                           contourOverlap,
                                                                                           overlapMargin);
            bool failed = placeBeams.Execute();
            Logger.GetInstance().AppendLogOutput("Generate plans and place beams output:", placeBeams.GetLogOutput());
            if (failed) return;
            if (placeBeams.VMATPlans.Any())
            {
                EclipseContext.GetInstance().VMATPlans = placeBeams.VMATPlans;
                Logger.GetInstance().PlanUIDs = placeBeams.VMATPlans.Select(x => x.UID).ToList();
            }
            if(placeBeams.FieldJunctions.Any())
            {
                _planOptimizationSetup = UpdateOptimizationConstraintsWithTSJunctions(placeBeams.FieldJunctions, _planOptimizationSetup);
                WeakReferenceMessenger.Default.Send(new RequestUpdateStructureIds(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id)));
            }
            WeakReferenceMessenger.Default.Send(new RequestUpdateOptimizationConstraintsMessage(_planOptimizationSetup));

            BeamPlacementTabBackground = Brushes.ForestGreen;
            OptimizationSetupTabBackground = Brushes.PaleVioletRed;
        }
        #endregion

        #region prepare for treatment
        protected override bool GenerateShiftNote()
        {
            ExternalPlanSetup plan = PlanPrepHelper.RetrieveVMATPlan(!string.IsNullOrEmpty(TMLIAutoPlannerSettings.CourseId) ? TMLIAutoPlannerSettings.CourseId : "VMAT-TMLI", PlanType.VMAT_TMLI);
            if (!ReferenceEquals(plan, null)) EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { plan };
            else return true;
            if (EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("legs")))
            {
                if (EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("legs")).Any(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"The AP/PA plan {EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("legs")).ToList().First(x => x.TreatmentOrientation != PatientOrientation.FeetFirstSupine).Id} is NOT in the FFS orientation!");
                    sb.AppendLine("THE COUCH SHIFTS FOR THESE PLANS WILL NOT BE ACCURATE! Please fix and try again!");
                    Logger.GetInstance().LogError(sb.ToString());
                    return true;
                }
            }

            Clipboard.SetText(PlanPrepHelper.GetTBITMLIShiftNote(EclipseContext.GetInstance().VMATPlans.First(), EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("legs")).ToList()).ToString());
            return false;
        }
        protected override bool SeparatePlans()
        {
            //separate the plans
            EclipseContext.GetInstance().Patient.BeginModifications();
            _planPrep = new PreparePlansForTreatment_TMLI();
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

        protected override void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            InitialDosePerFraction = _selectedTemplate.InitialRxDosePerFx;
            InitialNumberOfFractions = _selectedTemplate.InitialRxNumberOfFractions;
            WeakReferenceMessenger.Default.Send(new RequestAutoPlanTemplateChangedMessage(_selectedTemplate));
            List<RequestedTSStructureModel> prelimTargets = new List<RequestedTSStructureModel>(TMLIAutoPlannerSettings.RequestedPreliminaryTargets);
            prelimTargets.AddRange((_selectedTemplate as TMLIAutoPlanTemplate).RequestedPreliminaryTargets);
            WeakReferenceMessenger.Default.Send(new RequestUpdateTargetDerivationOperations(_selectedTemplate.TargetDerivationOperations));
            WeakReferenceMessenger.Default.Send(new RequestUpdateTargetStructures(prelimTargets.Distinct(new RequestedTSStructureModelComparer()).ToList()));
            Logger.GetInstance().Template = _selectedTemplate.TemplateName;
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
                                else if (parameter == "img export location")
                                {
                                    string result = ConfigurationHelper.VerifyPathIntegrity(value);
                                    if (!string.IsNullOrEmpty(result)) TMLIAutoPlannerSettings.ImportExportData.WriteLocation = result;
                                    else Logger.GetInstance().LogError($"Warning! {value} does NOT exist!");
                                }
                                else if (parameter == "RTStruct import location")
                                {
                                    string result = ConfigurationHelper.VerifyPathIntegrity(value);
                                    if (!string.IsNullOrEmpty(result)) TMLIAutoPlannerSettings.ImportExportData.ImportLocation = result;
                                    else Logger.GetInstance().LogError($"Warning! {value} does NOT exist!");
                                }
                                else if (parameter == "img export format")
                                {
                                    if (string.Equals(value, "dcm") || string.Equals(value, "png")) TMLIAutoPlannerSettings.ImportExportData.ExportFormat = ExportFormatTypeHelper.GetExportFormatType(value);
                                    else Logger.GetInstance().LogError("Only png and dcm image formats are supported for export!");
                                }
                                else if (parameter.Contains("daemon"))
                                {
                                    //CONTINUE HERE 070523!
                                    DaemonModel result = ConfigurationHelper.ParseDaemonSettings(line);
                                    if (result.Port != -1)
                                    {
                                        if (parameter.ToLower().Contains("aria")) TMLIAutoPlannerSettings.ImportExportData.AriaDBDaemon = result;
                                        else if (parameter.ToLower().Contains("vms file")) TMLIAutoPlannerSettings.ImportExportData.VMSFileDaemon = result;
                                        else if (parameter.ToLower().Contains("local")) TMLIAutoPlannerSettings.ImportExportData.LocalDaemon = result;
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
                                else if (parameter == "auto dose recalculation") TMLIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep = bool.Parse(value);
                            }
                            else if (line.Contains("create preliminary target")) TMLIAutoPlannerSettings.RequestedPreliminaryTargets.Add(ConfigurationHelper.ParseCreateTS(line));
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

            sb.AppendLine($"Import/export settings:");
            sb.AppendLine($"Image export path: {TMLIAutoPlannerSettings.ImportExportData.WriteLocation}");
            sb.AppendLine($"RT structure set import path: {TMLIAutoPlannerSettings.ImportExportData.ImportLocation}");
            sb.AppendLine($"Image export format: {TMLIAutoPlannerSettings.ImportExportData.ExportFormat}");

            if (TMLIAutoPlannerSettings.ImportExportData.AriaDBDaemon.IsInitialized)
            {
                sb.AppendLine("Aria database daemon:");
                sb.AppendLine($"AE Title: {TMLIAutoPlannerSettings.ImportExportData.AriaDBDaemon.AETitle}");
                sb.AppendLine($"IP: {TMLIAutoPlannerSettings.ImportExportData.AriaDBDaemon.IP}");
                sb.AppendLine($"Port: {TMLIAutoPlannerSettings.ImportExportData.AriaDBDaemon.Port}");
            }
            if (TMLIAutoPlannerSettings.ImportExportData.VMSFileDaemon.IsInitialized)
            {
                sb.AppendLine("Aria VMS File daemon:");
                sb.AppendLine($"AE Title: {TMLIAutoPlannerSettings.ImportExportData.VMSFileDaemon.AETitle}");
                sb.AppendLine($"IP: {TMLIAutoPlannerSettings.ImportExportData.VMSFileDaemon.IP}");
                sb.AppendLine($"Port: {TMLIAutoPlannerSettings.ImportExportData.VMSFileDaemon.Port}");
            }
            if (TMLIAutoPlannerSettings.ImportExportData.LocalDaemon.IsInitialized)
            {
                sb.AppendLine("Local daemon:");
                sb.AppendLine($"AE Title: {TMLIAutoPlannerSettings.ImportExportData.LocalDaemon.AETitle}");
                sb.AppendLine($"Port: {TMLIAutoPlannerSettings.ImportExportData.LocalDaemon.Port}");
            }
            sb.AppendLine();

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

        protected override void PerformOptimizationStructureDerivation(List<StructureOperationModel> operations)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
