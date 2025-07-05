using AutoPlannerHelpers.ViewModels;
using System;
using System.Windows;

namespace AutoPlannerHelpers.Views
{
    /// <summary>
    /// Interaction logic for ConfirmPrompt.xaml
    /// </summary>
    public partial class ModifyMarginView : Window
    {
        public ModifyMarginView()
        {
            InitializeComponent();
            Loaded += ViewLoaded;
        }
        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ModifyMarginViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}
