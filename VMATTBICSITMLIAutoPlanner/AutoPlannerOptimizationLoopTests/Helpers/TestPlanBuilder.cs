using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Telerik.JustMock;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerOptimizationLoopTests.Helpers
{
    public static class TestPlanBuilder
    {
        public static StructureSet GenerateDummyStructureSet(double width)
        {
            StructureSet ss = Mock.Create<StructureSet>();
            Structure body = Mock.Create<Structure>();
            Mock.Arrange(() => body.Id).Returns("body");
            MeshGeometry3D mesh = new MeshGeometry3D();
            Mock.Arrange(() => mesh.Bounds).Returns(new Rect3D(-width/2,0,0,width,0,0));
            Mock.Arrange(() => body.MeshGeometry).Returns(mesh);
            Mock.Arrange(() => ss.Structures).Returns(new List<Structure> { body});
            return ss;
        }

        public static ExternalPlanSetup GenerateLatIsoTestPlanSet(int numLatIsos, double ptWidth)
        {
            List<Beam> beams = TestBeamBuilder.GenerateTestBeamSetVMAT(numLatIsos);
            ExternalPlanSetup plan = Mock.Create<ExternalPlanSetup>();
            Mock.Arrange(() => plan.Beams).Returns(beams);
            Mock.Arrange(() => plan.StructureSet).Returns(GenerateDummyStructureSet(ptWidth));
            Application app = Mock.Create<Application>();
            return plan;
        }
    }
}
