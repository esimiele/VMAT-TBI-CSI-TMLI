using AutoPlannerHelpers.Models;
using System.Text;
using System.Windows;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;

namespace AutoPlannerHelpers.ViewModels
{
    public class SpecialOptimizationStructuresViewModel : ObservableObject
    {
        #region properties
        public ObservableCollectionPropertyNotify<SpecialOptimizationStructureModel> RequestedSpecialOptimizationStructures { get; set; }

        private List<SpecialOptimizationStructureModel> _defualtSpecialOptStructures = new List<SpecialOptimizationStructureModel> { };
        #endregion

        #region commands
        public ICommand DisplayInfoCommand { get; set; }
        public ICommand AddDefaultSpecialOptStructuresCommand { get; set; }
        public ICommand RemoveAllSpecialOptStructuresCommand { get; set; }
        public RelayCommand<SpecialOptimizationStructureModel> ClearRowCommand { get; set; }
        #endregion

        public SpecialOptimizationStructuresViewModel()
        {
            RequestedSpecialOptimizationStructures = new ObservableCollectionPropertyNotify<SpecialOptimizationStructureModel> { };
            DisplayInfoCommand = new RelayCommand(DisplayTSGenerationInfo);
            AddDefaultSpecialOptStructuresCommand = new RelayCommand(AddDefaultSpecialOptStructures);
            RemoveAllSpecialOptStructuresCommand = new RelayCommand(RemoveAllSpecialOptStructures);
            ClearRowCommand = new RelayCommand<SpecialOptimizationStructureModel>(ClearRow);
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestAutoPlanTemplateChangedMessage>(this, (r, m) =>
            {
                AutoPlanTemplateSelectionChanged(m.AutoPlanTemplate.SpecialOptimizationStructures);
            });
            WeakReferenceMessenger.Default.Register<RequestSpecialOptimizationStructures>(this, (r, m) =>
            {
                m.Reply(RequestedSpecialOptimizationStructures.ToList());
            });
        }

        private void DisplayTSGenerationInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("What's a Special Optimization Structure?");
            sb.AppendLine("Special optimization structures are structures that require special, non-standard rules to derive.");
            sb.AppendLine("E.g., ts_arms (lateral edges of the body expanded by a margin).");
            MessageBox.Show(sb.ToString());
        }

        public void AutoPlanTemplateSelectionChanged(IEnumerable<SpecialOptimizationStructureModel> templateSpecialOpStructures)
        {
            if (!templateSpecialOpStructures.Any()) return;
            _defualtSpecialOptStructures = new List<SpecialOptimizationStructureModel>(templateSpecialOpStructures);
            UpdateViewWithAutoPlanTemplateTSStructures(_defualtSpecialOptStructures);
        }

        public void AddDefaultSpecialOptStructures()
        {
            if (!_defualtSpecialOptStructures.Any()) return;
            UpdateViewWithAutoPlanTemplateTSStructures(_defualtSpecialOptStructures);
        }

        public void ClearRow(SpecialOptimizationStructureModel o)
        {
            RequestedSpecialOptimizationStructures.Remove(o);
        }

        public void UpdateViewWithAutoPlanTemplateTSStructures(List<SpecialOptimizationStructureModel> ops)
        {
            RequestedSpecialOptimizationStructures.Clear();
            foreach (SpecialOptimizationStructureModel itr in ops) RequestedSpecialOptimizationStructures.Add(itr);
        }

        private void RemoveAllSpecialOptStructures()
        {
            RequestedSpecialOptimizationStructures.Clear();
        }
    }
}
