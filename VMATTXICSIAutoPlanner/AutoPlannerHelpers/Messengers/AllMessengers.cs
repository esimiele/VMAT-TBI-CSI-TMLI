using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.Messengers
{
    #region plan preparation
    public class RequestGenerateShiftNoteMessage : RequestMessage<bool> { }

    public class RequestSeparatePlanMessage : RequestMessage<bool> { }
    public class RequestDoSeparatedPlansRequireDoseRecalculation : RequestMessage<bool> { }
    public class RequestRecalculateDoseForSeparatedPlans : RequestMessage<bool> { }
    public class RequestAreSeparatedPlansAutomaticallyRecalculated : RequestMessage<bool> { }
    #endregion

    #region set targets
    public class RequestTargetStructureDerivations : RequestMessage<List<StructureOperationModel>> { }
    public class RequestSetTargetsMessage : PlanTargetListMessage
    {
        public RequestSetTargetsMessage(IEnumerable<PlanTargetsModel> planTargets) { this.PlanTargets = new List<PlanTargetsModel>(planTargets); }
    }
    public class RequestUpdatePlanTargetsList : PlanTargetListMessage
    {
        public RequestUpdatePlanTargetsList(IEnumerable<PlanTargetsModel> planTargets) { this.PlanTargets = new List<PlanTargetsModel>(planTargets); }
    }

    public class PlanTargetListMessage
    {
        public List<PlanTargetsModel> PlanTargets { get; protected set; }
    }
    #endregion

    #region structure generation and manipulation
    public class RequestUpdateOptimizationStructureDerivations : RequestUpdateStructureDerivations
    {
        public RequestUpdateOptimizationStructureDerivations(List<StructureOperationModel> structureOperations)
        {
            this.StructureOperations = structureOperations;
        }
    }
    public class RequestUpdateTargetDerivationOperations : RequestUpdateStructureDerivations
    {

        public RequestUpdateTargetDerivationOperations(List<StructureOperationModel> structureOperations)
        {
            this.StructureOperations = structureOperations;
        }
    }

    public abstract class RequestUpdateStructureDerivations
    {
        public List<StructureOperationModel> StructureOperations { get; protected set; }
    }

    public class RequestPerformTargetDerivations : RequestUpdateStructureDerivations
    {
        public RequestPerformTargetDerivations(IEnumerable<StructureOperationModel> structureOperations)
        {
            this.StructureOperations = structureOperations.ToList();
        }
    }
    public class RequestPerformOptimizationStructureDerivations : RequestUpdateStructureDerivations
    {
        public RequestPerformOptimizationStructureDerivations(IEnumerable<StructureOperationModel> structureOperations)
        {
            this.StructureOperations = structureOperations.ToList();
        }
    }

    public class RequestUpdateSpecialOptimizationStructures
    {
        public List<SpecialOptimizationStructureModel> SpecialOptimizationStructures { get; private set; }
        public RequestUpdateSpecialOptimizationStructures(IEnumerable<SpecialOptimizationStructureModel> specialOptStructures) { this.SpecialOptimizationStructures = new List<SpecialOptimizationStructureModel>(specialOptStructures); }
    }

    public class RequestCropOverlapStructures : RequestMessage<List<string>> { }
    public class RequestRingStructures : RequestMessage<List<TSRingStructureModel>> { }
    public class RequestSpecialOptimizationStructures : RequestMessage<List<SpecialOptimizationStructureModel>> { }
    public class RequestUpdateRingStructures
    {
        public bool SkipStructureIdCheck { get; private set; }
        public List<TSRingStructureModel> Rings { get; private set; }
        public RequestUpdateRingStructures(IEnumerable<TSRingStructureModel> rings, bool skipStructureIdCheck) { this.Rings = new List<TSRingStructureModel>(rings); this.SkipStructureIdCheck = skipStructureIdCheck; }
    }

    public class RequestUpdateCropOverlapStructures
    {
        public bool SkipStructureIdCheck { get; private set; }
        public List<string> CropOverlapStructures { get; private set; }
        public RequestUpdateCropOverlapStructures(IEnumerable<string> cropOverlapStructures, bool skipStructureIdCheck) { this.CropOverlapStructures = new List<string>(cropOverlapStructures); this.SkipStructureIdCheck = skipStructureIdCheck; }
    }
    #endregion

    #region beam placement
    public class RequestUpdatePlanIsocenterList
    {
        public List<PlanIsocenterModel> PlanIsocenterList { get; private set; }
        public RequestUpdatePlanIsocenterList(IEnumerable<PlanIsocenterModel> isos) { this.PlanIsocenterList = isos.ToList(); }
    }

    public class RequestHideNumberOfVMATIsocenters
    {
        public RequestHideNumberOfVMATIsocenters() { }
    }

    public class RequestUpdateBeamPlacementDefaultSettings
    {
        public List<string> Linacs { get; private set; }
        public List<string> Energies { get; private set; }
        public bool ContourOverlap { get; private set; }
        public double ContourOverlapMargin { get; private set; }
        public IEnumerable<int> FieldsPerIsocenter { get; private set; }
        public RequestUpdateBeamPlacementDefaultSettings(List<string> linacs, List<string> energies, bool contourOverlap, double overlapMargin, IEnumerable<int> fieldsPerIso)
        {
            Linacs = linacs;
            Energies = energies;
            ContourOverlap = contourOverlap;
            ContourOverlapMargin = overlapMargin;
            FieldsPerIsocenter = fieldsPerIso;
        }
    }

    public class RequestGenerateAndPlaceBeams
    {
        public string SelectedLinac { get; private set; }
        public string SelectedEnergy { get; private set; }
        public bool ContourOverlap { get; private set; }
        public double ContourOverlapMargin { get; private set; }
        public List<PlanIsocenterModel> PlanIsocenters { get; private set; }
        public RequestGenerateAndPlaceBeams(string linac, string energy, bool overlap, double overlapMargin, IEnumerable<PlanIsocenterModel> isos)
        {
            SelectedLinac = linac;
            SelectedEnergy = energy;
            ContourOverlap = overlap;
            ContourOverlapMargin = overlapMargin;
            PlanIsocenters = isos.ToList();
        }
    }
    #endregion

    #region preliminary target generation
    public class RequestUpdateTargetStructures
    {
        public List<SpecialOptimizationStructureModel> Structures { get; private set; }
        public RequestUpdateTargetStructures(List<SpecialOptimizationStructureModel> structures) { this.Structures = structures; }
    }
    #endregion

    #region export ct
    public class RequestUpdateCTList
    {
        public IEnumerable<ExportCTModel> Images { get; private set; }
        public RequestUpdateCTList(IEnumerable<ExportCTModel> images) { this.Images = images; }
    }
    public class RequestExportCT
    {
        public ExportCTModel SelectedCTImage { get; private set; }
        public RequestExportCT(ExportCTModel selectedCTImage) { this.SelectedCTImage = selectedCTImage;}
    }
    #endregion

    #region optimization setup
    public class RequestSetOptimizationConstraintsMessage
    {
        public List<PlanOptimizationSetupModel> PlanOptimizationSetup { get; private set; }
        public RequestSetOptimizationConstraintsMessage(IEnumerable<PlanOptimizationSetupModel> constraints) { this.PlanOptimizationSetup = constraints.ToList(); }
    }

    public class RequestUpdateOptimizationConstraintsMessage
    {
        public List<PlanOptimizationSetupModel> PlanOptimizationSetup { get; private set; }
        public RequestUpdateOptimizationConstraintsMessage(IEnumerable<PlanOptimizationSetupModel> constraints) { this.PlanOptimizationSetup = constraints.ToList(); }
    }
    #endregion

    #region multiple
    public class RequestUpdateStructureIds
    {
        public List<string> StructureIds { get; private set; }
        public RequestUpdateStructureIds(IEnumerable<string> structureIds) { this.StructureIds = structureIds.ToList(); }
    }

    public class RequestUpdateScriptConfiguration
    {
        public StringBuilder ScriptConfiguration = new StringBuilder();
        public RequestUpdateScriptConfiguration(StringBuilder config) { ScriptConfiguration = config; }
    }
    #endregion

    #region optimization
    public class RequestPlanSelectionChanged
    {
        public RequestPlanSelectionChanged() { }
    }

    public class RequestOptimizationConstraintsFromPlan : RequestMessage<List<PlanOptimizationSetupModel>> { }

    public class RequestPlanObjectives : RequestMessage<List<PlanObjectiveModel>> { }
    public class RequestSelectPatient
    {
        public string PatientId { get; private set; }
        public PlanType PlanType { get; private set; }
        public string FullPreparationLogPath { get; private set; }
        public RequestSelectPatient(string mrn, PlanType type, string logPath)
        {
            PatientId = mrn;
            PlanType = type;
            FullPreparationLogPath = logPath;
        }
    }

    public class RequestUpdatePlanObjectives
    {
        public List<PlanObjectiveModel> PlanObjectives { get; private set; }
        public RequestUpdatePlanObjectives(IEnumerable<PlanObjectiveModel> planObjectives) { this.PlanObjectives = new List<PlanObjectiveModel>(planObjectives); }
    }
    #endregion
}
