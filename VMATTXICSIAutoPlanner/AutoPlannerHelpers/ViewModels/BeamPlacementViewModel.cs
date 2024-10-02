using System.Windows;
using AutoPlannerHelpers.Enums;
using Prism.Mvvm;

namespace AutoPlannerHelpers.ViewModels 
{
    public class BeamPlacementViewModel : BindableBase
    {
        #region properties
        private bool _contourFieldOverlapChecked;
        private Visibility _contourOverlapMarginVisible;
        private Visibility _requestedNumberOfIsosVisible;

        public bool ContourFieldOverlapChecked
        {
            get { return _contourFieldOverlapChecked; }
            set { SetProperty(ref _contourFieldOverlapChecked, value); UpdateContourFieldOverlapChecked(); }
        }

        public Visibility ContourOverlapMarginVisible
        {
            get { return _contourOverlapMarginVisible; }
            set { SetProperty(ref _contourOverlapMarginVisible, value); }
        }

        public Visibility RequestedNumberOfIsosVisible
        {
            get { return _requestedNumberOfIsosVisible; }
            set { SetProperty(ref _requestedNumberOfIsosVisible, value); }
        }
        #endregion

        public BeamPlacementViewModel(PlanType type) 
        {
            ContourOverlapMarginVisible = Visibility.Hidden;
            if (type == PlanType.VMAT_TBI) RequestedNumberOfIsosVisible = Visibility.Visible;
            else RequestedNumberOfIsosVisible = Visibility.Collapsed;
        }

        private void UpdateContourFieldOverlapChecked()
        {
            if (_contourFieldOverlapChecked) ContourOverlapMarginVisible = Visibility.Visible;
            else ContourOverlapMarginVisible = Visibility.Hidden;
        }
    }
}
