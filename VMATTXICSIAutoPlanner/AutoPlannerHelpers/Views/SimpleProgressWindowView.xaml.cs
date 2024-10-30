using AutoPlannerHelpers.Interfaces;
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
using System.Windows.Threading;

namespace AutoPlannerHelpers.Views
{
    /// <summary>
    /// Interaction logic for SimpleProgressWindow.xaml
    /// </summary>
    public partial class SimpleProgressWindowView : Window
    {
        public SimpleProgressWindowView()
        {
            InitializeComponent();
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
        }
        
    }
}
