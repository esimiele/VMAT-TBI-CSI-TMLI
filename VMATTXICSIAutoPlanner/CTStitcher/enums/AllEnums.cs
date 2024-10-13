namespace CTStitcher.enums
{
    /// <summary>
    /// CT scan orientation enum
    /// </summary>
    public enum ScanOrientation
    {
        HeadFirstSupine,
        FeetFirstSupine,
        Other
    };

    /// <summary>
    /// Available stitching algorithms as enum
    /// </summary>
    public enum StitchingAlgorithm
    {
        TranslationOnly,
        TranslationAndRotations,
        None
    };

    /// <summary>
    /// Available data write formats as enum
    /// </summary>
    public enum WriteFormat
    {
        DICOM,
        None
    };

    /// <summary>
    /// Enum to designate if the CT image of interest is the target or source image
    /// </summary>
    public enum RegistrationImageType
    {
        Target,
        Source,
        None
    };
}
