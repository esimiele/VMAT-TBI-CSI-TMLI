namespace AutoPlannerHelpers.Models
{
    public class DaemonModel
    {
        public string AETitle { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = -1;

        public DaemonModel() { }

        public DaemonModel(string ae, string ip, int p)
        {
            AETitle = ae;
            IP = ip;
            Port = p;
        }
    }
}
