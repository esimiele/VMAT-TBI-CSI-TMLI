using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Logging;
using AutoPlannerOptimizationLoop.ViewModels;
using AutoPlannerOptimizationLoop.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TBIPlanningAssistantHelpers.Helpers;

namespace AutoPlannerOptimizationLoop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string[] startupArgs = e.Args;
            //startupArgs = new string[] { "-m", "$TBI_test1", "-s", "1.2.246.352.71.4.929410884171.21.20250417221326", "-i", "1.3.46.670589.33.1.63844531006570350500002.5281130309244735734", "-p", "1.2.246.352.71.5.929410884171.21.20250408090703", "-c", "C1" };
            //startupArgs = new string[] { "-m","$TBI_test2", "-s", "1.2.246.352.71.4.929410884171.36.20250411123357", "-i", "1.3.46.670589.33.1.63871857683274500200002.4864773584640688701"};
            //startupArgs = new string[] { "-m","$TBI_test2", "-s", "1.2.246.352.71.4.929410884171.22.20250425095021", "-i", "1.3.46.670589.33.1.63871857683274500200002.4864773584640688701", "-p", "1.2.246.352.71.5.929410884171.22.20250408105332", "-c", "C1" };
            //startupArgs = new string[] { "-m", "$TBI_test3", "-s", "1.2.246.352.71.4.929410884171.37.20250411123816", "-i", "1.3.46.670589.33.1.63871776071379787200002.5208600913642991549" };
            //startupArgs = new string[] { "-m","$TBI_test3", "-s", "1.2.246.352.71.4.929410884171.23.20250425101753", "-i", "1.3.46.670589.33.1.63871776071379787200002.5208600913642991549", "-p", "1.2.246.352.71.5.929410884171.28.20250408120118", "-c", "C1" };
            ESAPIThreadContext.Initialize(Dispatcher);
            if (startupArgs.Any())
            {
                try
                {
                    if (EclipseContextHelper.GenerateEclipseContext(startupArgs.ToList())) return;
                }
                catch (Exception except)
                {
                    Logger.GetInstance().LogError($"Failed to initialize script because: {except.Message}");
                    Logger.GetInstance().LogError(except.StackTrace, true);
                    return;
                }
            }
            else
            {
                Logger.GetInstance().LogError("No startup arguments present!", true);
            }

            Thread t = new Thread(() =>
            {
                OptimizationLoopMainView mv = new OptimizationLoopMainView { DataContext = new OptimizationLoopMainViewModel(e.Args) };
                mv.ShowDialog();
                CloseApplication();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void CloseApplication()
        {
            ESAPIThreadContext.RunOnESAPIThread(() => { Application.Current.Shutdown(); });
        }
    }
}
