using AutoPlannerOptimizationLoop.Base;
using AutoPlannerOptimizationLoop.DataContainers;
using System.Collections.Generic;
using System.Linq;
using System;
using VMS.TPS.Common.Model.API;

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
                if (PreliminaryChecksSSAndImage(_data.StructureSet, _data.Prescriptions.Select(x => x.TargetId))) return true;
                if (PreliminaryChecksPlans(_data.Plans)) return true;

                ProvideUIUpdate(String.Format(" Commencing optimization loop!"));
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
        #endregion
    }
}
