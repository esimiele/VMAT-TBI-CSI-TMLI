using AutoPlannerHelpers.Models;
using System.Collections.Generic;

namespace AutoPlannerHelpers.PlanTemplateModels
{
    public class TBIAutoPlanTemplate : AutoPlanTemplateBase
    {
        #region Properties
        //this is only here for the display name data binding. All other references to the template name use the explicit get method
        public List<OptimizationConstraintModel> InitialOptimizationConstraints { get; set; } = new List<OptimizationConstraintModel> { };
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public TBIAutoPlanTemplate()
        {
        }

        /// <summary>
        /// Overloaded constructor taking an int as input
        /// </summary>
        /// <param name="count"></param>
        public TBIAutoPlanTemplate(int count)
        {
            TemplateName = $"Template: {count}";
        }

        /// <summary>
        /// Overloaded constructor taking a string as input
        /// </summary>
        /// <param name="name"></param>
        public TBIAutoPlanTemplate(string name)
        {
            TemplateName = name;
        }
    }
}
