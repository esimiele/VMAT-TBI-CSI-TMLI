using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportListener.Logging
{
    internal class ImportListenerLogging
    {
        public StringBuilder LogString = new StringBuilder();
        public void WriteLine(string str, bool suppressOutput = false)
        {
            if (!suppressOutput) Console.WriteLine(str);
            LogString.Append(str).Append(Environment.NewLine);
        }
        public void Write(string str, bool suppressOutput = false)
        {
            if (!suppressOutput) Console.Write(str);
            LogString.Append(str);

        }
        public void SaveLog(string Path, bool Append = false)
        {
            if (LogString != null && LogString.Length > 0)
            {
                if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);
                if (Append)
                {
                    using (StreamWriter file = File.AppendText(Path))
                    {
                        file.Write(LogString.ToString());
                        file.Close();
                        file.Dispose();
                    }
                }
                else
                {
                    using (StreamWriter file = new StreamWriter($"{Path}\\{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt"))
                    {
                        file.Write(LogString.ToString());
                        file.Close();
                        file.Dispose();
                    }
                }
            }
        }
    }
}
