using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Models
{
    public class FieldJunctionModel
    {
        public double OverlapCenterPositionZ { get; set; } = double.NaN;
        public int NumberOfCTSlices { get; set; } = -1;
        public int StartSlice { get; set; } = -1;
        public string JunctionStructureId { get; set; } = string.Empty;

        public FieldJunctionModel(double center, int numSlice, int start)
        {
            OverlapCenterPositionZ = center;
            NumberOfCTSlices = numSlice;
            StartSlice = start;
        }
    }
}
