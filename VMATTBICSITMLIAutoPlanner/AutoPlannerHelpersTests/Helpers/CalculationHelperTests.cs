using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoPlannerHelpers.Helpers.Tests
{
    [TestClass()]
    public class CalculationHelperTests
    {
        [TestMethod()]
        public void AreEqualTest()
        {
            bool expected = false;
            double x = 10;
            double y = 10.1;
            double tolerance = 0.001;
            Assert.AreEqual(expected, CalculationHelper.AreEqual(x, y, tolerance));

            expected = true;
            x = 10;
            y = 10.001;
            tolerance = 0.001;
            Assert.AreEqual(expected, CalculationHelper.AreEqual(x, y, tolerance));
        }

        [TestMethod()]
        public void ComputeAverageTest()
        {
            double expected = 14.0;
            double x = 10;
            double y = 20;
            Assert.AreNotEqual(expected, CalculationHelper.ComputeAverage(x, y));

            expected = 15.0;
            x = 10;
            y = 20;
            Assert.AreEqual(expected, CalculationHelper.ComputeAverage(x, y));
        }

        [TestMethod()]
        public void ComputeSliceTest()
        {
            int expected = 50;
            double z = 100;
            Assert.AreNotEqual(expected, CalculationHelper.ComputeSlice(z, 2.0, 2.0));
            Assert.AreEqual(expected, CalculationHelper.ComputeSlice(z, 0.0, 2.0));
        }
    }
}