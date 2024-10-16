using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using AutoPlannerHelpers.ViewModels;
using AutoPlannerHelpers.Views;
using AutoPlannerHelpers.Models;

namespace CSIAutoPlanner.ViewModels
{
    internal class MainViewModel : BindableBase
    {
        #region properties
        #endregion

        #region view objects
        private object _exportCT;

        public object ExportCT
        {
            get { return _exportCT; }
            set { SetProperty(ref _exportCT, value); }
        }

        #endregion

        #region commands
        #endregion

        public MainViewModel(string[] args)
        {
            List<ExportCTModel> models = new List<ExportCTModel>
            {
                new ExportCTModel("1", "CT 1", 100, DateTime.Now.ToString("yyyy-mm-dd")),
                new ExportCTModel("2", "CT 2", 200, "2019-01-01"),
                new ExportCTModel("3", "CT 3", 300, "2020-10-10"),
            };
            ExportCT = new CTExportView { DataContext = new CTExportViewModel(models) };
        }
    }
}
