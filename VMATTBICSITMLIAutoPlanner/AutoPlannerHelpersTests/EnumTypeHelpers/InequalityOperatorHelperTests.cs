using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoPlannerHelpers.Enums;

namespace AutoPlannerHelpers.EnumTypeHelpers.Tests
{
    [TestClass()]
    public class InequalityOperatorHelperTests
    {
        [TestMethod()]
        public void GetInequalityOperatorTest()
        {
            Assert.AreEqual(InequalityOperator.LessThan, InequalityOperatorHelper.GetInequalityOperator(" <"));
            Assert.AreEqual(InequalityOperator.LessThanOrEqualTo, InequalityOperatorHelper.GetInequalityOperator("<="));
            Assert.AreEqual(InequalityOperator.GreaterThan, InequalityOperatorHelper.GetInequalityOperator(">"));
            Assert.AreEqual(InequalityOperator.GreaterThanOrEqualTo, InequalityOperatorHelper.GetInequalityOperator(">="));
            Assert.AreEqual(InequalityOperator.Equal, InequalityOperatorHelper.GetInequalityOperator("="));
            Assert.AreEqual(InequalityOperator.None, InequalityOperatorHelper.GetInequalityOperator("=>"));
        }
    }
}