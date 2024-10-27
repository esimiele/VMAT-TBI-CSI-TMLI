using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoPlannerHelpers.Enums;

namespace AutoPlannerHelpers.EnumTypeHelpers.Tests
{
    [TestClass()]
    public class DVHMetricTypeHelperTests
    {
        [TestMethod()]
        public void GetDVHMetricTypeTest()
        {
            Assert.AreEqual(DVHMetric.Dmean, DVHMetricTypeHelper.GetDVHMetricType("Dmean"));
            Assert.AreEqual(DVHMetric.VolumeAtDose, DVHMetricTypeHelper.GetDVHMetricType("VolumeAtDose"));
            Assert.AreEqual(DVHMetric.DoseAtVolume, DVHMetricTypeHelper.GetDVHMetricType("DoseAtVolume"));
        }
    }
}