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
            TMLIMainView mv = new TMLIMainView { DataContext = new TMLIMainViewModel(e.Args) };
            mv.ShowDialog();
        }
    }
}
