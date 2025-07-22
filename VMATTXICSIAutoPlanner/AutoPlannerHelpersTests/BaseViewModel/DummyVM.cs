using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.BaseCore;

namespace AutoPlannerHelpersTests.BaseViewModel
{
    /// <summary>
    /// Dummy class necessary to derive from abstract base view model class
    /// </summary>
    public class DummyVM : AutoPlannerHelpers.BaseViewModel.BaseViewModel
    {
        public void SetPrescriptions(List<PrescriptionModel> prescriptions)
        {
            _prescriptions = prescriptions;
        }

        public void SetSelectedTemplate(AutoPlanTemplateBase template)
        { 
            _selectedTemplate = template; 
        }

        public DummyVM(PlanType type, string[] args) : base(type, args) { }
        protected override StringBuilder BuildScriptConfigurationInfo()
        {
            throw new NotImplementedException();
        }

        protected override void LoadScriptConfigurationSettings(string file)
        {
            throw new NotImplementedException();
        }

        protected override bool GenerateShiftNote()
        {
            throw new NotImplementedException();
        }

        protected override bool SeparatePlans()
        {
            throw new NotImplementedException();
        }

        protected override bool RecalculateDoseForSeparatePlans()
        {
            throw new NotImplementedException();
        }

        protected override void LaunchQuickStartGuide()
        {
            throw new NotImplementedException();
        }

        protected override void LaunchHelpGuide()
        {
            throw new NotImplementedException();
        }

        protected override GeneratePreliminaryTargetsBase GetTargetDerivationClassInstanceForPlanType(List<StructureOperationModel> preliminaryTargets)
        {
            throw new NotImplementedException();
        }

        protected override TSGenerationManipulationBase GetOptStructureDerivationClassInstanceForPlanType(List<StructureOperationModel> operations, List<SpecialOptimizationStructureModel> specialOps)
        {
            throw new NotImplementedException();
        }

        protected override GeneratePlansAndPlaceBeamsBase GetBeamPlacementClassInstanceForPlanType(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            throw new NotImplementedException();
        }

        protected override void PerformPlanTypeSpecificInitialization()
        {
            throw new NotImplementedException();
        }

        protected override void InitializePlanTypeSpecificMessengers()
        {
            throw new NotImplementedException();
        }

        protected override bool PhysicianTargetApprovalRequired()
        {
            throw new NotImplementedException();
        }

        protected override List<PrescriptionModel> BuildPlanTypeSpecificPrescriptionList(List<PlanTargetsModel> planTargets)
        {
            throw new NotImplementedException();
        }

        protected override void UpdatePlanTypeSpecificStructureOperationViews()
        {
            throw new NotImplementedException();
        }

        protected override void UpdatePlanTypeSpecificUIWithPlanTemplate()
        {
            throw new NotImplementedException();
        }
    }
}
