using AutoPlannerHelpers.Enums;

namespace AutoPlannerHelpers.Models
{
    public class ImportExportDataModel
    {
        //AE title, IP, port
        public DaemonModel AriaDBDaemon { get; set; } = new DaemonModel();
        public DaemonModel VMSFileDaemon { get; set; } = new DaemonModel();
        public DaemonModel LocalDaemon { get; set; } = new DaemonModel();
        public ImgExportFormat ExportFormat { get; set; } = ImgExportFormat.PNG;
        public string WriteLocation { get; set; } = string.Empty;
        public string ImportLocation { get; set; } = string.Empty;
    }
}
