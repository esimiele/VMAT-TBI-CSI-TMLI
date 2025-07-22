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
            sb.AppendLine("What's the difference between TS structure generation vs manipulation?");
            sb.AppendLine("TS structure generation involves adding structures to the structure set to shape the dose distribution. These include rings, preliminary targets, etc. E.g.,");
            sb.AppendLine("TS_ring900  -->  ring structure around the targets using a nominal dose level of 900 cGy to determine fall-off");
            sb.AppendLine("PTV_Spine  -->  preliminary target used to aid physician contouring of the final target that will be approved");
            sb.AppendLine("TS structure manipulation involves manipulating/modifying the structure itself or target structures. E.g.,");
            sb.AppendLine("(Ovaries, Crop target from structure, 1.5cm)  -->  modify the target structure such that the ovaries structure is cropped from the target with a 1.5 cm margin");
            sb.AppendLine("(Brainstem, Contour overlap, 0.0 cm)  -->  Identify the overlapping regions between the brainstem and target structure(s) and contour them as new structures");
            sb.AppendLine("Kidneys-1cm  -->  substructure for the Kidneys volume where the Kidneys are contracted by 1 cm");
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
