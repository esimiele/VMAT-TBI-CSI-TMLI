using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.UIHelpers;
using System.Windows;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerOptimizationLoop.DataContainers;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerOptimizationLoop.Core;
using AutoPlannerHelpers.Prompts;
using VMS.TPS.Common.Model.API;
using AutoPlannerOptimizationLoop.Settings;
using AutoPlannerHelpers.Views;
using System.Text;
using AutoPlannerOptimizationLoop.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using AutoPlannerOptimizationLoop.Views;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerOptimizationLoop.Helpers;

namespace AutoPlannerOptimizationLoop.ViewModels
{
    public class OptimizationLoopMainViewModel : ObservableObject
    {
        public ObservableCollection<AutoPlanTemplateBase> PlanTemplates { get; set; }

        #region properties
        private string _logFilePath;
        private string _documentationPath;
        private double _threshold;
        private double _lowDoseLimit;
        private bool _isDemo;
        private PlanType _selectedPlanType = PlanType.None;
        private List<string> _reminders = new List<string> { };
        private string _mrn;
        private List<string> _availableBasePlansForOptimization;
        private string _selectedBasePlanId;
        private List<string> _availableBoostPlansForOptimization;
        private string _selectedBoostPlanId;
        private AutoPlanTemplateBase _selectedTemplate;
        private double _basePlanDosePerFraction;
        private int _basePlanNumberOfFractions;
        private double _basePlanTotalDose;
        private List<string> _availableBasePlanNormalizationVolumes;
        private string _basePlanNormalizationVolume;
        private double _boostPlanDosePerFraction;
        private int _boostPlanNumberOfFractions;
        private double _boostPlanTotalDose;
        private List<string> _availableBoostPlanNormalizationVolumes;
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

        public PlanType SelectedPlanType
        {
            get { return _selectedPlanType; }
            set { SetProperty(ref _selectedPlanType, value); }
        }

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set 
            { 
                SetProperty(ref _structureIds, value);
                WeakReferenceMessenger.Default.Send(new RequestUpdateStructureIds(_structureIds));
            }
        }

        public List<string> AvailableBasePlansForOptimization
        {
            get { return _availableBasePlansForOptimization; }
            set { SetProperty(ref _availableBasePlansForOptimization, value); }
        }

        public string SelectedBasePlanId
        {
            get { return _selectedBasePlanId; }
            set { SetProperty(ref _selectedBasePlanId, value); UpdateSelectedPlanId(); }
        }

        public string SelectedBoostPlanId
        {
            get { return _selectedBoostPlanId; }
            set { SetProperty(ref _selectedBoostPlanId, value); UpdateSelectedPlanId(); }
        }

        public List<string> AvailableBoostPlansForOptimization
        {
            get { return _availableBoostPlansForOptimization; }
            set { SetProperty(ref _availableBoostPlansForOptimization, value); }
        }

        public AutoPlanTemplateBase SelectedTemplate
        {
            get { return _selectedTemplate; }
            set 
            { 
                SetProperty(ref _selectedTemplate, value);
                if (!ReferenceEquals(_selectedTemplate, null))
                {
                    WeakReferenceMessenger.Default.Send(new RequestUpdatePlanObjectives(_selectedTemplate.PlanObjectives));
                    if(!double.IsNaN(_selectedTemplate.PlanNormalizationValue)) PlanNormalizationValue = _selectedTemplate.PlanNormalizationValue;
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new RequestUpdatePlanObjectives(new List<PlanObjectiveModel> { }));
                }
            }
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

        public List<string> AvailableBasePlanNormalizationVolumes
        {
            get { return _availableBasePlanNormalizationVolumes; }
            set { SetProperty(ref _availableBasePlanNormalizationVolumes, value); }
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
            get { return _boostPlanDosePerFraction; }
            set { SetProperty(ref _boostPlanDosePerFraction, value); ResetRxDose(); }
        }

        public int BoostPlanNumberOfFractions
        {
            get { return _boostPlanNumberOfFractions; }
            set { SetProperty(ref _boostPlanNumberOfFractions, value); ResetRxDose(); }
        }

        public double BoostPlanTotalDose
        {
            get { return _boostPlanTotalDose; }
            set { SetProperty(ref _boostPlanTotalDose, value); }
        }

        public List<string> AvailableBoostPlanNormalizationVolumes
        {
            get { return _availableBoostPlanNormalizationVolumes; }
            set { SetProperty(ref _availableBoostPlanNormalizationVolumes, value); }
        }

        public string BoostPlanNormalizationVolume
        {
            get { return _boostPlanNormalizationVolume; }
            set { SetProperty(ref _boostPlanNormalizationVolume, value); }
        }
        #endregion

        #region view objects
        private object _scriptConfiguration;
        public object ScriptConfiguration
        {
            get { return _scriptConfiguration; }
            set { SetProperty(ref _scriptConfiguration, value); }
        }

        private object _planObjectives;
        public object PlanObjectives
        {
            get { return _planObjectives; }
            set { SetProperty(ref _planObjectives, value); }
        }

        private object _optimizationSetup;

        public object OptimizationSetup
        {
            get { return _optimizationSetup; }
            set { SetProperty(ref _optimizationSetup, value); }
        }

        #endregion

        #region commands
        public ICommand QuickStartCommand { get; set; }
        public ICommand DocumentationCommand { get; set; }
        public ICommand SelectPatientCommand { get; set; }
        public ICommand ShowPlanNormalizationInfoCommand { get; set; }
        #endregion

        public OptimizationLoopMainViewModel(string[] args)
        {
            QuickStartCommand = new RelayCommand(QuickStartHelp);
            DocumentationCommand = new RelayCommand(ShowDocumentation);
            SelectPatientCommand = new RelayCommand(PromptUserForPatientSelection);
            ShowPlanNormalizationInfoCommand = new RelayCommand(ShowPlanNormalizationInfo);
            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase> { };
            AvailableBasePlanNormalizationVolumes = new List<string> { };
            AvailableBoostPlanNormalizationVolumes = new List<string> { };
            AvailableBasePlansForOptimization = new List<string> { };
            AvailableBoostPlansForOptimization = new List<string> { };
            _structureIds = new List<string> { };
            AssignDefaultLogAndDocPaths();
            InitializeMessengers();
            PlanObjectives = new PlanObjectivesView { DataContext = new PlanObjectivesViewModel(_structureIds) };
            OptimizationSetup = new OptimizationConstraintsView { DataContext = new OptimizationConstraintsViewModel(_structureIds, new List<string> { "1", "2" }, _selectedPlanType) };
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

        private void ShowPlanNormalizationInfo()
        {
            MessageBox.Show("This is used to set the plan normalization. What percentage of the PTV volume should recieve the prescription dose?");
        }
        #endregion

        #region initialize
        public void Initialize()
        {
            LoadPatientStructureSetAndPlans();
            LoadConfigurationSettingsForPlanType(_selectedPlanType);
            if (OptimizationLoopSettings.Reminders.Any(x => x.ToLower().Contains("base dose")))
            {
                if (EclipseContext.GetInstance().VMATPlans.Any() && !EclipseContext.GetInstance().VMATPlans.First().Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("leg")))
                {
                    ESAPIThreadContext.ESAPIDispatcher.Invoke(() => { OptimizationLoopSettings.Reminders.Remove(OptimizationLoopSettings.Reminders.First(x => x.ToLower().Contains("base dose"))); });
                }
            }
            LoadTemplatePlanChoices(_selectedPlanType);
            InitializeUI();
            WeakReferenceMessenger.Default.Send(new RequestUpdateScriptConfiguration(BuildScriptConfigurationInfo()));
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestSetOptimizationConstraintsMessage>(this, (r, m) =>
            {
                List<PlanObjectiveModel> planObjectives = WeakReferenceMessenger.Default.Send(new RequestPlanObjectives());
                if (!planObjectives.Any()) return;
                StartOptimization(planObjectives, m.PlanOptimizationSetup);
            });
            WeakReferenceMessenger.Default.Register<RequestOptimizationConstraintsFromPlan>(this, (r, m) =>
            {
                m.Reply(GetOptimizationConstraintsFromPlans());
            });
            WeakReferenceMessenger.Default.Register<RequestSelectPatient>(this, (r, m) =>
            {
                LoadPatient(m);
                SelectedTemplate = null;
                LoadConfigurationSettingsForPlanType(_selectedPlanType);
                LoadTemplatePlanChoices(_selectedPlanType);
                if (PlanTemplates.Any(x => string.Equals(x.TemplateName, OptimizationLoopSettings.PlanPreparationTemplateUsed)))
                {
                    SelectedTemplate = PlanTemplates.First(x => string.Equals(x.TemplateName, OptimizationLoopSettings.PlanPreparationTemplateUsed));
                }
                WeakReferenceMessenger.Default.Send(new RequestUpdateScriptConfiguration(BuildScriptConfigurationInfo()));
            });
        }

        private void AssignDefaultLogAndDocPaths()
        {
            _logFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs\\";
            _documentationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\documentation\\";
        }

        public void InitializeUI()
        {
            List<string> planIds = new List<string> { "1", "2" };
            ESAPIThreadContext.RunOnESAPIThreadSync(() =>
            {
                if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null) || !EclipseContext.GetInstance().VMATPlans.Any())
                {
                    Logger.GetInstance().LogError("Error! Structure set, Application, or Plan is null! Unable to assign normalization volume!", true);
                    List<string> structures = PlanTemplates.SelectMany(x => x.PlanObjectives).Select(x => x.StructureId).ToList();
                    ESAPIThreadContext.ESAPIDispatcher.Invoke(() => { StructureIds = structures.Distinct().ToList(); });
                }
                else
                {
                    planIds = new List<string>(AvailableBasePlansForOptimization);
                }
            });

            if (PlanTemplates.Any(x => string.Equals(x.TemplateName, OptimizationLoopSettings.PlanPreparationTemplateUsed)))
            {
                SelectedTemplate = PlanTemplates.First(x => string.Equals(x.TemplateName, OptimizationLoopSettings.PlanPreparationTemplateUsed));
            }
        }
        #endregion

        #region load and open patient
        /// <summary>
        /// Utility method to load a patient into the script. Attempt to read the log file from the preparation script by default
        /// </summary>
        /// <param name="patmrn"></param>
        private void LoadPatientStructureSetAndPlans()
        {
            ESAPIThreadContext.RunOnESAPIThreadSync(() =>
            {
                if (!EclipseContext.GetInstance().IsInitialized) return;
                if (ReferenceEquals(EclipseContext.GetInstance().Patient, null) || LogHelper.GetNumberofMatchingLogFilesForMRN(EclipseContext.GetInstance().Patient.Id) != 1) PromptUserForPatientSelection();
                else LoadPatient(new RequestSelectPatient(EclipseContext.GetInstance().Patient.Id, _selectedPlanType, LogHelper.GetFullLogFileFromExistingMRN(EclipseContext.GetInstance().Patient.Id)));
            });
        }

        private void PromptUserForPatientSelection()
        {
            SelectPatientView spv = new SelectPatientView { DataContext = new SelectPatientViewModel() };
            spv.ShowDialog();
        }

        private void LoadPatient(RequestSelectPatient selection)
        {
            //called from select patient view model if successful
            ESAPIThreadContext.RunOnESAPIThreadSync(() =>
            {
                if (!EclipseContext.GetInstance().IsInitialized) return;
                if (!string.IsNullOrEmpty(selection.PatientId))
                {
                    OptimizationLoopSettings.ClearSettings();

                    EclipseContext.GetInstance().ClearContext();
                    EclipseContext.GetInstance().Patient = EclipseContext.GetInstance().Application.OpenPatientById(selection.PatientId);
                    if(!ReferenceEquals(EclipseContext.GetInstance().Patient, null))
                    {
                        ESAPIThreadContext.ESAPIDispatcher.Invoke(() =>
                        {
                            SelectedPlanType = selection.PlanType;
                            if (!string.IsNullOrEmpty(selection.FullPreparationLogPath) && !LoadLogFile(selection.FullPreparationLogPath)) OptimizationLoopSettings.PlanPreparationLogFileLoaded = true;
                        });
                        List<ExternalPlanSetup> thePlans = LoadPlans();

                        if(thePlans.Any())
                        {
                            EclipseContext.GetInstance().VMATPlans.Clear();
                            EclipseContext.GetInstance().VMATPlans.AddRange(thePlans);
                            EclipseContext.GetInstance().StructureSet = thePlans.First().StructureSet;
                            EclipseContext.GetInstance().Course = thePlans.First().Course;

                            ESAPIThreadContext.ESAPIDispatcher.Invoke(() =>
                            {
                                MRN = EclipseContext.GetInstance().Patient.Id;
                                StructureIds = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
                                UpdateNormalizationVolumes(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).Where(x => x.ToLower().Contains("ptv")), thePlans.Count() > 1 ? true : false);
                                UpdateAvailablePlans(EclipseContext.GetInstance().Course.ExternalPlanSetups.Where(x => !x.Id.ToLower().Contains("leg") && x.Beams.Any(y => !y.IsSetupField)).Select(x => x.Id), thePlans.Count() > 1 ? true : false);
                                SelectedBasePlanId = AvailableBasePlansForOptimization.First(x => string.Equals(x, thePlans.First().Id));
                                if (_selectedPlanType == PlanType.VMAT_CSI && thePlans.Count() > 1)
                                {
                                    SelectedBoostPlanId = AvailableBoostPlansForOptimization.First(x => string.Equals(x, thePlans.Last().Id));
                                }
                            });
                        }
                    }
                    else
                    {
                        Logger.GetInstance().LogError($"Error! Patient {selection.PatientId} does not exist! Exiting!");
                        return;
                    }
                }
            });
        }

        public List<ExternalPlanSetup> LoadPlans()
        {
            if (OptimizationLoopSettings.PlanUIDs.Any())
            {
                List<ExternalPlanSetup> plans = ExtractPlansBasedOnUIDsFromLogs(OptimizationLoopSettings.PlanUIDs);
                if (plans.Any()) return plans;
            }
            return ExtractPlansBasedOnContext();
        }

        private List<ExternalPlanSetup> ExtractPlansBasedOnUIDsFromLogs(List<string> uids)
        {
            List<ExternalPlanSetup> thePlans = new List<ExternalPlanSetup> { };
            foreach (string uid in uids)
            {
                ExternalPlanSetup tmp = EclipseContext.GetInstance().Patient.Courses.SelectMany(x => x.ExternalPlanSetups).FirstOrDefault(x => string.Equals(x.UID, uid));
                if (!ReferenceEquals(tmp, null)) thePlans.Add(tmp);
            }
            if (thePlans.Any() && thePlans.Count > 1)
            {
                if (!thePlans.All(x => string.Equals(x.Course.Id, thePlans.First().Course.Id)))
                {
                    Logger.GetInstance().LogError("Error! Plans parsed from log file belong to separate courses! They must exist in the source course! Please fix and try again");
                    return new List<ExternalPlanSetup> { };
                }
                else if (!thePlans.All(x => string.Equals(x.StructureSet.UID, thePlans.First().StructureSet.UID)))
                {
                    Logger.GetInstance().LogError("Error! Plans parsed from log file do NOT share the same structure set! They must use the same structure set! Please fix and try again");
                    return new List<ExternalPlanSetup> { };
                }
            }
            return thePlans;
        }

        private List<ExternalPlanSetup> ExtractPlansBasedOnContext()
        {
            List<ExternalPlanSetup> thePlans = new List<ExternalPlanSetup> { };
            //simple logic to try and guess which plans are which
            Course theCourse = null;
            List<Course> courses = EclipseContext.GetInstance().Patient.Courses.Where(x => x.Id.ToLower().Contains("csi") || x.Id.ToLower().Contains("tbi") || x.Id.ToLower().Contains("tmli")).ToList();
            if(!courses.Any())
            {
                Logger.GetInstance().LogError("Error! No courses found with the string 'csi', 'tbi', or 'tmli' contained in the id! Please fix and try again!");
                return thePlans;
            }
            else if (courses.Count != 1)
            {
                SelectItemPrompt SIP = new SelectItemPrompt("Please select a course:", courses.Select(x => x.Id).ToList());
                SIP.ShowDialog();
                if (!SIP.GetSelection()) return thePlans;
                theCourse = courses.FirstOrDefault(x => string.Equals(x.Id, SIP.GetSelectedItem()));
            }
            else theCourse = courses.First();

            thePlans = theCourse.ExternalPlanSetups.Where(x => !x.Id.ToLower().Contains("leg") && x.Beams.Any(y => !y.IsSetupField)).ToList();
            if (!thePlans.Any())
            {
                Logger.GetInstance().LogError($"Error! No plans found in course: {theCourse.Id}! Unable to determine which plan(s) should be used for optimization! Exiting!");
                return new List<ExternalPlanSetup> { };
            }
            else if (thePlans.Count > 1)
            {
                if (_selectedPlanType == PlanType.VMAT_CSI)
                {
                    SelectItemPrompt SequentialBoostNeeded = new SelectItemPrompt("Does this CSI case include a sequential boost?", new List<string> { "No", "Yes" });
                    SequentialBoostNeeded.Topmost = true;
                    SequentialBoostNeeded.ShowDialog();
                    if (!SequentialBoostNeeded.GetSelection()) return new List<ExternalPlanSetup> { };
                    if (string.Equals(SequentialBoostNeeded.GetSelectedItem(), "yes", StringComparison.OrdinalIgnoreCase))
                    {
                        //update both plan Id combo boxes
                        //UpdateAvailablePlans(thePlans.Select(x => x.Id), true);
                        //SelectedBasePlanId = AvailableBasePlansForOptimization.First();
                        //SelectedBoostPlanId = AvailableBoostPlansForOptimization.Last();
                        return new List<ExternalPlanSetup> { thePlans.First(), thePlans.Last()};
                    }
                }
                //no sequential boost --> need to select a single plan
                SelectItemPrompt SIP = new SelectItemPrompt("Please select a plan to optimize:", thePlans.Select(x => x.Id).ToList());
                SIP.Topmost = true;
                SIP.ShowDialog();
                if (!SIP.GetSelection()) return new List<ExternalPlanSetup> { };
                ExternalPlanSetup thePlan = thePlans.First(x => string.Equals(x.Id, SIP.GetSelectedItem()));
                thePlans = new List<ExternalPlanSetup> { thePlan };
            }
            return thePlans;
        }
        #endregion

        #region update UI
        private void UpdateSelectedPlanId()
        {
            if (string.IsNullOrEmpty(_selectedBasePlanId)) return;
            ESAPIThreadContext.RunOnESAPIThreadSync(() =>
            {
                if (_selectedPlanType == PlanType.VMAT_CSI && _availableBoostPlansForOptimization.Any())
                {
                    if( string.IsNullOrEmpty(_selectedBoostPlanId)) return;
                    EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup>
                    {
                        EclipseContext.GetInstance().Course.ExternalPlanSetups.First(x => string.Equals(_selectedBasePlanId, x.Id, StringComparison.OrdinalIgnoreCase)),
                        EclipseContext.GetInstance().Course.ExternalPlanSetups.First(x => string.Equals(_selectedBoostPlanId, x.Id, StringComparison.OrdinalIgnoreCase)),
                    };
                    if (!EclipseContext.GetInstance().VMATPlans.All(x => string.Equals(EclipseContext.GetInstance().VMATPlans.First().StructureSet.UID, x.StructureSet.UID)))
                    {
                        EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup>();
                        ESAPIThreadContext.ESAPIDispatcher.Invoke(() =>
                        {
                            SelectedBasePlanId = null;
                            SelectedBoostPlanId = null;
                            Logger.GetInstance().LogError("Error! Base plan and boost plan do NOT share the same structure set! Update plan selection and try again");
                        });
                        return;
                    }
                }
                else
                {
                    EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup>
                    {
                        EclipseContext.GetInstance().Course.ExternalPlanSetups.First(x => string.Equals(_selectedBasePlanId, x.Id, StringComparison.OrdinalIgnoreCase)),
                    };
                }
                if (!string.Equals(EclipseContext.GetInstance().StructureSet.UID, EclipseContext.GetInstance().VMATPlans.First().StructureSet.UID))
                {
                    EclipseContext.GetInstance().StructureSet = EclipseContext.GetInstance().VMATPlans.First().StructureSet;
                    ESAPIThreadContext.ESAPIDispatcher.Invoke(() =>
                    {
                        StructureIds = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
                        UpdateNormalizationVolumes(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).Where(x => x.ToLower().Contains("ptv")), _availableBasePlansForOptimization.Any());
                    });
                }
                ESAPIThreadContext.ESAPIDispatcher.Invoke(() =>
                {
                    UpdateUIWithPlanPrescriptionInfo();
                    WeakReferenceMessenger.Default.Send(new RequestPlanSelectionChanged());
                });
            });
        }

        private List<PlanOptimizationSetupModel> GetOptimizationConstraintsFromPlans()
        {
            List<PlanOptimizationSetupModel> planOptimizationSetup = new List<PlanOptimizationSetupModel> { };
            ESAPIThreadContext.RunOnESAPIThreadSync(() =>
            {
                foreach (ExternalPlanSetup itr in EclipseContext.GetInstance().VMATPlans)
                {
                    planOptimizationSetup.Add(new PlanOptimizationSetupModel(itr.Id, OptimizationSetupHelper.ReadConstraintsFromPlan(itr)));
                }
            });
            return planOptimizationSetup;
        }

        private void UpdateNormalizationVolumes(IEnumerable<string> structureIds, bool sequentialBoostNeeded = false)
        {
            BasePlanNormalizationVolume = null;
            BoostPlanNormalizationVolume = null;
            AvailableBasePlanNormalizationVolumes = new List<string>(structureIds);
            if (sequentialBoostNeeded)
            {
                AvailableBoostPlanNormalizationVolumes = new List<string>(structureIds);
            }
            else AvailableBoostPlanNormalizationVolumes = new List<string> { };
        }

        private void UpdateAvailablePlans(IEnumerable<string> planIds, bool sequentialBoostNeeded = false)
        {
            SelectedBasePlanId = null;
            SelectedBoostPlanId = null;
            AvailableBasePlansForOptimization = new List<string>(planIds);
            if (sequentialBoostNeeded)
            {
                AvailableBoostPlansForOptimization = new List<string>(planIds);
            }
            else AvailableBoostPlansForOptimization = new List<string> { };
        }

        private void ResetRxDose()
        {
            if (_basePlanNumberOfFractions > 0 && _basePlanDosePerFraction > 0)
            {
                BasePlanTotalDose = _basePlanDosePerFraction * _basePlanNumberOfFractions;
            }
            if(_boostPlanDosePerFraction > 0 && _boostPlanNumberOfFractions > 0)
            {
                BoostPlanTotalDose = _boostPlanDosePerFraction * _boostPlanNumberOfFractions;
            }
        }

        private void ClearAllRxDoses()
        {
            BasePlanDosePerFraction = 0;
            BasePlanNumberOfFractions = 0;
            BasePlanTotalDose = 0;
            BoostPlanDosePerFraction = 0;
            BoostPlanNumberOfFractions = 0;
            BoostPlanTotalDose = 0;
        }

        private void UpdateUIWithPlanPrescriptionInfo()
        {
            ClearAllRxDoses();
            BasePlanDosePerFraction = EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, _selectedBasePlanId)).DosePerFraction.Dose;
            BasePlanNumberOfFractions = (int)EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, _selectedBasePlanId)).NumberOfFractions;
            if (_selectedPlanType == PlanType.VMAT_CSI && EclipseContext.GetInstance().VMATPlans.Count() > 1)
            {
                BoostPlanDosePerFraction = EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, _selectedBoostPlanId)).DosePerFraction.Dose;
                BoostPlanNumberOfFractions = (int)EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, _selectedBoostPlanId)).NumberOfFractions;
            }
            if (_availableBasePlanNormalizationVolumes.Any())
            {
                if (OptimizationLoopSettings.PlanPreparationNormalizationVolumes.TryGetValue(_selectedBasePlanId, out var vol) && EclipseContext.GetInstance().StructureSet.Structures.Any(x => string.Equals(vol, x.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    BasePlanNormalizationVolume = AvailableBasePlanNormalizationVolumes.First(x => string.Equals(vol, x, StringComparison.OrdinalIgnoreCase));
                }
                else BasePlanNormalizationVolume = null;
                if (_selectedPlanType == PlanType.VMAT_CSI && _availableBoostPlansForOptimization.Any())
                {
                    if (OptimizationLoopSettings.PlanPreparationNormalizationVolumes.TryGetValue(_selectedBoostPlanId, out var bstVol) && EclipseContext.GetInstance().StructureSet.Structures.Any(x => string.Equals(bstVol, x.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        BoostPlanNormalizationVolume = AvailableBoostPlanNormalizationVolumes.First(x => string.Equals(bstVol, x, StringComparison.OrdinalIgnoreCase));
                    }
                    else BoostPlanNormalizationVolume = null;
                }
            }
            else
            {
                BasePlanNormalizationVolume = null;
                BoostPlanNormalizationVolume = null;
            }
        }
        #endregion

        #region start optimization
        public bool CanStartOptimizationUIInput(List<PlanObjectiveModel> planObj, List<PlanOptimizationSetupModel> planOpt)
        {
            if(string.IsNullOrEmpty(_selectedBasePlanId))
            {
                Logger.GetInstance().LogError("Error! Selected base plan Id is null! Please fix and try again!");
                return true;
            }
            else if (string.IsNullOrEmpty(_basePlanNormalizationVolume))
            {
                Logger.GetInstance().LogError("Error! Selected base plan normalization structure Id is null! Please fix and try again!");
                return true;
            }
            else if (_selectedPlanType == PlanType.VMAT_CSI && _availableBoostPlansForOptimization.Any())
            {
                if(string.IsNullOrEmpty(_selectedBoostPlanId))
                {
                    Logger.GetInstance().LogError("Error! Selected boost plan Id is null! Please fix and try again!");
                    return true;
                }
                else if (string.IsNullOrEmpty(_boostPlanNormalizationVolume))
                {
                    Logger.GetInstance().LogError("Error! Selected boost plan normalization structure Id is null! Please fix and try again!");
                    return true;
                }
            }
            if (_planNormalizationValue < 0.0 || _planNormalizationValue > 100.0)
            {
                Logger.GetInstance().LogError("Error! Target normalization is is either < 0% or > 100% \nExiting!");
                return true;
            }
            if (_maxNumberOfIterations < 1)
            {
                Logger.GetInstance().LogError("Number of requested optimizations needs to be greater than or equal to 1.\nExiting!");
                return true;
            }
            return false;
        }

        public void StartOptimization(List<PlanObjectiveModel> planObj, List<PlanOptimizationSetupModel> planOptSetup)
        {
            Logger.GetInstance().AppendLogOutput("Checking for valid constraints and objectives");
            if (CanStartOptimizationUIInput(planObj, planOptSetup)) return;
            ESAPIThreadContext.RunOnESAPIThread(() =>
            {
                if (!EclipseContext.GetInstance().IsInitialized)
                {
                    Logger.GetInstance().LogError("Script is not initialized! Unable to start the optimization loop!");
                    return;
                }
                if (ReferenceEquals(EclipseContext.GetInstance().Patient, null) || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null) || !EclipseContext.GetInstance().VMATPlans.Any())
                {
                    Logger.GetInstance().LogError("Error! Patient, structure set, or plan are null! Unable to proceed!");
                    return;
                }

                //StringBuilder sb = new StringBuilder();
                //if (PlanObjectives.Any())
                //{
                //    foreach (PlanObjectiveModel itr in PlanObjectives)
                //    {
                //        sb.AppendLine($"{itr.StructureId}, {itr.ConstraintType}, {itr.QueryVolume}, {itr.QueryDose}, {itr.QueryDoseUnits}");
                //    }
                //}
                //else sb.AppendLine("No plan objectives in list");
                //sb.AppendLine(" ");
                //if (PlanOptimizationConstraints.Any())
                //{
                //    foreach(PlanOptimizationSetupModel planopt in PlanOptimizationConstraints)
                //    {
                //        sb.AppendLine($"Plan: {planopt.PlanId}");
                //        foreach (OptimizationConstraintModel itr in planopt.OptimizationConstraints)
                //        {
                //            sb.AppendLine($"{itr.StructureId}, {itr.ConstraintType}, {itr.QueryDose}, {itr.QueryDoseUnits}, {itr.QueryVolume}, {itr.QueryVolumeUnits}, {itr.Priority}");
                //        }
                //    }
                //}
                //else sb.AppendLine("No optimization constraints in list");
                //MessageBox.Show(sb.ToString());

                EclipseContext.GetInstance().Patient.BeginModifications();
                if (planOptSetup.Any())
                {
                    foreach (PlanOptimizationSetupModel itr in planOptSetup)
                    {
                        ExternalPlanSetup plan = EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, itr.PlanId));
                        OptimizationSetupHelper.RemoveOptimizationConstraintsFromPLan(plan);
                        OptimizationSetupHelper.AssignOptConstraints(itr.OptimizationConstraints.Where(x => x.IsValidConstraint).ToList(), plan, true, 0.0);
                    }
                }

                Dictionary<string, string> normalizationVolumes = new Dictionary<string, string>
                {
                    { _selectedBasePlanId, _basePlanNormalizationVolume }
                };
                if (_selectedPlanType == PlanType.VMAT_CSI && _availableBoostPlansForOptimization.Any())
                {
                    normalizationVolumes.Add(_selectedBoostPlanId, _boostPlanNormalizationVolume);
                }

                OptDataContainer _data = GenerateOptimizationDataContainer(planObj, normalizationVolumes);
                OptimizationLoopBase opt;
                if (_selectedPlanType == PlanType.VMAT_TBI) opt = new VMATTBIOptimization(_data);
                else if (_selectedPlanType == PlanType.VMAT_CSI) opt = new VMATCSIOptimization(_data);
                else opt = new VMATTMLIOptimization(_data);
                //VMATTBIOptimization opt = new VMATTBIOptimization(new List<OptimizationConstraintModel> { new OptimizationConstraintModel("test", OptimizationObjectiveType.Lower, 100, Units.cGy, 100, 100)});
                if (opt.Execute()) return;
            });
        }

        public OptDataContainer GenerateOptimizationDataContainer(List<PlanObjectiveModel> planObj, Dictionary<string,string> normalizationVolumes)
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
                                        normalizationVolumes,
                                        planObj.Where(x => x.IsValidObjective).ToList(),
                                        requestedOptStructures,
                                        requestedPlanMetrics,
                                        _selectedPlanType,
                                        _planNormalizationValue,
                                        _maxNumberOfIterations,
                                        _runCoverageCheck,
                                        _runOneAdditionalOptimization,
                                        _copyAndSaveEachPlan,
                                        _structureIds.Any(x => x.ToLower().Contains("flash")),
                                        _threshold,
                                        _lowDoseLimit,
                                        _isDemo,
                                        _logFilePath,
                                        EclipseContext.GetInstance().Application);
        }
        #endregion

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
            if (_selectedPlanType == PlanType.VMAT_TBI) path = Path.Combine(path, "TBI\\");
            else if (_selectedPlanType == PlanType.VMAT_CSI) path = Path.Combine(path, "CSI\\");
            else path = Path.Combine(path, "TMLI\\");
            PlanTemplates.Clear();
            try
            {
                foreach (string itr in Directory.GetFiles(path, "*.ini", option).OrderBy(x => x))
                {
                    if(_selectedPlanType == PlanType.VMAT_TBI) PlanTemplates.Add(ConfigurationHelper.ReadTBITemplatePlan(itr, count++));
                    else if (_selectedPlanType == PlanType.VMAT_CSI) PlanTemplates.Add(ConfigurationHelper.ReadCSITemplatePlan(itr, count++));
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
                _reminders.Clear();
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
            if (string.IsNullOrEmpty(fullLogName) || !File.Exists(fullLogName)) return true;
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
                                    if (value.Contains("CSI")) SelectedPlanType = PlanType.VMAT_CSI;
                                    else if (value.Contains("TBI")) SelectedPlanType = PlanType.VMAT_TBI;
                                    else SelectedPlanType = PlanType.VMAT_TMLI;
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
            sb.AppendLine($"Plan type: {_selectedPlanType}");
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
                if(_selectedPlanType == PlanType.VMAT_TBI) sb.Append(ConfigurationUIHelper.PrintTBIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
                if(_selectedPlanType == PlanType.VMAT_CSI) sb.Append(ConfigurationUIHelper.PrintCSIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
                if(_selectedPlanType == PlanType.VMAT_TMLI) sb.Append(ConfigurationUIHelper.PrintTMLIPlanTemplateConfigurationParameters(PlanTemplates.ToList()));
            }
            return sb;
        }
        #endregion
    }
}
