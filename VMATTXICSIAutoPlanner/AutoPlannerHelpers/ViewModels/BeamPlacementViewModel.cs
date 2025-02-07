using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoPlannerHelpers.ViewModels 
{
    public class BeamPlacementViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<PlanIsocenterModel> PlanIsocenterList { get; set; }

        #region properties
        private bool _contourFieldOverlapChecked;
        private Visibility _contourOverlapMarginVisible;
        private Visibility _requestedNumberOfIsosVisible;
        private double _fieldOverlapMargin;
        private int _requestedNumberOfVMATIsos;
        private List<string> _availableLinacs;
        private string _selectedLinac;
        private List<string> _availableEnergies;
        private string _selectedEnergy;

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

        public double FieldOverlapMargin
        {
            get { return _fieldOverlapMargin; }
            set { SetProperty(ref _fieldOverlapMargin, value); }
        }

        public int RequestedNumberOfVMATIsos
        {
            get { return _requestedNumberOfVMATIsos; }
            set { SetProperty(ref _requestedNumberOfVMATIsos, value); }
        }

        public List<string> AvailableLinacs
        {
            get { return _availableLinacs; }
            set { SetProperty(ref _availableLinacs, value); }
        }

        public string SelectedLinac
        {
            get { return _selectedLinac; }
            set { SetProperty(ref _selectedLinac, value); }
        }

        public List<string> AvailableEnergies
        {
            get { return _availableEnergies; }
            set { SetProperty(ref _availableEnergies, value); }
        }

        public string SelectedEnergy
        {
            get { return _selectedEnergy; }
            set { SetProperty(ref _selectedEnergy, value); }
        }
        #endregion

        #region commands
        private ICommand _notifyMainVMExecuted;
        public ICommand UpdateNumberOfIsocentersCommand { get; set; }
        public ICommand CreatePlansAndPlaceBeamsCommand { get; set; }
        #endregion

        #region fields
        #endregion

        public BeamPlacementViewModel(ICommand NotifyMainVMExecuted, PlanType type)
        {
            _notifyMainVMExecuted = NotifyMainVMExecuted;
            ContourOverlapMarginVisible = Visibility.Hidden;
            if (type == PlanType.VMAT_CSI) RequestedNumberOfIsosVisible = Visibility.Collapsed;
            else RequestedNumberOfIsosVisible = Visibility.Visible;
            UpdateNumberOfIsocentersCommand = new RelayCommand(UpdateRequestedNumberOfVMATIsocenters);
            CreatePlansAndPlaceBeamsCommand = new RelayCommand(CreatePlansAndPlaceBeams);
            _requestedNumberOfVMATIsos = 0;
        }

        public void HideRequestedNumberOfIsos()
        {
            RequestedNumberOfIsosVisible = Visibility.Collapsed;
        }

        private void UpdateContourFieldOverlapChecked()
        {
            if (_contourFieldOverlapChecked) ContourOverlapMarginVisible = Visibility.Visible;
            else ContourOverlapMarginVisible = Visibility.Hidden;
        }

        public void PopulateBeamPlacementUI(List<PlanIsocenterModel> isos, List<string> linacs, List<string> energies)
        {
            _requestedNumberOfVMATIsos = isos.First().Isocenters.Count(x => x.BeamType == BeamType.VMAT);
            PlanIsocenterList = new ObservableCollectionPropertyNotify<PlanIsocenterModel> { };
            foreach(PlanIsocenterModel itr in isos) PlanIsocenterList.Add(itr);
            AvailableLinacs = new List<string>(linacs);
            AvailableEnergies = new List<string>(energies);
        }

        public void UpdateRequestedNumberOfVMATIsocenters() 
        { 
            if(_requestedNumberOfVMATIsos < 1 || _requestedNumberOfVMATIsos > 4)
            {
                Logger.GetInstance().LogError("Requested number of isocenters is not valid! Please fix and try again");
                return;
            }
            if(_requestedNumberOfVMATIsos != PlanIsocenterList.SelectMany(x => x.Isocenters).Count(x => x.BeamType == BeamType.VMAT))
            {
                //do something
                string planId = PlanIsocenterList.First().PlanId;
                int totalNumIsos = _requestedNumberOfVMATIsos + PlanIsocenterList.SelectMany(x => x.Isocenters).Count(x => x.BeamType == BeamType.APPA);
                PlanIsocenterList.Clear();
                PlanIsocenterList.Add(new PlanIsocenterModel(planId, IsoNameHelper.GetTBIVMATIsoNames(_requestedNumberOfVMATIsos, totalNumIsos)));
                if(totalNumIsos > _requestedNumberOfVMATIsos)
                {
                    PlanIsocenterList.Add(new PlanIsocenterModel("AP / PA upper legs", new IsocenterModel("AP / PA upper legs")));
                    if(totalNumIsos == _requestedNumberOfVMATIsos + 2)
                    {
                        PlanIsocenterList.Add(new PlanIsocenterModel("AP / PA lower legs", new IsocenterModel("AP / PA lower legs")));
                    }
                }
            }
        }

        public void CreatePlansAndPlaceBeams()
        {
            StringBuilder sb = new StringBuilder();
            foreach(PlanIsocenterModel itr in PlanIsocenterList)
            {
                sb.AppendLine($"Plan Id: {itr.PlanId}");
                foreach(IsocenterModel iso in itr.Isocenters)
                {
                    sb.AppendLine($"Isocenter {iso.IsocenterId}: {iso.NumberOfBeams}");
                }
            }
            MessageBox.Show( sb.ToString() );
            _notifyMainVMExecuted.Execute(null);
        }
    }
}
