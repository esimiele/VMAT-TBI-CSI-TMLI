using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerOptimizationLoop.Base;
using AutoPlannerOptimizationLoop.DataContainers;
using AutoPlannerOptimizationLoop.Helpers;
using AutoPlannerOptimizationLoop.Models;
using AutoPlannerOptimizationLoop.UIHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoop.Core
{
    public class VMATTMLIOptimization : OptimizationLoopBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_d"></param>
        public VMATTMLIOptimization(OptDataContainer _d)
        {
            _data = _d;
            InitializeLogPathAndName();
            CalculateNumberOfItemsToComplete();
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
            return false;
        }

        protected override List<OptimizationConstraintModel> DetermineNewOptimizationObjectives(ExternalPlanSetup plan, List<PlanOptConstraintsDeviationModel> diffPlanOpt, double totalCostOptimizationConstraints, List<OptimizationConstraintModel> optParams)
        {
            List<OptimizationConstraintModel> updatedConstraints = base.DetermineNewOptimizationObjectives(plan, diffPlanOpt, totalCostOptimizationConstraints, optParams);
            if (_data.Prescriptions.Any(x => CalculationHelper.AreEqual(x.CumulativeDoseToTarget, 2000)) || updatedConstraints.Any(x => x.ConstraintType == OptimizationObjectiveType.Lower && x.QueryDose >= 2000.0))
            {
                ProvideUIUpdate("TMLI plan is using the 20 Gy template");
                ProvideUIUpdate("Attempting to update 12 Gy target constraints depending on achieved coverage");
                //this is the 20 Gy plan template
                //grab 12 Gy target Id
                string lowerDoseTargetId = string.Empty;
                if (_data.Prescriptions.Any()) lowerDoseTargetId = _data.Prescriptions.First(x => CalculationHelper.AreEqual(x.CumulativeDoseToTarget, 1200.0)).TargetId;
                else if (_data.PlanObjectives.Any(x => x.ConstraintType == OptimizationObjectiveType.Lower && CalculationHelper.AreEqual(x.QueryDose, 1200.0))) lowerDoseTargetId = _data.PlanObjectives.First(x => x.ConstraintType == OptimizationObjectiveType.Lower && CalculationHelper.AreEqual(x.QueryDose, 1200.0)).StructureId;
                
                ProvideUIUpdate($"Lower dose 12 Gy target: {lowerDoseTargetId}");

                if (StructureTuningHelper.DoesStructureExistInSS(lowerDoseTargetId))
                {
                    ProvideUIUpdate($"{lowerDoseTargetId} exists in the structure set");

                    Structure lowerDoseTarget = StructureTuningHelper.GetStructureFromId(lowerDoseTargetId);
                    if (_data.PlanObjectives.Any(x => string.Equals(x.StructureId, lowerDoseTargetId, StringComparison.OrdinalIgnoreCase) && x.ConstraintType == OptimizationObjectiveType.Lower))
                    {

                        PlanObjectiveModel model = _data.PlanObjectives.First(x => string.Equals(x.StructureId, lowerDoseTargetId, StringComparison.OrdinalIgnoreCase));
                        ProvideUIUpdate($"Corresponding plan objective found for: {lowerDoseTargetId}");
                        ProvideUIUpdate($"{model.FriendlyName}");

                        ProvideUIUpdate($"Extracting lower dose objective for: {lowerDoseTargetId}");

                        double doseAtVolumeFromPlan = plan.GetDoseAtVolume(lowerDoseTarget, model.QueryVolume, VolumePresentation.Relative, model.QueryDoseUnits == Units.Percent ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute).Dose;
                        if (!double.IsNaN(doseAtVolumeFromPlan))
                        {
                            ProvideUIUpdate($"Dose at volume for {lowerDoseTargetId}: {doseAtVolumeFromPlan:0.0} cGy");

                            double relativeDiff = model.QueryDose / doseAtVolumeFromPlan;
                            ProvideUIUpdate($"Calculated relative difference from plan objective: {relativeDiff:0.0}");

                            foreach (OptimizationConstraintModel itr in updatedConstraints.Where(x => string.Equals(x.StructureId, "ts_" + lowerDoseTargetId, StringComparison.OrdinalIgnoreCase)))
                            {
                                ProvideUIUpdate($"Rescaling optimization objective: {itr.FriendlyName}");
                                ProvideUIUpdate($"Old query dose: {itr.QueryDose:0.0} cGy");
                                itr.QueryDose *= relativeDiff;
                                ProvideUIUpdate($"New query dose: {itr.QueryDose:0.0} cGy");
                            }
                        }
                    }
                }

            }
            return updatedConstraints;
        }

        protected override (bool, List<OptimizationConstraintModel>) UpdateHeaterCoolerStructures(ExternalPlanSetup plan, bool isFinalOptimization, List<RequestedOptimizationTSStructureModel> requestedTSStructures, bool removeExistingHeaterCoolerStructures = true)
        {
            (bool wasKilled, List<OptimizationConstraintModel> updatedConstraints) = base.UpdateHeaterCoolerStructures(plan, isFinalOptimization, requestedTSStructures, removeExistingHeaterCoolerStructures);
            //return immediately if the process was killed by the user OR if this is the final optimization. The reason for the final optimization is because the optimization continues using the current dose as 
            //intermediate with the plan normalization applied. If we then try to scale the cooler structures by ~20% with the normalization applied, it will screw up the plan terribly. Only apply this during the normal
            //optimization loop
            if (wasKilled) return (wasKilled, updatedConstraints);
            else if (isFinalOptimization)
            {
                ProvideUIUpdate("Final iteration of the optimization loop! Skipping scaling of cooler optimization structures");
                return (wasKilled, updatedConstraints);
            }
            if (_data.Prescriptions.Any(x => CalculationHelper.AreEqual(x.CumulativeDoseToTarget, 2000)) || updatedConstraints.Any(x => x.ConstraintType == OptimizationObjectiveType.Lower && x.QueryDose >= 2000.0))
            {
                ProvideUIUpdate("TMLI plan is using the 20 Gy template");
                ProvideUIUpdate("Attempting to update TS cooler structures");

                foreach (OptimizationConstraintModel itr in updatedConstraints.Where(x => x.StructureId.ToLower().Contains("cooler")))
                {
                    //only operate on cooler structures
                    ProvideUIUpdate($"Rescaling optimization objective: {itr.FriendlyName}");
                    ProvideUIUpdate($"Old query dose: {itr.QueryDose:0.0} {itr.QueryDoseUnits}");
                    itr.QueryDose *= plan.PlanNormalizationValue / 100.0;
                    ProvideUIUpdate($"New query dose: {itr.QueryDose:0.0} {itr.QueryDoseUnits}");
                    if ((itr.QueryDoseUnits == Units.Percent && itr.QueryDose < 100.0) || (itr.QueryDoseUnits == Units.cGy && itr.QueryDose <= 2040.0))
                    {
                        ProvideUIUpdate("Warning cooler structure upper objective adjusted below prescription dose. Truncating to 102% of 20 Gy");
                        itr.QueryDose = 102;
                        if (itr.QueryDoseUnits == Units.cGy) itr.QueryDose *= 2000.0 / 100.0;
                    }
                }
            }
            return (wasKilled, updatedConstraints);
            #endregion
        }
    }
}
