using System.Configuration;
using System.Data;
using System.Windows;
using AutoPlannerLauncher.ViewModels;
using AutoPlannerLauncher.Views;

namespace AutoPlannerLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LauncherMainView mv = new LauncherMainView { DataContext = new LauncherMainViewModel(e.Args) };
        }
    }

}
