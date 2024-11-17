using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System;
using Telerik.JustMock;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerOptimizationLoop.Helpers.Tests
{
    [TestClass()]
    public class PlanEvaluationHelperTests
    {
        [TestMethod()]
        public void GetDifferenceFromGoalTest()
        {
            Structure s = Mock.Create<Structure>();
            ExternalPlanSetup p = Mock.Create<ExternalPlanSetup>();
            DVHData data = Mock.Create<DVHData>();
            OptimizationConstraintModel optLower = new OptimizationConstraintModel("1", OptimizationObjectiveType.Lower, 200, Units.cGy, 100, 100);
            OptimizationConstraintModel optUpper = new OptimizationConstraintModel("1", OptimizationObjectiveType.Upper, 202, Units.cGy, 0, 100);
            OptimizationConstraintModel optMean = new OptimizationConstraintModel("1", OptimizationObjectiveType.Mean, 200, Units.cGy, 0, 100);

            Mock.Arrange(() => data.MeanDose).Returns(new VMS.TPS.Common.Model.Types.DoseValue(190, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.cGy));
            Mock.Arrange(() => p.GetDoseAtVolume(s, 100, VMS.TPS.Common.Model.Types.VolumePresentation.Relative, VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute)).Returns(new VMS.TPS.Common.Model.Types.DoseValue(185, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.cGy));
            Mock.Arrange(() => p.GetDoseAtVolume(s, 0, VMS.TPS.Common.Model.Types.VolumePresentation.Relative, VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute)).Returns(new VMS.TPS.Common.Model.Types.DoseValue(240, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.cGy));
            Mock.Arrange(() => p.GetDVHCumulativeData(s, VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute, VMS.TPS.Common.Model.Types.VolumePresentation.Relative, 0.1)).Returns(data);

            double expected = 15;
            double result = PlanEvaluationHelper.GetDifferenceFromGoal(p, optLower, s);

            Console.WriteLine($"Expected: {expected} | Result: {result}");
            Assert.IsTrue(CalculationHelper.AreEqual(expected, result));

            expected = 38;
            result = PlanEvaluationHelper.GetDifferenceFromGoal(p, optUpper, s);

            Console.WriteLine($"Expected: {expected} | Result: {result}");
            Assert.IsTrue(CalculationHelper.AreEqual(expected, result));

            //actual mean dose is < goal --> goal is met and difference is set to zero
            expected = 0;
            result = PlanEvaluationHelper.GetDifferenceFromGoal(p, optMean, s);

            Console.WriteLine($"Expected: {expected} | Result: {result}");
            Assert.IsTrue(CalculationHelper.AreEqual(expected, result));
        }
    }
}