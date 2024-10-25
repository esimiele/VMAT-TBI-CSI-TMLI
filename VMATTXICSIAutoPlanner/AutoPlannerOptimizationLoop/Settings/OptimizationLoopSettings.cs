using AutoPlannerHelpers.Models;
using System.Collections.Generic;

namespace AutoPlannerOptimizationLoop.Settings
{
    internal static class OptimizationLoopSettings
    {
        internal static List<string> PlanUIDs { get; set; } = new List<string> { };
        internal static bool PlanPreparationLogFileLoaded { get; set; } = false;
        internal static List<string> Reminders { get; set; } = new List<string> { };
        internal static string PlanPreparationTemplateUsed { get; set; } = string.Empty;
        internal static List<PrescriptionModel> PlanPreparationPrescriptions { get; set; } = new List<PrescriptionModel> { };
        internal static Dictionary<string, string> PlanPreparationTsTargets { get; set; } = new Dictionary<string, string> { };
        internal static Dictionary<string, string> PlanPreparationNormalizationVolumes { get; set; } = new Dictionary<string, string> { };
        internal static List<PlanOptimizationSetupModel> PlanPreparationOptimizationSetup { get; set; } = new List<PlanOptimizationSetupModel> { };
    }
}
