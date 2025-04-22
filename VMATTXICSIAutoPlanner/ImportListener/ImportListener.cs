using EvilDICOM.Core;
using EvilDICOM.Core.Helpers;
using EvilDICOM.Network;
using EvilDICOM.Network.Enums;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using VMS.TPS.Common.Model.API;

namespace ImportListener
{
    class ImportListener
    {
        static int updateFrequencyMSec = 100;
        static bool filePresent = false;
        static bool fileReadyForImport = false;
        static string theFile;
        static string SSID;
        static double elapsedSec = 0.0;
        static System.Timers.Timer aTimer = null;
        const string _twirl = "-\\|/";
        static private int index = 0;
        static bool playAnimation = true;
        static Logging.ImportListenerLogging log;

        /// <summary>
        /// Main function. Input arguments are passed from calling script and include import path, mrn, aria database daemon info, local daemon info, and timeout period
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            log = new Logging.ImportListenerLogging();
            //args = new string[] { "\\\\shariatscap105\\Dicom\\RSDCM\\Import\\", "CSI55", "VMSDBD" ,"10.151.176.60" ,"51402" ,"DCMTK" ,"50400" ,"3600", "TMLI" };
            ImportSettingsModel importSettings = ImportListenerHelper.ParseInputArguments(args.ToList());
            if (importSettings.IsValid) Run(importSettings);
            else log.WriteLine("Error! Unable to parse command line arguments! Cannot listen for RT structure set! Exiting");

            string logLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs\\preparation\\" + importSettings.PatientPlanType + "\\" + importSettings.MRN + "\\ImportSS\\";
            log.WriteLine("Press any key to exit");
            log.SaveLog(logLocation, false);
            Console.ReadLine();
        }

        /// <summary>
        /// Run control for the listener
        /// </summary>
        /// <returns></returns>
        private static bool Run(ImportSettingsModel settings)
        {
            try
            {
                SetTimer();
                PrintConfiguration(settings);
                log.WriteLine("Listening for RT structure set...");
                ListenForRTStruct(settings.ImportPath, settings.MRN, settings.TimeoutSec);
                if (filePresent)
                {
                    //wait one minute to ensure autocontouring model is done writing rt struct
                    ResetTimer(false);
                    log.WriteLine("Waiting for RT Struct file to be free for import...");
                    WaitForFile(settings.TimeoutSec);
                    if (fileReadyForImport)
                    {
                        if (!ImportRTStructureSet(settings))
                        {
                            //open aria and check if the spinal cord and brain are high res. If so, launch CSI autoplanning code
                            //and instruct to auto downsample to normal res
                            if (CheckIfImportedStructuresAreHighRes(settings.MRN))
                            {
                                if (LaunchExe("CSIAutoPlanner", settings.MRN))
                                {
                                    Environment.Exit(0);
                                }
                            }
                        }
                    }
                    else log.WriteLine($"Auto contours for patient ({settings.MRN}) were being used by another process and could not be imported. Exiting");
                }
                else log.WriteLine($"Auto contours for patient ({settings.MRN}) not found in time allotted. Exiting");
            }
            catch (Exception e)
            {
                aTimer.Stop();
                Console.Error.WriteLine(e.ToString());
                Console.Error.WriteLine(e.StackTrace);
                return true;
            }
            if (aTimer != null) aTimer.Dispose();
            return false;
        }

        /// <summary>
        /// Helper method to launch the executable with name matching the supplied name
        /// </summary>
        /// <param name="exeName"></param>
        private static bool LaunchExe(string exeName, string mrn)
        {
            bool successfulLaunch = false;
            string path = AppExePath(exeName);
            if (!string.IsNullOrEmpty(path))
            {
                ProcessStartInfo p = new ProcessStartInfo(path)
                {
                    //something vague and unique. -d for downsample
                    Arguments = $"-d {mrn} {SSID}"
                };
                Process.Start(p);
                log.WriteLine($"Launched VMAT CSI autoplanning code to automatically down sample high-res structures!");
                successfulLaunch = true;
            }
            else log.WriteLine($"Error! {exeName} executable NOT found!");
            return successfulLaunch;
        }

        /// <summary>
        /// Same method in the .cs launcher (can't use external libraries in single file plugins)
        /// </summary>
        /// <param name="exeName"></param>
        /// <returns></returns>
        private static string AppExePath(string exeName)
        {
            return FirstExePathIn(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), exeName);
        }

        /// <summary>
        /// Same method in the .cs launcher (can't use external libraries in single file plugins)
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="exeName"></param>
        /// <returns></returns>
        private static string FirstExePathIn(string dir, string exeName)
        {
            return Directory.GetFiles(dir, "*.exe").FirstOrDefault(x => x.Contains(exeName));
        }

        /// <summary>
        /// Print the listener configuration settings for this run
        /// </summary>
        /// <param name="listening"></param>
        private static void PrintConfiguration(ImportSettingsModel settings)
        {
            log.WriteLine(DateTime.Now.ToString());
            log.WriteLine("Configuration:");
            log.WriteLine($"Import path: {settings.ImportPath}");
            log.WriteLine($"Patient Id: {settings.MRN}");
            log.WriteLine($"Aria DB Daemon AE Title: {settings.AriaDBAET}");
            log.WriteLine($"Aria DB Daemon IP: {settings.AriaDBIP}");
            log.WriteLine($"Aria DB Daemon Port: {settings.AriaDBPort}");
            log.WriteLine($"Local Daemon AE Title: {settings.LocalAET}");
            log.WriteLine($"Local Daemon Port: {settings.LocalPort}");
            log.WriteLine($"Requested timeout: {settings.TimeoutSec} seconds");
            log.WriteLine("");
        }

        /// <summary>
        /// Create, initialize, and start a timer to keep track of how much time has elapsed while listening
        /// </summary>
        private static void SetTimer()
        {
            // Create a timer with a 100 msec interval.
            aTimer = new System.Timers.Timer(updateFrequencyMSec);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Start();
        }

        /// <summary>
        /// Simple method to reset the elapsed time to 0 and restart the time
        /// </summary>
        /// <param name="stopAnimation"></param>
        private static void ResetTimer(bool stopAnimation)
        {
            aTimer.Stop();
            elapsedSec = 0.0;
            playAnimation = stopAnimation;
            aTimer.Start();
        }

        /// <summary>
        /// Once the structure set file has been found in the import folder,
        /// </summary>
        private static void WaitForFile(double timeoutSec)
        {
            while (!fileReadyForImport && elapsedSec < timeoutSec)
            {
                if (!IsFileLocked(new FileInfo(theFile)))
                {
                    fileReadyForImport = true;
                    Console.Write("\b");
                    log.WriteLine($"RT Struct file ({theFile}) is ready for import");
                    log.WriteLine("");
                }
                Wait(10000);
            }
            log.WriteLine($"Elapsed time: {elapsedSec:0.0} sec");
        }

        /// <summary>
        /// Janky method to see if the listener can import/access the structure set file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            //file is not locked
            return false;
        }

        /// <summary>
        /// Called each 'tick' event. Updates the UI and increments the elapsed time by the update frequency
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //increment the time on the progress window for each "tick", which is set to intervals of 0.1 second
            if (playAnimation) UpdateProgress();
            elapsedSec += (double)updateFrequencyMSec / 1000;
        }

        /// <summary>
        /// Update the UI with the twirl animation
        /// </summary>
        private static void UpdateProgress()
        {
            Console.Write("\b");
            Console.Write(_twirl[index++ % _twirl.Length]);
        }

        /// <summary>
        /// Method to construct the aria database and local daemons
        /// </summary>
        /// <returns></returns>
        private static (Entity, Entity) ConstructDaemons(ImportSettingsModel settings)
        {
            Entity ariaDBDaemon = new Entity(settings.AriaDBAET, settings.AriaDBIP, settings.AriaDBPort);
            Entity localDaemon = Entity.CreateLocal(settings.LocalAET, settings.LocalPort);
            return (ariaDBDaemon, localDaemon);
        }

        /// <summary>
        /// Simple method to monitor the import folder for the structure set file (checks every 10 sec). Once found, set the file present flag to true
        /// </summary>
        private static void ListenForRTStruct(string importPath, string mrn, double timeoutSec)
        {
            while (!filePresent && elapsedSec < timeoutSec)
            {
                if (CheckDirectoryForRTStruct(importPath, mrn))
                {
                    filePresent = true;
                    aTimer.Stop();
                    Console.Write("\b");
                    log.WriteLine($"Auto contours for patient {mrn} found");
                    log.WriteLine("");
                }
                Wait(10000);
            }
            log.WriteLine($"Elapsed time: {elapsedSec:0.0} sec");
        }

        /// <summary>
        /// Helper function to wait a specified amount of msec
        /// </summary>
        /// <param name="waitTime"></param>
        private static void Wait(int waitTime)
        {
            Thread.Sleep(waitTime);
        }

        /// <summary>
        /// Query the import folder for new dicom files. Open each dicom file present in import folder and see if the patient mrn matches the 
        /// mrn supplied as an input argument to the listener
        /// </summary>
        /// <returns></returns>
        private static bool CheckDirectoryForRTStruct(string importPath, string mrn)
        {
            foreach (string file in Directory.GetFiles(importPath))
            {
                //get the names of each patient whose CT data is in the CT DICOM dump directory
                DICOMObject dcmObj = DICOMObject.Read(file);
                if (string.Equals(dcmObj.FindFirst(TagHelper.PatientID).DData as string, mrn))
                {
                    theFile = file;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Push the dicom structure set file to the aria database
        /// </summary>
        /// <returns></returns>
        private static bool ImportRTStructureSet(ImportSettingsModel settings)
        {
            log.WriteLine("Importing structure set now...");
            bool importFailed = false;
            (Entity ariaDBDaemon, Entity localDaemon) = ConstructDaemons(settings);
            if (PingDaemon(ariaDBDaemon, localDaemon)) return true;

            DICOMSCU client = new DICOMSCU(localDaemon);
            EvilDICOM.Network.SCUOps.CStorer storer = client.GetCStorer(ariaDBDaemon);
            ushort msgId = 1;
            DICOMObject dcm = DICOMObject.Read(theFile);
            SSID = dcm.FindFirst(TagHelper.StructureSetLabel).DData as string;

            log.WriteLine("Executing C-store operation now...");
            EvilDICOM.Network.DIMSE.CStoreResponse response = storer.SendCStore(dcm, ref msgId);
            //EvilDICOM.Network.DIMSE.CStoreResponse response = null;
            if (response == null)
            {
                response = new EvilDICOM.Network.DIMSE.CStoreResponse();
                response.Status = CheckAriaDBForImportedSS(settings.MRN, dcm);
            }
            if ((Status)response.Status != Status.SUCCESS)
            {
                log.WriteLine($"CStore failed");
                importFailed = true;
            }
            else
            {
                log.WriteLine($"DICOM C-Store from {localDaemon.AeTitle} => {ariaDBDaemon.AeTitle} @{ariaDBDaemon.IpAddress}:{ariaDBDaemon.Port}: {(Status)response.Status}");
                RemoveRTStructDcmFile(theFile);
            }
            return importFailed;
        }

        /// <summary>
        /// Helper method to check the aria DB for the imported structure set. Used in cases where the c-store response returns a null object, which 
        /// typically happens due to a network timeout error. 
        /// </summary>
        /// <param name="mrn"></param>
        /// <param name="dcm"></param>
        /// <returns></returns>
        private static ushort CheckAriaDBForImportedSS(string mrn, DICOMObject dcm)
        {
            Status importedSuccess = Status.FAILURE;
            //connection timed-out or something. Usually successfully imports --> directly check Aria DB through ESAPI
            log.WriteLine("Warning! CStore response was null! This typically happens when the network connection times out");
            log.WriteLine("Attempting to directly check the Aria DB if the structure set was imported successfully");
            try
            {
                Application app = Application.CreateApplication();
                Patient pi = app.OpenPatientById(mrn);
                if (pi != null)
                {
                    if (!string.IsNullOrEmpty(SSID))
                    {
                        if (pi.StructureSets.Any(x => string.Equals(SSID, x.Id)))
                        {
                            importedSuccess = Status.SUCCESS;
                        }
                        else log.WriteLine("Structure set not found in Aria!");
                    }
                    else log.WriteLine($"Error! Structure set id is null or empty!");
                }
                else
                {
                    log.WriteLine($"Error! Could not open patient {mrn}!");
                }
                app.ClosePatient();
                app.Dispose();
            }
            catch (Exception e)
            {
                log.WriteLine($"Error! Unable to connect to aria DB to check if structure set was successfully imported! Check manually!");
                log.WriteLine(e.Message);
                log.WriteLine(e.StackTrace);
            }
            return (ushort)importedSuccess;
        }

        /// <summary>
        /// Helper method to check the imported structures in the Aria DB and see if they were imported as high res
        /// </summary>
        /// <param name="mrn"></param>
        /// <returns></returns>
        private static bool CheckIfImportedStructuresAreHighRes(string mrn)
        {
            bool isHighRes = false;
            try
            {
                Application app = Application.CreateApplication();
                Patient pi = app.OpenPatientById(mrn);
                if (pi != null)
                {
                    if (!string.IsNullOrEmpty(SSID))
                    {
                        if (pi.StructureSets.Count(x => string.Equals(SSID, x.Id)) == 1)
                        {
                            StructureSet ss = pi.StructureSets.First(x => string.Equals(SSID, x.Id));
                            if (ss.Structures.Any(x => x.Id.ToLower().Contains("spinalcord") && !x.IsEmpty && x.IsHighResolution))
                            {
                                log.WriteLine($"Spinal cord was imported as high resolution!");
                                isHighRes = true;
                            }
                            else if (ss.Structures.Any(x => x.Id.ToLower().Contains("brain") && !x.IsEmpty && x.IsHighResolution))
                            {
                                log.WriteLine($"Brain was imported as high resolution!");
                                isHighRes = true;
                            }
                        }
                        else log.WriteLine("Structure set not found in Aria or more than one structure set found with the same Id! Check manually!");
                    }
                    else log.WriteLine($"Error! Structure set id is null or empty!");
                    app.ClosePatient();
                    app.Dispose();
                }
                else
                {
                    log.WriteLine($"Error! Could not open patient {mrn}!");
                }
            }
            catch (Exception e)
            {
                log.WriteLine($"Error! Unable to connect to aria DB to check if the imported structures were imported as high resolution! Check manually!");
                log.WriteLine(e.Message);
                log.WriteLine(e.StackTrace);
            }
            return isHighRes;
        }

        /// <summary>
        /// Simple method to ensure the local and aria database daemons can communicate
        /// </summary>
        /// <param name="daemon"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        private static bool PingDaemon(Entity daemon, Entity local)
        {
            log.WriteLine($"C Echo from {local.AeTitle} => {daemon.AeTitle} @ {daemon.IpAddress} : {daemon.Port}");
            DICOMSCU client = new DICOMSCU(local);
            //5 sec timeout
            bool success = client.Ping(daemon, 5000);
            log.WriteLine($"Success: {success}", !success);
            return !success;
        }

        /// <summary>
        /// Once the structure set file has been imported, remove the dicom file from the import folder
        /// </summary>
        /// <param name="theFile"></param>
        /// <returns></returns>
        private static bool RemoveRTStructDcmFile(string theFile)
        {
            log.WriteLine($"Removing {theFile} now");
            try
            {
                File.Delete(theFile);
                log.WriteLine($"{theFile} has been removed");
                return false;
            }
            catch (Exception e)
            {
                log.WriteLine($"Could not remove {theFile}");
                log.WriteLine(e.Message);
                log.WriteLine(e.StackTrace);
                return true;
            }
        }
    }
}
