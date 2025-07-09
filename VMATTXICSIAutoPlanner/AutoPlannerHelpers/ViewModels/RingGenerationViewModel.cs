using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpers.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class RingGenerationViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<TSRingStructureModel> RequestedRingStructures { get; set; }
        public ObservableCollectionPropertyNotify<string> StructureIdsPostUnion { get; set; }

        #region properties
        #endregion

        #region commands
        public ICommand AddRingCommand { get; set; }
        public ICommand AddDefaultRingsCommand { get; set; }
        public ICommand ClearRingListCommand { get; set; }
        public RelayCommand<TSRingStructureModel> SpecifyAdditionalOperationCommand { get; set; }
        public RelayCommand<TSRingStructureModel> ClearRowCommand { get; set; }
        #endregion

        #region fields
        private AutoPlanTemplateBase _selectedTemplate;
        #endregion

        public RingGenerationViewModel(List<string> ids) 
        {
            StructureIdsPostUnion = new ObservableCollectionPropertyNotify<string> { };
            StructureIdsPostUnion.AddRange(ids);
            RequestedRingStructures = new ObservableCollectionPropertyNotify<TSRingStructureModel> { };
            AddRingCommand = new RelayCommand(AddRing);
            SpecifyAdditionalOperationCommand = new RelayCommand<TSRingStructureModel>(SpecifyAdditionalOperation);
            AddDefaultRingsCommand = new RelayCommand(AddDefaultRings);
            ClearRingListCommand = new RelayCommand(ClearRingList);
            ClearRowCommand = new RelayCommand<TSRingStructureModel>(ClearRow);
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestAutoPlanTemplateChangedMessage>(this, (r, m) =>
            {
                AutoPlanTemplateSelectionChanged(m.AutoPlanTemplate);
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                StructureIdsPostUnion.Clear();
                StructureIdsPostUnion.AddRange(m.StructureIds);
                StructureIdsPostUnion.Refresh();
            });
            WeakReferenceMessenger.Default.Register<RequestRingStructures>(this, (r, m) =>
            {
                m.Reply(this.RequestedRingStructures.ToList());
            });
        }

        public void AutoPlanTemplateSelectionChanged(AutoPlanTemplateBase template, bool skipStructureCheck = true)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            UpdateViewWithAutoPlanTemplateRings(skipStructureCheck);
        }

        private void UpdateViewWithAutoPlanTemplateRings(bool skipStructureIdCheck)
        {
            RequestedRingStructures.Clear();
            List<TSRingStructureModel> templateRings;
            if (_selectedTemplate.GetType() == typeof(CSIAutoPlanTemplate)) templateRings = (_selectedTemplate as CSIAutoPlanTemplate).Rings;
            else if (_selectedTemplate.GetType() == typeof(TMLIAutoPlanTemplate)) templateRings = (_selectedTemplate as TMLIAutoPlanTemplate).Rings;
            else return;
            foreach (TSRingStructureModel itr in templateRings)
            {
                if (skipStructureIdCheck)
                {
                    if (!StructureIdsPostUnion.Any(x => string.Equals(x, itr.TargetId, StringComparison.OrdinalIgnoreCase))) StructureIdsPostUnion.Add(itr.TargetId);
                    RequestedRingStructures.Add(itr);
                }
                else if (StructureIdsPostUnion.Any(x => string.Equals(x, itr.TargetId, StringComparison.OrdinalIgnoreCase)))
                {
                    //only add it they base structure exists in the structure set
                    RequestedRingStructures.Add(itr);
                }
            }
        }

        public void AddRing()
        {
            RequestedRingStructures.Add(new TSRingStructureModel(StructureIdsPostUnion.First(), 0.0, 0.0, 0.0));
        }

        public void AddDefaultRings()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            UpdateViewWithAutoPlanTemplateRings(false);
        }

        public void ClearRingList()
        {
            RequestedRingStructures.Clear();
        }

        public void ClearRow(TSRingStructureModel item)
        {
            RequestedRingStructures.Remove(item);
        }

        private void SpecifyAdditionalOperation(TSRingStructureModel model)
        {
            //open new view with current additional operation
            string ringName = $"TS_ring{model.DoseLevel}";
            if (StructureIdsPostUnion.Any(x => string.Equals(x, ringName)))
            {
                ringName += "_1";
                if (StructureIdsPostUnion.Any(x => string.Equals(x, ringName)))
                {
                    Logger.GetInstance().LogError($"Error! Unable to update ring structure Id to: {ringName}! Exiting");
                    return;
                }
            }
            AdditionalRingOperationView view = new AdditionalRingOperationView { DataContext = new AdditionalRingOperationViewModel(ringName, model.AdditionalStructureOperation, StructureIdsPostUnion.ToList()) };
            view.ShowDialog();
        }
    }
}
