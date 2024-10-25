using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.UIHelpers;
using System.Windows;
using AutoPlannerHelpers.ViewModels;
using Prism.Commands;
using Prism.Mvvm;
using AutoPlannerOptimizationLoop.DataContainers;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerOptimizationLoop.Core;
using AutoPlannerHelpers.Prompts;
using AutoPlannerOptimizationLoop.Prompts;
using VMS.TPS.Common.Model.Types;
using System.Drawing.Drawing2D;
using VMS.TPS.Common.Model.API;
using AutoPlannerOptimizationLoop.Settings;

namespace AutoPlannerOptimizationLoop.ViewModels
{
    public class OptimizationLoopMainViewModel : BindableBase
    {
        public ObservableCollection<AutoPlanTemplateBase> PlanTemplates { get; set; }
        public ObservableCollectionPropertyNotify<PlanObjectiveModel> PlanObjectives { get; set; }
        public ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> PlanOptimizationConstraints { get; set; }

        #region properties
        private string _logFilePath;
        private string _documentationPath;
        private double _threshold;
        private double _lowDoseLimit;
        private bool _isDemo;
        private List<string> _reminders = new List<string> { };
        private string _mrn;
        private PlanType _planType;
        private string _planId;
        private AutoPlanTemplateBase _selectedTemplate;
        private double _basePlanDosePerFraction;
        private int _basePlanNumberOfFractions;
        private double _basePlanTotalDose;
        private string _basePlanNormalizationVolume;
        private bool _runCoverageCheck;
        private bool _copyAndSaveEachPlan;
        private int _maxNumberOfIterations;
        private bool _runOneAdditionalOptimization;
        private double _planNormalizationValue;
        private string _scriptConfiguration;
        private List<string> _structureIds;

        public string MRN
        {
            get { return _mrn; }
            set { SetProperty(ref _mrn, value); }
        }

        public string PlanId
        {
            get { return _planId; }
            set { _planId = value; }
        }

        public AutoPlanTemplateBase SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { SetProperty(ref _selectedTemplate, value); UpdateUIWithSelectedPlanTemplate(); }
        }

        public double BasePlanDosePerFraction
        {
            get { return _basePlanDosePerFraction; }
            set { SetProperty(ref _basePlanDosePerFraction, value); ResetRxDose(); }
        }

        public int BasePlanNumberOfFractions
        {
            get { return _basePlanNumberOfFractions; }
            set { SetProperty(ref _basePlanNumberOfFractions, value); ResetRxDose(); }
        }

        public double BasePlanTotalDose
        {
            get { return _basePlanTotalDose; }
            set { SetProperty(ref _basePlanTotalDose, value); }
        }

        public string BasePlanNormalizationVolume
        {
            get { return _basePlanNormalizationVolume; }
            set { SetProperty(ref _basePlanNormalizationVolume, value); }
        }

        public bool RunCoverageCheck
        {
            get { return _runCoverageCheck; }
            set { SetProperty(ref _runCoverageCheck, value); }
        }

        public bool CopyAndSaveEachPlan
        {
            get { return _copyAndSaveEachPlan; }
            set { SetProperty(ref _copyAndSaveEachPlan, value); }
        }

        public int MaxNumberOfIterations
        {
            get { return _maxNumberOfIterations; }
            set { SetProperty(ref _maxNumberOfIterations, value); }
        }

        public bool RunOneAdditionalOptimization
        {
            get { return _runOneAdditionalOptimization; }
            set { SetProperty(ref _runOneAdditionalOptimization, value); }
        }

        public double PlanNormalizationValue
        {
            get { return _planNormalizationValue; }
            set { SetProperty(ref _planNormalizationValue, value); }
        }

        public string ScriptConfiguration
        {
            get { return _scriptConfiguration; }
            set { SetProperty(ref _scriptConfiguration, value); }
        }

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set { SetProperty(ref _structureIds, value); }
        }
        #endregion

        #region commands
        public DelegateCommand QuickStartCommand { get; set; }
        public DelegateCommand DocumentationCommand { get; set; }
        public DelegateCommand OpenPatientCommand { get; set; }
        public DelegateCommand AddPlanObjectiveCommand { get; set; }
        public DelegateCommand ClearPlanObjectiveListCommand { get; set; }
        public DelegateCommand AddOptimizationConstraintCommand { get; set; }
        public DelegateCommand ClearOptimizationConstraintListCommand { get; set; }
        public DelegateCommand<object> ClearRowCommand { get; set; }
        public DelegateCommand StartOptimizationCommand { get; set; }
        #endregion

        #region fields
        #endregion

        public OptimizationLoopMainViewModel(string[] args)
        {
            QuickStartCommand = new DelegateCommand(QuickStartHelp);
            DocumentationCommand = new DelegateCommand(ShowDocumentation);
            AddPlanObjectiveCommand = new DelegateCommand(AddPlanObjective);
            ClearPlanObjectiveListCommand = new DelegateCommand(ClearPlanObjectives);
            AddOptimizationConstraintCommand = new DelegateCommand(AddOptimizationObjective);
            ClearOptimizationConstraintListCommand = new DelegateCommand(ClearOptimizationConstraints);
            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase> { };
            PlanObjectives = new ObservableCollectionPropertyNotify<PlanObjectiveModel> { };
            PlanOptimizationConstraints = new ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> { };
            ClearRowCommand = new DelegateCommand<object>(ClearRow);
            StartOptimizationCommand = new DelegateCommand(StartOptimization);

            //InitializeUI();
        }

        private void AssignDefaultLogAndDocPaths()
        {
            _logFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs\\";
            _documentationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\documentation\\";
        }

        #region help and documentation
        private void QuickStartHelp()
        {
            MessageBox.Show("It works!");
        }
        private void ShowDocumentation()
        {
            MessageBox.Show("working on it");
        }
        #endregion

        #region initialize
        public void InitializeUI()
        {
            AssignDefaultLogAndDocPaths();
            LoadTemplatePlanChoices(PlanType.VMAT_TBI);
            LoadConfigurationSettingsForPlanType(_planType);
            //DisplayConfigurationParameters(configurationFile);

            _planType = PlanType.VMAT_TBI;
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null) || !EclipseContext.GetInstance().VMATPlans.Any())
            {
                Logger.GetInstance().LogError("Error! Structure set, Application, or Plan is null! Unable to assign normalization volume!", true);
                List<string> structures = PlanTemplates.SelectMany(x => x.PlanObjectives).Select(x => x.StructureId).ToList();
                //structures.AddRange(PlanTemplates.SelectMany(x => x.InitialOptimizationConstraints).Select(x => x.StructureId).ToList());
                StructureIds = structures.Distinct().ToList();
                return;
            }

            StructureIds = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
            //PlanId = EclipseContext.GetInstance().Plan.Id;
            if (StructureTuningHelper.DoesStructureExistInSS("_Matchline", EclipseContext.GetInstance().StructureSet, true))
            {
                if (StructureTuningHelper.DoesStructureExistInSS("PTV^Upper", EclipseContext.GetInstance().StructureSet, true))
                {
                    BasePlanNormalizationVolume = StructureTuningHelper.GetStructureFromId("PTV^Upper", EclipseContext.GetInstance().StructureSet).Id;
                }
                else
                {
                    Logger.GetInstance().LogError($"Error! Matchline structure exists in structure set, but PTV^Upper does not exist! Unable to proceed!");
                    return;
                }
            }
            else if (StructureTuningHelper.DoesStructureExistInSS("PTV^Body", EclipseContext.GetInstance().StructureSet, true))
            {
                BasePlanNormalizationVolume = StructureTuningHelper.GetStructureFromId("PTV^Body", EclipseContext.GetInstance().StructureSet).Id;
            }
        }
        #endregion

        #region load and open patient
        /// <summary>
        /// Utility method to load a patient into the script. Attempt to read the log file from the preparation script
        /// </summary>
        /// <param name="patmrn"></param>
        private void LoadPatient(string patmrn)
        {
            string currentMRN = _mrn;
            string mrn = patmrn;
            string fullLogName;
            bool cancel = false;
            if (string.IsNullOrEmpty(patmrn))
            {
                (bool, string, string) result = PromptUserForPatientSelection();
                cancel = result.Item1;
                mrn = result.Item2;
                fullLogName = result.Item3;
            }
            else
            {
                fullLogName = LogHelper.GetFullLogFileFromExistingMRN(mrn, _logFilePath);
            }
            if (!cancel)
            {
                if (!string.IsNullOrEmpty(mrn))
                {
                    if (!string.Equals(mrn, currentMRN))
                    {
                        if (!string.IsNullOrEmpty(fullLogName))
                        {
                            if (!LoadLogFile(fullLogName)) OptimizationLoopSettings.PlanPreparationLogFileLoaded = true;
                        }
                        LoadConfigurationSettingsForPlanType(_planType);
                        OpenPatient(mrn);
                        LoadTemplatePlanChoices(_planType);
                        if (_planType == PlanType.VMAT_TBI && OptimizationLoopSettings.Reminders.Any(x => x.ToLower().Contains("base dose")))
                        {
                            if (!EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("legs"))) OptimizationLoopSettings.Reminders.Remove(OptimizationLoopSettings.Reminders.First(x => x.ToLower().Contains("base dose")));
                        }
                        //selectPatientBtn.Background = System.Windows.Media.Brushes.DarkGray;
                    }
                }
                else MessageBox.Show($"Entered MRN: {mrn} is invalid! Please re-enter and try again");
            }
        }

        private (bool, string, string) PromptUserForPatientSelection()
        {
            //open the patient with the user-entered MRN number
            bool cancel = false;
            string mrn = "";
            string fullLogName = "";
            SelectPatient sp = new SelectPatient(_logFilePath);
            sp.ShowDialog();
            if (!sp.selectionMade)
            {
                cancel = true;
                return (cancel, mrn, fullLogName); ;
            }
            else
            {
                (string, string) result = sp.GetPatientMRN();
                mrn = result.Item1;
                fullLogName = result.Item2;
            }

            return (cancel, mrn, fullLogName);
        }

        private void OpenPatient(string pat_mrn)
        {
            try
            {
                //pi = app.OpenPatientById(pat_mrn);
                //MRN = EclipseContext.GetInstance().Patient.Id;
                ////grab instances of the course and VMAT tbi plans that were created using the binary plug in script. This is explicitly here to let the user know if there is a problem with the course OR plan
                ////Course c = pi.Courses.FirstOrDefault(x => x.Id.ToLower() == "vmat tbi");
                //GetStructureSetAndPlans();
                //if (!EclipseContext.GetInstance().VMATPlans.Any())
                //{
                //    MessageBox.Show("No plans found!");
                //    return;
                //}
                ////ensure the correct plan target is selected and all requested objectives have a matching structure that exists in the structure set (needs to be done after structure set has been assinged)
                //PopulateOptimizationTab(optimizationParamSP);

                ////populate the prescription text boxes with the prescription stored in the VMAT TBI plan
                //PopulateRx();

                //planObjectiveHeader.Background = System.Windows.Media.Brushes.PaleVioletRed;
            }
            catch
            {
                MessageBox.Show("No such patient exists!");
            }
        }

        /// <summary>
        /// Helper method to retrieve the structure set and list of plans
        /// </summary>
        /// <returns></returns>
        private void GetStructureSetAndPlans()
        {
            //grab an instance of the VMAT TBI plan. Return null if it isn't found
            if (OptimizationLoopSettings.PlanUIDs.Any())
            {
                //should automatically be in order in terms of cumulative Rx (lowest to highest)
                foreach (string uid in OptimizationLoopSettings.PlanUIDs)
                {
                    ExternalPlanSetup tmp = EclipseContext.GetInstance().Patient.Courses.SelectMany(x => x.ExternalPlanSetups).FirstOrDefault(x => string.Equals(x.UID, uid));
                    if (tmp != null) EclipseContext.GetInstance().VMATPlans.Add(tmp);
                }
            }
            else
            {
                //simple logic to try and guess which plans are which
                Course theCourse = null;
                List<Course> courses = EclipseContext.GetInstance().Patient.Courses.Where(x => x.Id.ToLower().Contains("vmat csi") || x.Id.ToLower().Contains("vmat tbi")).ToList();
                if (!courses.Any()) return;
                if (courses.Count > 1)
                {
                    SelectItemPrompt SIP = new SelectItemPrompt("Please select a course:", courses.Select(x => x.Id).ToList());
                    SIP.ShowDialog();
                    if (!SIP.GetSelection()) return;
                    theCourse = courses.FirstOrDefault(x => string.Equals(x.Id, SIP.GetSelectedItem()));
                }
                else theCourse = courses.First();
                if (theCourse.Id.ToLower().Contains("csi"))
                {
                    _planType = PlanType.VMAT_CSI;
                }
                else
                {
                    _planType = PlanType.VMAT_TBI;
                }

                List<ExternalPlanSetup> thePlans = theCourse.ExternalPlanSetups.OrderBy(x => x.CreationDateTime).ToList();
                if (thePlans.Count > 2)
                {
                    MessageBox.Show($"Error! More than two plans found in course: {theCourse.Id}! Unable to determine which plan(s) should be used for optimization! Exiting!");
                }
                else if (thePlans.Count < 1)
                {
                    MessageBox.Show($"Error! No plans found in course: {theCourse.Id}! Unable to determine which plan(s) should be used for optimization! Exiting!");
                }
                else if (thePlans.Count == 2 && (thePlans.First().StructureSet != thePlans.Last().StructureSet))
                {
                    MessageBox.Show($"Error! Structure set in first plan ({thePlans.First().Id}) is not the same as the structure set in second plan ({thePlans.Last().Id})! Exiting!");
                }
                else
                {
                    EclipseContext.GetInstance().VMATPlans.AddRange(thePlans);
                }
            }
        }
        #endregion

        private void ResetRxDose()
        {
            if (BasePlanNumberOfFractions > 0 && BasePlanDosePerFraction > 0)
            {
                double priorTotalDose = BasePlanTotalDose;
                BasePlanTotalDose = BasePlanDosePerFraction * BasePlanNumberOfFractions;
                if (BasePlanTotalDose != priorTotalDose)
                {
                    foreach (PlanObjectiveModel itr in PlanObjectives)
                    {
                        if (itr.QueryDoseUnits == Units.cGy)
                        {
                            itr.QueryDose = Math.Round(itr.QueryDose * BasePlanTotalDose / priorTotalDose, 1);
                        }
                    }
                    PlanObjectives.Refresh();
                    //foreach (OptimizationConstraintModel itr in OptimizationConstraints)
                    //{
                    //    if (itr.QueryDoseUnits == Units.cGy)
                    //    {
                    //        itr.QueryDose = Math.Round(itr.QueryDose * BasePlanTotalDose / priorTotalDose, 1);
                    //    }
                    //}
                    //OptimizationConstraints.Refresh();
                }
            }
        }

        private void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;

            //BasePlanDosePerFraction = _selectedTemplate.InitialRxDosePerFx;
            //BasePlanNumberOfFractions = _selectedTemplate.InitialRxNumberOfFractions;
            //BasePlanTotalDose = _selectedTemplate.InitialRxNumberOfFractions * _selectedTemplate.InitialRxDosePerFx;
            //PlanObjectives.Clear();
            //foreach (PlanObjectiveModel itr in _selectedTemplate.PlanObjectives)
            //{
            //    PlanObjectives.Add(new PlanObjectiveModel(itr));
            //}
            //PlanOptimizationConstraints.Clear();
            //foreach (OptimizationConstraintModel itr in _selectedTemplate.InitialOptimizationConstraints)
            //{
            //    PlanOptimizationConstraints.Add(new OptimizationConstraintModel(itr));
            //}
        }

        public void AddPlanObjective()
        {
            if (!ReferenceEquals(PlanObjectives, null))
            {
                PlanObjectives.Add(new PlanObjectiveModel(_structureIds.First(), OptimizationObjectiveType.None, 0, Units.None, 0, Units.None));
            }
        }

        public void ClearPlanObjectives()
        {
            PlanObjectives.Clear();
        }

        public void AddOptimizationObjective()
        {
            if (PlanOptimizationConstraints.Count() == 1)
            {
                //logic for multiple plans
                SelectItemPrompt SIP = new SelectItemPrompt("Please selct a plan to add a constraint!", new List<string>(PlanOptimizationConstraints.Select(x => x.PlanId)));
                SIP.ShowDialog();
                if (!SIP.GetSelection()) return;
                PlanOptimizationSetupModel planOptSetupModel = PlanOptimizationConstraints.First(x => string.Equals(x.PlanId, SIP.GetSelectedItem()));
                List<OptimizationConstraintModel> constraints = planOptSetupModel.OptimizationConstraints;
                constraints.Add(GenerateNewEmptyOptimizationConstraint());
                PlanOptimizationConstraints.Refresh();
            }
            else if (!PlanOptimizationConstraints.Any())
            {
                PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel("1", GenerateNewEmptyOptimizationConstraint()));
            }
            else
            {
                PlanOptimizationConstraints.First().OptimizationConstraints.Add(GenerateNewEmptyOptimizationConstraint());
                PlanOptimizationConstraints.Refresh();
            }
        }

        private OptimizationConstraintModel GenerateNewEmptyOptimizationConstraint()
        {
            return new OptimizationConstraintModel(_structureIds.First(), OptimizationObjectiveType.None, 0.0, Units.None, 0.0, 0);
        }

        public void ClearOptimizationConstraints()
        {
            PlanOptimizationConstraints.Clear();
        }

        public void ClearRow(object o)
        {
            if (o.GetType() == typeof(PlanObjectiveModel))
            {
                PlanObjectiveModel p = o as PlanObjectiveModel;
                if (PlanObjectives.Contains(p))
                {
                    PlanObjectives.Remove(p);
                }
            }
            else
            {
                OptimizationConstraintModel opt = o as OptimizationConstraintModel;
                if (PlanOptimizationConstraints.SelectMany(x => x.OptimizationConstraints).Contains(opt))
                {
                    PlanOptimizationSetupModel planOptSetupModel = PlanOptimizationConstraints.First(x => x.OptimizationConstraints.Contains(opt));
                    List<OptimizationConstraintModel> constraints = planOptSetupModel.OptimizationConstraints;
                    constraints.Remove(opt);
                    PlanOptimizationConstraints.Refresh();
                }
            }
        }

        public void StartOptimization()
        {
            //if (!EclipseContext.GetInstance().IsInitialized)
            //{
            //    Logger.GetInstance().LogError("Script is not initialized! Unable to generate AP/PA plan for TBI patient!");
            //    return;
            //}
            //if (ReferenceEquals(EclipseContext.GetInstance().Patient, null) || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null) || !EclipseContext.GetInstance().VMATPlans.Any())
            //{
            //    Logger.GetInstance().LogError("Error! Patient, structure set, or plan are null! Unable to proceed!");
            //    return;
            //}
            //Logger.GetInstance().AppendLogOutput("Checking for valid constraints and objectives");

            //StringBuilder sb = new StringBuilder();
            //if (objectives.Any())
            //{
            //    foreach (PlanObjectiveModel itr in objectives)
            //    {
            //        sb.AppendLine($"{itr.StructureId}, {itr.ConstraintType}, {itr.QueryVolume}, {itr.QueryDose}, {itr.QueryDoseUnits}");
            //    }
            //}
            //else sb.AppendLine("No plan objectives in list");
            //sb.AppendLine(" ");
            //if(constraints.Any())
            //{
            //    foreach(OptimizationConstraintModel itr in constraints)
            //    {
            //        sb.AppendLine($"{itr.StructureId}, {itr.ConstraintType}, {itr.QueryDose}, {itr.QueryDoseUnits}, {itr.QueryVolume}, {itr.QueryVolumeUnits}, {itr.Priority}");
            //    }
            //}
            //else sb.AppendLine("No optimization constraints in list");

            //MessageBox.Show(sb.ToString());
            //List<OptimizationConstraintModel> constraints = OptimizationConstraints.Where(x => x.IsValidConstraint).ToList();
            //OptimizationSetupHelper.AssignOptConstraints(constraints, EclipseContext.GetInstance().Plan, false, 0.0);
            //OptDataContainer _data = GenerateOptimizationDataContainer();
            //VMATTBIOptimization opt = new VMATTBIOptimization(_data);
            VMATTBIOptimization opt = new VMATTBIOptimization(new List<OptimizationConstraintModel> { new OptimizationConstraintModel("test", OptimizationObjectiveType.Lower, 100, Units.cGy, 100, 100)});
            if (opt.Execute()) return;

        }

        public OptDataContainer GenerateOptimizationDataContainer()
        {
            List<PrescriptionModel> prescriptions = new List<PrescriptionModel>
            {
                //new PrescriptionModel(EclipseContext.GetInstance().Plan.Id, "PTV^Body", BasePlanNumberOfFractions, new DoseValue(BasePlanDosePerFraction, DoseValue.DoseUnit.cGy), BasePlanDosePerFraction * BasePlanNumberOfFractions)
            };
            Dictionary<string, string> normalizationVolumes = new Dictionary<string, string> { };
            //normalizationVolumes.Add(EclipseContext.GetInstance().Plan.Id, BasePlanNormalizationVolume);
            List<RequestedOptimizationTSStructureModel> requestedOptStructures = new List<RequestedOptimizationTSStructureModel> { };
            List<RequestedPlanMetricModel> requestedPlanMetrics = new List<RequestedPlanMetricModel> { };
            if (!ReferenceEquals(SelectedTemplate, null))
            {
                requestedOptStructures.AddRange(SelectedTemplate.RequestedOptimizationTSStructures);
                requestedPlanMetrics.AddRange(SelectedTemplate.RequestedPlanMetrics);
            }

            List<PlanObjectiveModel> objectives = PlanObjectives.Where(x => x.IsValidObjective).ToList();
            return new OptDataContainer(EclipseContext.GetInstance().VMATPlans,
                                        prescriptions,
                                        normalizationVolumes,
                                        objectives,
                                        requestedOptStructures,
                                        requestedPlanMetrics,
                                        PlanType.VMAT_TBI,
                                        PlanNormalizationValue,
                                        MaxNumberOfIterations,
                                        RunCoverageCheck,
                                        RunOneAdditionalOptimization,
                                        CopyAndSaveEachPlan,
                                        false,
                                        _threshold,
                                        _lowDoseLimit,
                                        _isDemo,
                                        _logFilePath,
                                        EclipseContext.GetInstance().Application);
        }

        #region script configuration
        /// <summary>
        /// Helper method to read all the plan template files from the appropriate directory depending on the plan type being considered
        /// </summary>
        /// <param name="type"></param>
        private void LoadTemplatePlanChoices(PlanType type)
        {
            int count = 1;
            SearchOption option = SearchOption.AllDirectories;
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\templates\\TBI\\";
            PlanTemplates.Clear();
            try
            {
                foreach (string itr in Directory.GetFiles(path, "*.ini", option).OrderBy(x => x))
                {
                    PlanTemplates.Add(ConfigurationHelper.ReadTBITemplatePlan(itr, count++));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error could not load plan template file because: {e.Message}!");
            }
        }

        /// <summary>
        /// Utility method to read the configuration .ini file and load the requested settings into memory
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool LoadConfigurationSettings(string file)
        {
            try
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line) && line.Substring(0, 1) != "%")
                        {
                            //useful info on this line
                            if (line.Contains("="))
                            {
                                string parameter = line.Substring(0, line.IndexOf("="));
                                string value = line.Substring(line.IndexOf("=") + 1, line.Length - line.IndexOf("=") - 1);
                                if (double.TryParse(value, out double result))
                                {
                                    if (parameter == "default number of optimizations") MaxNumberOfIterations = int.Parse(value);
                                    else if (parameter == "default plan normalization") PlanNormalizationValue = double.Parse(value);
                                    else if (parameter == "decision threshold") _threshold = result;
                                    else if (parameter == "relative lower dose limit") _lowDoseLimit = result;
                                }
                                else if (parameter == "demo")
                                {
                                    if (value != "") _isDemo = bool.Parse(value);
                                }
                                else if (parameter == "run coverage check")
                                {
                                    if (value != "") RunCoverageCheck = bool.Parse(value);
                                }
                                else if (parameter == "run additional optimization")
                                {
                                    if (value != "") RunOneAdditionalOptimization = bool.Parse(value);
                                }
                                else if (parameter == "copy and save each plan")
                                {
                                    if (value != "") CopyAndSaveEachPlan = bool.Parse(value);
                                }
                            }
                            else if (line.Contains("add reminder"))
                            {
                                _reminders.Add(line.Substring(line.IndexOf("{") + 1, line.IndexOf("}") - line.IndexOf("{") - 1));
                            }
                        }
                    }
                    reader.Close();
                }
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error could not load configuration file because: {e.Message}\n\nAssuming default parameters");
                return true;
            }
        }

        /// <summary>
        /// Method to determine which set of configuration parameters to load depending on the type of plan being considered
        /// </summary>
        /// <param name="type"></param>
        private void LoadConfigurationSettingsForPlanType(PlanType type)
        {
            List<string> configurationFiles = new List<string> { };
            if (type == PlanType.VMAT_CSI)
            {
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_CSI_config.ini");
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\CSI_optimization_config.ini");
            }
            else
            {
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TBI_config.ini");
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\TBI_optimization_config.ini");
            }
            foreach (string itr in configurationFiles)
            {
                if (File.Exists(itr)) LoadConfigurationSettings(itr);
            }
        }

        /// <summary>
        /// Utility method to read the log file from the preparation script for the selected patient 
        /// and store the information so it can be used by this script
        /// </summary>
        /// <param name="fullLogName"></param>
        /// <returns></returns>
        private bool LoadLogFile(string fullLogName)
        {
            try
            {
                using (StreamReader reader = new StreamReader(fullLogName))
                {
                    string line;
                    while (!(line = reader.ReadLine()).Equals("Errors and warnings:"))
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            //useful info on this line
                            if (line.Contains("="))
                            {
                                string parameter = line.Substring(0, line.IndexOf("="));
                                string value = line.Substring(line.IndexOf("=") + 1, line.Length - line.IndexOf("=") - 1);
                                if (parameter == "Plan type")
                                {
                                    if (value.Contains("CSI")) _planType = PlanType.VMAT_CSI;
                                    else if (value.Contains("TBI")) _planType = PlanType.VMAT_TBI;
                                    else _planType = PlanType.VMAT_TMLI;
                                }
                                else if (parameter == "Template")
                                {
                                    //plan objectives will be updated in OpenPatient method
                                    OptimizationLoopSettings.PlanPreparationTemplateUsed = value;
                                }
                            }
                            else if (line.Contains("Prescriptions:"))
                            {
                                while (!string.IsNullOrEmpty((line = reader.ReadLine().Trim())))
                                {
                                    OptimizationLoopSettings.PlanPreparationPrescriptions.Add(LogHelper.ParsePrescriptionsFromLogFile(line));
                                }
                            }
                            else if (line.Contains("Plan UIDs:"))
                            {
                                while (!string.IsNullOrEmpty((line = reader.ReadLine().Trim())))
                                {
                                    OptimizationLoopSettings.PlanUIDs.Add(line);
                                }
                            }
                            else if (line.Contains("TS Targets:"))
                            {
                                while (!string.IsNullOrEmpty((line = reader.ReadLine().Trim())))
                                {
                                    KeyValuePair<string, string> tsTGT = LogHelper.ParseKeyValuePairFromLogFile(line);
                                    OptimizationLoopSettings.PlanPreparationTsTargets.Add(tsTGT.Key, tsTGT.Value);
                                }
                            }
                            else if (line.Contains("Normalization volumes:"))
                            {
                                while (!string.IsNullOrEmpty((line = reader.ReadLine().Trim())))
                                {
                                    KeyValuePair<string, string> normVol = LogHelper.ParseKeyValuePairFromLogFile(line);
                                    OptimizationLoopSettings.PlanPreparationNormalizationVolumes.Add(normVol.Key, normVol.Value);
                                }
                            }
                            else if (line.Contains("Optimization constraints:"))
                            {
                                string planId = "";
                                List<OptimizationConstraintModel> tmpConstraints = new List<OptimizationConstraintModel> { };
                                while (!string.IsNullOrEmpty((line = reader.ReadLine().Trim())))
                                {
                                    if (!line.Contains("{"))
                                    {
                                        if (tmpConstraints.Any())
                                        {
                                            OptimizationLoopSettings.PlanPreparationOptimizationSetup.Add(new PlanOptimizationSetupModel(planId, new List<OptimizationConstraintModel>(tmpConstraints)));
                                        }
                                        planId = line;
                                        tmpConstraints = new List<OptimizationConstraintModel> { };
                                    }
                                    else
                                    {
                                        tmpConstraints.Add(ConfigurationHelper.ParseOptimizationConstraint(line));
                                    }
                                }
                                if (tmpConstraints.Any())
                                {
                                    OptimizationLoopSettings.PlanPreparationOptimizationSetup.Add(new PlanOptimizationSetupModel(planId, new List<OptimizationConstraintModel>(tmpConstraints)));
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error could not load log file because: {e.Message}");
                return true;
            }
        }

        /// <summary>
        /// Simple helper method print the loaded configuration parameters to the UI on the Script Configuration tab
        /// </summary>
        private void DisplayConfigurationParameters(string file)
        {
            ScriptConfiguration = $"{DateTime.Now}" + Environment.NewLine;
            ScriptConfiguration += $"Configuration file: {file}" + Environment.NewLine + Environment.NewLine;
            ScriptConfiguration += $"Documentation path: {_documentationPath}" + Environment.NewLine + Environment.NewLine;
            ScriptConfiguration += $"Log file path: {_logFilePath}" + Environment.NewLine + Environment.NewLine;
            ScriptConfiguration += "Default run parameters:" + Environment.NewLine;
            ScriptConfiguration += $"Demo mode: {_isDemo}" + Environment.NewLine;
            ScriptConfiguration += $"Run coverage check: {_runCoverageCheck}" + Environment.NewLine;
            ScriptConfiguration += $"Run additional optimization: {_runOneAdditionalOptimization}" + Environment.NewLine;
            ScriptConfiguration += $"Copy and save each optimized plan: {_copyAndSaveEachPlan}" + Environment.NewLine;
            ScriptConfiguration += $"Plan normalization: {_planNormalizationValue}% (i.e., PTV V100% = {_planNormalizationValue}%)" + Environment.NewLine;
            ScriptConfiguration += $"Decision threshold: {_threshold}" + Environment.NewLine;
            ScriptConfiguration += $"Relative lower dose limit: {_lowDoseLimit}" + Environment.NewLine + Environment.NewLine;

            //if (PlanTemplates.Any()) ScriptConfiguration += ConfigurationUIHelper.PrintPlanTemplateConfigurationParameters(PlanTemplates.Cast<AutoPlanTemplateBase>().ToList()).ToString();
        }
        #endregion
    }
}
