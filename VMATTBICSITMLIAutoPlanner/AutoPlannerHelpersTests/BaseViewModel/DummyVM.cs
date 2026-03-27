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

        protected override void PerformPlanTypeSpecificInitialization()
        {
            return;
        }

        protected override void InitializePlanTypeSpecificMessengers()
        {
            return;
        }

        protected override void LaunchQuickStartGuide()
        {
            return;
        }

        protected override void LaunchHelpGuide()
        {
            return;
        }

        protected override GeneratePreliminaryTargetsBase GetTargetDerivationClassInstanceForPlanType(List<StructureOperationModel> preliminaryTargets)
        {
            throw new NotImplementedException();
        }

        protected override List<PrescriptionModel> BuildPlanTypeSpecificPrescriptionList(List<PlanTargetsModel> planTargets)
        {
            throw new NotImplementedException();
        }


        protected override void UpdatePlanTypeSpecificStructureOperationViews()
        {
            return;
        }

        protected override bool PhysicianTargetApprovalRequired()
        {
            return false;
        }

        protected override TSGenerationManipulationBase GetOptStructureDerivationClassInstanceForPlanType(List<StructureOperationModel> operations, List<SpecialOptimizationStructureModel> specialOps)
        {
            throw new NotImplementedException();

        }

        protected override GeneratePlansAndPlaceBeamsBase GetBeamPlacementClassInstanceForPlanType(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            throw new NotImplementedException();

        }

        protected override bool GenerateShiftNote()
        {
            return false;
        }

        protected override bool SeparatePlans()
        {
            return false;
        }

        protected override bool RecalculateDoseForSeparatePlans()
        {
            return false;
        }

        protected override void UpdatePlanTypeSpecificUIWithPlanTemplate()
        {
            return;
        }

        protected override void LoadScriptConfigurationSettings(string file)
        {
            return;
        }

        protected override StringBuilder BuildScriptConfigurationInfo()
        {
            return new StringBuilder();
        }

        public DummyVM(PlanType type, string[] args) : base(type, args) { }
    }
}
