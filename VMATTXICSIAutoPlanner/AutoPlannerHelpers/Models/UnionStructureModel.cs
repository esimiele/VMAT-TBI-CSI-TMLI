using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Models
{
    public class UnionStructureModel
    {
        public Structure Structure_Left { get; set; } = null;
        public Structure Structure_Right { get; set; } = null;
        public string ProposedUnionStructureId { get; set; } = string.Empty;

        public UnionStructureModel() { }

        public UnionStructureModel(Structure structure_Left, Structure structure_Right, string proposedUnionStructureId)
        {
            Structure_Left = structure_Left;
            Structure_Right = structure_Right;
            ProposedUnionStructureId = proposedUnionStructureId;
        }
    }
}
