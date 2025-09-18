using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerOptimizationLoop.Base;
using AutoPlannerOptimizationLoop.DataContainers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoop.Core
{
    public class VMATTBIOptimization : OptimizationLoopBase
    {
        private List<OptimizationConstraintModel> _constraints;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_d"></param>
        public VMATTBIOptimization(OptDataContainer _d)
        {
            _data = _d;
            InitializeLogPathAndName();
            CalculateNumberOfItemsToComplete();
        }

        public VMATTBIOptimization(List<OptimizationConstraintModel> opt) 
        { 
            _constraints = opt;
        }

        /// <summary>
        /// Primary run control
        /// </summary>
        /// <returns></returns>
        protected override bool Run()
        {
            try
            {
                PrintRunSetupInfo();
                //preliminary checks
                if (PreliminaryChecksSSAndImage(_data.StructureSet, TargetsHelper.GetAllTargetIds(_data.Prescriptions).Any() ? TargetsHelper.GetAllTargetIds(_data.Prescriptions) : _data.NormalizationVolumes.Select(x => x.Value))) return true;
                if (PreliminaryChecksPlans(_data.Plans)) return true;

                ProvideUIUpdate("Commencing optimization loop!");
                if (RunOptimizationLoop(_data.Plans)) return true;

                OptimizationRunCompleted();
            }
            catch (Exception e)
            {
                ProvideUIUpdate($"{e.Message}", true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method to calculate the total number of items to complete during this optimization loop run
        /// </summary>
        protected void CalculateNumberOfItemsToComplete()
        {
            overallCalcItems = 4;
            overallCalcItems += _data.Plans.Count;
            if (_data.RunCoverageCheck) overallCalcItems += 4 * _data.Plans.Count;
            int optLoopItems = 6 * _data.NumberOfIterations * _data.Plans.Count;
            if (_data.OneMoreOptimization) optLoopItems += 3;
            overallCalcItems += optLoopItems;
        }

        #region optimization loop
        /// <summary>
        /// Overridden method to handle any remaining run options once the maximum number of iterations has been reached in the optimization loop
        /// </summary>
        /// <param name="plans"></param>
        /// <returns></returns>
        protected override bool ResolveRunOptions(List<ExternalPlanSetup> plans)
        {
            if (_data.OneMoreOptimization)
            {
                if (RunOneMoreOptionizationToLowerHotspots(plans)) return true;
            }
            if (_data.UseFlash)
            {
                if (RemoveFlashAndRecalc(plans)) return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method to remove the virtual bolus structure from the structure set, recalculate the dose, and renormalize to the original PTV without flash
        /// </summary>
        /// <param name="plans"></param>
        /// <returns></returns>
        private bool RemoveFlashAndRecalc(List<ExternalPlanSetup> plans)
        {
            ProvideUIUpdate(100 * ++overallPercentCompletion / overallCalcItems, Environment.NewLine + "Removing flash, recalculating dose, and renormalizing to TS_PTV_VMAT!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

            Structure bolus = StructureTuningHelper.GetStructureFromId("bolus_flash"); ;
            if (bolus == null)
            {
                //no structure named bolus_flash found. This is a problem. 
                ProvideUIUpdate("No structure named 'BOLUS_FLASH' found in structure set! Exiting!", true);
                return true;
            }
            else
            {
                //reset dose calculation matrix for each plan in the current course. Sorry! You will have to recalculate dose to EVERY plan!
                string calcModel = _data.Plans.First().GetCalculationModel(CalculationType.PhotonVolumeDose);
                List<ExternalPlanSetup> plansWithCalcDose = new List<ExternalPlanSetup> { };
                foreach (ExternalPlanSetup itr in plans.First().Course.ExternalPlanSetups)
                {
                    if (itr.IsDoseValid && string.Equals(itr.StructureSet.UID, _data.StructureSet.UID))
                    {
                        itr.ClearCalculationModel(CalculationType.PhotonVolumeDose);
                        itr.SetCalculationModel(CalculationType.PhotonVolumeDose, calcModel);
                        plansWithCalcDose.Add(itr);
                    }
                }
                //reset the bolus dose to undefined
                bolus.ResetAssignedHU();

                //recalculate dose to all the plans that had previously had dose calculated in the current course
                foreach (ExternalPlanSetup itr in plansWithCalcDose)
                {
                    CalculateDose(_data.IsDemo, itr, _data.Application);
                    ProvideUIUpdate(100 * ++overallPercentCompletion / overallCalcItems, "Dose calculated, normalizing plan!");
                    ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
                    if (plans.Any(x => x == itr))
                    {
                        //force the plan to normalize to TS_PTV_VMAT after removing flash
                        double normalizationValue = NormalizePlan(itr, TargetsHelper.GetTargetStructureForPlanType("", false, _data.PlanType), _data.TreatmentPercentage, _data.TargetCoverageNormalization);
                        if (double.IsNaN(normalizationValue)) return true;
                        itr.PlanNormalizationValue = normalizationValue;
                        ProvideUIUpdate(100 * ++overallPercentCompletion / overallCalcItems, $"{itr.Id} normalized. Normalization value = {normalizationValue:0.0}%");
                    }
                    else
                    {
                        ProvideUIUpdate(100 * ++overallPercentCompletion / overallCalcItems, $"Plan: {itr.Id} is not contained in the plan list! Skipping normalization!");
                    }
                }
            }
            return false;
        }
        #endregion
    }
}
