using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;

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

        protected override void GeneratePlansAndPlaceBeams(string linac, string energy, bool contourOverlap, double overlapMargin, List<PlanIsocenterModel> PlanIsocenters)
        {
            throw new NotImplementedException();
        }

        protected override void LoadScriptConfigurationSettings(string file)
        {
            throw new NotImplementedException();
        }

        protected override void PerformTSStructureGenerationManipulation(List<RequestedTSStructureModel> structuresToGenerate, List<RequestedTSManipulationModel> manipulations)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateUIWithSelectedPlanTemplate()
        {
            throw new NotImplementedException();
        }

        protected override bool VerifyTargetsIntegrity(List<PlanTargetsModel> parsedTargets)
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
    }
}
