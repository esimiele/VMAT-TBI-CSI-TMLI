using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using TBIAutoPlanner.Settings;

namespace TBIAutoPlanner.Core
{
    internal class GeneratePreliminaryTargets_TBI : GeneratePreliminaryTargetsBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargets_TBI(IEnumerable<StructureOperationModel> tgts) :
            base(tgts, TBIAutoPlannerSettings.CloseProgressWindowOnFinish)
        {
        }
    }
}
