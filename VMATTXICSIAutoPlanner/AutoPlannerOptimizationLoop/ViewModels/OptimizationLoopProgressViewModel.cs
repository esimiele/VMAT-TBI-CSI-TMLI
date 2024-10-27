using AutoPlannerOptimizationLoop.Helpers;
using AutoPlannerOptimizationLoop.Views;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using AutoPlannerOptimizationLoop.Enums;
using Prism.Commands;
using AutoPlannerHelpers.Helpers;

namespace AutoPlannerOptimizationLoop.ViewModels
{
    public abstract class OptimizationLoopProgressViewModel : BindableBase
    {
        #region properties
        private string _progressInfo;
        private string _taskID;
        private int _taskProgress;
        private int _overallProgress;
        private OptimizationLoopStatus _runStatus;
        private string _runTime;
        private SolidColorBrush _taskProgressBackground;
        private SolidColorBrush _overallProgressBackground;
        private SolidColorBrush _runStatusBackground;

        public string ProgressInfo
        {
            get { return _progressInfo; }
            set { SetProperty(ref _progressInfo, value); }
        }

        public string TaskId
        {
            get { return _taskID; }
            set { SetProperty(ref _taskID, value); }
        }

        public int TaskProgress
        {
            get { return _taskProgress; }
            set { SetProperty(ref _taskProgress, value); }
        }

        public int OverallProgress
        {
            get { return _overallProgress; }
            set { SetProperty(ref _overallProgress, value); }
        }

        public OptimizationLoopStatus RunStatus
        {
            get { return _runStatus; }
            set { SetProperty(ref _runStatus, value); UpdateRunStatusBackground(); }
        }

        public string RunTime
        {
            get { return _runTime; }
            set { SetProperty(ref _runTime, value); }
        }

        public SolidColorBrush TaskProgressBackground
        {
            get { return _taskProgressBackground; }
            set { SetProperty(ref _taskProgressBackground, value); }
        }

        public SolidColorBrush OverallProgressBackground
        {
            get { return _overallProgressBackground; }
            set { SetProperty(ref _overallProgressBackground, value); }
        }

        public SolidColorBrush RunStatusBackground
        {
            get { return _runStatusBackground; }
            set { SetProperty(ref _runStatusBackground, value); }
        }

        protected string ElapsedRunTime { get => $"{sw.Elapsed.Hours:00}:{sw.Elapsed.Minutes:00}:{sw.Elapsed.Seconds:00}"; }
        protected bool AbortOptimization { get => _runStatus == OptimizationLoopStatus.Canceling; }
        #endregion

        #region commands
        public DelegateCommand WindowClosingCommand { get; set; }
        public DelegateCommand AbortRunCommand { get; set; }
        #endregion

        private bool isFinished;
        private bool canClose;
        //used to copy the instances of the background thread and the optimizationLoop class
        //path to where the log files should be written
        protected string logPath;
        protected string fileName;
        protected string fileNameErrorsWarnings;
        //get instances of the stopwatch and dispatch timer to report how long the calculation takes at each reporting interval
        private Stopwatch sw = new Stopwatch();
        private System.Timers.Timer _timer = new System.Timers.Timer();

        public OptimizationLoopProgressViewModel()
        {
            RunStatusBackground = Brushes.White;
            WindowClosingCommand = new DelegateCommand(WindowClosing);
            AbortRunCommand = new DelegateCommand(AbortRun);
        }

        /// <summary>
        /// Method to initialize the dispatcher and this class instance on the main thread to be able to marshal updates back to the UI thread.
        /// Launch the optimization loop (call the Run method)
        /// </summary>
        public void DoStuff(ESAPIWorker slave)
        {
            slave.DoWork(() =>
            {
                //start the stopwatch
                sw.Start();
                _timer.Start();
                RunTime = $"{0:00}:{0:00}:{0:00}";
                RunStatus = OptimizationLoopStatus.Running;
                //start the tasks asynchronously
                if (Run()) slave.isError = true;
                //stop the stopwatch
                sw.Stop();
                _timer.Stop();
            });
        }

        protected virtual bool Run() { return false; }

        public bool Execute()
        {
            ESAPIWorker slave = new ESAPIWorker();
            //create a new frame (multithreading jargon)
            DispatcherFrame frame = new DispatcherFrame();

            slave.RunOnNewThread(() =>
            {
                OptimizationLoopProgressView pv = new OptimizationLoopProgressView { DataContext = this };
                _timer.Interval = 1000;
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(Dt_tick);
                DoStuff(slave);
                pv.ShowDialog();
                //tell the code to hold until the progress window closes.
                frame.Continue = false;
            });
            Dispatcher.PushFrame(frame);
            return slave.isError;
        }

        /// <summary>
        /// Tick event. Called every tick interval on the stopwatch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Dt_tick(object sender, EventArgs e)
        {
            //increment the time on the progress window for each "tick", which is set to intervals of 1 second
            if (sw.IsRunning)
            {
                TimeSpan ts = sw.Elapsed;
                RunTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
        }

        #region update UI methods
        /// <summary>
        /// Update the current task category label information
        /// </summary>
        /// <param name="message"></param>
        protected void UpdateUILabel(string message)
        {
            TaskId = message;
        }

        /// <summary>
        /// Simple method to update the text block in the UI and progress bar with information from the main thread running the optimization loop
        /// </summary>
        /// <param name="percentComplete"></param>
        /// <param name="message"></param>
        /// <param name="fail"></param>
        //two overloaded methods to provide periodic updates on the progress of the optimization loop
        protected void ProvideUIUpdate(int percentComplete, string message, bool fail = false)
        {
            if (fail) FailEvent();
            TaskProgress = percentComplete;
            if (!string.IsNullOrEmpty(message))
            {
                ProgressInfo += message + Environment.NewLine;
                //UpdateLogFile(message);
            }
        }

        /// <summary>
        /// Simple method to update the text block in the UI and progress bar with information from the main thread running the optimization loop
        /// </summary>
        /// <param name="percentComplete"></param>
        /// <param name="message"></param>
        /// <param name="fail"></param>
        //two overloaded methods to provide periodic updates on the progress of the optimization loop
        protected void ProvideUIUpdate(int percentComplete, bool fail = false)
        {
            if (fail) FailEvent();
            TaskProgress = percentComplete;
        }

        /// <summary>
        /// Overloaded method to update the text block in the UI
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fail"></param>
        protected void ProvideUIUpdate(string message, bool fail = false)
        {
            if (fail) FailEvent();
            ProgressInfo += message + Environment.NewLine;
            UpdateLogFile(message);
        }

        /// <summary>
        /// Update the overall optimization loop progress bar in the UI
        /// </summary>
        /// <param name="percentComplete"></param>
        protected void UpdateOverallProgress(int percentComplete)
        {
            OverallProgress = percentComplete;
        }

        /// <summary>
        /// Method to help handle the case where the optimization loop ran into a problem and the UI needs to indicate that the optimization
        /// loop failed
        /// </summary>
        private void FailEvent()
        {
            TaskProgressBackground = Brushes.Red;
            //taskProgress.Foreground = Brushes.Red;
            OverallProgressBackground = Brushes.Red;
            //overallProgress.Foreground = Brushes.Red;
            RunStatus = OptimizationLoopStatus.Failed;
            sw.Stop();
            _timer.Stop();
            canClose = true;
        }
        #endregion

        #region logging
        /// <summary>
        /// Helper method to initialize the log file that will be written during the optimization loop. Copies file path and full file names
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="errorsWarnings"></param>
        protected void InitializeLogFile(string path, string name, string errorsWarnings)
        {
            logPath = path;
            fileName = name;
            fileNameErrorsWarnings = errorsWarnings;
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
        }

        /// <summary>
        /// Simple method to append the supplied string text to the log file
        /// </summary>
        /// <param name="output"></param>
        private void UpdateLogFile(string output)
        {
            //verify the directory exists prior to writing the log
            //if (Directory.Exists(logPath))
            //{
            //    output += Environment.NewLine;
            //    File.AppendAllText(fileName, output);
            //}
            //else
            //{
            //    ProvideUIUpdate($"Warning! {logPath} does not exist! Could not write to log file!", false);
            //}
        }
        #endregion

        #region abort run
        /// <summary>
        /// Event when user hits the abort button on the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AbortRun()
        {
            //the user wants to stop the optimization loop. Set the abortOpt flag to true.
            //The optimization loop will stop when it reaches an appropriate point
            if (!isFinished)
            {
                string message = Environment.NewLine + Environment.NewLine +
                    " Abort command received!" + Environment.NewLine + " The optimization loop will be stopped at the next available stopping point!" + Environment.NewLine + " Be patient!";
                ProgressInfo += message + Environment.NewLine;
                RunStatus = OptimizationLoopStatus.Canceling;
                UpdateLogFile(message);
            }
        }

        /// <summary>
        /// Helper method to handle the case when the optimization loop was terminated early
        /// </summary>
        protected void OptimizationRunAborted()
        {
            //the user requested to abort the optimization loop
            RunStatus = OptimizationLoopStatus.Aborted;
            isFinished = true;
            CleanUpRun();
        }

        /// <summary>
        /// Helper method the handle the case when the optimization loop completed
        /// </summary>
        protected void OptimizationRunCompleted()
        {
            //the optimization loop finished successfully
            RunStatus = OptimizationLoopStatus.Finished;
            isFinished = true;
            CleanUpRun();
        }

        /// <summary>
        /// Helper method to do any last minute cleaning up of the UI thread including stopping the stopwatch, indicating to the UI thread it is safe to 
        /// close, and print any warning messages that appeared during the run in the text block in the UI
        /// </summary>
        private void CleanUpRun()
        {
            //stop the clock and report the total run time. Also set the canClose flag to true to let the code know the background thread has finished working and it is safe to close
            sw.Stop();
            _timer.Stop();
            canClose = true;
            OverallProgress = 100;
            ProvideUIUpdate(100, Environment.NewLine + "Finished!", false);
            ProvideUIUpdate($"Total run time: {ElapsedRunTime}" + Environment.NewLine, false);

            ProvideUIUpdate("Errors and warnings:", false);
            LoadAndPrintErrorsWarnings();
        }

        private void UpdateRunStatusBackground()
        {
            if(_runStatus == OptimizationLoopStatus.Canceling) RunStatusBackground = Brushes.Yellow;
            else if (_runStatus == OptimizationLoopStatus.Aborted || _runStatus == OptimizationLoopStatus.Failed) RunStatusBackground = Brushes.Red;
            else if (_runStatus == OptimizationLoopStatus.Finished) RunStatusBackground = Brushes.LimeGreen;
        }

        /// <summary>
        /// Print the errors and warning messages that were encountered during the run to the user. These are written to a temp log file throughout
        /// the optimization loop. This temp file is loaded and its contents are printed to the text block in the UI
        /// </summary>
        private void LoadAndPrintErrorsWarnings()
        {
            if (!File.Exists(fileNameErrorsWarnings))
            {
                ProvideUIUpdate("None", false);
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(fileNameErrorsWarnings))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line)) ProvideUIUpdate(line, false);
                    }
                    reader.Close();
                }
                return;
            }
            catch (Exception e)
            {
                ProvideUIUpdate($"Error! Could not load errors and warnings log because: {e.Message}", true);
            }
        }
        #endregion

        /// <summary>
        /// Event raised when the window is closing. Since the UI is running on a separate thread, ensure the main thread is in a place that is appropriate
        /// for stopping. If not, the main thread will be frozen and unresponsive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowClosing()
        {
            //extremely annoying message letting the user know that they cannot shut down the program until the optimization loop reaches a safe stopping point. The confirm window will keep popping up until 
            //the optimization loop reaches a safe stopping point. At that time, the user can close the application. If the user closes the taskProgress window before that time, the background thread will still be working.
            //If the user forces the application to close, the timestamp within eclipse will still be there and it is not good to kill multithreaded applications in this way.
            //Basically, this code is an e-bomb, and will ensure the program can't be killed by the user until a safe stopping point has been reached (at least without the use of the task manager)
            while (!canClose)
            {
                RunStatus = OptimizationLoopStatus.Canceling;
                MessageBox.Show("I can't close until the optimization loop has stopped!"
                    + Environment.NewLine + "Please wait until the abort status says 'Aborted' or 'Finished' and then click 'Confirm'.");
            }
        }
    }
}
