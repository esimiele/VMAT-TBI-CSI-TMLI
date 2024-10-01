using System.Collections.Generic;
using System.Windows;
using TBIAutoPlanner.ViewModels;
using TBIAutoPlanner.Views;

namespace TBIAutoPlanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            List<string> theArguments = new List<string> { };
            for (int i = 0; i < e.Args.Length; i++) theArguments.Add(e.Args[i]);

            MainView mv = new MainView { DataContext = new MainViewModel(theArguments) };
            mv.ShowDialog();
        }
    }
}
