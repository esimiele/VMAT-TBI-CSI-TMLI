namespace AutoPlannerHelpers.Context
{
    public class PatientMetaData
    {

        //get/set methods
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MRN { get; set; } = string.Empty;
        public string DOB { get; set; } = string.Empty;

        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="context"></param>
        public PatientMetaData(EclipseContext context) 
        {
            FirstName = context.Patient.FirstName;
            MiddleName = context.Patient.MiddleName;
            LastName = context.Patient.LastName;
            MRN = context.Patient.Id;
            DOB = context.Patient.DateOfBirth.ToString();
        }

        /// <summary>
        /// Overloaded constructor in the case where the script is run outside of Eclipse on dicom files
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="middleName"></param>
        /// <param name="lastName"></param>
        /// <param name="mRN"></param>
        /// <param name="dOB"></param>
        public PatientMetaData(string firstName, string middleName, string lastName, string mRN, string dOB = "")
        {
            FirstName = firstName;
            MiddleName = middleName;
            LastName = lastName;
            MRN = mRN;
            DOB = dOB;
        }
    }
}
