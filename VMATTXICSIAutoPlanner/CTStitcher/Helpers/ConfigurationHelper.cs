using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CTStitcher.Helpers
{
    public class ConfigurationHelper
    {
        //get methods
        public double[,] TransformMatrix { get; private set; } = new double[4,4];
        public StringBuilder ErrorMessage { get; private set; } = new StringBuilder();

        /// <summary>
        /// Method to force this class to be static while being able to have properties/fields
        /// </summary>
        /// <returns></returns>
        public static ConfigurationHelper GetInstance()
        {
            if (_instance != null) return _instance;
            else return _instance = new ConfigurationHelper ();
        }

        //data members
        private static ConfigurationHelper _instance;

        /// <summary>
        /// Helper method to read the 4x4 transformation matrix from the selected text file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool ReadTransformMatrixFromFile(string file)
        {
            try
            {
                ErrorMessage.Clear();
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line) && line.Substring(0, 1) != "%")
                        {
                            if(line.Equals(":begin transformation matrix:"))
                            {
                                int rowCount = 0;
                                double[,] matrix = new double[4, 4];
                                while(!(line = reader.ReadLine()).Equals(":end transformation matrix:"))
                                {
                                    double[] row = ParseTransformMatrixRow(line);
                                    if (!ReferenceEquals(row, null))
                                    {
                                        for (int i = 0; i < row.Length; i++)
                                        {
                                            TransformMatrix[rowCount, i] = row[i];
                                        }
                                    }
                                    else
                                    {
                                        ErrorMessage.AppendLine($"Error! Line is not valid! Exiting");
                                        ErrorMessage.AppendLine($"Row number: {rowCount}");
                                        ErrorMessage.AppendLine(line);
                                        TransformMatrix = new double[4, 4];
                                        return true;
                                    }
                                    rowCount++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessage.AppendLine($"Could parse 4x4 transformation matrix because: {e.Message}");
                ErrorMessage.AppendLine(e.StackTrace);
                TransformMatrix = new double[4, 4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method to read one row of the 4x4 transformation matrix from the text file
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public double[] ParseTransformMatrixRow(string line)
        {
            double[] row = new double[4];

            if (!VerifyLineIntegrity(line)) return null;
            line = CropLine(line, "{");
            int colCount = 0;
            while (line.Contains(","))
            {
                if(!double.TryParse(line.Substring(0, line.IndexOf(",")), out row[colCount++]))
                {
                    return null;
                }
                line = CropLine(line, ",");
            }
            if (!double.TryParse(line.Substring(0, line.IndexOf("}")), out row[colCount]))
            {
                return null;
            }
            
            return row;
        }

        /// <summary>
        /// Utility method to verify the integrity of the supplied line (row) of the transformation matrix from the text file
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool VerifyLineIntegrity(string line)
        {
            if (string.IsNullOrEmpty(line) || 
                !line.Contains("{") || 
                !line.Contains("}") || 
                !line.Contains(",") || 
                line.Count(x => (x == ',')) != 3) return false;
            return true;
        }

        /// <summary>
        /// Helper function to crop a string using a specified cropping character. All characters in the supplied string will be removed up to the first instance of the
        /// supplied character and the remainder will be returned
        /// </summary>
        /// <param name="line"></param>
        /// <param name="cropChar"></param>
        /// <returns></returns>
        public static string CropLine(string line, string cropChar) 
        { 
            return line.Substring(line.IndexOf(cropChar) + 1, line.Length - line.IndexOf(cropChar) - 1); 
        }
    }
}
