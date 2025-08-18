using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AutoPlannerOptimizationLoop.ViewModels;
using AutoPlannerOptimizationLoop.Helpers;

namespace AutoPlannerOptimizationLoop.Views
{
    /// <summary>
    /// Interaction logic for OptimizationLoopMainView.xaml
    /// </summary>
    public partial class OptimizationLoopMainView : Window
    {
        public OptimizationLoopMainView()
        {
            InitializeComponent();
            Loaded += ViewLoaded;
            Closed += ViewClosed;
        }

        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is OptimizationLoopMainViewModel vm)
            {
                ESAPIThreadContext.UIDispatcher.BeginInvoke(() => vm.Initialize());
            }
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            ESAPIThreadContext.UIDispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
        }
    }
}
