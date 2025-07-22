using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using CSIAutoPlanner.Settings;
using System.Collections.Generic;
using AutoPlannerHelpers.BaseCore;

namespace CSIAutoPlanner.Core
{
    internal class GeneratePreliminaryTargets_CSI : GeneratePreliminaryTargetsBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargets_CSI(IEnumerable<StructureOperationModel> tgts) :
            base(tgts, CSIAutoPlannerSettings.CloseProgressWindowOnFinish)
        { }
    }
}
