using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Text;

namespace AutoPlannerHelpers.ViewModels
{
    public class ScriptConfigurationViewModel : ObservableObject
    {
        #region properties
        private string _scriptConfig;

        public string ScriptConfig
        {
            get { return _scriptConfig; }
            set { SetProperty(ref _scriptConfig, value); }
        }
        #endregion

        public ScriptConfigurationViewModel(StringBuilder config) 
        { 
            ScriptConfig = config.ToString();
            InitializeMessengers();
        }

        public void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdateScriptConfiguration>(this, (r, m) =>
            {
                ScriptConfig = m.ScriptConfiguration.ToString();
            });
        }
    }
}
