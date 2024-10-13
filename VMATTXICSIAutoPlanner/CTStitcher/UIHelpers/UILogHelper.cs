using AutoPlannerHelpers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace CTStitcher.UIHelpers
{
    internal static class UILogHelper
    {
        /// <summary>
        /// Write the created Eclipse context to the UI log textblock. Write the values of all non-null objects
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tb"></param>
        internal static string FormatEclipseContextForUILog(EclipseContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Eclipse context:");
            sb.AppendLine($"    Application: {(context.HasValue(context.Application) ? "Initialized" : "")}");
            sb.AppendLine($"    User: {(context.HasValue(context.Application) ? context.Application.CurrentUser.Name : "")}");
            sb.AppendLine($"    Patient MRN: {(context.HasValue(context.Patient) ? context.Patient.Id : "")}");
            sb.AppendLine($"    Course: {(context.HasValue(context.Course) ? context.Course.Id : "")}");
            //sb.AppendLine($"    Plan: {(context.HasValue(context.Plan) ? context.Plan.Id : "")}");
            sb.AppendLine($"    Structure set: {(context.HasValue(context.StructureSet) ? context.StructureSet.Id : "")}");
            sb.AppendLine($"    Image FOR: {(context.HasValue(context.ImageFOR) ? context.ImageFOR : "")}");
            sb.AppendLine("    CT images:");
            if (context.HasValue(context.CTImages))
            {
                foreach (Image itr in context.CTImages) sb.AppendLine($"        {itr.Id}");
            }
            else sb.AppendLine("        None");
            sb.AppendLine("Registrations:");
            if (context.HasValue(context.Registrations))
            {
                foreach (Registration itr in context.Registrations) sb.AppendLine($"        {itr.Id}");
            }
            else sb.AppendLine("        None");
            return sb.ToString();
        }
    }
}
