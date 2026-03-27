using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.JustMock;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoopTests.Helpers
{
    internal static class TestBeamBuilder
    {
        public static List<Beam> GenerateTestBeamSetVMAT(int numLatIsos)
        {
            List<VVector> testIsoPositions = BuildLaterlTestIsocenterPositions(numLatIsos);
            List<Beam> beams = BuildBeamList(testIsoPositions, BuildTestCPCForBeam(testIsoPositions.Count, false, BuildJawPositions(numLatIsos), false));

            //test data, expected
            return beams;
        }

        private static List<Beam> BuildBeamList(List<VVector> testIsoPositions, List<List<ControlPointParameters>> cpp)
        {
            List<Beam> beams = new List<Beam> { };
            for (int i = 0; i < testIsoPositions.Count; i++)
            {
                Beam b = Mock.Create<Beam>();
                Mock.Arrange(() => b.Id).Returns($"{i}");
                Mock.Arrange(() => b.IsSetupField).Returns(false);
                Mock.Arrange(() => b.IsocenterPosition).Returns(testIsoPositions.ElementAt(i));
                BeamParameters p = Mock.Create<BeamParameters>();
                Mock.Arrange(() => p.ControlPoints).Returns(cpp.ElementAt(i));
                Mock.Arrange(() => b.GetEditableParameters()).Returns(p);
                beams.Add(b);
            }
            return beams;
        }

        public static List<List<ControlPointParameters>> BuildTestCPCForBeam(int numBeams, bool isAPPA, List<VRect<double>> jawPos, bool isSlidingWindow = true)
        {
            List<List<ControlPointParameters>> listlistcpp = new List<List<ControlPointParameters>> { };
            int mssCount = 1;
            for (int i = 0; i < numBeams; i++)
            {
                List<ControlPointParameters> listcpp = new List<ControlPointParameters>();
                for (int j = 0; j < (i + 1) * numBeams; j++)
                {
                    ControlPointParameters cpp = Mock.Create<ControlPointParameters>();
                    if (isAPPA)
                    {
                        if (isSlidingWindow)
                        {
                            //all control points are unique
                            Mock.Arrange(() => cpp.GantryAngle).Returns(i);
                            Mock.Arrange(() => cpp.CollimatorAngle).Returns(90.0);
                            Mock.Arrange(() => cpp.JawPositions).Returns(new VRect<double>(-150, -150, 150, 150));
                            Mock.Arrange(() => cpp.MetersetWeight).Returns(i * j + i + j);

                        }
                        else
                        {
                            //multiple static segments --> CP[1] = CP[2], CP[3] = CP[4], etc.
                            Mock.Arrange(() => cpp.GantryAngle).Returns(i);
                            Mock.Arrange(() => cpp.CollimatorAngle).Returns(90.0);
                            Mock.Arrange(() => cpp.JawPositions).Returns(new VRect<double>(-150, -150, 150, 150));
                            Mock.Arrange(() => cpp.MetersetWeight).Returns(mssCount);
                            if (j % 2 == 0) mssCount++;
                        }
                    }
                    else
                    {
                        Mock.Arrange(() => cpp.GantryAngle).Returns(j);
                        Mock.Arrange(() => cpp.CollimatorAngle).Returns(i * 2);
                        if(!jawPos.Any()) Mock.Arrange(() => cpp.JawPositions).Returns(new VRect<double>(-i, -j, i, j));
                        else Mock.Arrange(() => cpp.JawPositions).Returns(jawPos.ElementAt(i));
                        Mock.Arrange(() => cpp.MetersetWeight).Returns(i * j + i + j);
                        float[,] leafPositions = new float[2,60];
                        for (int iLeaf = 0; iLeaf < leafPositions.GetLength(1); iLeaf++)
                        {
                            if(iLeaf < 5 + i || iLeaf > 54 - i)
                            {
                                leafPositions[0, iLeaf] = -4.0f;
                                leafPositions[1, iLeaf] = -4.0f;
                            }
                            else
                            {
                                leafPositions[0, iLeaf] = -i*j + j;
                                leafPositions[1, iLeaf] = i*j + j;
                            }
                        }
                        Mock.Arrange(() => cpp.LeafPositions).Returns(leafPositions);
                    }
                    Mock.Arrange(() => cpp.PatientSupportAngle).Returns(0.0);
                    Mock.Arrange(() => cpp.Index).Returns(j);
                    listcpp.Add(cpp);
                }
                listlistcpp.Add(listcpp);
            }
            return listlistcpp;
        }

        public static List<VRect<double>> BuildJawPositions(int numLatIsos)
        {
            if (numLatIsos == 1)
            {
                return new List<VRect<double>>
                {
                    //head
                    new VRect<double>(-195.0,-195.0,20.0,195.0),
                    new VRect<double>(-20.0,-195.0,195.0,195.0),
                    //chest
                    new VRect<double>(-195.0,-195.0,20.0,195.0),
                    new VRect<double>(-20.0,-195.0,195.0,195.0),
                    new VRect<double>(-195.0,-195.0,20.0,195.0),
                    //abdomen
                    new VRect<double>(-195.0,-195.0,20.0,195.0),
                    new VRect<double>(-20.0,-195.0,195.0,195.0),
                    new VRect<double>(-195.0,-195.0,20.0,195.0),
                    //pelvis
                    new VRect<double>(-195.0,-195.0,20.0,195.0),
                    new VRect<double>(-20.0,-195.0,195.0,195.0),
                };
            }
            else return new List<VRect<double>> { };
        }

        private static List<VVector> BuildLaterlTestIsocenterPositions(int numLatIsos)
        {
            if (numLatIsos == 1)
            {
                return new List<VVector>
                {
					//head
					new VVector(0,0, -10),
                    new VVector(0,0, -10),
					//Mid chest
					new VVector(0,0, -15),
                    new VVector(0,0, -15),
                    new VVector(0,0, -15),
					//Mid abdomen
					new VVector(0,0, -25),
                    new VVector(0,0, -25),
                    new VVector(0,0, -25),
					//Pelvis
					new VVector(0,0, -35),
                    new VVector(0,0, -35),
                };
            }
            else if (numLatIsos == 2)
            {
                return new List<VVector>
                {
					//head
					new VVector(0,0, -10),
                    new VVector(0,0, -10),
					//L chest
					new VVector(5,0, -15),
                    new VVector(5,0, -15),
                    new VVector(5,0, -15),
					//R chest
					new VVector(-5,0, -15),
                    new VVector(-5,0, -15),
                    new VVector(-5,0, -15),
					//L Abdomen
					new VVector(5,0, -25),
                    new VVector(5,0, -25),
                    new VVector(5,0, -25),
					//R abdomen
					new VVector(-5,0, -25),
                    new VVector(-5,0, -25),
                    new VVector(-5,0, -25),
					//Pelvis
					new VVector(0,0, -35),
                    new VVector(0,0, -35),
                };
            }
            else if (numLatIsos == 3)
            {
                return new List<VVector>
                {
					//head
					new VVector(0,0, -10),
                    new VVector(0,0, -10),
					//L chest
					new VVector(5,0, -15),
                    new VVector(5,0, -15),
                    new VVector(5,0, -15),
					//Mid chest
					new VVector(0,0, -15),
                    new VVector(0,0, -15),
                    new VVector(0,0, -15),
					//R chest
					new VVector(-5,0, -15),
                    new VVector(-5,0, -15),
                    new VVector(-5,0, -15),
					//L Abdomen
					new VVector(5,0, -25),
                    new VVector(5,0, -25),
                    new VVector(5,0, -25),
					//Mid abdomen
					new VVector(0,0, -25),
                    new VVector(0,0, -25),
                    new VVector(0,0, -25),
					//R abdomen
					new VVector(-5,0, -25),
                    new VVector(-5,0, -25),
                    new VVector(-5,0, -25),
					//Pelvis
					new VVector(0,0, -35),
                    new VVector(0,0, -35),
                };
            }
            else return new List<VVector> { };
        }
    }
}
