using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerHelpers.Messengers;

namespace AutoPlannerHelpers.ViewModels
{
    public class CTExportViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<ExportCTModel> CTImageList { get; set; }

        #region properties
        public ExportCTModel SelectedCTImage { get; private set; } = null;
        #endregion

        #region commands
        public ICommand ExportCTCommand { get; set; }
        public ICommand ShowExportCTInfoCommand { get; set; }
        public ICommand KeyboardTestCommand { get; set; }
        public RelayCommand<ExportCTModel> CTImageSelectionChangedCommand { get; set; }
        #endregion

        public CTExportViewModel(List<ExportCTModel> ctImages)
        {
            CTImageList = new ObservableCollectionPropertyNotify<ExportCTModel> { };
            foreach(ExportCTModel itr in ctImages) CTImageList.Add(itr);
            ShowExportCTInfoCommand = new RelayCommand(ShowExportCTInfo);
            CTImageSelectionChangedCommand = new RelayCommand<ExportCTModel>(CTImageSelectionChanged);
            ExportCTCommand = new RelayCommand(ExportCT);
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
            if (!CTImageList.Any(x => x.SelectedForExport)) return;
            SelectedCTImage = CTImageList.FirstOrDefault(x => x.SelectedForExport);
            WeakReferenceMessenger.Default.Send(new RequestExportCT(SelectedCTImage));
        }
    }
}
