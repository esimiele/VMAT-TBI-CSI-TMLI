using AutoPlannerHelpers.ViewModels;
using System;
using System.Windows;

namespace AutoPlannerHelpers.Views
{
    /// <summary>
    /// Interaction logic for ConfirmPrompt.xaml
    /// </summary>
    public partial class AdditionalRingOperationView : Window
    {
        public AdditionalRingOperationView()
        {
            InitializeComponent();
            Loaded += ViewLoaded;
        }
        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdditionalRingOperationViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
                vm.RequestedReEvaluationOfCanExecute();
            }
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}
