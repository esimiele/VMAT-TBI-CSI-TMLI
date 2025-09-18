namespace AutoPlannerHelpers.Models
{
    public class TSRingStructureModel
    {
        public string TargetId { get; set; } = string.Empty;
        public string RingId { get; set; } = string.Empty;
        public double MarginFromTargetInCM { get; set; } = double.NaN;
        public double RingThicknessInCM { get; set; } = double.NaN;
        public double DoseLevel { get; set; } = double.NaN;
        public StructureOperationModel AdditionalStructureOperation { get; set; } = new StructureOperationModel();

        public TSRingStructureModel(string id, double margin, double thickness, double dose)
        {
            TargetId = id;
            RingId = $"TS_ring{dose}";
            MarginFromTargetInCM = margin;
            RingThicknessInCM = thickness;
            DoseLevel = dose;
        }

        public TSRingStructureModel(string id, double margin, double thickness, double dose, StructureOperationModel op)
        {
            TargetId = id;
            RingId = $"TS_ring{dose}";
            MarginFromTargetInCM = margin;
            RingThicknessInCM = thickness;
            DoseLevel = dose;
            AdditionalStructureOperation = op;
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
