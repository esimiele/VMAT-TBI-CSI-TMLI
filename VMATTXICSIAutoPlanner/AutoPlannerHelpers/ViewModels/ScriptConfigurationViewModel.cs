using CommunityToolkit.Mvvm.ComponentModel;
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
            set { _scriptConfig = value; }
        }

        #endregion

        public ScriptConfigurationViewModel(StringBuilder config) 
        { 
            ScriptConfig = config.ToString();
        }
    }
}
