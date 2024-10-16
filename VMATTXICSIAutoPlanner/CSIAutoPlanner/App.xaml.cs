using System.Windows;
using CSIAutoPlanner.ViewModels;
using CSIAutoPlanner.Views;

namespace CSIAutoPlanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainView mv = new MainView { DataContext = new MainViewModel(e.Args) };
            mv.ShowDialog();
        }
    }
}
