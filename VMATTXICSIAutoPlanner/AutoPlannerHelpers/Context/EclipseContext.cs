using System.Collections.Generic;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Context
{
    public class EclipseContext
    {
        private static EclipseContext _instance;
        public static EclipseContext GetInstance()
        {
            if (!ReferenceEquals(_instance, null)) return _instance;
            else return _instance = new EclipseContext();
        }

        public bool IsInitialized { get => !ReferenceEquals(Application, null); }
        public Application Application { get; set; } = null;
        public Patient Patient { get; set; } = null;
        public Course Course { get; set; } = null;
        public List<ExternalPlanSetup> VMATPlans { get; set; } = new List<ExternalPlanSetup> { };
        public StructureSet StructureSet { get; set; } = null;
        public IEnumerable<Registration> Registrations { get; set; } = new List<Registration> { };
        public IEnumerable<Image> CTImages { get; set; } = new List<Image> { };
        public string ImageFOR { get; set; } = "";
        public string UserName { get; set; } = "";
        public string UserId { get; set; } = "";
    }
}
