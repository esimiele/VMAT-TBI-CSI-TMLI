using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
