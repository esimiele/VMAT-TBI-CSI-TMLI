using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using Prism.Mvvm;
using Prism.Commands;
using System.Windows;
using System.Linq;

namespace AutoPlannerHelpers.ViewModels
{
    public class CTExportViewModel :BindableBase
    {
        public ObservableCollectionPropertyNotify<ExportCTModel> CTImageList { get; set; }
        #region properties
        #endregion

        #region commands
        public DelegateCommand ExportCTCommand { get; set; }
        public DelegateCommand ShowExportCTInfoCommand { get; set; }
        public DelegateCommand<ExportCTModel> CTImageSelectionChangedCommand { get; set; }
        #endregion

        public CTExportViewModel(List<ExportCTModel> ctImages)
        {
            CTImageList = new ObservableCollectionPropertyNotify<ExportCTModel> { };
            foreach(ExportCTModel itr in ctImages) CTImageList.Add(itr);
            ShowExportCTInfoCommand = new DelegateCommand(ShowExportCTInfo);
            CTImageSelectionChangedCommand = new DelegateCommand<ExportCTModel>(CTImageSelectionChanged);
            ExportCTCommand = new DelegateCommand(ExportCT);
        }

        public void ShowExportCTInfo()
        {
            MessageBox.Show("Select a CT image to export to the deep learning model for autocontouring");
        }

        public void CTImageSelectionChanged(ExportCTModel model)
        {
            if(CTImageList.Any(x => x != model && x.SelectedForExport))
            {
                foreach (var ctImage in CTImageList.Where(x => x != model && x.SelectedForExport))
                {
                    ctImage.SelectedForExport = false;
                }
                CTImageList.Refresh();
            }
        }

        public void ExportCT()
        {

        }
    }
}
