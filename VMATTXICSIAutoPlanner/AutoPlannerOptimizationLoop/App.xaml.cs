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
            //startupArgs = new string[] { "-m", "$TBIDryRun_1", "-s", "1.2.246.352.71.4.251621835082.759513.20250506104500", "-i", "1.2.246.352.221.5230223905471954425310822822047724447", "-p", "1.2.246.352.71.5.251621835082.1766061.20250506062818", "-c", "VMAT-TBI" };
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
