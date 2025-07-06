namespace AutoPlannerHelpers.Models
{
    public class TSRingStructureModel
    {
        public string TargetId { get; set; } = string.Empty;
        public string RingId { get; set; } = string.Empty;
        public double MarginFromTargetInCM { get; set; } = double.NaN;
        public double RingThicknessInCM { get; set; } = double.NaN;
        public double DoseLevel { get; set; } = double.NaN;
        public StructureOperationModel AdditionalStructureOperation { get; set; } = null;

        public TSRingStructureModel(string id, double margin, double thickness, double dose, string ringId = "")
        {
            TargetId = id;
            RingId = ringId;
            MarginFromTargetInCM = margin;
            RingThicknessInCM = thickness;
            DoseLevel = dose;
        }

        public TSRingStructureModel(TSRingStructureModel r)
        {
            TargetId = r.TargetId;
            RingId = r.RingId;
            MarginFromTargetInCM = r.MarginFromTargetInCM;
            RingThicknessInCM = r.RingThicknessInCM;
            DoseLevel = r.DoseLevel;
            AdditionalStructureOperation = r.AdditionalStructureOperation;
        }
    }
}
