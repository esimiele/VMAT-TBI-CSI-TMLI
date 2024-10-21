using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using AutoPlannerHelpers.Models;

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

        public DummyVM(PlanType type, string[] args) : base(type, args) { }
        protected override StringBuilder BuildScriptConfigurationInfo()
        {
            throw new NotImplementedException();
        }

        protected override void GeneratePlansAndPlaceBeams()
        {
            throw new NotImplementedException();
        }

        protected override void LoadScriptConfigurationSettings(string file)
        {
            throw new NotImplementedException();
        }

        protected override void PerformTSStructureGenerationManipulation()
        {
            throw new NotImplementedException();
        }

        protected override void PreparePlanForTreatment()
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
    }
}
