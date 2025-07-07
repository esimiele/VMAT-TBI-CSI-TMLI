using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

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
        public ICommand UpdateNumberOfIsocentersCommand { get; set; }
        public ICommand CreatePlansAndPlaceBeamsCommand { get; set; }
        #endregion

        #region fields
        private List<int> _fieldsPerIso = new List<int> { 0,0,0,0,0,0,0};
        #endregion

        public BeamPlacementViewModel(PlanType type)
        {
            ContourOverlapMarginVisible = Visibility.Hidden;
            if (type == PlanType.VMAT_CSI) RequestedNumberOfIsosVisible = Visibility.Collapsed;
            else RequestedNumberOfIsosVisible = Visibility.Visible;
            UpdateNumberOfIsocentersCommand = new RelayCommand(UpdateRequestedNumberOfVMATIsocenters);
            CreatePlansAndPlaceBeamsCommand = new RelayCommand(CreatePlansAndPlaceBeams);
            PlanIsocenterList = new ObservableCollectionPropertyNotify<PlanIsocenterModel> { };
            AvailableEnergies = new List<string> { };
            AvailableLinacs = new List<string> { };
            FieldOverlapMargin = 0.0;
            _requestedNumberOfVMATIsos = 0;
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdateBeamPlacementDefaultSettings>(this, (r, m) =>
            {
                UpdateDefaultViewSettings(m.Linacs, m.Energies, m.ContourOverlap, m.ContourOverlapMargin, m.FieldsPerIsocenter);
            });
            WeakReferenceMessenger.Default.Register<RequestHideNumberOfVMATIsocenters>(this, (r, m) =>
            {
                HideRequestedNumberOfIsos();
            });
            WeakReferenceMessenger.Default.Register<RequestUpdatePlanIsocenterList>(this, (r, m) =>
            {
                PopulateBeamPlacementUI(m.PlanIsocenterList);
            });
        }

        public void HideRequestedNumberOfIsos()
        {
            RequestedNumberOfIsosVisible = Visibility.Collapsed;
        }

        public void UpdateDefaultViewSettings(List<string> linacs, List<string> energies, bool contourOverlap, double overlapMargin, IEnumerable<int> fieldsIso)
        {
            AvailableEnergies.AddRange(energies);
            SelectedEnergy = energies.First();
            AvailableLinacs.AddRange(linacs);
            SelectedLinac = linacs.First();
            if (contourOverlap)
            {
                ContourFieldOverlapChecked = true;
                FieldOverlapMargin = overlapMargin;
            }
            _fieldsPerIso.Clear();
            _fieldsPerIso.AddRange(fieldsIso);
        }

        private void UpdateContourFieldOverlapChecked()
        {
            if (_contourFieldOverlapChecked) ContourOverlapMarginVisible = Visibility.Visible;
            else ContourOverlapMarginVisible = Visibility.Hidden;
        }

        public void PopulateBeamPlacementUI(List<PlanIsocenterModel> isos)
        {
            RequestedNumberOfVMATIsos = isos.First().Isocenters.Count(x => x.BeamType == BeamType.VMAT);
            PlanIsocenterList.Clear();
            foreach (PlanIsocenterModel itr in isos)
            {
                if(itr.Isocenters.Any(x => x.NumberOfBeams == -1))
                {
                    for (int i = 0; i < itr.Isocenters.Count(); i++)
                    {
                        if (itr.Isocenters.ElementAt(i).NumberOfBeams == -1) itr.Isocenters.ElementAt(i).NumberOfBeams = _fieldsPerIso.ElementAt(i);
                    }
                }
                PlanIsocenterList.Add(itr);
            }
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
                int numAPPAIsos = PlanIsocenterList.SelectMany(x => x.Isocenters).Count(x => x.BeamType == BeamType.APPA);
                PlanIsocenterList.Clear();
                List<IsocenterModel> newIsos = IsoNameHelper.GetTBIVMATIsoNames(_requestedNumberOfVMATIsos, totalNumIsos);
                for (int i = 0; i < newIsos.Count(); i++)
                {
                    newIsos.ElementAt(i).NumberOfBeams = _fieldsPerIso.ElementAt(i);
                }

                PlanIsocenterList.Add(new PlanIsocenterModel(planId, newIsos));
                if(numAPPAIsos > 0)
                {
                    if(numAPPAIsos == 1) PlanIsocenterList.Add(new PlanIsocenterModel($"_legs", new IsocenterModel($"legs", 2, BeamType.APPA)));
                    else
                    {
                        for (int i = 0; i < numAPPAIsos; i++)
                        {
                            PlanIsocenterList.Add(new PlanIsocenterModel($"{(i == 0 ? "_upper" : "_lower")} legs", new IsocenterModel($"{(i == 0 ? "upper" : "lower")} legs", 2, BeamType.APPA)));
                        }
                    }
                }
            }
        }

        public void CreatePlansAndPlaceBeams()
        {
            WeakReferenceMessenger.Default.Send(new RequestGenerateAndPlaceBeams(_selectedLinac, _selectedEnergy, _contourFieldOverlapChecked, _fieldOverlapMargin, PlanIsocenterList));
        }
    }
}
