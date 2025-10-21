using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using System.Linq;
using TMLIAutoPlanner.Settings;
using VMS.TPS.Common.Model.API;

namespace TMLIAutoPlanner.Core
{
    internal class PreparePlansForTreatment_TMLI : PlanPreparationBase
    {
        private List<ExternalPlanSetup> appaPlans = new List<ExternalPlanSetup> { };

        internal PreparePlansForTreatment_TMLI() 
        {
            VMATPlan = EclipseContext.GetInstance().VMATPlans.First();
            if (VMATPlan.Course.ExternalPlanSetups.Any(x => x.Id.ToLower().Contains("legs")))
            {
                appaPlans = VMATPlan.Course.ExternalPlanSetups.Where(x => x.Id.ToLower().Contains("legs")).ToList();
            }
            SetCloseOnFinish(TMLIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        #region Run Control
        /// <summary>
        /// Run control
        /// </summary>
        /// <returns></returns>
        protected override bool Run()
        {
            UpdateUILabel("Running:");
            if (_recalculateDoseOnly)
            {
                if (DoseRecalcNeeded && ReCalculateDose()) return true;
                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished calculating dose!");
                ProvideUIUpdate($"Run time: {ElapsedRunTime} (mm:ss)");
            }
            else
            {
                if (PreliminaryChecks()) return true;
                if (SeparatePlans()) return true;
                if (TMLIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep && DoseRecalcNeeded && ReCalculateDose()) return true;
                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished separating plans!");
                ProvideUIUpdate($"Run time: {ElapsedRunTime} (mm:ss)");
            }
            return false;
        }
        #endregion

        #region Preliminary Checks
        /// <summary>
        /// Preliminary checks
        /// </summary>
        /// <returns></returns>
        private bool PreliminaryChecks()
        {
            UpdateUILabel("Preliminary Checks:");
            ProvideUIUpdate($"Checking {VMATPlan.Id} ({VMATPlan.UID}) is valid for preparation");
            if (CheckBeamNameFormatting(VMATPlan)) return true;
            if (CheckIfDoseRecalcNeeded(VMATPlan)) DoseRecalcNeeded = true;
            if (appaPlans.Any())
            {
                foreach (ExternalPlanSetup itr in appaPlans)
                {
                    ProvideUIUpdate($"Checking {itr.Id} ({itr.UID}) is valid for preparation");
                    if (CheckBeamNameFormatting(itr)) return true;
                }
            }
            ProvideUIUpdate(100, "Preliminary checks complete");
            return false;
        }
        #endregion

        #region Separate the plans
        /// <summary>
        /// Helper utility method to separate the VMAT and AP/PA isocenters into separate plans
        /// </summary>
        /// <returns></returns>
        private bool SeparatePlans()
        {
            UpdateUILabel("Separating plans:");
            int percentComplete = 0;
            int calcItems = 3;
            ProvideUIUpdate(0, "Initializing...");
            List<List<Beam>> vmatBeamsPerIso = PlanPrepHelper.ExtractBeamsPerIso(VMATPlan);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved list of beams for each isocenter for plan: {VMATPlan.Id}");

            numVMATIsos = vmatBeamsPerIso.Count;
            if (appaPlans.Any())
            {
                numIsos = appaPlans.Count() + numVMATIsos;
                ProvideUIUpdate(100 * ++percentComplete / ++calcItems, $"Retrieved list of beams for each isocenter for appa plans");
            }

            //get the isocenter names using the isoNameHelper class
            List<IsocenterModel> isoNames = new List<IsocenterModel>(IsoNameHelper.GetTBIVMATIsoNames(numVMATIsos, numIsos));
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved isocenter names for plan: {VMATPlan.Id}");

            if (appaPlans.Any())
            {
                isoNames.AddRange(IsoNameHelper.GetTBIAPPAIsoNames(numVMATIsos, numIsos));
                ProvideUIUpdate(100 * ++percentComplete / ++calcItems, $"Retrieved isocenter names for appa plans");
            }

            ProvideUIUpdate($"Separating isocenters in plan {VMATPlan.Id} into separate plans");
            if (SeparatePlan(VMATPlan, vmatBeamsPerIso, isoNames)) return true;
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Successfully separated isocenters in plan {VMATPlan.Id}");

            return false;
        }
        #endregion
    }
}
