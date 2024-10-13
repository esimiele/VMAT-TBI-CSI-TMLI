using System.Windows;
using TMLIAutoPlanner.ViewModels;
using TMLIAutoPlanner.Views;

namespace TMLIAutoPlanner
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
