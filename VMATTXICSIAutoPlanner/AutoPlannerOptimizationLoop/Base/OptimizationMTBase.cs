using AutoPlannerOptimizationLoop.Helpers;
using AutoPlannerOptimizationLoop.ViewModels;
using AutoPlannerOptimizationLoop.Views;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AutoPlannerOptimizationLoop.Base
{
    //public class OptimizationMTbase
    //{
    //    private Dispatcher _dispatch;
    //    private OptimizationLoopProgressViewModel _pw;
    //    private bool isInitialized = false;
    //    protected string logPath;
    //    protected string fileName;
    //    protected string fileNameErrorsWarnings;

    //    /// <summary>
    //    /// Virtual run control
    //    /// </summary>
    //    /// <returns></returns>
    //    public virtual bool Run()
    //    {
    //        return false;
    //    }

    //    /// <summary>
    //    /// Execute method, which generates a new thread, launches the optimization progress window on this thread, and holds the code
    //    /// progression until the progress window is closed. The actual optimization is launched from the optimization progress window
    //    /// </summary>
    //    /// <returns></returns>
    //    public bool Execute()
    //    {
    //        ESAPIWorker slave = new ESAPIWorker();
    //        //create a new frame (multithreading jargon)
    //        DispatcherFrame frame = new DispatcherFrame();
    //        isInitialized = true;
    //        OptimizationLoopProgressViewModel pvm = new OptimizationLoopProgressViewModel();

    //        slave.RunOnNewThread(() =>
    //        {
    //            //pass the progress window the newly created thread and this instance of the optimizationLoop class.
    //            OptimizationLoopProgressView pv = new OptimizationLoopProgressView { DataContext = pvm };
    //            //pvm.SetCallerClass(slave, this);
    //            pv.ShowDialog();

    //            //tell the code to hold until the progress window closes.
    //            frame.Continue = false;
    //        });
    //        Dispatcher.PushFrame(frame);
    //        return slave.isError;
    //    }

    //    /// <summary>
    //    /// Helper method to copy the instances of the dispatcher and progress window objects so we can push information from the main thread
    //    /// to the UI thread
    //    /// </summary>
    //    /// <param name="d"></param>
    //    /// <param name="p"></param>
    //    public void SetDispatcherAndUIInstance(Dispatcher d, OptimizationLoopProgressViewModel p)
    //    {
    //        _dispatch = d;
    //        _pw = p;
    //        //perform logging on progress window UI thread
    //        //_dispatch.BeginInvoke((Action)(() => { _pw.InitializeLogFile(logPath, fileName, fileNameErrorsWarnings); }));
    //    }

    //    /// <summary>
    //    /// Set the abort status of the optimization loop. Set from classes derived from this class
    //    /// </summary>
    //    /// <param name="message"></param>
    //    protected void SetAbortUIStatus(string message)
    //    {
    //        //if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.abortStatus.Text = message; }));
    //        //else Console.WriteLine(message);
    //    }

    //    /// <summary>
    //    /// Helper method to update the operation category label in the UI
    //    /// </summary>
    //    /// <param name="message"></param>
    //    public void UpdateUILabel(string message)
    //    {
    //        if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.UpdateUILabel(message); }));
    //        else Console.WriteLine(message);
    //    }

    //    /// <summary>
    //    /// Simple method to add a message to the UI textblock and update the progress bar
    //    /// </summary>
    //    /// <param name="percentComplete"></param>
    //    /// <param name="message"></param>
    //    /// <param name="fail"></param>
    //    protected void ProvideUIUpdate(int percentComplete, string message = "", bool fail = false)
    //    {
    //        if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.ProvideUpdate(percentComplete, message, fail); }));
    //        else Console.WriteLine(message);
    //    }

    //    /// <summary>
    //    /// Overloaded UI update method that only adds a message to the UI textblock
    //    /// </summary>
    //    /// <param name="message"></param>
    //    /// <param name="fail"></param>
    //    protected void ProvideUIUpdate(string message, bool fail = false)
    //    {
    //        if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.ProvideUpdate(message, fail); }));
    //        else Console.WriteLine(message);
    //    }

    //    /// <summary>
    //    /// Method to update the overall progress bar on the UI
    //    /// </summary>
    //    /// <param name="percentComplete"></param>
    //    protected void UpdateOverallProgress(int percentComplete)
    //    {
    //        if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.UpdateOverallProgress(percentComplete); }));
    //    }

    //    /// <summary>
    //    /// Get the current abort status from the UI thread
    //    /// </summary>
    //    /// <returns></returns>
    //    protected bool GetAbortStatus()
    //    {
    //        return _pw.GetAbortStatus();
    //    }

    //    /// <summary>
    //    /// Tell the UI thread (supervising the optimization loop) to kill the optimization loop
    //    /// </summary>
    //    protected void KillOptimizationLoop()
    //    {
    //        if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.OptimizationRunAborted(); }));
    //    }

    //    /// <summary>
    //    /// Send a message to the UI thread from the Main thread that the optimization loop has finished and it's time to wrap up
    //    /// </summary>
    //    protected void OptimizationLoopFinished()
    //    {
    //        if (isInitialized) _dispatch.BeginInvoke((Action)(() => { _pw.SetFinishStatus(true); _pw.OptimizationRunCompleted(); }));
    //    }

    //    /// <summary>
    //    /// Get the total elapsed time of the optimization loop from the UI thread
    //    /// </summary>
    //    /// <returns></returns>
    //    protected string GetElapsedTime()
    //    {
    //        return _pw.GetElapsedTime();
    //    }
    //}
}
