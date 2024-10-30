using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Interfaces;
using AutoPlannerHelpers.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AutoPlannerHelpers.ViewModels
{
    public class SimpleProgressWindowViewModel : BindableBase
    {
        #region properties
        private int _taskProgress;
        private string _progressInfo;
        private SolidColorBrush _taskProgressBackground;
        private string _runTime;
        private string _taskID;

        public int TaskProgress
        {
            get { return _taskProgress; }
            set { SetProperty(ref _taskProgress, value); }
        }
        public string TaskId
        {
            get { return _taskID; }
            set { SetProperty(ref _taskID, value); }
        }

        public string ProgressInfo
        {
            get { return _progressInfo; }
            set { SetProperty(ref _progressInfo, value); }
        }

        public SolidColorBrush TaskProgressBackground
        {
            get { return _taskProgressBackground; }
            set { SetProperty(ref _taskProgressBackground, value); }
        }

        public string RunTime
        {
            get { return _runTime; }
            set { SetProperty(ref _runTime, value); }
        }

        protected string LogOutput { get => ProgressInfo; }
        protected string ElapsedRunTime { get => $"{sw.Elapsed.Hours:00}:{sw.Elapsed.Minutes:00}:{sw.Elapsed.Seconds:00}"; }
        #endregion

        #region fields
        private Stopwatch sw = new Stopwatch();
        private System.Timers.Timer _timer = new System.Timers.Timer();
        private EventHandler OnRequestClose;
        private bool _closeOnSuccessfulFinish = false;
        private int _closeTimeOut = 3000;
        #endregion

        #region commands
        #endregion

        public void DoStuff(ESAPIWorker slave)
        {
            slave.DoWork(() =>
            {
                //start the stopwatch
                sw.Start();
                _timer.Start();
                RunTime = $"{0:00}:{0:00}:{0:00}";
                //start the tasks asynchronously
                if (Run()) slave.isError = true;
                //stop the stopwatch
                sw.Stop();
                _timer.Stop();
                if (_closeOnSuccessfulFinish && !slave.isError) OnRequestClose(this, new EventArgs());
            });
        }

        public void SetCloseOnFinish(bool closeOnFinish, int timeout)
        {
            _closeOnSuccessfulFinish = closeOnFinish;
            _closeTimeOut = timeout;
        }

        protected virtual bool Run() { return false; }

        public bool Execute()
        {
            ESAPIWorker slave = new ESAPIWorker();
            //create a new frame (multithreading jargon)
            DispatcherFrame frame = new DispatcherFrame();

            slave.RunOnNewThread(() =>
            {
                SimpleProgressWindowView pv = new SimpleProgressWindowView { DataContext = this };
                this.OnRequestClose += (s, e) => pv.Dispatcher.BeginInvoke(() => { Thread.Sleep(_closeTimeOut); pv.Close(); });

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
        protected void ProvideUIUpdate(int percentComplete, string message, bool fail = false)
        {
            if (fail) FailEvent();
            TaskProgress = percentComplete;
            if (!string.IsNullOrEmpty(message))
            {
                ProgressInfo += message + Environment.NewLine;
            }
        }

        /// <summary>
        /// Simple method to update the text block in the UI and progress bar with information from the main thread running the optimization loop
        /// </summary>
        /// <param name="percentComplete"></param>
        /// <param name="message"></param>
        /// <param name="fail"></param>
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
        }


        /// <summary>
        /// Method to help handle the case where the optimization loop ran into a problem and the UI needs to indicate that the optimization
        /// loop failed
        /// </summary>
        private void FailEvent()
        {
            TaskProgressBackground = Brushes.Red;
            sw.Stop();
            _timer.Stop();
            UpdateUILabel("Failed");
        }
        #endregion

        private void HandleCloseOnFinish(Dispatcher dispatch)
        {
            //dispatch.Invoke((Action)(() => { Thread.Sleep(_closeTimeOut); }));
            //dispatch.Invoke((Action)(() => { this.Close(); }));
        }

    }
}
