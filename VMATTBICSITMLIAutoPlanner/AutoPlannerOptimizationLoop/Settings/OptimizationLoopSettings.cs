using AutoPlannerHelpers.Models;
using System;
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
        public static List<Tuple<int, double>> MLCIndexesY1 { get; } = new List<Tuple<int, double>>
        {
            {Tuple.Create(0, -200.0)},
            {Tuple.Create(1, -190.0)},
            {Tuple.Create(2, -180.0)},
            {Tuple.Create(3, -170.0)},
            {Tuple.Create(4, -160.0)},
            {Tuple.Create(5, -150.0)},
            {Tuple.Create(6, -140.0)},
            {Tuple.Create(7, -130.0)},
            {Tuple.Create(8, -120.0)},
            {Tuple.Create(9, -110.0)},
            {Tuple.Create(10, -100.0)},
            {Tuple.Create(11, -95.0)},
            {Tuple.Create(12, -90.0)},
            {Tuple.Create(13, -85.0)},
            {Tuple.Create(14, -80.0)},
            {Tuple.Create(15, -75.0)},
            {Tuple.Create(16, -70.0)},
            {Tuple.Create(17, -65.0)},
            {Tuple.Create(18, -60.0)},
            {Tuple.Create(19, -55.0)},
            {Tuple.Create(20, -50.0)},
            {Tuple.Create(21, -45.0)},
            {Tuple.Create(22, -40.0)},
            {Tuple.Create(23, -35.0)},
            {Tuple.Create(24, -30.0)},
            {Tuple.Create(25, -25.0)},
            {Tuple.Create(26, -20.0)},
            {Tuple.Create(27, -15.0)},
            {Tuple.Create(28, -10.0)},
            {Tuple.Create(29, -5.0)},
        };

        public static List<Tuple<int, double>> MLCIndexesY2 { get; } = new List<Tuple<int, double>>
        {
            {Tuple.Create(30, 5.0)},
            {Tuple.Create(31, 10.0)},
            {Tuple.Create(32, 15.0)},
            {Tuple.Create(33, 20.0)},
            {Tuple.Create(34, 25.0)},
            {Tuple.Create(35, 30.0)},
            {Tuple.Create(36, 35.0)},
            {Tuple.Create(37, 40.0)},
            {Tuple.Create(38, 45.0)},
            {Tuple.Create(39, 50.0)},
            {Tuple.Create(40, 55.0)},
            {Tuple.Create(41, 60.0)},
            {Tuple.Create(42, 65.0)},
            {Tuple.Create(43, 70.0)},
            {Tuple.Create(44, 75.0)},
            {Tuple.Create(45, 80.0)},
            {Tuple.Create(46, 85.0)},
            {Tuple.Create(47, 90.0)},
            {Tuple.Create(48, 95.0)},
            {Tuple.Create(49, 100.0)},
            {Tuple.Create(50, 110.0)},
            {Tuple.Create(51, 120.0)},
            {Tuple.Create(52, 130.0)},
            {Tuple.Create(53, 140.0)},
            {Tuple.Create(54, 150.0)},
            {Tuple.Create(55, 160.0)},
            {Tuple.Create(56, 170.0)},
            {Tuple.Create(57, 180.0)},
            {Tuple.Create(58, 190.0)},
            {Tuple.Create(59, 200.0)},
        };
        internal static void ClearSettings()
        {
            PlanUIDs.Clear();
            PlanPreparationLogFileLoaded = false;
            PlanPreparationTemplateUsed = string.Empty;
            PlanPreparationPrescriptions = new List<PrescriptionModel> { };
            PlanPreparationTsTargets = new Dictionary<string, string> { };
            PlanPreparationNormalizationVolumes = new Dictionary<string, string> { };
            PlanPreparationOptimizationSetup = new List<PlanOptimizationSetupModel> { };
        }
    }
}
