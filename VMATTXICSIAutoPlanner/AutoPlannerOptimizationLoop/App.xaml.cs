using System.Configuration;
using System.Data;
using System.Windows;
using AutoPlannerOptimizationLoop.Views;
using AutoPlannerOptimizationLoop.ViewModels;

namespace AutoPlannerOptimizationLoop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            OptimizationLoopMainView mv = new OptimizationLoopMainView { DataContext = new OptimizationLoopMainViewModel(e.Args) };
            mv.ShowDialog();
        }
    }
}
