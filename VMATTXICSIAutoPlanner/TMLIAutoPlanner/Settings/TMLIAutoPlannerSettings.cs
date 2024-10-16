using System.Collections.Generic;
using VMS.TPS.Common.Model.Types;

namespace TMLIAutoPlanner.Settings
{
    internal class TMLIAutoPlannerSettings
    {
        internal static bool CloseProgressWindowOnFinish { get; set; } = true;
        internal static string CourseId { get; set; } = "VMAT TMLI";
        internal static bool CheckTTCollision { get; set; } = true;
        internal static bool ContourFieldOverlap { get; set; } = true;
        internal static double ContourFieldOverlapMarginInCM { get; set; } = 1.0;
        internal static List<string> AvailableLinacs { get; set; } = new List<string>();
        internal static List<string> AvailableEnergies { get; set; } = new List<string>();
        internal static List<int> BeamsPerIsocenter { get; set; } = new List<int> { 4, 3, 3, 2, 2, 2, 2 };
        internal static List<double> CollimatorRotations { get; set; } = new List<double> { 3.0, 357.0, 90.0, 90.0 };
        internal static List<VRect<double>> JawPositions { get; set; } = new List<VRect<double>>
        {
            new VRect<double>(-20.0, -200.0, 200.0, 200.0),
            new VRect<double>(-200.0, -200.0, 20.0, 200.0),
            new VRect<double>(-200.0, -200.0, 0.0, 200.0),
            new VRect<double>(0.0, -200.0, 200.0, 200.0),
        };
        internal static string DoseCalculationAlgorithm { get; set; } = "AAA_15605";
        internal static bool UseGPUForDosecalculation { get; set; } = false;
        internal static string OptimizationAlorithm { get; set; } = "PO_15605";
        internal static bool UseGPUForOptimization { get; set; } = false;
        internal static string MRLevelRestart { get; set; } = "MR3";
        internal static bool ShowStitchCTTab { get; set; } = true;
    }
}
