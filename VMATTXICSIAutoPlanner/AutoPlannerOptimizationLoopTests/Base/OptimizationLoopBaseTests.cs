using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Models;
using AutoPlannerOptimizationLoop.Core;
using VMS.TPS.Common.Model.API;
using Telerik.JustMock;
using VMS.TPS.Common.Model.Types;
using AutoPlannerOptimizationLoopTests.EqualityComparers;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AutoPlannerOptimizationLoop.Base.Tests
{
    [TestClass()]
    public class OptimizationLoopBaseTests
    {
        [TestMethod()]
        public void PreliminaryChecksSSAndImageTest()
        {
            StructureSet ss = Mock.Create<StructureSet>();
            Image img = Mock.Create<Image>();
            Series series = Mock.Create<Series>();
            Mock.Arrange(() => series.ImagingDeviceId).Returns("");
            Mock.Arrange(() => img.Series).Returns(series);
            Mock.Arrange(() => ss.Image).Returns(img);

            bool expected = true;
            VMATTBIOptimization opt = new VMATTBIOptimization(new DataContainers.OptDataContainer());
            bool result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { });
            Assert.AreEqual(expected, result);

            Mock.Arrange(() => series.ImagingDeviceId).Returns("dummy");
            Mock.Arrange(() => img.HasUserOrigin).Returns(false);
            result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { });
            Assert.AreEqual(expected, result);

            VVector origin = new VVector();
            Mock.Arrange(() => img.HasUserOrigin).Returns(true);
            Mock.Arrange(() => img.UserOrigin).Returns(origin);
            result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { });
            Assert.AreEqual(expected, result);

            Structure body = Mock.Create<Structure>();
            Mock.Arrange(() => body.Id).Returns("body");
            Mock.Arrange(() => body.IsEmpty).Returns(false);
            Mock.Arrange(() => body.IsPointInsideSegment(origin)).Returns(false);
            Mock.Arrange(() => ss.Structures).Returns(new List<Structure> { body });
            result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { });
            Assert.AreEqual(expected, result);

            Mock.Arrange(() => body.IsPointInsideSegment(origin)).Returns(true);
            result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { });
            Assert.AreEqual(expected, result);

            Structure target1 = Mock.Create<Structure>();
            Mock.Arrange(() => target1.Id).Returns("target1");
            Mock.Arrange(() => target1.IsEmpty).Returns(false);
            Structure target2 = Mock.Create<Structure>();
            Mock.Arrange(() => target2.Id).Returns("target2");
            Mock.Arrange(() => target2.IsEmpty).Returns(true);
            Mock.Arrange(() => ss.Structures).Returns(new List<Structure> { body, target1, target2 });
            result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { target1.Id, target2.Id });
            Assert.AreEqual(expected, result);

            Mock.Arrange(() => target2.IsEmpty).Returns(false);
            result = opt.PreliminaryChecksSSAndImage(ss, new List<string> { target1.Id, target2.Id });
            Assert.AreEqual(false, result);
        }

        [TestMethod()]
        public void RoundIsocenterPositionTest()
        {
            VVector test = new VVector(12.0, 37.0, 54.999);
            ExternalPlanSetup p = Mock.Create<ExternalPlanSetup>();
            Mock.Arrange(() => p.StructureSet.Image.DicomToUser(test, p)).Returns(test);
            VVector expected = new VVector(10, 40, 50);
            Mock.Arrange(() => p.StructureSet.Image.UserToDicom(new VVector(10, 40, 50), p)).Returns(expected);
            VMATTBIOptimization opt = new VMATTBIOptimization(new DataContainers.OptDataContainer());
            VVector result = opt.RoundIsocenterPosition(test, p);

            //this test will only pass if the vvector argument passed to usertodicom method exactly matches the expected result
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"Expected: {expected[i]} | result: {result[i]}");
                Assert.AreEqual(expected[i], result[i]);
            }
        }

        public OptimizationPointObjective GeneratePointObjective(string structureId, double dose, double volume, int priority)
        {
            OptimizationPointObjective pointObj = Mock.Create<OptimizationPointObjective>();
            Mock.Arrange(() => pointObj.StructureId).Returns(structureId);
            Mock.Arrange(() => pointObj.Dose).Returns(new DoseValue(dose, DoseValue.DoseUnit.cGy));
            Mock.Arrange(() => pointObj.Priority).Returns(priority);
            Mock.Arrange(() => pointObj.Volume).Returns(volume);
            Mock.Arrange(() => pointObj.Operator).Returns(OptimizationObjectiveOperator.Upper);
            return pointObj;
        }

        public OptimizationMeanDoseObjective GenerateMeanObjective(string structureId, double dose, int priority)
        {
            OptimizationMeanDoseObjective meanObj = Mock.Create<OptimizationMeanDoseObjective>();
            Mock.Arrange(() => meanObj.StructureId).Returns(structureId);
            Mock.Arrange(() => meanObj.Dose).Returns(new DoseValue(dose, DoseValue.DoseUnit.cGy));
            Mock.Arrange(() => meanObj.Priority).Returns(priority);
            return meanObj;
        }

        public List<OptimizationObjective> BuildDummyOptConstraints(int numConstraints)
        {
            List<OptimizationObjective> optimizationObjectives = new List<OptimizationObjective>
            {
                GeneratePointObjective("PTV^Upper", 200, 100, 100),
                GeneratePointObjective("PTV^Upper", 202, 0, 100),
                GeneratePointObjective("TS_jnx1", 200, 100, 100),
                GeneratePointObjective("TS_jnx1", 202, 0, 100),
            };

            for (int i = 0; i < numConstraints; i++)
            {
                optimizationObjectives.Add(GeneratePointObjective(i.ToString(), (i + 1) * 10, 100 - i, i * 10));
                optimizationObjectives.Add(GenerateMeanObjective(i.ToString(), (i + 1) * 10, i * 10));
            }

            return optimizationObjectives;
        }

        public List<OptimizationConstraintModel> GenerateExpectedOptConstraintModelList(int numConstraints)
        {
            List<OptimizationConstraintModel> constraints = new List<OptimizationConstraintModel>
            {
                new OptimizationConstraintModel("PTV^Upper", OptimizationObjectiveType.Upper, 200, Units.cGy, 100, 100),
                new OptimizationConstraintModel("PTV^Upper", OptimizationObjectiveType.Upper, 202, Units.cGy, 0, 100),
                new OptimizationConstraintModel("TS_jnx1", OptimizationObjectiveType.Upper, 200, Units.cGy, 100, 100),
                new OptimizationConstraintModel("TS_jnx1", OptimizationObjectiveType.Upper, 202, Units.cGy, 0, 100),
            };

            for (int i = 0; i < numConstraints; i++)
            {
                constraints.Add(new OptimizationConstraintModel(i.ToString(), OptimizationObjectiveType.Upper, (i + 1) * 10, Units.cGy, 100 - i, (int)Math.Ceiling((double)(i * 10 * 2) / 3)));
                constraints.Add(new OptimizationConstraintModel(i.ToString(), OptimizationObjectiveType.Mean, (i + 1) * 10, Units.cGy, 0, (int)Math.Ceiling((double)(i * 10 * 2) / 3)));
            }
            return constraints;
        }

        [TestMethod()]
        public void InitializeOptimizationConstriantsTest()
        {
            ExternalPlanSetup plan = Mock.Create<ExternalPlanSetup>();
            OptimizationSetup optimization = Mock.Create<OptimizationSetup>();
            Mock.Arrange(() => optimization.Objectives).Returns(BuildDummyOptConstraints(10));
            Mock.Arrange(() => plan.OptimizationSetup).Returns(optimization);

            List<OptimizationConstraintModel> expected = GenerateExpectedOptConstraintModelList(10);
            VMATTBIOptimization opt = new VMATTBIOptimization(new DataContainers.OptDataContainer());

            List<OptimizationConstraintModel> result = opt.InitializeOptimizationConstriants(plan);
            OptimizationConstraintComparer comparer = new OptimizationConstraintComparer();

            for (int i = 0; i < expected.Count; i++)
            {
                Console.WriteLine("Expected:");
                Console.WriteLine(comparer.Print(expected.ElementAt(i)));

                Console.WriteLine("Result:");
                Console.WriteLine(comparer.Print(result.ElementAt(i)));
                Assert.IsTrue(comparer.Equals(expected.ElementAt(i), result.ElementAt(i)));
                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine("-----------------------------------------------");
            }
        }

        [TestMethod()]
        public void NormalizePlanTest()
        {
            ExternalPlanSetup plan = Mock.Create<ExternalPlanSetup>();
            Mock.Arrange(() => plan.Id).Returns("dummy");
            Mock.Arrange(() => plan.IsDoseValid).Returns(true);
            DoseValue rxDose = new DoseValue(200.0, DoseValue.DoseUnit.cGy);
            Mock.Arrange(() => plan.TotalDose).Returns(rxDose);
            Structure target = Mock.Create<Structure>();
            Mock.Arrange(() => target.IsEmpty).Returns(false);
            Mock.Arrange(() => plan.GetVolumeAtDose(target, rxDose, VolumePresentation.Relative)).Returns(80.0);

            double targetCoverage = 90.0;
            Mock.Arrange(() => plan.GetDoseAtVolume(target, targetCoverage, VolumePresentation.Relative, DoseValuePresentation.Absolute)).Returns(new DoseValue(190, DoseValue.DoseUnit.cGy));

            double expected = 95.0;
            VMATTBIOptimization opt = new VMATTBIOptimization(new DataContainers.OptDataContainer());
            double result = opt.NormalizePlan(plan, target, 100.0, targetCoverage);

            Console.WriteLine($"Expected: {expected}");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual(expected, result);
        }

        public List<PlanObjectiveModel> GenerateDummyPlanObjectiveList(int numObj)
        {
            List<PlanObjectiveModel> obj = new List<PlanObjectiveModel>();
            for (int i = 0; i < numObj; i++)
            {
                obj.Add(new PlanObjectiveModel(i.ToString(), i % 2 == 0 ? OptimizationObjectiveType.Upper : OptimizationObjectiveType.Lower, (i + 1) * 10, Units.cGy, i % 2 == 0 ? 0 : 100));
            }
            return obj;
        }

        public ExternalPlanSetup GenerateDummyPlan(List<PlanObjectiveModel> obj)
        {
            ExternalPlanSetup p = Mock.Create<ExternalPlanSetup>();
            StructureSet ss = Mock.Create<StructureSet>();
            List<Structure> structures = new List<Structure>();
            int count = 0;
            foreach (PlanObjectiveModel itr in obj)
            {
                Structure s = Mock.Create<Structure>();
                Mock.Arrange(() => s.Id).Returns(itr.StructureId);
                Mock.Arrange(() => s.IsEmpty).Returns(false);
                Mock.Arrange(() => p.GetDoseAtVolume(s, itr.QueryVolume, VolumePresentation.Relative, DoseValuePresentation.Absolute)).Returns(new DoseValue(itr.QueryDose - count * 2, DoseValue.DoseUnit.cGy));
                structures.Add(s);
                count++;
            }
            Mock.Arrange(() => ss.Structures).Returns(structures);
            Mock.Arrange(() => p.StructureSet).Returns(ss);

            return p;
        }

        [TestMethod()]
        public void EvaluateResultVsOptimizationConstraintsTest()
        {
            List<PlanObjectiveModel> obj = GenerateDummyPlanObjectiveList(5);
            ExternalPlanSetup p = GenerateDummyPlan(obj);
        }
    }
}