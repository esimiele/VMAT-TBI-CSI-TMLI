using AutoPlannerHelpers.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TBIAutoPlanner.Core
{
    internal class tester : SimpleProgressWindowViewModel
    {
        internal tester()
        {
            SetCloseOnFinish(true, 500);
        }

        protected override bool Run()
        {
            UpdateUILabel("Counting");
            for (int i = 0; i < 100; i++)
            {
                ProvideUIUpdate(i);
                Thread.Sleep(50);
            }
            return false;
        }
    }
}
