using AutoPlannerOptimizationLoop.Helpers;
using AutoPlannerOptimizationLoopTests.EqualityComparers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoopTests.Helpers
{
    [TestClass()]
    public class BeamHelperTests
    {
        [TestMethod()]
        public void GetY1IndexTest()
        {
            double jawPos = -195.0;
            int result = BeamHelper.GetY1Index(jawPos);
            Assert.AreEqual(1, result);

            result = BeamHelper.GetY1Index(-147.0);
            Assert.AreEqual(6, result);

            result = BeamHelper.GetY1Index(20.0);
            Assert.AreEqual(29, result);
        }

        [TestMethod()]
        public void GetY2IndexTest()
        {
            double jawPos = 195.0;
            int result = BeamHelper.GetY2Index(jawPos);
            Assert.AreEqual(58, result);

            result = BeamHelper.GetY2Index(147.0);
            Assert.AreEqual(53, result);

            result = BeamHelper.GetY2Index(-20.0);
            Assert.AreEqual(30, result);
        }

        [TestMethod()]
        public void ConvertY1IndexToPositionTest()
        {
            int index = 9;
            double result = BeamHelper.ConvertY1IndexToPosition(index);
            Assert.AreEqual(-110.0, result);

            result = BeamHelper.ConvertY1IndexToPosition(31);
            Assert.AreEqual(0.0, result);

            result = BeamHelper.ConvertY1IndexToPosition(16);
            Assert.AreEqual(-70.0, result);
        }

        [TestMethod()]
        public void ConvertY2IndexToPositionTest()
        {
            int index = 39;
            double result = BeamHelper.ConvertY2IndexToPosition(index);
            Assert.AreEqual(50.0, result);

            result = BeamHelper.ConvertY2IndexToPosition(29);
            Assert.AreEqual(0.0, result);

            result = BeamHelper.ConvertY2IndexToPosition(56);
            Assert.AreEqual(170.0, result);
        }

        [TestMethod()]
        public void GetStaticMLCsInFieldTest()
        {
            ExternalPlanSetup p = TestPlanBuilder.GenerateLatIsoTestPlanSet(1, 500);
            List<Beam> beams = TestBeamBuilder.GenerateTestBeamSetVMAT(1);

            List<Tuple<Beam, List<int>>> expected = new List<Tuple<Beam, List<int>>> { };
            for (int i = 0; i < beams.Count; i++)
            {
                List<int> closedMLCs = new List<int> { };
                for (int j = 1; j < 5 + i; j++)
                {
                    closedMLCs.Add(j);
                    closedMLCs.Add(59 - j);
                }
                expected.Add(Tuple.Create(beams.ElementAt(i), closedMLCs.OrderBy(x => x).ToList()));
            }

            List<Tuple<Beam, List<int>>> result = BeamHelper.GetStaticMLCsInField(p);

            Assert.AreEqual(expected.Count(), result.Count());
            for (int i = 0; i < expected.Count; i++)
            {
                Console.WriteLine($"Beam IDs: {expected.ElementAt(i).Item1.Id} | {result.ElementAt(i).Item1.Id}");
                Assert.AreEqual(expected.ElementAt(i).Item2.Count(), result.ElementAt(i).Item2.Count());
                for (int j = 0; j < expected.ElementAt(i).Item2.Count(); j++)
                {
                    Console.WriteLine($"MLC indexes: {expected.ElementAt(i).Item2.ElementAt(j)} | {result.ElementAt(i).Item2.ElementAt(j)}");
                }
            }
        }

        [TestMethod()]
        public void ConvertMLCIndexesToJawPosTest()
        {
            ExternalPlanSetup p = TestPlanBuilder.GenerateLatIsoTestPlanSet(1, 500);
            List<Beam> beams = TestBeamBuilder.GenerateTestBeamSetVMAT(1);

            List<Tuple<Beam, VRect<double>>> expected = new List<Tuple<Beam, VRect<double>>>
            {
                //head
                Tuple.Create(beams[0], new VRect<double>(-195.0,-150.0,20.0,150.0)),
                Tuple.Create(beams[1], new VRect<double>(-20.0,-140.0,195.0,140.0)),
                //chest
                Tuple.Create(beams[2], new VRect<double>(-195.0,-130.0,20.0,130.0)),
                Tuple.Create(beams[3], new VRect<double>(-20.0,-120.0,195.0,120.0)),
                Tuple.Create(beams[4], new VRect<double>(-195.0,-110.0,20.0,110.0)),
                //abdomen
                Tuple.Create(beams[5], new VRect<double>(-195.0,-100.0,20.0,100.0)),
                Tuple.Create(beams[6], new VRect<double>(-20.0,-95.0,195.0,95.0)),
                Tuple.Create(beams[7], new VRect<double>(-195.0,-90.0,20.0,90.0)),
                //pelvis
                Tuple.Create(beams[8], new VRect<double>(-195.0,-85.0,20.0,85.0)),
                Tuple.Create(beams[9], new VRect<double>(-20.0,-80.0,195.0,80.0)),
            };

            List<Tuple<Beam, VRect<double>>> result = BeamHelper.ConvertMLCIndexesToJawPos(BeamHelper.GetStaticMLCsInField(p));
            JawPositionComparer jpc = new JawPositionComparer();

            Assert.AreEqual(expected.Count(), result.Count());
            for (int i = 0; i < expected.Count(); i++)
            {
                Console.WriteLine($"Beam id: {expected.ElementAt(i).Item1.Id} | {result.ElementAt(i).Item1.Id}");
                Console.WriteLine($"{jpc.Print(expected.ElementAt(i).Item2)} | {jpc.Print(result.ElementAt(i).Item2)}");
                Assert.IsTrue(jpc.Equals(expected[i].Item2, result[i].Item2));
            }
        }
    }
}
