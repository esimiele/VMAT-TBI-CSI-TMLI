using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.Models
{
    public class TargetDerivationModel
    {
        public string TargetId { get; set; } = string.Empty;
        public List<StructureOperationModel> Derivations { get; set; } = new List<StructureOperationModel> { };

        public TargetDerivationModel(string targetId, List<StructureOperationModel> derivations)
        {
            TargetId = targetId;
            Derivations = derivations;
        }
    }
}
