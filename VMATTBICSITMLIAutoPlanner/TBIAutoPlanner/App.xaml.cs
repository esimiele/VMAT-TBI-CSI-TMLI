using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
            TBIMainView mv = new TBIMainView { DataContext = new TBIMainViewModel(e.Args) };
            mv.ShowDialog();
        }
    }
}
