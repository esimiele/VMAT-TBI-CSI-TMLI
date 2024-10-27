
namespace CTStitcher.Utilities.Tests
{
    [TestClass()]
    public class CTVoxelRescalerTests
    {
        [TestMethod()]
        public void ConvertFromVoxelValueToHUTest()
        {
            double slope = 1;
            double intercept = -1024;
            short val = 100;

            short expected = -924;
            short result = CTVoxelRescaler.ConvertFromVoxelValueToHU(val, slope, intercept);
            Assert.AreEqual(expected, result, 0.001);
        }

        [TestMethod()]
        public void ConvertFromVoxelValueToHUTest1()
        {
            double slope = 1;
            double intercept = -1024;
            int val = 100;

            short expected = -924;
            short result = CTVoxelRescaler.ConvertFromVoxelValueToHU(val, slope, intercept);
            Assert.AreEqual(expected, result, 0.001);
        }

        [TestMethod()]
        public void ConvertFromHUToVoxelValueTest()
        {
            double slope = 1;
            double intercept = -1024;
            short val = 100;

            short expected = 1124;
            short result = CTVoxelRescaler.ConvertFromHUToVoxelValue(val, slope, intercept);
            Assert.AreEqual(expected, result, 0.001);
        }
    }
}