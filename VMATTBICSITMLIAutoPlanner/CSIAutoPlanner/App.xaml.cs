using CSIAutoPlanner.ViewModels;
using CSIAutoPlanner.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CSIAutoPlanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CSIMainView mv = new CSIMainView { DataContext = new CSIMainViewModel(e.Args) };
            mv.ShowDialog();
        }
    }
}
