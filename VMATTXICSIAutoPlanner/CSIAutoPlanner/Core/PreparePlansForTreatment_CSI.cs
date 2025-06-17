using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using CSIAutoPlanner.Settings;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace CSIAutoPlanner.Core
{
    internal class PreparePlansForTreatment_CSI : PlanPreparationBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vmat"></param>
        /// <param name="closePW"></param>
        public PreparePlansForTreatment_CSI()
        {
            VMATPlan = EclipseContext.GetInstance().VMATPlans.First();
            SetCloseOnFinish(CSIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        #region Run Control
        /// <summary>
        /// Run control
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            UpdateUILabel("Running:");
            if (_recalculateDoseOnly)
            {
                if (DoseRecalcNeeded && ReCalculateDose()) return true;
                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished calculating dose!");
                ProvideUIUpdate($"Run time: {GetElapsedTime()} (mm:ss)");
            }
            else
            {
                if (PreliminaryChecks()) return true;
                if (SeparatePlans()) return true;
                if (CSIAutoPlannerSettings.AutoDoseRecalculationDuringPlanPrep && DoseRecalcNeeded && ReCalculateDose()) return true;
                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished separating plans!");
                ProvideUIUpdate($"Run time: {GetElapsedTime()} (mm:ss)");
            }
            return false;
        }
        #endregion

        #region Preliminary Checks
        /// <summary>
        /// Preliminary checks prior to preparing plan for approval
        /// </summary>
        /// <returns></returns>
        private bool PreliminaryChecks()
        {
            UpdateUILabel("Preliminary Checks:");
            ProvideUIUpdate($"Checking {VMATPlan.Id} ({VMATPlan.UID}) is valid for preparation");
            if (CheckBeamNameFormatting(VMATPlan)) return true;
            if (CheckIfDoseRecalcNeeded(VMATPlan)) DoseRecalcNeeded = true;
            ProvideUIUpdate(100, "Preliminary checks complete");
            return false;
        }
        #endregion

        #region Separate the vmat plan
        /// <summary>
        /// Separate the VMAT plan into separate plans: one for each isocenter. Much of the heavy lifting for this method is performed in PlanPrepBase
        /// </summary>
        /// <returns></returns>
        private bool SeparatePlans()
        {
            UpdateUILabel("Separating VMAT plan:");
            int percentComplete = 0;
            int calcItems = 2;
            ProvideUIUpdate(0, "Initializing...");
            List<List<Beam>> beamsPerIso = PlanPrepHelper.ExtractBeamsPerIso(VMATPlan);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved list of beams for each isocenter for plan: {VMATPlan.Id}");
            List<IsocenterModel> isoNames = IsoNameHelper.GetCSIIsoNames(beamsPerIso.Count);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved isocenter names for plan: {VMATPlan.Id}");
            ProvideUIUpdate($"Separating isocenters in plan {VMATPlan.Id} into separate plans");
            if (SeparatePlan(VMATPlan, beamsPerIso, isoNames)) return true;
            ProvideUIUpdate(100, $"Successfully separated isocenters in plan {VMATPlan.Id}");
            return false;
        }
        #endregion
    }
}
