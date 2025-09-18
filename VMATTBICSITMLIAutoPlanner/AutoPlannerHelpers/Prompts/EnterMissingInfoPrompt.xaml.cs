using System.Windows;

namespace AutoPlannerHelpers.Prompts
{
    /// <summary>
    /// Interaction logic for AddMissingInfoPrompt.xaml
    /// </summary>
    public partial class EnterMissingInfoPrompt : Window
    {
        public bool GetSelection {  get => _confirm; }
        public string EnteredValue { get => _value; }

        private bool _confirm = false;
        private string _value = string.Empty;

        public EnterMissingInfoPrompt(string message, string info, string button1Content = "Confirm", string button2Content = "Cancel")
        {
            InitializeComponent();
            informationTB.Text = message;
            requestedInfo.Content = info;
            Button1.Content = button1Content;
            Button2.Content = button2Content;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            _confirm = true;
            _value = valueTB.Text;
            this.Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
