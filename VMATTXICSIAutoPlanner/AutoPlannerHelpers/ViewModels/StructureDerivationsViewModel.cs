using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoPlannerHelpers.ViewModels
{
    public class StructureDerivationsViewModel : ObservableObject
    {
        #region properties
        public ObservableCollectionPropertyNotify<StructureOperationModel> RequestedStructureOperations { get; set; }
        public ObservableCollectionPropertyNotify<string> StructureIds { get; set; }

        private string _viewHeaderLabel;

        public string ViewHeaderLabel
        {
            get { return _viewHeaderLabel; }
            set { SetProperty(ref _viewHeaderLabel, value); }
        }
        #endregion

        #region fields
        private List<StructureOperationModel> _defaultOperationsFromTemplate = new List<StructureOperationModel>();
        private bool _isUsedForTargetDerivations = false;
        private bool _skipStructureIdCheck = true;
        #endregion

        #region commands
        public ICommand AddStructureOperationCommand { get; set; }
        public ICommand AddDefaultStructureOperationsCommand { get; set; }
        public ICommand RemoveAllStructureOperationsCommand { get; set; }
        public RelayCommand<(string,StructureMarginModel)> ModifyMarginCommand { get; set; }
        public ICommand PerformTSGenerationManipulationCommand { get; set; }
        public RelayCommand<StructureOperationModel> StructureSelectionChangedCommand { get; set; }
        public RelayCommand<StructureOperationModel> ClearRowCommand { get; set; }
        #endregion

        public StructureDerivationsViewModel(List<string> structureIds, bool isUsedForTargetDerivations = false) 
        {
            AddStructureOperationCommand = new RelayCommand(AddTSManipulation);
            AddDefaultStructureOperationsCommand = new RelayCommand(AddDefaultTSManipulations);
            PerformTSGenerationManipulationCommand = new RelayCommand(PerformTSGenerationManipulation);
            RemoveAllStructureOperationsCommand = new RelayCommand(RemoveAllTSManipulations);
            ModifyMarginCommand = new RelayCommand<(string, StructureMarginModel)>(ModifyMargin, CanModifyMargin);
            StructureSelectionChangedCommand = new RelayCommand<StructureOperationModel>(StructureSelectionChanged);
            ClearRowCommand = new RelayCommand<StructureOperationModel>(ClearRow);
            StructureIds = new ObservableCollectionPropertyNotify<string> { "--Add New--" };
            StructureIds.AddRange(structureIds);
            RequestedStructureOperations = new ObservableCollectionPropertyNotify<StructureOperationModel> { };
            _isUsedForTargetDerivations = isUsedForTargetDerivations;
            if (isUsedForTargetDerivations) ViewHeaderLabel = "Target Derivation Operations";
            else ViewHeaderLabel = "Optimization Structure Derivations";
            RequestedStructureOperations.CollectionChanged += RequestedStructureOperations_CollectionChanged;
            InitializeMessengers(isUsedForTargetDerivations);
        }

        private void InitializeMessengers(bool isUsedForTargetDerivations)
        {
            if(isUsedForTargetDerivations)
            {
                WeakReferenceMessenger.Default.Register<RequestUpdateTargetDerivationOperations>(this, (r, m) =>
                {
                    AutoPlanTemplateSelectionChanged(m.StructureOperations, m.SkipStructureIdCheck);
                });
                WeakReferenceMessenger.Default.Register<RequestTargetStructureDerivations>(this, (r, m) =>
                {
                    m.Reply(RequestedStructureOperations.ToList());
                });
            }
            else
            {
                WeakReferenceMessenger.Default.Register<RequestUpdateOptimizationStructureDerivations>(this, (r, m) =>
                {
                    AutoPlanTemplateSelectionChanged(m.StructureOperations, m.SkipStructureIdCheck);
                });
            }
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                StructureIds.Clear();
                StructureIds.AddRange(m.StructureIds);
                StructureIds.Refresh();
            });
        }

        public void AutoPlanTemplateSelectionChanged(List<StructureOperationModel> operations, bool skipStructureIdCheck)
        {
            _defaultOperationsFromTemplate = operations;
            _skipStructureIdCheck = skipStructureIdCheck;
            UpdateViewWithAutoPlanTemplateStructureDerivations();
        }

        private void UpdateViewWithAutoPlanTemplateStructureDerivations()
        {
            RequestedStructureOperations.Clear();
            foreach (StructureOperationModel itr in _defaultOperationsFromTemplate)
            {
                if (_skipStructureIdCheck) RequestedStructureOperations.Add(itr);
                else if (StructureIds.Any(x => string.Equals(x, itr.StructureA, StringComparison.OrdinalIgnoreCase)) && StructureIds.Any(x => string.Equals(x, itr.StructureB, StringComparison.OrdinalIgnoreCase)))
                {
                    //only add it they base structure exists in the structure set
                    string structureAId = StructureIds.First(x => string.Equals(x, itr.StructureA, StringComparison.OrdinalIgnoreCase));
                    string structureBId = StructureIds.First(x => string.Equals(x, itr.StructureB, StringComparison.OrdinalIgnoreCase));
                    if (!StructureIds.Any(x => string.Equals(x, itr.OutputStructure, StringComparison.OrdinalIgnoreCase))) StructureIds.Add(itr.OutputStructure);
                    StructureOperationModel newItem = new StructureOperationModel(structureAId, itr.Operation, structureBId, itr.OutputStructure, itr.MarginA, itr.MarginB, itr.IsTemporary);
                    newItem.PropertyChanged += RequestedStructureOperation_PropertyChanged;
                    RequestedStructureOperations.Add(newItem);
                }
            }
        }

        private void RequestedStructureOperations_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RequestedReEvaluationOfCanExecute();
        }

        public void RequestedReEvaluationOfCanExecute()
        {
            Application.Current.Dispatcher.BeginInvoke(() => { ModifyMarginCommand.NotifyCanExecuteChanged(); }, DispatcherPriority.Render);
        }

        private void RequestedStructureOperation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(StructureOperationModel.StructureA) or nameof(StructureOperationModel.MarginA))
            {
                RequestedReEvaluationOfCanExecute();
            }
        }

        private void AddTSManipulation()
        {
            StructureOperationModel newItem = new StructureOperationModel(StructureIds.ElementAt(1), Enums.StructureDerivationOperation.None, StructureIds.ElementAt(1), StructureIds.ElementAt(1));
            newItem.PropertyChanged += RequestedStructureOperation_PropertyChanged;
            RequestedStructureOperations.Add(newItem);
        }

        private void AddDefaultTSManipulations()
        {
            UpdateViewWithAutoPlanTemplateStructureDerivations();
        }

        private void RemoveAllTSManipulations()
        {
            RequestedStructureOperations.Clear();
        }

        private void StructureSelectionChanged(StructureOperationModel item)
        {
            if (item.StructureIdList.Any(x => string.Equals(x, "--Add New--")))
            {
                string msg = "Enter the Id of the requested structure!";
                EnterMissingInfoPrompt EMIP = new EnterMissingInfoPrompt(msg, "Id:");
                EMIP.ShowDialog();
                if (EMIP.GetSelection && !StructureIds.Contains(EMIP.EnteredValue))
                {
                    StructureIds.Add(EMIP.EnteredValue);
                    if (string.Equals(item.StructureA, "--Add New--")) item.StructureA = StructureIds.Last();
                    else if (string.Equals(item.StructureB, "--Add New--")) item.StructureB = StructureIds.Last();
                    else item.OutputStructure = StructureIds.Last();
                    StructureIds.Refresh();
                    RequestedStructureOperations.Refresh();
                }
            }
        }

        private void ModifyMargin((string structureid, StructureMarginModel model) parameters)
        {
            ModifyMarginView view = new ModifyMarginView { DataContext = new ModifyMarginViewModel(parameters.structureid, parameters.model) };
            view.ShowDialog();
        }

        private bool CanModifyMargin((string structureid, StructureMarginModel model) parameters)
        {
            return !string.IsNullOrEmpty(parameters.structureid) && !ReferenceEquals(parameters.model, null);
        }

        private void ClearRow(StructureOperationModel item)
        {
            RequestedStructureOperations.Remove(item);
        }

        public void PerformTSGenerationManipulation()
        {
            if(_isUsedForTargetDerivations)
            {
                WeakReferenceMessenger.Default.Send(new RequestPerformTargetDerivations(RequestedStructureOperations));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new RequestPerformOptimizationStructureDerivations(RequestedStructureOperations));
            }
        }
    }
}
