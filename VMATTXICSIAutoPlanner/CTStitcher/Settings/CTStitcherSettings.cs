using CTStitcher.enums;
using System.Collections.Generic;

namespace CTStitcher.Settings
{
    public class CTStitcherSettings
    {
        public static StitchingAlgorithm DefaultStitchingAlgorithm { get; } = StitchingAlgorithm.TranslationOnly;

        public static IEnumerable<StitchingAlgorithm> StitchingAlgorithms { get; } = new List<StitchingAlgorithm>
        {
            StitchingAlgorithm.TranslationOnly,
            StitchingAlgorithm.TranslationAndRotations
        };

        public static IEnumerable<StitchingAlgorithm> AvailableStitchingAlgorithms { get; } = new List<StitchingAlgorithm> { StitchingAlgorithm.TranslationOnly };

        public static WriteFormat WriteFormat { get; } = WriteFormat.DICOM;

        public static int MatchSliceReviewBuffer { get; } = 30;

        //public static string DefaultWritePath { get; } = Assembly.GetExecutingAssembly().Location;

        public static string DefaultWritePath { get; } = @"\\ARIAIMGPRFIL1\va_data$\TBICTExports";
    }
}
