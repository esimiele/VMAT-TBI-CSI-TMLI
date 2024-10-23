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

namespace AutoPlannerOptimizationLoop.ViewModels
{
    public class OptimizationLoopMainViewModel : BindableBase
    {
        public ObservableCollection<TBIAutoPlanTemplate> PlanTemplates { get; set; }
        public ObservableCollectionPropertyNotify<PlanObjectiveModel> PlanObjectives { get; set; }
        public ObservableCollectionPropertyNotify<OptimizationConstraintModel> OptimizationConstraints { get; set; }

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

        public OptimizationLoopMainViewModel()
        {
            QuickStartCommand = new DelegateCommand(QuickStartHelp);
            DocumentationCommand = new DelegateCommand(ShowDocumentation);
            AddPlanObjectiveCommand = new DelegateCommand(AddPlanObjective);
            ClearPlanObjectiveListCommand = new DelegateCommand(ClearPlanObjectives);
            AddOptimizationConstraintCommand = new DelegateCommand(AddOptimizationObjective);
            ClearOptimizationConstraintListCommand = new DelegateCommand(ClearOptimizationConstraints);
            PlanTemplates = new ObservableCollection<TBIAutoPlanTemplate> { };
            PlanObjectives = new ObservableCollectionPropertyNotify<PlanObjectiveModel> { };
            OptimizationConstraints = new ObservableCollectionPropertyNotify<OptimizationConstraintModel> { };
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
            string configurationFile = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\configuration\\", "*.ini").SingleOrDefault();
            LoadConfigurationSettings(configurationFile);
            LoadTemplatePlanChoices(PlanType.VMAT_TBI);
            DisplayConfigurationParameters(configurationFile);

            _planType = PlanType.VMAT_TBI;
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null) || !EclipseContext.GetInstance().VMATPlans.Any())
            {
                Logger.GetInstance().LogError("Error! Structure set, Application, or Plan is null! Unable to assign normalization volume!", true);
                List<string> structures = PlanTemplates.SelectMany(x => x.PlanObjectives).Select(x => x.StructureId).ToList();
                structures.AddRange(PlanTemplates.SelectMany(x => x.InitialOptimizationConstraints).Select(x => x.StructureId).ToList());
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
                    foreach (OptimizationConstraintModel itr in OptimizationConstraints)
                    {
                        if (itr.QueryDoseUnits == Units.cGy)
                        {
                            itr.QueryDose = Math.Round(itr.QueryDose * BasePlanTotalDose / priorTotalDose, 1);
                        }
                    }
                    OptimizationConstraints.Refresh();
                }
            }
        }

        private void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(SelectedTemplate, null)) return;

            TBIAutoPlanTemplate template = SelectedTemplate as TBIAutoPlanTemplate;
            BasePlanDosePerFraction = template.InitialRxDosePerFx;
            BasePlanNumberOfFractions = template.InitialRxNumberOfFractions;
            BasePlanTotalDose = template.InitialRxNumberOfFractions * template.InitialRxDosePerFx;
            PlanObjectives.Clear();
            foreach (PlanObjectiveModel itr in template.PlanObjectives)
            {
                PlanObjectives.Add(new PlanObjectiveModel(itr));
            }
            OptimizationConstraints.Clear();
            foreach (OptimizationConstraintModel itr in template.InitialOptimizationConstraints)
            {
                OptimizationConstraints.Add(new OptimizationConstraintModel(itr));
            }
        }

        public void AddPlanObjective()
        {
            if (!ReferenceEquals(PlanObjectives, null))
            {
                PlanObjectives.Add(new PlanObjectiveModel(StructureIds.First(), OptimizationObjectiveType.None, 0, Units.None, 0, Units.None));
            }
        }

        public void ClearPlanObjectives()
        {
            PlanObjectives.Clear();
        }

        public void AddOptimizationObjective()
        {
            if (!ReferenceEquals(OptimizationConstraints, null))
            {
                OptimizationConstraints.Add(new OptimizationConstraintModel(StructureIds.First(), OptimizationObjectiveType.None, 0.0, Units.cGy, 0, 0));
            }
        }

        public void ClearOptimizationConstraints()
        {
            OptimizationConstraints.Clear();
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
                if (OptimizationConstraints.Contains(opt))
                {
                    OptimizationConstraints.Remove(opt);
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
