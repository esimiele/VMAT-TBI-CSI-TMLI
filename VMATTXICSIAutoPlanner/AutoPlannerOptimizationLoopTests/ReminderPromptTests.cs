using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace AutoPlannerOptimizationLoop.Prompts.Tests
{
    [TestClass()]
    public class ReminderPromptTests
    {
        [TestMethod()]
        public void ReminderPromptTest()
        {
            List<string> testReminders = new List<string>
            {
                "Avoid entry through _arms set in optimizer",
                "Upper legs plan set as base dose in optimizer",
            };

            ReminderPrompt rp = new ReminderPrompt(testReminders);
            rp.ShowDialog();

            Assert.AreEqual(rp.ConfirmAll, true);
        }
    }
}