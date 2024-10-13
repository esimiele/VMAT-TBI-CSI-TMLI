using CTStitcher.UIHelpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CTStitcher.Views
{
    /// <summary>
    /// Interaction logic for CTStitcherView.xaml
    /// </summary>
    public partial class CTStitcherView : UserControl
    {
        private ViewModels.CTStitcherViewModel _vm;
        public CTStitcherView()
        {
            InitializeComponent();
            _vm = new ViewModels.CTStitcherViewModel();
            DataContext = _vm;
        }


        /// <summary>
        /// Not an easy way to pass mousewheelargs to vm from view. Need this hybrid approach to avoid complicated logic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseWheelMove(object sender, MouseWheelEventArgs e)
        {
            Grid theGrid = sender as Grid;
            if (theGrid.Name.Contains("axial")) _vm.UpdateAxialImage(e.Delta, axialImage);
            if (theGrid.Name.Contains("coronal")) _vm.UpdateCoronalImage(e.Delta, coronalImage);
            if (theGrid.Name.Contains("sagittal")) _vm.UpdateSagittalImage(e.Delta, sagittalImage);
        }
    }
}
