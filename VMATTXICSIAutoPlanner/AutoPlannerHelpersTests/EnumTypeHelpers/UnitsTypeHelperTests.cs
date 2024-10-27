using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoPlannerHelpers.Enums;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerHelpers.EnumTypeHelpers.Tests
{
    [TestClass()]
    public class UnitsTypeHelperTests
    {
        [TestMethod()]
        public void GetUnitsTypeTest()
        {
            Assert.AreEqual(Units.cc, UnitsTypeHelper.GetUnitsType("cc"));
            Assert.AreEqual(Units.cGy, UnitsTypeHelper.GetUnitsType("cGy"));
            Assert.AreEqual(Units.Percent, UnitsTypeHelper.GetUnitsType("%"));
            Assert.AreEqual(Units.Percent, UnitsTypeHelper.GetUnitsType("relative"));
        }

        [TestMethod()]
        public void GetDoseUnitTest()
        {
            Assert.AreEqual(DoseValue.DoseUnit.Percent, UnitsTypeHelper.GetDoseUnit(Units.Percent));
            Assert.AreEqual(DoseValue.DoseUnit.cGy, UnitsTypeHelper.GetDoseUnit(Units.cGy));
            Assert.AreEqual(DoseValue.DoseUnit.Unknown, UnitsTypeHelper.GetDoseUnit(Units.cc));
        }
    }
}