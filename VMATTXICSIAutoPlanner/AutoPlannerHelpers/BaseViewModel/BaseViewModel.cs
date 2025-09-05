using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using AutoPlannerHelpers.BaseCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerHelpers.Messengers;
using VMS.TPS.Common.Model.API;
using StructureApprovalStatus = VMS.TPS.Common.Model.Types.StructureApprovalStatus;
using System.Windows.Media;

namespace AutoPlannerHelpers.BaseViewModel
{
    public abstract class BaseViewModel : ObservableObject
    {
        public ObservableCollection<AutoPlanTemplateBase> PlanTemplates { get; set; }

        #region properties
        protected string _patientMRN;
        protected string _structureSetId;
        protected AutoPlanTemplateBase _selectedTemplate;

        protected double _initialDosePerFraction;
        protected int _initialNumberOfFractions;
        protected double _initialPlanTotalDose;
        private SolidColorBrush _specifyTargetsTabBackground;
        private SolidColorBrush _structureTuningTabBackground;
        private SolidColorBrush _optimizationStructureDerivationBackground;
        private SolidColorBrush _beamPlacementTabBackground;
        private SolidColorBrush _optimizationSetupTabBackground;
        private SolidColorBrush _TargetStructureDerivationsBackground;
        private SolidColorBrush _setTargetsTabBackground;

        public string PatientMRN
        {
            get { return _patientMRN; }
            set { SetProperty(ref _patientMRN, value); }
        }

        public string StructureSetId
        {
            get { return _structureSetId; }
            set { SetProperty(ref _structureSetId, value); }
        }

        public AutoPlanTemplateBase SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { SetProperty(ref _selectedTemplate, value); UpdateUIWithSelectedPlanTemplate(); }
        }

        public double InitialDosePerFraction
        {
            get { return _initialDosePerFraction; }
            set { SetProperty(ref _initialDosePerFraction, value); ResetInitialRxDose(); }
        }

        public int InitialNumberOfFractions
        {
            get { return _initialNumberOfFractions; }
            set { SetProperty(ref _initialNumberOfFractions, value); ResetInitialRxDose(); }
        }

        public double InitialPlanTotalDose
        {
            get { return _initialPlanTotalDose; }
            set { SetProperty(ref _initialPlanTotalDose, value); }
        }

        public SolidColorBrush SpecifyTargetsTabBackground
        {
            get { return _specifyTargetsTabBackground; }
            set { SetProperty(ref _specifyTargetsTabBackground, value); }
        }

        public SolidColorBrush StructureTuningTabBackground
        {
            get { return _structureTuningTabBackground; }
            set { SetProperty(ref _structureTuningTabBackground, value); }
        }

        public SolidColorBrush OptimizationStructureDerivationBackground
        {
            get { return _optimizationStructureDerivationBackground; }
            set { SetProperty(ref _optimizationStructureDerivationBackground, value); }
        }

        public SolidColorBrush BeamPlacementTabBackground
        {
            get { return _beamPlacementTabBackground; }
            set { SetProperty(ref _beamPlacementTabBackground, value); }
        }

        public SolidColorBrush OptimizationSetupTabBackground
        {
            get { return _optimizationSetupTabBackground; }
            set { SetProperty(ref _optimizationSetupTabBackground, value); }
        }

        public SolidColorBrush TargetStructureDerivationsBackground
        {
            get { return _TargetStructureDerivationsBackground; }
            set { SetProperty(ref _TargetStructureDerivationsBackground, value); }
        }

        public SolidColorBrush SetTargetsTabBackground
        {
            get { return _setTargetsTabBackground; }
            set { SetProperty(ref _setTargetsTabBackground, value); }
        }
        #endregion

        #region view objects
        private object _targetStructureDerivations;
        private object _specifyTargets;
        private object _optimizationSetup;
        private object _specialOptimizationStructures;
        private object _optimizationStructureDerivations;
        private object _beamPlacement;
        private object _planPreparation;
        private object _scriptConfiguration;

        public object TargetStructureDerivations
        {
            get { return _targetStructureDerivations; }
            set { SetProperty(ref _targetStructureDerivations, value); }
        }

        public object SpecifyTargets
        {
            get { return _specifyTargets; }
            set { SetProperty(ref _specifyTargets, value); }
        }

        public object SpecialOptimizationStructures
        {
            get { return _specialOptimizationStructures; }
            set { SetProperty(ref _specialOptimizationStructures, value); }
        }

        public object OptimizationStructureDerivations
        {
            get { return _optimizationStructureDerivations; }
            set { SetProperty(ref _optimizationStructureDerivations, value); }
        }

        public object OptimizationSetup
        {
            get { return _optimizationSetup; }
            set { SetProperty(ref _optimizationSetup, value); }
        }

        public object ScriptConfiguration
        {
            get { return _scriptConfiguration; }
            set { SetProperty(ref _scriptConfiguration, value); }
        }

        public object BeamPlacement
        {
            get { return _beamPlacement; }
            set { SetProperty(ref _beamPlacement, value); }
        }

        public object PlanPreparation
        {
            get { return _planPreparation; }
            set { SetProperty(ref _planPreparation, value); }
        }
        #endregion

        #region commands
        public ICommand QuickStartGuideCommand { get; set; }
        public ICommand HelpGuideCommand { get; set; }
        public ICommand WindowClosingCommand { get; set; }
        #endregion

        #region fields
        protected List<PrescriptionModel> _prescriptions = new List<PrescriptionModel> { };
        protected List<PlanOptimizationSetupModel> _planOptimizationSetup = new List<PlanOptimizationSetupModel> { };
        protected List<PlanIsocenterModel> _planIsocenters = new List<PlanIsocenterModel> { };
        private List<string> _structureIdsPostUnion = new List<string> { };
        protected List<string> StructureIdsPostUnion
        {
            get => _structureIdsPostUnion;
            set
            {
                _structureIdsPostUnion = value;
                WeakReferenceMessenger.Default.Send(new RequestUpdateStructureIds(_structureIdsPostUnion));
            }
        }
        protected string _generalConfigurationFile = string.Empty;
        protected PlanPreparationBase _planPrep = null;
        private PlanType _planType;
        #endregion

        public BaseViewModel(PlanType type, string[] args)
        {
            _planType = type;
            Logger.GetInstance().PlanType = _planType;
            if (args.Any()) EclipseContextHelper.GenerateEclipseContext(args.ToList());

            TargetStructureDerivations = new StructureDerivationsView { DataContext = new StructureDerivationsViewModel(true) };
            SpecifyTargets = new SpecifyTargetsView { DataContext = new SetTargetsViewModel() };
            SpecialOptimizationStructures = new SpecialOptimizationStructuresView { DataContext = new SpecialOptimizationStructuresViewModel() };
            OptimizationStructureDerivations = new StructureDerivationsView { DataContext = new StructureDerivationsViewModel(false) };
            BeamPlacement = new BeamPlacementView { DataContext = new BeamPlacementViewModel(type) };
            OptimizationSetup = new OptimizationSetupView { DataContext = new OptimizationSetupViewModel(type) };
            PlanPreparation = new PlanPreparationView { DataContext = new PlanPreparationViewModel() };

            PlanTemplates = new ObservableCollection<AutoPlanTemplateBase>() { };
            QuickStartGuideCommand = new RelayCommand(LaunchQuickStartGuide);
            HelpGuideCommand = new RelayCommand(LaunchHelpGuide);
            WindowClosingCommand = new RelayCommand(WindowClosing);

            SpecifyTargetsTabBackground = Brushes.PaleVioletRed;
            StructureTuningTabBackground = Brushes.LightGray;
            OptimizationStructureDerivationBackground = Brushes.LightGray;
            BeamPlacementTabBackground = Brushes.LightGray;
            OptimizationSetupTabBackground = Brushes.LightGray;

            InitializeGeneralMessengers();
            PerformPlanTypeSpecificInitialization();
            InitializePlanTypeSpecificMessengers();
            ScriptConfiguration = new ScriptConfigurationView { DataContext = new ScriptConfigurationViewModel(BuildScriptConfigurationInfo()) };

            if (EclipseContext.GetInstance().IsInitialized)
            {
                if (!ReferenceEquals(EclipseContext.GetInstance().Patient, null)) PatientMRN = EclipseContext.GetInstance().Patient.Id;
                if (EclipseContext.GetInstance().CTImages.Any())
                {
                    WeakReferenceMessenger.Default.Send(new RequestUpdateCTList(EclipseContext.GetInstance().CTImages.ToList().ConvertAll(x => new ExportCTModel(x.Series.Id, x.Id, x.ZSize, x.HistoryDateTime.ToString()))));
                }
                if (!ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
                {
                    StructureSetId = EclipseContext.GetInstance().StructureSet.Id;
                    StructureIdsPostUnion = StructureTuningHelper.GenerateStructureIdListPostUnion(EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList());

                    if (!PhysicianTargetApprovalRequired() || EclipseContext.GetInstance().StructureSet.Structures.Any(x => x.ApprovalHistory.First().ApprovalStatus == StructureApprovalStatus.Approved && x.Id.ToLower().Contains("ptv")))
                    {
                        SetTargetsTabBackground = Brushes.PaleVioletRed;
                        TargetStructureDerivationsBackground = Brushes.LightGray;
                    }
                    else
                    {
                        TargetStructureDerivationsBackground = Brushes.PaleVioletRed;
                        SetTargetsTabBackground = Brushes.LightGray;
                    }
                }
            }
            else
            {
                TargetStructureDerivationsBackground = Brushes.LightGray;
                SetTargetsTabBackground = Brushes.PaleVioletRed;
                List<ExportCTModel> models = new List<ExportCTModel>
                {
                    new ExportCTModel("1", "CT 1", 100, DateTime.Now.ToString("yyyy-mm-dd")),
                    new ExportCTModel("2", "CT 2", 200, "2019-01-01"),
                    new ExportCTModel("3", "CT 3", 300, "2020-10-10"),
                };
                WeakReferenceMessenger.Default.Send(new RequestUpdateCTList(models));
                StructureIdsPostUnion = PlanTemplates.SelectMany(x => x.GenerateStructureIdList()).Distinct().ToList();
            }
        }

        #region messengers
        private void InitializeGeneralMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestPerformTargetDerivations>(this, (r, m) =>
            {
                PerformTargetStructureDerivations(m.StructureOperations);
            });
            WeakReferenceMessenger.Default.Register<RequestSetTargetsMessage>(this, (r, m) =>
            {
                SetTargets(m.PlanTargets);
            });
            WeakReferenceMessenger.Default.Register<RequestPerformOptimizationStructureDerivations>(this, (r, m) =>
            {
                PerformOptimizationStructureDerivations(m.StructureOperations);
            });
            WeakReferenceMessenger.Default.Register<RequestGenerateAndPlaceBeams>(this, (r, m) =>
            {
                GeneratePlansAndPlaceBeams(m.SelectedLinac, m.SelectedEnergy, m.ContourOverlap, m.ContourOverlapMargin, m.PlanIsocenters);
            });
            WeakReferenceMessenger.Default.Register<RequestSetOptimizationConstraintsMessage>(this, (r, m) =>
            {
                AssignOptimizationConstraints(m.PlanOptimizationSetup);
            });
            WeakReferenceMessenger.Default.Register<RequestGenerateShiftNoteMessage>(this, (r, m) =>
            {
                m.Reply(GenerateShiftNote());
            });
            WeakReferenceMessenger.Default.Register<RequestSeparatePlanMessage>(this, (r, m) =>
            {
                m.Reply(SeparatePlans());
            });
            WeakReferenceMessenger.Default.Register<RequestDoSeparatedPlansRequireDoseRecalculation>(this, (r, m) =>
            {
                m.Reply(_planPrep.DoseRecalcNeeded);
            });
            WeakReferenceMessenger.Default.Register<RequestRecalculateDoseForSeparatedPlans>(this, (r, m) =>
            {
                m.Reply(RecalculateDoseForSeparatePlans());
            });
        }
        #endregion

        #region plan type specific initialization
        protected abstract void PerformPlanTypeSpecificInitialization();

        protected abstract void InitializePlanTypeSpecificMessengers();
        #endregion

        #region information and help
        protected abstract void LaunchQuickStartGuide();
        protected abstract void LaunchHelpGuide();
        #endregion

        #region specify targets
        protected virtual void PerformTargetStructureDerivations(List<StructureOperationModel> preliminaryTargets, bool showCompletionMessage = true)
        {
            if (!EclipseContext.GetInstance().IsInitialized || !preliminaryTargets.Any()) return;
            GeneratePreliminaryTargetsBase generateTargets = GetTargetDerivationClassInstanceForPlanType(preliminaryTargets);
            EclipseContext.GetInstance().Patient.BeginModifications();
            bool result = generateTargets.Execute();
            //grab the log output regardless if it passes or fails
            Logger.GetInstance().AppendLogOutput("Preliminary target generation output:", generateTargets.LogOutput);
            Logger.GetInstance().OpType = ScriptOperationType.GeneratePrelimTargets;
            if (result) return;
            Logger.GetInstance().AddedPrelimTargetsStructures = generateTargets.AddedTargetstructures;
            TargetStructureDerivationsBackground = Brushes.ForestGreen;
            StructureIdsPostUnion = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
            if (showCompletionMessage) MessageBox.Show("Structure set is prepared and ready for physician to review targets!");
        }

        protected abstract GeneratePreliminaryTargetsBase GetTargetDerivationClassInstanceForPlanType(List<StructureOperationModel> preliminaryTargets);

        protected void SetTargets(List<PlanTargetsModel> planTargets)
        {
            if (!planTargets.Any()) return;
            if (VerifyPlansIntegrity(planTargets)) return;
            if (VerifyTargetsIntegrity(planTargets.SelectMany(x => x.Targets))) return;

            _prescriptions = BuildPlanTypeSpecificPrescriptionList(planTargets);
            if (!_prescriptions.Any()) return;
            Logger.GetInstance().Prescriptions = _prescriptions;

            _planOptimizationSetup = BuildPlanOptimizationSetupList();

            UpdatePlanTypeSpecificStructureOperationViews();
            WeakReferenceMessenger.Default.Send(new RequestUpdateOptimizationStructureDerivations(_selectedTemplate.OptimizationStructureDerivations));
            WeakReferenceMessenger.Default.Send(new RequestUpdateSpecialOptimizationStructures(_selectedTemplate.SpecialOptimizationStructures));

            SpecifyTargetsTabBackground = Brushes.ForestGreen;
            StructureTuningTabBackground = Brushes.PaleVioletRed;
            OptimizationStructureDerivationBackground = Brushes.PaleVioletRed;
        }

        protected abstract List<PrescriptionModel> BuildPlanTypeSpecificPrescriptionList(List<PlanTargetsModel> planTargets);

        protected abstract void UpdatePlanTypeSpecificStructureOperationViews();

        protected bool VerifyTargetsIntegrity(IEnumerable<TargetModel> targets)
        {
            if (EclipseContext.GetInstance().IsInitialized && !ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                if (PhysicianTargetApprovalRequired())
                {
                    if (!AreRequestedPrescriptionTargetsApproved(targets)) return true;
                }
                else if (targets.Select(x => x.TargetId).Any(x => !StructureTuningHelper.DoesStructureExistInSS(x, true)))
                {
                    IEnumerable<string> missingTargets = targets.Select(x => x.TargetId).Where(x => !StructureTuningHelper.DoesStructureExistInSS(x, true));
                    List<StructureOperationModel> targetDerivations = WeakReferenceMessenger.Default.Send(new RequestTargetStructureDerivations());
                    if (missingTargets.All(x => targetDerivations.Any(y => string.Equals(y.OutputStructure, x, StringComparison.OrdinalIgnoreCase))))
                    {
                        //missing targets from structure set are contained in target derivation list --> derive the targets
                        PerformTargetStructureDerivations(targetDerivations, false);
                        if (missingTargets.Any(x => !StructureTuningHelper.DoesStructureExistInSS(x, true)))
                        {
                            Logger.GetInstance().LogError("Requested targets are still missing from the structure set following target derivation! Exiting!");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected abstract bool PhysicianTargetApprovalRequired();

        protected bool VerifyPlansIntegrity(List<PlanTargetsModel> parsedTargets)
        {
            //verify selected targets are APPROVED
            int numAllowedPlans = _planType == PlanType.VMAT_CSI ? 2 : 1;
            if (parsedTargets.Select(x => x.PlanId).Distinct().Count() > numAllowedPlans)
            {
                Logger.GetInstance().LogError($"Error! Number of requested plans ({parsedTargets.Select(x => x.PlanId).Distinct().Count()}) is greater than the number of allowed plans for plan type {_planType}!");
                return true;
            }
            return false;
        }

        protected bool AreRequestedPrescriptionTargetsApproved(IEnumerable<TargetModel> targets)
        {
            foreach (TargetModel target in targets)
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(target.TargetId, true))
                {
                    Logger.GetInstance().LogError($"Error! {target.TargetId} is either NOT present in structure set or is not contoured!");
                    return false;
                }
                else
                {
                    //structure is present and contoured
                    StructureApprovalStatus approvalStatus = StructureTuningHelper.GetStructureFromId(target.TargetId).ApprovalHistory.First().ApprovalStatus;
                    if (approvalStatus != StructureApprovalStatus.Approved)
                    {
                        Logger.GetInstance().LogError($"Error! {target.TargetId} is NOT approved!" + Environment.NewLine + $"{target.TargetId} approval status: {approvalStatus}");
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region optimization structure generation
        protected void PerformOptimizationStructureDerivations(List<StructureOperationModel> operations)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                Logger.GetInstance().LogError("Error! Script is not connected to aria or no structure set loaded! Cannot perform TS generation/manipulation!");
                return;
            }
            List<SpecialOptimizationStructureModel> specialOptStructures = WeakReferenceMessenger.Default.Send(new RequestSpecialOptimizationStructures());
            TSGenerationManipulationBase generateTS = GetOptStructureDerivationClassInstanceForPlanType(operations, specialOptStructures);

            EclipseContext.GetInstance().Patient.BeginModifications();
            bool failed = generateTS.Execute();
            Logger.GetInstance().AppendLogOutput("TS Generation and manipulation output:", generateTS.LogOutput);
            if (failed) return;

            _planIsocenters = generateTS.PlanIsocentersList;

            WeakReferenceMessenger.Default.Send(new RequestUpdatePlanIsocenterList(_planIsocenters));
            StructureIdsPostUnion = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
            UpdateOptimizationSetup(generateTS);

            StructureTuningTabBackground = Brushes.ForestGreen;
            OptimizationStructureDerivationBackground = Brushes.ForestGreen;
            BeamPlacementTabBackground = Brushes.PaleVioletRed;

            Logger.GetInstance().AddedStructures = generateTS.AddedStructureIds;
            Logger.GetInstance().OptimizationStructureDerivations = operations;
            Logger.GetInstance().TSTargets = generateTS.PlanTargets.SelectMany(x => x.Targets).ToDictionary(x => x.TargetId, x => x.TsTargetId);
            Logger.GetInstance().NormalizationVolumes = generateTS.NormalizationVolumes;
            Logger.GetInstance().PlanIsocenters = generateTS.PlanIsocentersList;

            //_planIsocenters.Add(new PlanIsocenterModel("test", new List<IsocenterModel> { new IsocenterModel("1", 2, BeamType.VMAT), new IsocenterModel("2", 3, BeamType.VMAT), new IsocenterModel("3", 4, BeamType.VMAT) }));
            //_planIsocenters.Add(new PlanIsocenterModel("doubleTest", new List<IsocenterModel> { new IsocenterModel("4", 2, BeamType.APPA) }));
        }

        protected abstract TSGenerationManipulationBase GetOptStructureDerivationClassInstanceForPlanType(List<StructureOperationModel> operations, List<SpecialOptimizationStructureModel> specialOps);
        #endregion

        #region plan and beam placement
        protected void GeneratePlansAndPlaceBeams(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                Logger.GetInstance().LogError("Error! Script is not connected to aria or no structure set loaded! Cannot perform beam placement!");
                return;
            }
            _planIsocenters = PlanIsocenters;
            GeneratePlansAndPlaceBeamsBase placeBeams = GetBeamPlacementClassInstanceForPlanType(linac, energy, contourOverlap, overlapMargin, PlanIsocenters);
            bool failed = placeBeams.Execute();
            Logger.GetInstance().AppendLogOutput("Generate plans and place beams output:", placeBeams.GetLogOutput());
            if (failed) return;
            if (placeBeams.VMATPlans.Any())
            {
                EclipseContext.GetInstance().VMATPlans = placeBeams.VMATPlans;
                Logger.GetInstance().PlanUIDs = placeBeams.VMATPlans.Select(x => x.UID).ToList();
            }
            if (placeBeams.FieldJunctions.Any())
            {
                _planOptimizationSetup = UpdateOptimizationConstraintsWithTSJunctions(placeBeams.FieldJunctions, _planOptimizationSetup);
                StructureIdsPostUnion = EclipseContext.GetInstance().StructureSet.Structures.Select(x => x.Id).ToList();
            }
            WeakReferenceMessenger.Default.Send(new RequestUpdateOptimizationConstraintsMessage(_planOptimizationSetup));

            BeamPlacementTabBackground = Brushes.ForestGreen;
            OptimizationSetupTabBackground = Brushes.PaleVioletRed;
        }

        protected abstract GeneratePlansAndPlaceBeamsBase GetBeamPlacementClassInstanceForPlanType(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters);
        #endregion

        #region optimization setup
        public List<PlanOptimizationSetupModel> BuildPlanOptimizationSetupList()
        {
            if (!ReferenceEquals(_selectedTemplate, null))
            {
                return OptimizationSetupHelper.RetrieveOptConstraintsFromTemplate(_selectedTemplate, _prescriptions, _planType);
            }
            else
            {
                List<PlanOptimizationSetupModel> result = new List<PlanOptimizationSetupModel> { };
                foreach (PlanTargetsModel itr in TargetsHelper.GroupPrescriptionsByPlanIdAndOrderByTargetRx(_prescriptions))
                {
                    List<OptimizationConstraintModel> constraints = new List<OptimizationConstraintModel>();
                    foreach (TargetModel target in itr.Targets)
                    {
                        constraints.Add(new OptimizationConstraintModel(target.TargetId, OptimizationObjectiveType.Lower, target.TargetRxDose, Units.cGy, 100.0, 100));
                        constraints.Add(new OptimizationConstraintModel(target.TargetId, OptimizationObjectiveType.Upper, 1.02 * target.TargetRxDose, Units.cGy, 0.0, 100));
                    }
                    result.Add(new PlanOptimizationSetupModel(itr.PlanId, constraints));
                }
                return result;
            }
        }
        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithTSTargets(List<PlanTargetsModel> planTargets, List<PlanOptimizationSetupModel> planConstraints)
        {
            //update optimization constraint list to replace target constraints with ts targets
            foreach (PlanTargetsModel itr in planTargets)
            {
                if (planConstraints.Any(x => string.Equals(x.PlanId, itr.PlanId)))
                {
                    List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(x.PlanId, itr.PlanId)).OptimizationConstraints;
                    foreach (TargetModel target in itr.Targets)
                    {
                        if (constraints.Any(x => string.Equals(x.StructureId, target.TargetId)))
                        {
                            foreach (OptimizationConstraintModel matchingTargetConstraint in constraints.Where(x => string.Equals(x.StructureId, target.TargetId)))
                            {
                                matchingTargetConstraint.StructureId = target.TsTargetId;
                            }
                        }
                    }
                }
            }
            return planConstraints;
        }

        protected virtual void UpdateOptimizationSetup(TSGenerationManipulationBase generateTS)
        {
            _planOptimizationSetup = UpdateOptimizationConstraintsWithTSTargets(generateTS.PlanTargets, _planOptimizationSetup);
        }

        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithRings(List<TSRingStructureModel> rings, List<PlanOptimizationSetupModel> planConstraints, int ringPrioity = 80)
        {
            foreach (TSRingStructureModel itr in rings)
            {
                string planId = string.Empty;
                if (planConstraints.Count > 1)
                {
                    if (_prescriptions.Any(x => string.Equals(itr.TargetId, x.TargetId)))
                    {
                        //grab the plan that contains this specific target
                        planId = _prescriptions.First(x => string.Equals(itr.TargetId, x.TargetId)).PlanId;
                    }
                    else continue;
                }
                else planId = _prescriptions.First().PlanId;

                //grab the optimization constraints that belong to this plan
                List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(planId, x.PlanId)).OptimizationConstraints;
                //insert the ts ring constraint
                constraints.Add(new OptimizationConstraintModel(itr.RingId, OptimizationObjectiveType.Upper, itr.DoseLevel, Units.cGy, 0.0, ringPrioity));
            }
            return planConstraints;
        }


        public List<PlanOptimizationSetupModel> UpdateOptimizationConstraintsWithTSJunctions(List<PlanFieldJunctionModel> junctions, List<PlanOptimizationSetupModel> planConstraints)
        {
            //update optimization constraint list to replace target constraints with ts targets
            foreach (PlanFieldJunctionModel itr in junctions)
            {
                //get the dose for the last prescription item that has the same plan id as the current plan field junction model (should already be sorted by cumulative dose to targets)
                double dose = _prescriptions.Last(x => string.Equals(itr.PlanId, x.PlanId)).CumulativeDoseToTarget;
                List<OptimizationConstraintModel> constraints = planConstraints.First(x => string.Equals(x.PlanId, itr.PlanId)).OptimizationConstraints;
                foreach (FieldJunctionModel jnx in itr.FieldJunctions)
                {
                    constraints.Add(new OptimizationConstraintModel(jnx.JunctionStructureId, OptimizationObjectiveType.Lower, dose, Units.cGy, 100.0, 100));
                    constraints.Add(new OptimizationConstraintModel(jnx.JunctionStructureId, OptimizationObjectiveType.Upper, 1.02 * dose, Units.cGy, 0.0, 100));
                }
            }
            return planConstraints;
        }

        protected void AssignOptimizationConstraints(List<PlanOptimizationSetupModel> PlanOptimizationConstraints)
        {
            if (!EclipseContext.GetInstance().VMATPlans.Any())
            {
                Logger.GetInstance().LogError("Error! No vmat plans generated from beam placement! No plans to assign optimization constraints to!");
                return;
            }
            bool constraintsAssigned = false;
            foreach (PlanOptimizationSetupModel itr in PlanOptimizationConstraints)
            {
                //additional check if the plan was not found in the list of VMATplans
                if (EclipseContext.GetInstance().VMATPlans.Any(x => string.Equals(x.Id, itr.PlanId)))
                {
                    ExternalPlanSetup plan = EclipseContext.GetInstance().VMATPlans.First(x => string.Equals(x.Id, itr.PlanId));
                    OptimizationSetupHelper.AssignOptConstraints(itr.OptimizationConstraints, plan, false, 0.0);
                    constraintsAssigned = true;
                }
                else Logger.GetInstance().LogError($"{itr.PlanId} not found!");
            }
            if (constraintsAssigned)
            {
                string message = "Optimization objectives have been successfully set!" + Environment.NewLine + Environment.NewLine + "Please review the generated structures, placed isocenters, placed beams, and optimization parameters!";
                MessageBox.Show(message);
                Logger.GetInstance().OptimizationConstraints = PlanOptimizationConstraints.ToList();
                if (!Logger.GetInstance().PlanUIDs.Any())
                {
                    foreach (string itr in EclipseContext.GetInstance().VMATPlans.OrderBy(x => x.CreationDateTime).Select(y => y.UID))
                    {
                        Logger.GetInstance().PlanUIDs.Add(itr);
                    }
                }
                Logger.GetInstance().OpType = ScriptOperationType.FullPreparationForOptimization;
                OptimizationSetupTabBackground = Brushes.ForestGreen;
            }
            else Logger.GetInstance().LogError("Error! No optimization constraints assigned!");
        }
        #endregion

        #region prep plan for treatment
        protected abstract bool GenerateShiftNote();
        protected abstract bool SeparatePlans();
        protected abstract bool RecalculateDoseForSeparatePlans();
        #endregion

        #region update UI
        public void ResetInitialRxDose()
        {
            if (InitialNumberOfFractions > 0 && InitialDosePerFraction > 0)
            {
                //double priorTotalDose = PlanTotalDose;
                InitialPlanTotalDose = Math.Round(_initialDosePerFraction * _initialNumberOfFractions, 1);
            }
        }

        protected void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            InitialDosePerFraction = _selectedTemplate.InitialRxDosePerFx;
            InitialNumberOfFractions = _selectedTemplate.InitialRxNumberOfFractions;
            UpdatePlanTypeSpecificUIWithPlanTemplate();
            WeakReferenceMessenger.Default.Send(new RequestUpdatePlanTargetsList(_selectedTemplate.PlanTargets));
            WeakReferenceMessenger.Default.Send(new RequestUpdateTargetDerivationOperations(_selectedTemplate.TargetDerivationOperations));
            Logger.GetInstance().Template = _selectedTemplate.TemplateName;
        }

        protected abstract void UpdatePlanTypeSpecificUIWithPlanTemplate();
        #endregion

        #region script configuration
        protected abstract void LoadScriptConfigurationSettings(string file);
        protected abstract StringBuilder BuildScriptConfigurationInfo();
        #endregion

        public void WindowClosing()
        {
            if (EclipseContext.GetInstance().IsInitialized)
            {
                ScriptClosingHelper.CloseApplication(false);
            }
        }
    }
}
