using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.ViewModels
{
    public class PlanPreparationViewModel : BindableBase
    {
        #region properties
        private string _shiftNoteText;
        private string _separatePlansText;
        private System.Windows.Media.SolidColorBrush _shiftNoteTBBackground;
        private System.Windows.Media.SolidColorBrush _separatePlansTBBackground;

        public string ShiftNoteText
        {
            get { return _shiftNoteText; }
            set { SetProperty(ref _shiftNoteText, value); }
        }

        public string SeparatePlansText
        {
            get { return _separatePlansText; }
            set { SetProperty(ref _separatePlansText, value); }
        }

        public System.Windows.Media.SolidColorBrush ShiftNoteTBBackground
        {
            get { return _shiftNoteTBBackground; }
            set { SetProperty(ref _shiftNoteTBBackground, value); }
        }

        public System.Windows.Media.SolidColorBrush SeparatePlansTBBackground
        {
            get { return _separatePlansTBBackground; }
            set { SetProperty(ref _separatePlansTBBackground, value); }
        }
        #endregion

        #region commands
        public DelegateCommand GenerateShiftNoteCommand { get; set; }
        public DelegateCommand SeparatePlansCommand { get; set; }
        #endregion

        public PlanPreparationViewModel()
        {
            ShiftNoteText = "NO";
            SeparatePlansText = "NO";

            ShiftNoteTBBackground = System.Windows.Media.Brushes.Red;
            SeparatePlansTBBackground = System.Windows.Media.Brushes.Red;

            GenerateShiftNoteCommand = new DelegateCommand(GenerateShiftNote);
            SeparatePlansCommand = new DelegateCommand(SeparatePlans);
        }

        public void GenerateShiftNote()
        {

        }

        public void SeparatePlans()
        {

        }
    }
}
