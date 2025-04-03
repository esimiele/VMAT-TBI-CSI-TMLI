using AutoPlannerHelpers.Models;
using System.Collections.Generic;

namespace AutoPlannerHelpers.PlanTemplateModels
{
    public class TMLIAutoPlanTemplate : AutoPlanTemplateBase
    {
        #region Properties
        //this is only here for the display name data binding. All other references to the template name use the explicit get method
        public List<OptimizationConstraintModel> InitialOptimizationConstraints { get; set; } = new List<OptimizationConstraintModel> { };
        public List<TSRingStructureModel> Rings { get; set; } = new List<TSRingStructureModel>();
        public List<RequestedTSStructureModel> RequestedPreliminaryTargets { get; set; } = new List<RequestedTSStructureModel> { };
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public TMLIAutoPlanTemplate()
        {
        }

        /// <summary>
        /// Overloaded constructor taking an int as input
        /// </summary>
        /// <param name="count"></param>
        public TMLIAutoPlanTemplate(int count)
        {
            TemplateName = $"Template: {count}";
        }

        /// <summary>
        /// Overloaded constructor taking a string as input
        /// </summary>
        /// <param name="name"></param>
        public TMLIAutoPlanTemplate(string name)
        {
            TemplateName = name;
        }
    }
}
