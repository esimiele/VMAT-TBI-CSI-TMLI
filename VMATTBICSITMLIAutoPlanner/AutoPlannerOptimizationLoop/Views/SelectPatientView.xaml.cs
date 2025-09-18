using AutoPlannerOptimizationLoop.ViewModels;
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

namespace AutoPlannerOptimizationLoop.Views
{
    /// <summary>
    /// Interaction logic for SelectPatientView.xaml
    /// </summary>
    public partial class SelectPatientView : Window
    {
        public SelectPatientView()
        {
            InitializeComponent();
            Loaded += ViewLoaded;
        }

        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SelectPatientViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
