namespace AutoPlannerHelpers.Models
{
    public class ExportCTModel
    {
        public string SeriesId { get; set; } = string.Empty;
        public string CTId { get; set; } = string.Empty;
        public int NumberOfSlices { get; set; } = -1;
        public string CreationDate { get; set; } = string.Empty;
        public bool SelectedForExport { get; set; } = false;

        public ExportCTModel(string sid, string ctid, int numSlices, string date, bool selected = false) 
        {
            SeriesId = sid;
            CTId = ctid;
            NumberOfSlices = numSlices;
            CreationDate = date;
            SelectedForExport = selected;
        }
    }
}
