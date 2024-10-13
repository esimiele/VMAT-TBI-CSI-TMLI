using CTStitcher.enums;

namespace CTStitcher.Models
{
    public class CTImageMetaDataModel
    {
        public ScanOrientation ScanOrientation { get; set; } = ScanOrientation.HeadFirstSupine;
        public VectorModel ImageOrientation { get; set; } = new VectorModel();
        public int XSize { get; set; } = 0;
        public int YSize { get; set; } = 0;
        public int ZSize { get; set; } = 0;
        public double XRes { get; set; } = 0.0;
        public double YRes { get; set; } = 0.0;
        public double ZRes { get; set; } = 0.0;
        public double RescaleIntercept { get; set; } = 0.0;
        public double RescaleSlope { get; set; } = 0.0;
        public string Id { get; set; } = "";
        public string FOR { get; set; } = "";
        public string StudyUID { get; set; } = "";
        public string SeriesUID { get; set; } = "";
        public string ImagingDeviceSerialNumber { get; set; } = "";
        public string ImagingDeviceManufacturer { get; set; } = "";
        public string ImagingDeviceModel { get; set; } = "";
    }
}
