using AutoPlannerHelpers.Models;
using System.Collections.Generic;

namespace AutoPlannerHelpers.PlanTemplateModels
{
    public class TBIAutoPlanTemplate : AutoPlanTemplateBase
    {
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
