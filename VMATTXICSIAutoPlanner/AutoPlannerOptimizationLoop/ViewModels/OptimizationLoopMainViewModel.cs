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
using AutoPlannerHelpers.Views;
using System.Text;
using System.Data;

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
        private PlanType _planType = PlanType.VMAT_CSI;
        private List<string> _reminders = new List<string> { };
        private string _mrn;
        private AutoPlanTemplateBase _selectedTemplate;
        private double _basePlanDosePerFraction;
        private int _basePlanNumberOfFractions;
        private double _basePlanTotalDose;
        private string _basePlanNormalizationVolume;
        private double _boostplanDosePerFraction;
        private int _boostPlanNumberOfFractions;
        private double _boostPlanTotalDose;
        private string _boostPlanNormalizationVolume;
        private bool _runCoverageCheck;
        private bool _copyAndSaveEachPlan;
        private int _maxNumberOfIterations;
        private bool _runOneAdditionalOptimization;
        private double _planNormalizationValue;
        private List<string> _structureIds;

        public string MRN
        {
            get { return _mrn; }
            set { SetProperty(ref _mrn, value); }
        }

        public PlanType PlanType
        {
            get { return _planType; }
            set { SetProperty(ref _planType, value); }
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

        public double BoostPlanDosePerFraction
        {
            get { return _boostplanDosePerFraction; }
            set { SetProperty(ref _boostplanDosePerFraction, value); }
        }

        public int BoostPlanNumberOfFractions
        {
            get { return _boostPlanNumberOfFractions; }
            set { SetProperty(ref _boostPlanNumberOfFractions, value); }
        }

        public double BoostPlanTotalDose
        {
            get { return _boostPlanTotalDose; }
            set { SetProperty(ref _basePlanTotalDose, value); }
        }

        public string BoostPlanNormalizationVolume
        {
            get { return _boostPlanNormalizationVolume; }
            set { SetProperty(ref _boostPlanNormalizationVolume, value); }
        }

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set { SetProperty(ref _structureIds, value); }
        }
        #endregion

        #region view objects
        private object _scriptConfiguration;
        public object ScriptConfiguration
        {
            get { return _scriptConfiguration; }
            set { SetProperty(ref _scriptConfiguration, value); }
        }
        #endregion

        #region commands
        public DelegateCommand QuickStartCommand { get; set; }
        public DelegateCommand DocumentationCommand { get; set; }
        public DelegateCommand OpenPatientCommand { get; set; }
        public DelegateCommand AddPlanObjectiveCommand { get; set; }
        public DelegateCommand ClearPlanObjectiveListCommand { get; set; }
        public DelegateCommand AddOptimizationConstraintCommand { get; set; }
        public DelegateCommand GetOptConstraintsFromPlanCommand { get; set; }
        public DelegateCommand GetOptConstraintsFromLogsCommand { get; set; }
        public DelegateCommand ClearOptimizationConstraintListCommand { get; set; }
        public DelegateCommand<object> ClearRowCommand { get; set; }
        public DelegateCommand StartOptimizationCommand { get; set; }
        #endregion

        public OptimizationLoopMainViewModel(string[] args)
        {
            QuickStartCommand = new DelegateCommand(QuickStartHelp);
            DocumentationCommand = new DelegateCommand(ShowDocumentation);
            AddPlanObjectiveCommand = new DelegateCommand(AddPlanObjective);
            ClearPlanObjectiveListCommand = new DelegateCommand(ClearPlanObjectives);
            AddOptimizationConstraintCommand = new DelegateCommand(AddOptimizationObjective);
            GetOptConstraintsFromPlanCommand = new DelegateCommand(GetOptimizationConstraintsFromPlan);
            GetOptConstraintsFromLogsCommand = new DelegateCommand(GetOptimizationConstraintsFromLogs);
            ClearOptimizationConstraintListCommand = new DelegateCommand(ClearOptimizationConstraints);
            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase> { };
            PlanObjectives = new ObservableCollectionPropertyNotify<PlanObjectiveModel> { };
            PlanOptimizationConstraints = new ObservableCollectionPropertyNotify<PlanOptimizationSetupModel> { };
            ClearRowCommand = new DelegateCommand<object>(ClearRow);
            StartOptimizationCommand = new DelegateCommand(StartOptimization);
            if (args.Any()) EclipseContextHelper.GenerateEclipseContext(args.ToList());
            Initialize();
            InitializeUI();
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };
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
        public void Initialize()
        {
            AssignDefaultLogAndDocPaths();
            LoadPatient();
            LoadConfigurationSettingsForPlanType(_planType);
            LoadTemplatePlanChoices(_planType);
        }

        private void AssignDefaultLogAndDocPaths()
        {
            _logFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs\\";
            _documentationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\documentation\\";
        }

        public void InitializeUI()
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null) || !EclipseContext.GetInstance().VMATPlans.Any())
            {
                Logger.GetInstance().LogError("Error! Structure set, Application, or Plan is null! Unable to assign normalization volume!", true);
                List<string> structures = PlanTemplates.SelectMany(x => x.PlanObjectives).Select(x => x.StructureId).ToList();
                //structures.AddRange(PlanTemplates.SelectMany(x => x.InitialOptimizationConstraints).Select(x => x.StructureId).ToList());
                StructureIds = structures.Distinct().ToList();
                return;
            }
            else StructureIds = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
            if(PlanTemplates.Any(x => string.Equals(x.TemplateName, OptimizationLoopSettings.PlanPreparationTemplateUsed)))
            {
                SelectedTemplate = PlanTemplates.First(x => string.Equals(x.TemplateName, OptimizationLoopSettings.PlanPreparationTemplateUsed));
            }
            if(OptimizationLoopSettings.PlanPreparationNormalizationVolumes.Any())
            {
                BasePlanNormalizationVolume = OptimizationLoopSettings.PlanPreparationNormalizationVolumes.First().Value;
                if (OptimizationLoopSettings.PlanPreparationNormalizationVolumes.Count == 2 && _planType == PlanType.VMAT_CSI)
                {
                    BoostPlanNormalizationVolume = OptimizationLoopSettings.PlanPreparationNormalizationVolumes.Last().Value;
                }
            }
            foreach(PlanOptimizationSetupModel itr in OptimizationLoopSettings.PlanPreparationOptimizationSetup)
            {
                PlanOptimizationConstraints.Add(itr);
            }
        }
        #endregion

        #region load and open patient
        /// <summary>
        /// Utility method to load a patient into the script. Attempt to read the log file from the preparation script
        /// </summary>
        /// <param name="patmrn"></param>
        private void LoadPatient()
        {
            if (!EclipseContext.GetInstance().IsInitialized) return;
            string fullLogName = string.Empty;
            if(ReferenceEquals(EclipseContext.GetInstance().Patient, null))
            {
                SelectPatient sp = new SelectPatient(_logFilePath);
                sp.ShowDialog();
                if (!sp.SelectionMade) return;
                EclipseContext.GetInstance().Patient = EclipseContext.GetInstance().Application.OpenPatientById(sp.PatientMRN);
                if(ReferenceEquals(EclipseContext.GetInstance().Patient, null))
                {
                    MessageBox.Show($"Patient: {sp.PatientMRN} not found! Exiting initialization!");
                    return;
                }
                fullLogName = sp.FullLogFileName;
            }
            else fullLogName = LogHelper.GetFullLogFileFromExistingMRN(EclipseContext.GetInstance().Patient.Id, _logFilePath);
            if (!LoadLogFile(fullLogName)) OptimizationLoopSettings.PlanPreparationLogFileLoaded = true;
            else
            {
                MessageBox.Show($"Error! Failed to ready plan preparation log file for patient: {EclipseContext.GetInstance().Patient.Id}! Exiting initialization!");
                return;
            }
            LoadPlansAndStructureSet();
            if (_planType == PlanType.VMAT_TBI && OptimizationLoopSettings.Reminders.Any(x => x.ToLower().Contains("base dose")))
            {
                if (!EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("legs"))) OptimizationLoopSettings.Reminders.Remove(OptimizationLoopSettings.Reminders.First(x => x.ToLower().Contains("base dose")));
            }
        }

        public void LoadPlansAndStructureSet()
        {
            //grab an instance of the VMAT TBI plan. Return null if it isn't found
            if (OptimizationLoopSettings.PlanUIDs.Any())
            {
                //should automatically be in order in terms of cumulative Rx (lowest to highest)
                foreach (string uid in OptimizationLoopSettings.PlanUIDs)
                {
                    ExternalPlanSetup tmp = EclipseContext.GetInstance().Patient.Courses.SelectMany(x => x.ExternalPlanSetups).FirstOrDefault(x => string.Equals(x.UID, uid));
                    if (!ReferenceEquals(tmp, null)) EclipseContext.GetInstance().VMATPlans.Add(tmp);
                }
                if(EclipseContext.GetInstance().VMATPlans.Any())
                {
                    EclipseContext.GetInstance().StructureSet = EclipseContext.GetInstance().VMATPlans.First().StructureSet;
                    EclipseContext.GetInstance().Course = EclipseContext.GetInstance().VMATPlans.First().Course;
                }
            }
            else
            {
                //simple logic to try and guess which plans are which
                Course theCourse = null;
                List<Course> courses = EclipseContext.GetInstance().Patient.Courses.Where(x => x.Id.ToLower().Contains("csi") || x.Id.ToLower().Contains("tbi") || x.Id.ToLower().Contains("tmli")).ToList();
                if (!courses.Any()) return;
                if (courses.Count > 1)
                {
                    SelectItemPrompt SIP = new SelectItemPrompt("Please select a course:", courses.Select(x => x.Id).ToList());
                    SIP.ShowDialog();
                    if (!SIP.GetSelection()) return;
                    theCourse = courses.FirstOrDefault(x => string.Equals(x.Id, SIP.GetSelectedItem()));
                }
                else theCourse = courses.First();
                if (theCourse.Id.ToLower().Contains("csi")) _planType = PlanType.VMAT_CSI;
                else if (theCourse.Id.ToLower().Contains("tbi")) _planType = PlanType.VMAT_TBI;
                else _planType = PlanType.VMAT_TMLI;

                List<ExternalPlanSetup> thePlans = theCourse.ExternalPlanSetups.OrderBy(x => x.CreationDateTime).ToList();
                if (thePlans.Count > 2)
                {
                    MessageBox.Show($"Error! More than two plans found in course: {theCourse.Id}! Unable to determine which plan(s) should be used for optimization! Exiting!");
                    return;
                }
                else if (!thePlans.Any())
                {
                    MessageBox.Show($"Error! No plans found in course: {theCourse.Id}! Unable to determine which plan(s) should be used for optimization! Exiting!");
                    return;
                }
                else if (thePlans.Count == 2 && (thePlans.First().StructureSet != thePlans.Last().StructureSet))
                {
                    MessageBox.Show($"Error! Structure set in first plan ({thePlans.First().Id}) is not the same as the structure set in second plan ({thePlans.Last().Id})! Exiting!");
                    return;
                }
                else
                {
                    EclipseContext.GetInstance().VMATPlans.AddRange(thePlans);
                    EclipseContext.GetInstance().StructureSet = thePlans.First().StructureSet;
                    EclipseContext.GetInstance().Course = theCourse;
                }
            }
        }
        #endregion

        private void ResetRxDose()
        {
            if (BasePlanNumberOfFractions > 0 && BasePlanDosePerFraction > 0)
            {
                BasePlanTotalDose = BasePlanDosePerFraction * BasePlanNumberOfFractions;
            }
            if(BoostPlanDosePerFraction > 0 && BoostPlanNumberOfFractions > 0)
            {
                BoostPlanTotalDose = BoostPlanDosePerFraction * BoostPlanNumberOfFractions;
            }
        }

        private void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            if (_planType == PlanType.VMAT_TBI)
            {
                BasePlanDosePerFraction = (_selectedTemplate as TBIAutoPlanTemplate).InitialRxDosePerFx;
                BasePlanNumberOfFractions = (_selectedTemplate as TBIAutoPlanTemplate).InitialRxNumberOfFractions;
            }
            else if (_planType == PlanType.VMAT_CSI)
            {
                BasePlanDosePerFraction = (_selectedTemplate as CSIAutoPlanTemplate).InitialRxDosePerFx;
                BasePlanNumberOfFractions = (_selectedTemplate as CSIAutoPlanTemplate).InitialRxNumberOfFractions;
                if ((_selectedTemplate as CSIAutoPlanTemplate).BoostRxDosePerFx != 0.1)
                {
                    BoostPlanDosePerFraction = (_selectedTemplate as CSIAutoPlanTemplate).BoostRxDosePerFx;
                    BoostPlanNumberOfFractions = (_selectedTemplate as CSIAutoPlanTemplate).BoostRxNumberOfFractions;
                }
            }
            else
            {
                BasePlanDosePerFraction = (_selectedTemplate as TMLIAutoPlanTemplate).InitialRxDosePerFx;
                BasePlanNumberOfFractions = (_selectedTemplate as TMLIAutoPlanTemplate).InitialRxNumberOfFractions;
            }
            PlanObjectives.Clear();
            foreach (PlanObjectiveModel itr in _selectedTemplate.PlanObjectives)
            {
                PlanObjectives.Add(new PlanObjectiveModel(itr));
            }

            if(!OptimizationLoopSettings.PlanPreparationOptimizationSetup.Any())
            {
                PlanOptimizationConstraints.Clear();
                if (_planType == PlanType.VMAT_TBI) PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel("1", (_selectedTemplate as TBIAutoPlanTemplate).InitialOptimizationConstraints));
                else if (_planType == PlanType.VMAT_CSI)
                {
                    PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel("1", (_selectedTemplate as CSIAutoPlanTemplate).InitialOptimizationConstraints));
                    if ((_selectedTemplate as CSIAutoPlanTemplate).BoostRxDosePerFx != 0.1)
                    {
                        PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel("2", (_selectedTemplate as CSIAutoPlanTemplate).BoostOptimizationConstraints));
                    }
                }
                else PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel("1", (_selectedTemplate as TMLIAutoPlanTemplate).InitialOptimizationConstraints));
            }
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
            if (PlanOptimizationConstraints.Count() > 1)
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

        public void GetOptimizationConstraintsFromPlan()
        {
            if (!EclipseContext.GetInstance().IsInitialized || !EclipseContext.GetInstance().VMATPlans.Any()) return;
            PlanOptimizationConstraints.Clear();
            foreach(ExternalPlanSetup itr in EclipseContext.GetInstance().VMATPlans)
            {
                PlanOptimizationConstraints.Add(new PlanOptimizationSetupModel(itr.Id, OptimizationSetupHelper.ReadConstraintsFromPlan(itr)));
            }
        }

        public void GetOptimizationConstraintsFromLogs()
        {
            foreach (PlanOptimizationSetupModel itr in OptimizationLoopSettings.PlanPreparationOptimizationSetup)
            {
                PlanOptimizationConstraints.Add(itr);
            }
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

            StringBuilder sb = new StringBuilder();
            if (PlanObjectives.Any())
            {
                foreach (PlanObjectiveModel itr in PlanObjectives)
                {
                    sb.AppendLine($"{itr.StructureId}, {itr.ConstraintType}, {itr.QueryVolume}, {itr.QueryDose}, {itr.QueryDoseUnits}");
                }
            }
            else sb.AppendLine("No plan objectives in list");
            sb.AppendLine(" ");
            if (PlanOptimizationConstraints.Any())
            {
                foreach(PlanOptimizationSetupModel planopt in PlanOptimizationConstraints)
                {
                    sb.AppendLine($"Plan: {planopt.PlanId}");
                    foreach (OptimizationConstraintModel itr in planopt.OptimizationConstraints)
                    {
                        sb.AppendLine($"{itr.StructureId}, {itr.ConstraintType}, {itr.QueryDose}, {itr.QueryDoseUnits}, {itr.QueryVolume}, {itr.QueryVolumeUnits}, {itr.Priority}");
                    }
                }
            }
            else sb.AppendLine("No optimization constraints in list");
            MessageBox.Show(sb.ToString());

            if(PlanOptimizationConstraints.Any())
            {
                foreach(PlanOptimizationSetupModel itr in PlanOptimizationConstraints)
                {
                    OptimizationSetupHelper.AssignOptConstraints(itr.OptimizationConstraints.Where(x => x.IsValidConstraint).ToList(), EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, itr.PlanId)), false, 0.0);
                }
            }
            //OptDataContainer _data = GenerateOptimizationDataContainer();
            //VMATTBIOptimization opt = new VMATTBIOptimization(_data);
            VMATTBIOptimization opt = new VMATTBIOptimization(new List<OptimizationConstraintModel> { new OptimizationConstraintModel("test", OptimizationObjectiveType.Lower, 100, Units.cGy, 100, 100)});
            if (opt.Execute()) return;

        }

        public OptDataContainer GenerateOptimizationDataContainer()
        {
            List<RequestedOptimizationTSStructureModel> requestedOptStructures = new List<RequestedOptimizationTSStructureModel> { };
            List<RequestedPlanMetricModel> requestedPlanMetrics = new List<RequestedPlanMetricModel> { };
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                requestedOptStructures.AddRange(_selectedTemplate.RequestedOptimizationTSStructures);
                requestedPlanMetrics.AddRange(_selectedTemplate.RequestedPlanMetrics);
            }

            return new OptDataContainer(EclipseContext.GetInstance().VMATPlans,
                                        OptimizationLoopSettings.PlanPreparationPrescriptions,
                                        OptimizationLoopSettings.PlanPreparationNormalizationVolumes,
                                        PlanObjectives.Where(x => x.IsValidObjective).ToList(),
                                        requestedOptStructures,
                                        requestedPlanMetrics,
                                        _planType,
                                        _planNormalizationValue,
                                        _maxNumberOfIterations,
                                        _runCoverageCheck,
                                        _runOneAdditionalOptimization,
                                        _copyAndSaveEachPlan,
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
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\templates\\";
            if (_planType == PlanType.VMAT_TBI) path = Path.Combine(path, "TBI\\");
            else if (_planType == PlanType.VMAT_CSI) path = Path.Combine(path, "CSI\\");
            else path = Path.Combine(path, "TMLI\\");
            PlanTemplates.Clear();
            try
            {
                foreach (string itr in Directory.GetFiles(path, "*.ini", option).OrderBy(x => x))
                {
                    if(_planType == PlanType.VMAT_TBI) PlanTemplates.Add(ConfigurationHelper.ReadTBITemplatePlan(itr, count++));
                    else if (_planType == PlanType.VMAT_CSI) PlanTemplates.Add(ConfigurationHelper.ReadCSITemplatePlan(itr, count++));
                    else PlanTemplates.Add(ConfigurationHelper.ReadTMLITemplatePlan(itr, count++));
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
            else if (type == PlanType.VMAT_TBI)
            {
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TBI_config.ini");
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\TBI_optimization_config.ini");
            }
            else
            {
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\VMAT_TMLI_config.ini");
                configurationFiles.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\TMLI_optimization_config.ini");
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
                                    if (value.Contains("CSI")) PlanType = PlanType.VMAT_CSI;
                                    else if (value.Contains("TBI")) PlanType = PlanType.VMAT_TBI;
                                    else PlanType = PlanType.VMAT_TMLI;
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
        private StringBuilder BuildScriptConfigurationInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{DateTime.Now}");
            sb.AppendLine($"Plan type: {_planType}");
            sb.AppendLine($"Documentation path: {_documentationPath}");
            sb.AppendLine($"Log file path: {_logFilePath}");
            sb.AppendLine("Default run parameters:");
            sb.AppendLine($"Demo mode: {_isDemo}");
            sb.AppendLine($"Run coverage check: {_runCoverageCheck}");
            sb.AppendLine($"Run additional optimization: {_runOneAdditionalOptimization}");
            sb.AppendLine($"Copy and save each optimized plan: {_copyAndSaveEachPlan}");
            sb.AppendLine($"Plan normalization: {_planNormalizationValue}% (i.e., PTV V100% = {_planNormalizationValue}%)");
            sb.AppendLine($"Decision threshold: {_threshold}");
            sb.AppendLine($"Relative lower dose limit: {_lowDoseLimit}");

            if (PlanTemplates.Any())
            {
                if(_planType == PlanType.VMAT_TBI) sb.Append(ConfigurationUIHelper.PrintTBIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
                if(_planType == PlanType.VMAT_CSI) sb.Append(ConfigurationUIHelper.PrintCSIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
                if(_planType == PlanType.VMAT_TMLI) sb.Append(ConfigurationUIHelper.PrintTMLIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
            }
            return sb;
        }
        #endregion
    }
}
