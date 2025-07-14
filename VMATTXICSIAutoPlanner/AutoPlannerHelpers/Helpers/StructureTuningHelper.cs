using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Delegates;
using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Helpers
{
    public static class StructureTuningHelper
    {
        public static List<string> GenerateStructureIdListPostUnion(List<string> ids)
        {
            //check if structures need to be unioned before adding defaults
            List<UnionStructureModel> structuresToUnion = new List<UnionStructureModel>(CheckStructuresToUnion(EclipseContext.GetInstance().StructureSet.Structures.Where(x => !x.IsEmpty).Select(x => x.Id)));
            ids.AddRange(structuresToUnion.Select(x => x.ProposedUnionStructureId));
            return ids;
        }

        /// <summary>
        /// Helper method to look through the structure set and identify a list of left and right structures that should be unioned together
        /// </summary>
        /// <param name="selectedSS"></param>
        /// <returns></returns>
        public static List<UnionStructureModel> CheckStructuresToUnion(IEnumerable<string> structureIds)
        {
            //left structure, right structure, unioned structure name
            List<UnionStructureModel> structuresToUnion = new List<UnionStructureModel> { };
            List<string> LStructs = structureIds.Where(x => (x.Substring(x.Length - 2, 2).ToLower() == "_l" || x.Substring(x.Length - 2, 2).ToLower() == " l")).ToList();
            List<string> RStructs = structureIds.Where(x => (x.Substring(x.Length - 2, 2).ToLower() == "_r" || x.Substring(x.Length - 2, 2).ToLower() == " r")).ToList();
            foreach (string itr in LStructs)
            {
                string RStruct = RStructs.FirstOrDefault(x => x.Substring(0, x.Length - 2) == itr.Substring(0, itr.Length - 2));
                string newName = AddProperEndingToName(itr.Substring(0, itr.Length - 2).ToLower());
                if (!string.IsNullOrEmpty(RStruct) && !structureIds.Any(x => string.Equals(x.ToLower(), newName.ToLower())))
                {
                    structuresToUnion.Add(new UnionStructureModel(itr, RStruct, newName));
                }
            }
            return structuresToUnion;
        }

        /// <summary>
        /// Simple helper method to add the proper ending to the unioned structure name
        /// </summary>
        /// <param name="initName"></param>
        /// <returns></returns>
        private static string AddProperEndingToName(string initName)
        {
            string unionedName;
            if (initName.Substring(initName.Length - 1, 1) == "y" && initName.Substring(initName.Length - 2, 2) != "ey") unionedName = initName.Substring(0, initName.Length - 1) + "ies";
            else if (initName.Substring(initName.Length - 1, 1) == "s") unionedName = initName + "es";
            else unionedName = initName + "s";
            return unionedName;
        }

        /// <summary>
        /// Helper method to union all identified left and right structures
        /// </summary>
        /// <returns></returns>
        public static bool UnionLRStructures(ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, "Checking for L and R structures to union!");
            List<UnionStructureModel> structuresToUnion = CheckStructuresToUnion(EclipseContext.GetInstance().StructureSet.Structures.Where(x => !x.IsEmpty).Select(x => x.Id));
            if (structuresToUnion.Any())
            {
                int calcItems = structuresToUnion.Count;
                int numUnioned = 0;
                foreach (UnionStructureModel itr in structuresToUnion)
                {
                    Structure newStructure = GetStructureFromId(itr.ProposedUnionStructureId, true);
                    Structure L = GetStructureFromId(itr.Structure_Left);
                    Structure R = GetStructureFromId(itr.Structure_Right);
                    newStructure.SegmentVolume = ContourHelper.ContourUnion(L, R, new StructureMarginModel(0.0), new StructureMarginModel(0.0));
                    if (newStructure.IsEmpty)
                    {
                        ProvideUIUpdate(0, $"Error! {newStructure.Id} is empty following union of L/R structures!", true);
                        return true;
                    }
                    ProvideUIUpdate(100* (++numUnioned / calcItems), $"Unioned {itr.ProposedUnionStructureId}");
                }
                ProvideUIUpdate(100, "Structures unioned successfully!");
            }
            else ProvideUIUpdate(100, "No structures to union!");
            return false;
        }

        /// <summary>
        /// Super helpful method to return the first structure with Id matching the supplied Id from the structure set
        /// </summary>
        /// <param name="id"></param>
        /// <param name="selectedSS"></param>
        /// <param name="createIfEmpty"></param>
        /// <returns></returns>
        public static Structure GetStructureFromId(string id, bool createIfEmpty = false, string dcmType = "")
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                throw new Exception("Error! Eclipse context not initialized! Unable to retrieve structure object from structure set!");
            }
            Structure theStructure = null;
            if (DoesStructureExistInSS(id))
            {
                theStructure = EclipseContext.GetInstance().StructureSet.Structures.First(x => string.Equals(x.Id.ToLower(), id.ToLower()));
            }
            else if (createIfEmpty)
            {
                //DICOM types
                //Possible values are "AVOIDANCE", "CAVITY", "CONTRAST_AGENT", "CTV", "EXTERNAL", "GTV", "IRRAD_VOLUME", 
                //"ORGAN", "PTV", "TREATED_VOLUME", "SUPPORT", "FIXATION", "CONTROL", and "DOSE_REGION". 
                if(string.IsNullOrEmpty(dcmType))
                {
                    //try and figure out the dcm type based on supplied structure id
                    dcmType = "CONTROL";
                    if (id.ToLower().Contains("gtv")) dcmType = "GTV";
                    else if (id.ToLower().Contains("ctv")) dcmType = "CTV";
                    else if (id.ToLower().Contains("ptv")) dcmType = "PTV";
                }
                if (EclipseContext.GetInstance().StructureSet.CanAddStructure(dcmType, id))
                {
                    theStructure = EclipseContext.GetInstance().StructureSet.AddStructure(dcmType, id);
                }
            }
            return theStructure;
        }

        public static List<Structure> GetStructuresFromIdList(IEnumerable<string> ids, bool returnNonNullStructuresOnly = false)
        {
            List<Structure> theStructures = new List<Structure> { };
            foreach (string itr in ids)
            {
                Structure s = GetStructureFromId(itr);
                if (returnNonNullStructuresOnly && !ReferenceEquals(s, null)) theStructures.Add(s);
                else theStructures.Add(s);
            }
            return theStructures;
        }

        /// <summary>
        /// Super helpful method to determine if the supplied structur ids exists in the structure set
        /// </summary>
        /// <param name="id"></param>
        /// <param name="selectedSS"></param>
        /// <param name="checkIsEmpty"></param>
        /// <returns></returns>
        public static bool DoesStructureExistInSS(string id, bool checkIsEmpty = false)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                throw new Exception("Error! Eclipse context not initialized! Unable to determine if structure exists in structure set!");
            }
            if (!checkIsEmpty) return EclipseContext.GetInstance().StructureSet.Structures.Any(x => string.Equals(id.ToLower(), x.Id.ToLower()));
            else return EclipseContext.GetInstance().StructureSet.Structures.Any(x => string.Equals(id.ToLower(), x.Id.ToLower()) && !x.IsEmpty);
        }

        /// <summary>
        /// Helper method to determine if any of the supplied structure ids exist in the structure set
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="selectedSS"></param>
        /// <param name="checkIsEmpty"></param>
        /// <returns></returns>
        public static bool DoesStructureExistInSS(List<string> ids, bool checkIsEmpty = false)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                throw new Exception("Error! Eclipse context not initialized! Unable to determine if structure exists in structure set!");
            }
            foreach (string itr in ids)
            {
                if (checkIsEmpty)
                {
                    if (EclipseContext.GetInstance().StructureSet.Structures.Any(x => string.Equals(itr.ToLower(), x.Id.ToLower()) && !x.IsEmpty)) return true;
                }
                else
                {
                    if (EclipseContext.GetInstance().StructureSet.Structures.Any(x => string.Equals(itr.ToLower(), x.Id.ToLower()))) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Simple method to determine if there is overlap between the supplied target and normal structures
        /// </summary>
        /// <param name="target"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static bool IsOverlap(Structure target, System.Windows.Media.Media3D.Point3DCollection normal)
        {
            return normal.Any(x => target.IsPointInsideSegment(new VMS.TPS.Common.Model.Types.VVector(x.X, x.Y, x.Z)));
        }
    }
}
