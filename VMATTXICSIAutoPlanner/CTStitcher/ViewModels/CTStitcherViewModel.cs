using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using CTStitcher.enums;
using CTStitcher.Settings;
using System.IO;
using System.Reflection;
using CTStitcher.Helpers;
using System.Windows;
using System.Windows.Forms;
using Prism.Commands;
using CTStitcher.ImageFormatConverters;
using CTStitcher.Runners;
using CTStitcher.UIHelpers;
using CTStitcher.Models;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CTStitcher.ImageWriters;
using Image = System.Windows.Controls.Image;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Enums;

namespace CTStitcher.ViewModels
{
    public class CTStitcherViewModel : BindableBase
    {
        #region fields
        private bool isInitialized = false;
        PatientMetaData patient;
        int currentAxialSlice = 0;
        int currentCoronalSlice = 0;
        int currentSagittalSlice = 0;
        int MatchSlice = 1;
        Bitmap[] processedAxialCT;
        Bitmap[] processedSagittalCT;
        Bitmap[] processedCoronalCT;
        List<int> convertedSagittalSlices = new List<int>();
        List<int> convertedCoronalSlices = new List<int>();
        CTImageModel StitchedCT = null;
        List<int> AxialSlicesToReview = new List<int>();
        int TotalSlicesToReview = 0;
        #endregion

        #region properties
        private List<string> _hfsCTScans;
        private string _selectedHFSScan;
        private string _selectedFFSScan;
        private List<string> _ffsCTScans;
        private List<StitchingAlgorithm> _algorithms;
        private StitchingAlgorithm _selectedAlgorithm;
        private string _uiLogOutput;
        private BitmapSource _axialImage;
        private BitmapSource _coronalImage;
        private BitmapSource _sagittalImage;
        private string _axialSliceNumber;
        private double _progress;
        private Visibility _pushToAriaVisibility;

        public List<string> HFSCTScans
        {
            get { return _hfsCTScans; }
            set { SetProperty(ref _hfsCTScans, value); }
        }

        public string SelectedHFSScan
        {
            get { return _selectedHFSScan; }
            set { SetProperty(ref _selectedHFSScan, value); }
        }

        public string SelectedFFSScan
        {
            get { return _selectedFFSScan; }
            set { SetProperty(ref _selectedFFSScan, value); }
        }

        public List<string> FFSCTScans
        {
            get { return _ffsCTScans; }
            set { SetProperty(ref _ffsCTScans, value); }
        }

        public List<StitchingAlgorithm> Algorithms
        {
            get { return _algorithms; }
            set { SetProperty(ref _algorithms, value); }
        }

        public StitchingAlgorithm SelectedAlgorithm
        {
            get { return _selectedAlgorithm; }
            set { SetProperty(ref _selectedAlgorithm, value); }
        }

        public string UILogOutput
        {
            get { return _uiLogOutput; }
            set { value += Environment.NewLine; SetProperty(ref _uiLogOutput, value); Logger.GetInstance().AppendLogOutput(value); }
        }

        public BitmapSource DisplayedAxialImage
        {
            get { return _axialImage; }
            set { SetProperty(ref _axialImage, value); }
        }

        public BitmapSource DisplayedCoronalImage
        {
            get { return _coronalImage; }
            set { SetProperty(ref _coronalImage, value); }
        }

        public BitmapSource DisplayedSagittalImage
        {
            get { return _sagittalImage; }
            set { SetProperty(ref _sagittalImage, value); }
        }

        public string AxialSliceNumber
        {
            get { return _axialSliceNumber; }
            set { SetProperty(ref _axialSliceNumber, value); }
        }

        public double ReviewProgress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        public Visibility PushToAriaVisibility
        {
            get { return _pushToAriaVisibility; }
            set { SetProperty(ref _pushToAriaVisibility, value); }
        }
        #endregion

        #region Commands
        public DelegateCommand ReadCTFromDICOMCommand { get; set; }
        public DelegateCommand StitchCTsCommand { get; set; }
        public DelegateCommand PushStitchedCTToAriaCommand { get; set; }
        public DelegateCommand BypassStitchingAndPushToAriaCommand { get; set; }
        #endregion

        public CTStitcherViewModel() 
        {
            ReadCTFromDICOMCommand = new DelegateCommand(LoadCTFromDICOM);
            StitchCTsCommand = new DelegateCommand(StitchCTsTogether);
            PushStitchedCTToAriaCommand = new DelegateCommand(PushStitchedCTToAria);
            BypassStitchingAndPushToAriaCommand = new DelegateCommand(BypassStitchingAndPushStitchedCTToAria);
            PushToAriaVisibility = Visibility.Hidden;
            if (EclipseContext.GetInstance().IsInitialized && EclipseContext.GetInstance().CTImages.Any()) InitializeImages(EclipseContext.GetInstance().CTImages.Select(x => x.Id).ToList());
        }

        /// <summary>
        /// Initialize the CT images so the user can select the HFS and FFS images for stitching
        /// </summary>
        /// <param name="imageIds"></param>
        /// <returns></returns>
        public void InitializeImages(IEnumerable<string> imageIds)
        {
            (imageIds as List<string>).Insert(0, "--select--");
            HFSCTScans = new List<string>(imageIds);
            FFSCTScans = new List<string>(imageIds);
            SelectedHFSScan = HFSCTScans.First();
            SelectedFFSScan = FFSCTScans.First();

            Algorithms = new List<StitchingAlgorithm>(CTStitcherSettings.AvailableStitchingAlgorithms);
            SelectedAlgorithm = Algorithms.First(x => x == CTStitcherSettings.DefaultStitchingAlgorithm);
            ReviewProgress = 0;
            isInitialized = true;
        }

        public void StitchCTsTogether()
        {
            if (!isInitialized) return;
            if (string.Equals(SelectedHFSScan, "--select--") || string.Equals(SelectedFFSScan, "--select--"))
            {
                Logger.GetInstance().LogError("Error! Image or stitching algorithm was not selected in the drop down menus! Exiting");
                return;
            }
            else if (string.Equals(SelectedHFSScan, SelectedFFSScan))
            {
                Logger.GetInstance().LogError("Error! Either selected HFS scan and FFS scan are the same! Nothing to stitch! Exiting");
                return;
            }


            RegistrationPPModel registration;
            CTReaderHelper readerHelper = new CTReaderHelper();

            Logger.GetInstance().OpType = ScriptOperationType.StitchCT;
            if (EclipseContext.GetInstance().IsInitialized)
            {
                UILogOutput += UILogHelper.FormatEclipseContextForUILog(EclipseContext.GetInstance());
                //read images from Eclipse
                if (readerHelper.ReadCTImage(EclipseContext.GetInstance().CTImages.First(x => string.Equals(SelectedHFSScan, x.Id)), RegistrationImageType.Target))
                {
                    UILogOutput += readerHelper.UILog;
                    return;
                }
                if (readerHelper.ReadCTImage(EclipseContext.GetInstance().CTImages.First(x => string.Equals(SelectedFFSScan, x.Id)), RegistrationImageType.Source))
                {
                    UILogOutput += readerHelper.UILog;
                    return;
                }

                if (readerHelper.BuildRegistrationPP(EclipseContext.GetInstance().Registrations))
                {
                    UILogOutput += readerHelper.UILog;
                    return;
                }
                registration = readerHelper.RegistrationPP;
            }
            else
            {
                //Read data from DICOM
                if (readerHelper.ReadCTImage(SelectedHFSScan, RegistrationImageType.Target, true))
                {
                    UILogOutput += readerHelper.UILog;
                    return;
                }
                patient = readerHelper.PatientMetaData;
                Logger.GetInstance().MRN = patient.MRN;

                if (readerHelper.ReadCTImage(SelectedFFSScan, RegistrationImageType.Source))
                {
                    UILogOutput += readerHelper.UILog;
                    return;
                }

                //double[,] transformMatrix = new double[4, 4] { { 1,0,0, 3.7285},
                //                                              { 0,1,0, -0.488},
                //                                              { 0,0,1, 379.2401},
                //                                              { 0, 0, 0, 1} };
                //real transform (case 1)
                //double[,] transformMatrix = new double[4, 4] { { 0.999931, -0.011243, -0.003381, 4.194895},
                //                                              { 0.011263, 0.999918, 0.006121, 0.598348},
                //                                              { 0.003312, -0.006159, 0.999976, 379.467438},
                //                                              { 0, 0, 0, 1} };
                //real transform (case 2)
                //double[,] transformMatrix = new double[4, 4] { { 0.999868, 0.008270, -0.014018, 4.4223},
                //                                              { -0.008379, 0.999935, -0.007742, 1.4444},
                //                                              { 0.013953, 0.007858, 0.999872, 448.5766},
                //                                              { 0, 0, 0, 1} };
                if (readerHelper.BuildRegistrationPP(ConfigurationHelper.GetInstance().TransformMatrix))
                {
                    UILogOutput += readerHelper.UILog;
                    return;
                }
                registration = readerHelper.RegistrationPP;
            }
            UILogOutput += CTStitcherUIHelper.FormatRegisitrationPPDataForUILog(registration);

            CTStitcherRunner stitcher = new CTStitcherRunner();
            if (stitcher.StitchCTImages(registration, SelectedAlgorithm))
            {
                Logger.GetInstance().LogError(stitcher.ErrorMessages);
                UILogOutput += stitcher.ErrorMessages;
                return;
            }
            StitchedCT = stitcher.StitchedCT;
            Logger.GetInstance().AppendLogOutput("CT Stitching Output: ", stitcher.LogOutput);
            UILogOutput += CTStitcherUIHelper.FormatCTMetaDataForUILog(StitchedCT.MetaData, StitchedCT.Origin);
            MatchSlice = stitcher.MatchSlice;
            UILogOutput += $"Matchslice: {MatchSlice}";

            //convert the resulting image and prepare for viewing by the user
            CTFormatConverter formatConverter = new CTFormatConverter(StitchedCT, MatchSlice);
            if (formatConverter.Execute())
            {
                Logger.GetInstance().LogError(formatConverter.ErrorMessage);
                UILogOutput += formatConverter.ErrorMessage;
                return;
            }
            processedAxialCT = formatConverter.ProcessedCTData;
            Logger.GetInstance().AppendLogOutput("CT reformatting to bmp:", formatConverter.GetLogOutput());

            InitializeCTParametersForViewing();

            UpdateAxialImage();
            UpdateCoronalImage();
            UpdateSagittalImage();
        }

        #region Read CT from dicom
        public void LoadCTFromDICOM()
        {
            //UILogHelper.AppendLogOutput("Selecting folder containing CT DICOM images", StitcherUILogTB);
            string transformMatrixFilePath = GetFilePath();
            if (string.IsNullOrEmpty(transformMatrixFilePath) || !Path.GetExtension(transformMatrixFilePath).Equals(".txt"))
            {
                Logger.GetInstance().LogError("Error! Selected transformation matrix file is invalid! Exiting!");
                return;
            }
            IEnumerable<string> patientImagesIds = GetDCMCTImageFolders(transformMatrixFilePath);
            if (!patientImagesIds.Any())
            {
                Logger.GetInstance().LogError("Selected DICOM directory has NO CT image folders! Exiting Initialization!");
                return;
            }
            if (ConfigurationHelper.GetInstance().ReadTransformMatrixFromFile(transformMatrixFilePath))
            {
                Logger.GetInstance().LogError(ConfigurationHelper.GetInstance().ErrorMessage);
                return;
            }
            InitializeImages(patientImagesIds);
        }

        private string GetFilePath()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Assembly.GetExecutingAssembly().Location;
            if (ofd.ShowDialog() != DialogResult.OK) return "";
            UILogOutput += $"Transform matrix file path: {ofd.FileName}";
            return ofd.FileName;
        }

        private IEnumerable<string> GetDCMCTImageFolders(string transformFilePath)
        {
            string path = Path.GetDirectoryName(transformFilePath);
            IEnumerable<string> directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

            UILogOutput += $"Dicom image folders: {path}";
            foreach (string itr in directories) UILogOutput += itr;
            return directories;
        }
        #endregion

        #region write stitched CT
        private void PushStitchedCTToAria()
        {
            //if we got to this point, either the script successfully stitched together two esapi images or two dicom images
            string writePath = CTStitcherSettings.DefaultWritePath;
            if (string.IsNullOrEmpty(writePath) || !Directory.Exists(writePath))
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = CTStitcherSettings.DefaultWritePath;
                if (ofd.ShowDialog() != DialogResult.OK) return;
                writePath = Path.GetDirectoryName(ofd.FileName);
            }

            CTWriterDCM writer;
            writer = new CTWriterDCM(StitchedCT,
                                     patient.LastName,
                                     patient.FirstName,
                                     patient.MiddleName,
                                     patient.MRN,
                                     writePath,
                                     WriteFormat.DICOM);

            if (writer.Execute())
            {
                Logger.GetInstance().LogError(writer.ErrorMessage);
                return;
            }
            Logger.GetInstance().AppendLogOutput("Write stitched CT output:", writer.GetLogOutput());

            //DACTExportImportRunner runner = new DACTExportImportRunner(writer.FinalWritePath);
            //if (runner.Execute())
            //{
            //    Logger.GetInstance().LogError("Import to aria & push to autocontouring failed");
            //    Logger.GetInstance().AppendLogOutput(runner.ErrorMessage);
            //}
            //Logger.GetInstance().AppendLogOutput("DicomAnon API calls output:", runner.GetLogOutput());
            return;
        }

        private void BypassStitchingAndPushStitchedCTToAria()
        {
            Logger.GetInstance().OpType = ScriptOperationType.StitchCT;
            if (ReferenceEquals(patient, null))
            {
                Logger.GetInstance().LogError("Patient meta data is missing! Unable to push to aria and autocontouring! Exiting!");
                return;
            }
            if (string.IsNullOrEmpty(patient.MRN))
            {
                Logger.GetInstance().LogError("Error! Patient MRN is missing! Unable to push to aria and autocontouring! Exiting!");
                return;
            }
            if (!Directory.Exists(Path.Combine(CTStitcherSettings.DefaultWritePath, patient.MRN)))
            {
                Logger.GetInstance().LogError($"Error! Required dicom folder path {Path.Combine(CTStitcherSettings.DefaultWritePath, patient.MRN)} is missing! Unable to push to aria and autocontouring! Exiting!");
                return;
            }
            if (!Directory.GetFiles(Path.Combine(CTStitcherSettings.DefaultWritePath, patient.MRN), "*.dcm").First().Contains("merged"))
            {
                Logger.GetInstance().LogError($"Error! The dicom files in {Path.Combine(CTStitcherSettings.DefaultWritePath, patient.MRN)} were NOT generated by this script! Unable to push to aria and autocontouring! Exiting!");
                return;
            }
            //DACTExportImportRunner runner = new DACTExportImportRunner(Path.Combine(CTStitcherSettings.DefaultWritePath, PatientMetaData.MRN));
            //if (runner.Execute())
            //{
            //    Logger.GetInstance().LogError("Import to aria failed");
            //    Logger.GetInstance().AppendLogOutput(runner.ErrorMessage);
            //}
            //Logger.GetInstance().AppendLogOutput("DicomAnon API calls output:", runner.GetLogOutput());
        }
        #endregion

        #region StitchedCT UI rendering and review
        private void InitializeCTParametersForViewing()
        {
            processedCoronalCT = new Bitmap[StitchedCT.MetaData.YSize];
            processedSagittalCT = new Bitmap[StitchedCT.MetaData.XSize];
            int lowSliceReview = Math.Max(MatchSlice - CTStitcherSettings.MatchSliceReviewBuffer, 0);
            int highSliceReview = Math.Min(MatchSlice + CTStitcherSettings.MatchSliceReviewBuffer, StitchedCT.MetaData.ZSize);
            for (int i = lowSliceReview; i < highSliceReview; i++) AxialSlicesToReview.Add(i);
            TotalSlicesToReview = AxialSlicesToReview.Count;
            ReviewProgress = 0;

            currentAxialSlice = MatchSlice - CTStitcherSettings.MatchSliceReviewBuffer - 1;
            currentCoronalSlice = StitchedCT.MetaData.YSize / 2;
            currentSagittalSlice = StitchedCT.MetaData.XSize / 2;
            UILogOutput += "Settings for stitched CT review:";
            UILogOutput += $"Total slices to review: {TotalSlicesToReview}";
            UILogOutput += $"Slice buffer to review: {CTStitcherSettings.MatchSliceReviewBuffer}";
            UILogOutput += $"Low slice to review: {lowSliceReview}";
            UILogOutput += $"High slice to review: {highSliceReview}";
        }

        public void UpdateAxialImage(int delta = 0, Image img = null)
        {
            if (ReferenceEquals(StitchedCT, null) || Keyboard.IsKeyDown(Key.LeftCtrl)) return;
            if (delta > 0)
            {
                if (currentAxialSlice < StitchedCT.MetaData.ZSize - 1) ++currentAxialSlice;
                else currentAxialSlice = StitchedCT.MetaData.ZSize - 1;
            }
            else if (delta < 0)
            {
                if (currentAxialSlice >= 1) --currentAxialSlice;
                else currentAxialSlice = 0;
            }
            Logger.GetInstance().AppendLogOutput($"Rendering axial slice: {currentAxialSlice}");
            DisplayedAxialImage = CTDisplayHelper.Bitmap2BitmapImage(processedAxialCT[currentAxialSlice],
                                                                        Math.Min(StitchedCT.MetaData.XSize, StitchedCT.MetaData.YSize));
            UpdateReviewProgress(currentAxialSlice);
            AxialSliceNumber = currentAxialSlice.ToString();
        }

        private void UpdateReviewProgress(int axialSlice)
        {
            if (AxialSlicesToReview.Any(x => x == axialSlice))
            {
                //axial slice needs to be reviewed
                AxialSlicesToReview.Remove(axialSlice);
                ReviewProgress = 100 * (double)(TotalSlicesToReview - AxialSlicesToReview.Count) / TotalSlicesToReview;
                Logger.GetInstance().AppendLogOutput($"Review progress: {ReviewProgress}");
                if (ReviewProgress >= 100)
                {
                    Logger.GetInstance().AppendLogOutput("Stitched CT review complete!");
                    PushToAriaVisibility = Visibility.Visible;
                }
            }
        }

        public void UpdateCoronalImage(int delta = 0, Image img = null)
        {
            if (ReferenceEquals(StitchedCT, null) || Keyboard.IsKeyDown(Key.LeftCtrl)) return;
            if (delta > 0)
            {
                if (currentCoronalSlice < StitchedCT.MetaData.YSize - 1) ++currentCoronalSlice;
                else currentCoronalSlice = StitchedCT.MetaData.YSize - 1;
            }
            else if (delta < 0)
            {
                if (currentCoronalSlice >= 1) --currentCoronalSlice;
                else currentCoronalSlice = 0;
            }
            if (!convertedCoronalSlices.Any(x => x == currentCoronalSlice))
            {
                Logger.GetInstance().AppendLogOutput($"Generating coronal image and caching for slice: {currentCoronalSlice}");
                processedCoronalCT[currentCoronalSlice] = CTDisplayHelper.GenerateCoronalBMPFromCTData(StitchedCT.MetaData.XSize,
                                                                                                        currentCoronalSlice,
                                                                                                        MatchSlice,
                                                                                                        Math.Min(125, MatchSlice),
                                                                                                        Math.Min(99, StitchedCT.MetaData.ZSize - MatchSlice),
                                                                                                        processedAxialCT);
                convertedCoronalSlices.Add(currentCoronalSlice);
            }
            Logger.GetInstance().AppendLogOutput($"Rendering coronal slice: {currentCoronalSlice}");
            DisplayedCoronalImage = CTDisplayHelper.Bitmap2BitmapImage(processedCoronalCT[currentCoronalSlice],
                                                                        Math.Min(StitchedCT.MetaData.XSize, StitchedCT.MetaData.ZSize));
            
        }

        public void UpdateSagittalImage(int delta = 0, Image img = null)
        {
            if (ReferenceEquals(StitchedCT, null) || Keyboard.IsKeyDown(Key.LeftCtrl)) return;
            if (delta > 0)
            {
                if (currentSagittalSlice < StitchedCT.MetaData.XSize - 1) ++currentSagittalSlice;
                else currentSagittalSlice = StitchedCT.MetaData.XSize - 1;
            }
            else if (delta < 0)
            {
                if (currentSagittalSlice >= 1) --currentSagittalSlice;
                else currentSagittalSlice = 0;
            }
            if (!convertedSagittalSlices.Any(x => x == currentSagittalSlice))
            {
                Logger.GetInstance().AppendLogOutput($"Generating sagittal image and caching for slice: {currentSagittalSlice}");
                processedSagittalCT[currentSagittalSlice] = CTDisplayHelper.GenerateSagittalBMPFromCTData(StitchedCT.MetaData.YSize,
                                                                                                            currentSagittalSlice,
                                                                                                            MatchSlice,
                                                                                                            Math.Min(125, MatchSlice),
                                                                                                            Math.Min(99, StitchedCT.MetaData.ZSize - MatchSlice),
                                                                                                            processedAxialCT);
                convertedSagittalSlices.Add(currentSagittalSlice);
            }
            Logger.GetInstance().AppendLogOutput($"Rendering sagittal slice: {currentSagittalSlice}");
            DisplayedSagittalImage = CTDisplayHelper.Bitmap2BitmapImage(processedSagittalCT[currentSagittalSlice],
                                                                        Math.Min(StitchedCT.MetaData.YSize, StitchedCT.MetaData.ZSize));
        }
        #endregion
    }
}
