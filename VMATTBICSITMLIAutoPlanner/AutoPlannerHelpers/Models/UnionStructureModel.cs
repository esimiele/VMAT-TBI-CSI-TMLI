
namespace AutoPlannerHelpers.Models
{
    public class UnionStructureModel
    {
        public string Structure_Left { get; set; } = null;
        public string Structure_Right { get; set; } = null;
        public string ProposedUnionStructureId { get; set; } = string.Empty;

        public UnionStructureModel() { }

        public UnionStructureModel(string structure_Left, string structure_Right, string proposedUnionStructureId)
        {
            Structure_Left = structure_Left;
            Structure_Right = structure_Right;
            ProposedUnionStructureId = proposedUnionStructureId;
        }
    }
}
