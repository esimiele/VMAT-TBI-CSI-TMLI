using AutoPlannerHelpers.Delegates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Models;
using System.Windows.Media.Converters;

namespace AutoPlannerHelpers.Helpers
{
    public static class ContourHelper
    {
        /// <summary>
        /// Helper method to crop the given structure from the body structure
        /// </summary>
        /// <param name="theStructure"></param>
        /// <param name="EclipseContext.GetInstance().StructureSet"></param>
        /// <param name="marginInCm"></param>
        /// <returns></returns>
        public static (bool, StringBuilder) CropStructureFromBody(Structure theStructure, double marginInCm, string bodyId = "Body")
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            //margin is in cm
            if (!string.IsNullOrEmpty(bodyId))
            {
                Structure body = StructureTuningHelper.GetStructureFromId(bodyId);
                if (!ReferenceEquals(body, null))
                {
                    if (marginInCm >= -5.0 && marginInCm <= 5.0) theStructure.SegmentVolume = theStructure.SegmentVolume.And(body.SegmentVolume.Margin(marginInCm * 10));
                    else
                    {
                        sb.AppendLine("Cropping margin from body MUST be within +/- 5.0 cm!");
                        fail = true;
                    }
                }
                else
                {
                    sb.AppendLine("Could not find body structure! Can't crop target from body!");
                    fail = true;
                }
            }
            else
            {
                sb.AppendLine("Requested body structure id is null or empty! Exiting!");
                fail = true;
            }
            return (fail, sb);
        }

        public static bool PerformStructureOperation(StructureOperationModel structureOperation, UIUpdateMessageOnlyDelegate ProvideUIUpdate)
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().StructureSet, null))
            {
                throw new Exception("Error! Eclipse context not initialized! Unable to perform structure derivations!");
            }
            if (!StructureTuningHelper.DoesStructureExistInSS(structureOperation.StructureA, true) && !StructureTuningHelper.DoesStructureExistInSS(structureOperation.StructureB, true))
            {
                Structure StructureA = StructureTuningHelper.GetStructureFromId(structureOperation.StructureA);
                Structure StructureB = StructureTuningHelper.GetStructureFromId(structureOperation.StructureB);
                Structure OutputStructure = StructureTuningHelper.GetStructureFromId(structureOperation.OutputStructure, true);
                switch (structureOperation.Operation)
                {
                    case StructureDerivationOperation.Intersection:
                        OutputStructure.SegmentVolume = ContourIntersection(StructureA, StructureB, structureOperation.MarginA, structureOperation.MarginB);
                        if (OutputStructure.IsEmpty) ProvideUIUpdate($"Warning! Output structure ({OutputStructure.Id}) is empty following {structureOperation.Operation} operation!");
                        break;
                    case StructureDerivationOperation.Union:
                        OutputStructure.SegmentVolume = ContourUnion(StructureA, StructureB, structureOperation.MarginA, structureOperation.MarginB);
                        if (OutputStructure.IsEmpty) ProvideUIUpdate($"Warning! Output structure ({OutputStructure.Id}) is empty following {structureOperation.Operation} operation!");

                        break;
                    case StructureDerivationOperation.Crop:
                        OutputStructure.SegmentVolume = CropStructureFromStructure(StructureA, StructureB, structureOperation.MarginA, structureOperation.MarginB);
                        if (OutputStructure.IsEmpty) ProvideUIUpdate($"Warning! Output structure ({OutputStructure.Id}) is empty following {structureOperation.Operation} operation!");

                        break;
                    case StructureDerivationOperation.XOR:
                        OutputStructure.SegmentVolume = ContourXOR(StructureA, StructureB, structureOperation.MarginA, structureOperation.MarginB);
                        if (OutputStructure.IsEmpty) ProvideUIUpdate($"Warning! Output structure ({OutputStructure.Id}) is empty following {structureOperation.Operation} operation!");

                        break;
                    case StructureDerivationOperation.CutInferiorTo:
                        OutputStructure.SegmentVolume = CutStructureInfToStructure(StructureA, StructureB);
                        if (OutputStructure.IsEmpty) ProvideUIUpdate($"Warning! Output structure ({OutputStructure.Id}) is empty following {structureOperation.Operation} operation!");

                        break;
                    case StructureDerivationOperation.CutSuperiorTo:
                        OutputStructure.SegmentVolume = CutStructureSupToStructure(StructureA, StructureB);
                        if (OutputStructure.IsEmpty) ProvideUIUpdate($"Warning! Output structure ({OutputStructure.Id}) is empty following {structureOperation.Operation} operation!");
                        break;
                }
            }
            else
            {
                ProvideUIUpdate($"Error! Missing required structures for derivation! Exiting!", true);
                ProvideUIUpdate($"{structureOperation.StructureA} {structureOperation.Operation} {structureOperation.StructureB}");
                return true;
            }
            return false;
        }

        public static SegmentVolume ContourIntersection(Structure a, Structure b, StructureMarginModel marginA, StructureMarginModel marginB)
        {
            //margin is in cm
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.SegmentVolume.AsymmetricMargin(marginA.AxisAlignedMargins).And(b.SegmentVolume.AsymmetricMargin(marginB.AxisAlignedMargins));
            return null;
        }

        public static SegmentVolume ContourUnion(Structure a, Structure b, StructureMarginModel marginA, StructureMarginModel marginB)
        {
            //margin is in cm
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.SegmentVolume.AsymmetricMargin(marginA.AxisAlignedMargins).Or(b.SegmentVolume.AsymmetricMargin(marginB.AxisAlignedMargins));
            return null;
        }

        public static SegmentVolume CropStructureFromStructure(Structure a, Structure b, StructureMarginModel marginA, StructureMarginModel marginB)
        {
            //margin is in cm
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.SegmentVolume.AsymmetricMargin(marginA.AxisAlignedMargins).Sub(b.SegmentVolume.AsymmetricMargin(marginB.AxisAlignedMargins));
            return null;
        }

        public static SegmentVolume ContourXOR(Structure a, Structure b, StructureMarginModel marginA, StructureMarginModel marginB)
        {
            //margin is in cm
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.SegmentVolume.AsymmetricMargin(marginA.AxisAlignedMargins).Xor(b.SegmentVolume.AsymmetricMargin(marginB.AxisAlignedMargins));
            return null;
        }

        public static SegmentVolume CutStructureInfToStructure(Structure a, Structure b)
        {
            int slice = CalculationHelper.ComputeSlice(b.MeshGeometry.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            for (int i = 0; i < slice; i++)
            {
                a.ClearAllContoursOnImagePlane(i);
            }
            return a.SegmentVolume;
        }

        public static SegmentVolume CutStructureSupToStructure(Structure a, Structure b)
        {
            int slice = CalculationHelper.ComputeSlice(b.MeshGeometry.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            for (int i = 0; i < slice; i++)
            {
                a.ClearAllContoursOnImagePlane(i);
            }
            return a.SegmentVolume;
        }

        /// <summary>
        /// Contour overlap between two structures ONTO the normal structure
        /// </summary>
        /// <param name="target"></param>
        /// <param name="normal"></param>
        /// <param name="marginInCm"></param>
        /// <returns></returns>
        //public static (bool, StringBuilder) ContourOverlap(Structure target, Structure normal, double marginInCm)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    bool fail = false;
        //    //margin is in cm
        //    if (!ReferenceEquals(target, null) && !ReferenceEquals(normal, null))
        //    {
        //        if (marginInCm >= -5.0 && marginInCm <= 5.0) normal.SegmentVolume = target.SegmentVolume.And(normal.SegmentVolume.Margin(marginInCm * 10));
        //        else
        //        {
        //            sb.AppendLine("Added margin MUST be within +/- 5.0 cm!");
        //            fail = true;
        //        }
        //    }
        //    else
        //    {
        //        sb.AppendLine("Error either target or normal structures are missing! Can't contour overlap between target and normal structure!");
        //        fail = true;
        //    }
        //    return (fail, sb);
        //}

        /// <summary>
        /// Helper method to combine/union a list of structures onto structureToUnion
        /// </summary>
        /// <param name="structuresToCombine"></param>
        /// <param name="structureToUnion"></param>
        /// <returns></returns>
        public static (bool, StringBuilder) ContourUnion(List<Structure> structuresToCombine, Structure structureToUnion, double marginInCm = 0.0)
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            foreach (Structure itr in structuresToCombine)
            {
                if (!ReferenceEquals(itr, null) && !ReferenceEquals(structureToUnion, null))
                {
                    structureToUnion.SegmentVolume = itr.SegmentVolume.Or(structureToUnion.SegmentVolume.Margin(marginInCm * 10));
                }
                else
                {
                    sb.AppendLine("Error either target or normal structures are missing! Can't union target and normal structure!");
                    fail = true;
                }
            }
            return (fail, sb);
        }

        /// <summary>
        /// Helper mthod to create a ring structure from the supplied target structure using specified margin and thickness values
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ring"></param>
        /// <param name="EclipseContext.GetInstance().StructureSet"></param>
        /// <param name="marginInCm"></param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        public static (bool, StringBuilder) CreateRing(Structure target, Structure ring, double marginInCm, double thickness)
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            //margin is in cm
            if ((marginInCm >= -5.0 && marginInCm <= 5.0) && (thickness + marginInCm >= -5.0 && thickness + marginInCm <= 5.0))
            {
                ring.SegmentVolume = target.Margin((thickness + marginInCm) * 10);
                ring.SegmentVolume = ring.Sub(target.Margin(marginInCm * 10));
                CropStructureFromBody(ring, 0.0);
            }
            else
            {
                sb.AppendLine("Added margin or ring thickness + margin MUST be within +/- 5.0 cm! Exiting!");
                fail = true;
            }
            return (fail, sb);
        }

        /// <summary>
        /// Helper method to contour a PRV volume from the supplied base structure id using specified margin
        /// </summary>
        /// <param name="baseStructureId"></param>
        /// <param name="PRVStructure"></param>
        /// <param name="EclipseContext.GetInstance().StructureSet"></param>
        /// <param name="marginInCm"></param>
        /// <returns></returns>
        public static (bool, StringBuilder) ContourPRVVolume(string baseStructureId, Structure PRVStructure, double marginInCm)
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            Structure baseStructure = StructureTuningHelper.GetStructureFromId(baseStructureId);
            if (!ReferenceEquals(baseStructure, null))
            {
                if (marginInCm >= -5.0 && marginInCm <= 5.0) PRVStructure.SegmentVolume = baseStructure.SegmentVolume.Margin(marginInCm * 10);
                else
                {
                    sb.AppendLine($"Error! Requested PRV margin ({marginInCm:0.0} cm) is outside +/- 5 cm! Exiting!");
                    fail = true;
                }
            }
            else
            {
                sb.AppendLine($"Error! Cannot find base structure: {baseStructureId}! Exiting!");
                fail = true;
            }
            return (fail, sb);
        }

        /// <summary>
        /// Helper method to copy the supplied base structure ONTO the structureToContour
        /// </summary>
        /// <param name="baseStructure"></param>
        /// <param name="structureToContour"></param>
        /// <returns></returns>
        public static (bool, StringBuilder) CopyStructureOntoStructure(Structure baseStructure, Structure structureToContour, double marginInCM = 0.0)
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            try
            {
                structureToContour.SegmentVolume = baseStructure.SegmentVolume.Margin(marginInCM * 10);
            }
            catch (Exception e)
            {
                sb.AppendLine($"Error! Could not copy {baseStructure.Id} onto {structureToContour.Id} because:");
                sb.AppendLine(e.Message);
            }
            return (fail, sb);
        }

        /// <summary>
        /// Helper method to contour the overlap between two structures and union it with the unionStructure ONTO the unionStructure
        /// </summary>
        /// <param name="target"></param>
        /// <param name="normal"></param>
        /// <param name="unionStructure"></param>
        /// <param name="EclipseContext.GetInstance().StructureSet"></param>
        /// <param name="marginInCm"></param>
        /// <returns></returns>
        public static (bool, StringBuilder) ContourOverlapAndUnion(Structure target, Structure normal, Structure unionStructure, double marginInCm)
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            //margin is in cm
            if (!ReferenceEquals(target, null) && !ReferenceEquals(normal, null))
            {
                if (marginInCm >= -5.0 && marginInCm <= 5.0)
                {
                    Structure dummy = EclipseContext.GetInstance().StructureSet.AddStructure("CONTROL", "Dummy");
                    dummy.SegmentVolume = target.And(normal.Margin(marginInCm * 10));
                    unionStructure.SegmentVolume = unionStructure.SegmentVolume.Or(dummy.SegmentVolume.Margin(0.0));
                    EclipseContext.GetInstance().StructureSet.RemoveStructure(dummy);
                }
                else
                {
                    sb.AppendLine("Added margin MUST be within +/- 5.0 cm!");
                    fail = true;
                }
            }
            else
            {
                sb.AppendLine("Error either target or normal structures are missing! Can't contour overlap between target and normal structure!");
                fail = true;
            }
            return (fail, sb);
        }

        /// <summary>
        /// Helper method to generate a margin for a structure on the given CT slice using the supplied contour points at the specified distance
        /// </summary>
        /// <param name="points"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static VVector[] GenerateContourPoints(VVector[] points, double distance)
        {
            VVector[] newPoints = new VVector[points.GetLength(0)];
            double centerX = (points.Max(p => p.x) + points.Min(p => p.x)) / 2;
            double centerY = (points.Max(p => p.y) + points.Min(p => p.y)) / 2;
            for (int i = 0; i < points.GetLength(0); i++)
            {
                double r = Math.Sqrt(Math.Pow(points[i].x - centerX, 2) + Math.Pow(points[i].y - centerY, 2));
                VVector u = new VVector((points[i].x - centerX) / r, (points[i].y - centerY) / r, 0);
                newPoints[i] = new VVector(u.x * (r + distance) + centerX, u.y * (r + distance) + centerY, 0);
                //ProvideUIUpdate($"{points[i][j].x - centerX:0.00}, {points[i][j].y - centerY:0.00}, {r:0.00}, {u.x:0.00}, {u.y:0.00}, {centerX:0.00}, {centerY:0.00}");
            }
            return newPoints;
        }

        /// <summary>
        /// Get the maximum lateral project of the supplied structure in the anterior-posterior and lateral directions at the given isocenter location
        /// </summary>
        /// <param name="target"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static (double, StringBuilder) GetMaxLatProjectionDistance(Structure target, VVector isoPos)
        {
            StringBuilder sb = new StringBuilder();
            double maxDimension = 0;
            Point3DCollection pts = target.MeshGeometry.Positions;
            if (Math.Abs(pts.Max(p => p.X) - isoPos.x) > maxDimension) maxDimension = Math.Abs(pts.Max(p => p.X) - isoPos.x);
            if (Math.Abs(pts.Min(p => p.X) - isoPos.x) > maxDimension) maxDimension = Math.Abs(pts.Min(p => p.X) - isoPos.x);
            if (Math.Abs(pts.Max(p => p.Y) - isoPos.y) > maxDimension) maxDimension = Math.Abs(pts.Max(p => p.Y) - isoPos.y);
            if (Math.Abs(pts.Min(p => p.Y) - isoPos.y) > maxDimension) maxDimension = Math.Abs(pts.Min(p => p.Y) - isoPos.y);
            sb.AppendLine($"Iso position: ({isoPos.x:0.0}, {isoPos.y:0.0}, {isoPos.z:0.0}) mm");
            sb.AppendLine($"Max lateral dimension: {maxDimension:0.0} mm");
            return (maxDimension, sb);
        }

        /// <summary>
        /// Get the maximum lateral projection of a structure in the anterior-posterior and lateral directions using the supplied bounding box for the structure at the given isocenter location
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static (double, StringBuilder) GetMaxLatProjectionDistance(VVector[] boundingBox, VVector isoPos)
        {
            StringBuilder sb = new StringBuilder();
            double maxDimension = 0;
            if (Math.Abs(boundingBox.Max(p => p.x) - isoPos.x) > maxDimension) maxDimension = Math.Abs(boundingBox.Max(p => p.x) - isoPos.x);
            if (Math.Abs(boundingBox.Min(p => p.x) - isoPos.x) > maxDimension) maxDimension = Math.Abs(boundingBox.Min(p => p.x) - isoPos.x);
            if (Math.Abs(boundingBox.Max(p => p.y) - isoPos.y) > maxDimension) maxDimension = Math.Abs(boundingBox.Max(p => p.y) - isoPos.y);
            if (Math.Abs(boundingBox.Min(p => p.y) - isoPos.y) > maxDimension) maxDimension = Math.Abs(boundingBox.Min(p => p.y) - isoPos.y);
            sb.AppendLine($"Iso position: ({isoPos.x:0.0}, {isoPos.y:0.0}, {isoPos.z:0.0}) mm");
            sb.AppendLine($"Max lateral dimension: {maxDimension:0.0} mm");
            return (maxDimension, sb);
        }

        /// <summary>
        /// Create a 2D bounding box for the specified target in the ant-post and lateral directions with added margin (in cm)
        /// </summary>
        /// <param name="theStructure"></param>
        /// <param name="addedMargin"></param>
        /// <returns></returns>
        public static (VVector[], StringBuilder) GetLateralBoundingBoxForStructure(Structure theStructure, double addedMargin = 0.0)
        {
            StringBuilder sb = new StringBuilder();
            VVector[] boundingBox;

            Point3DCollection pts = theStructure.MeshGeometry.Positions;
            double xMax = pts.Max(p => p.X) + addedMargin * 10;
            double xMin = pts.Min(p => p.X) - addedMargin * 10;
            double yMax = pts.Max(p => p.Y) + addedMargin * 10;
            double yMin = pts.Min(p => p.Y) - addedMargin * 10;

            sb.AppendLine($"Lateral bounding box for structure: {theStructure.Id}");
            sb.AppendLine($"Added margin: {addedMargin} cm");
            sb.AppendLine($" xMax: {xMax}");
            sb.AppendLine($" xMin: {xMin}");
            sb.AppendLine($" yMax: {yMax}");
            sb.AppendLine($" yMin: {yMin}");

            boundingBox = new[] {
                                new VVector(xMax, yMax, 0),
                                new VVector(xMax, 0, 0),
                                new VVector(xMax, yMin, 0),
                                new VVector(0, yMin, 0),
                                new VVector(xMin, yMin, 0),
                                new VVector(xMin, 0, 0),
                                new VVector(xMin, yMax, 0),
                                new VVector(0, yMax, 0)};

            return (boundingBox, sb);
        }

        /// <summary>
        /// Helper method to check if the supplied list of structures exist and are high resolution
        /// </summary>
        /// <param name="baseTargets"></param>
        /// <returns></returns>
        public static bool CheckHighResolutionAndConvert(List<string> structures, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, "Checking for high res structures:");
            foreach (string itr in structures)
            {
                if (StructureTuningHelper.DoesStructureExistInSS(itr, true))
                {
                    Structure tmp = StructureTuningHelper.GetStructureFromId(itr);
                    ProvideUIUpdate(0, $"Checking if {tmp.Id} is high resolution");
                    if (tmp.IsHighResolution)
                    {
                        string id = tmp.Id;
                        ProvideUIUpdate(0, $"{id} is high resolution. Converting to default resolution now");

                        if (OverWriteHighResStructureWithLowResStructure(tmp, ProvideUIUpdate))
                        {
                            ProvideUIUpdate(0, $"Error! Unable to overwrite existing high res structure {tmp.Id} with default resolution structure! Exiting!", true);
                            return true;
                        }
                        ProvideUIUpdate(0, $"{id} has been converted to low resolution");
                    }
                    else
                    {
                        ProvideUIUpdate(0, $"{tmp.Id} is already defualt resolution");
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Overloaded method to account for the fact the Eclipse may or may not append numbers to the structure names (because it thinks the names are taken)
        /// </summary>
        /// <param name="structures"></param>
        /// <param name="EclipseContext.GetInstance().StructureSet"></param>
        /// <param name="ProvideUIUpdate"></param>
        /// <returns></returns>
        public static bool CheckHighResolutionAndConvert(List<Structure> structures, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, "Checking for high res structures:");
            foreach (Structure itr in structures)
            {
                ProvideUIUpdate(0, $"Checking if {itr.Id} is high resolution");
                if (itr.IsHighResolution)
                {
                    string id = itr.Id;
                    ProvideUIUpdate(0, $"{id} is high resolution. Converting to default resolution now");

                    if (OverWriteHighResStructureWithLowResStructure(itr, ProvideUIUpdate))
                    {
                        ProvideUIUpdate(0, $"Error! Unable to overwrite existing high res structure {itr.Id} with default resolution structure! Exiting!", true);
                        return true;
                    }
                    ProvideUIUpdate(0, $"{id} has been converted to low resolution");
                }
                else
                {
                    ProvideUIUpdate(0, $"{itr.Id} is already defualt resolution");
                }
            }
            return false;
        }

        /// <summary>
        /// Method to take a high resolution structure as input and overwrite it with a new structure that is default resolution
        /// </summary>
        /// <param name="theStructure"></param>
        /// <returns></returns>
        private static bool OverWriteHighResStructureWithLowResStructure(Structure theStructure, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, $"Retrieving all contour points for: {theStructure.Id}");
            int startSlice = CalculationHelper.ComputeSlice(theStructure.MeshGeometry.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            int stopSlice = CalculationHelper.ComputeSlice(theStructure.MeshGeometry.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            ProvideUIUpdate(0, $"Start slice: {startSlice}");
            ProvideUIUpdate(0, $"Stop slice: {stopSlice}");
            VVector[][][] structurePoints = GetAllContourPoints(theStructure, startSlice, stopSlice, ProvideUIUpdate);
            ProvideUIUpdate(0, $"Contour points for: {theStructure.Id} loaded");

            ProvideUIUpdate(0, $"Removing and re-adding {theStructure.Id} to structure set");
            (bool fail, Structure lowResStructure) = RemoveAndReAddStructure(theStructure, ProvideUIUpdate);
            if (fail) return true;

            ProvideUIUpdate(0, $"Contouring {lowResStructure.Id} now");
            ContourLowResStructure(structurePoints, lowResStructure, startSlice, stopSlice, ProvideUIUpdate);
            return false;
        }

        /// <summary>
        /// Helper method to remove the supplied high resolution structure, then add a new structure with the same id as the high resolution 
        /// structure (automatically defaults to default resolution)
        /// </summary>
        /// <param name="theStructure"></param>
        /// <returns></returns>
        private static (bool, Structure) RemoveAndReAddStructure(Structure theStructure, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, "Removing and re-adding structure:");
            Structure newStructure = null;
            string id = theStructure.Id;
            string dicomType = theStructure.DicomType;
            if (EclipseContext.GetInstance().StructureSet.CanRemoveStructure(theStructure))
            {
                EclipseContext.GetInstance().StructureSet.RemoveStructure(theStructure);
                if (EclipseContext.GetInstance().StructureSet.CanAddStructure(dicomType, id))
                {
                    newStructure = EclipseContext.GetInstance().StructureSet.AddStructure(dicomType, id);
                    ProvideUIUpdate(0, $"{newStructure.Id} has been added to the structure set");
                }
                else
                {
                    ProvideUIUpdate(0, $"Could not re-add structure: {id}. Exiting", true);
                    return (true, newStructure);
                }
            }
            else
            {
                ProvideUIUpdate(0, $"Could not remove structure: {id}. Exiting", true);
                return (true, newStructure);
            }
            return (false, newStructure);
        }

        /// <summary>
        /// Similar to the contourlowresstructure method in generatetsbase, except instead of supplying the high res structure as an
        /// input argument, the contour points for the high res structure are directly supplied
        /// </summary>
        /// <param name="structurePoints"></param>
        /// <param name="lowResStructure"></param>
        /// <param name="startSlice"></param>
        /// <param name="stopSlice"></param>
        /// <returns></returns>
        private static bool ContourLowResStructure(VVector[][][] structurePoints, Structure lowResStructure, int startSlice, int stopSlice, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, $"Contouring {lowResStructure.Id}:");
            //Write the high res contour points on the newly added low res structure
            int percentComplete = 0;
            int calcItems = stopSlice - startSlice + 1;
            for (int slice = startSlice; slice <= stopSlice; slice++)
            {
                VVector[][] points = structurePoints[percentComplete];
                for (int i = 0; i < points.GetLength(0); i++)
                {
                    if (lowResStructure.IsPointInsideSegment(points[i][0]) ||
                        lowResStructure.IsPointInsideSegment(points[i][points[i].GetLength(0) - 1]) ||
                        lowResStructure.IsPointInsideSegment(points[i][(int)(points[i].GetLength(0) / 2)]))
                    {
                        lowResStructure.SubtractContourOnImagePlane(points[i], slice);
                    }
                    else lowResStructure.AddContourOnImagePlane(points[i], slice);
                }
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
            }
            return false;
        }

        /// <summary>
        /// Helper method to retrive the contour points for the supplied structure on all contoured CT slices
        /// </summary>
        /// <param name="theStructure"></param>
        /// <param name="startSlice"></param>
        /// <param name="stopSlice"></param>
        /// <returns></returns>
        public static VVector[][][] GetAllContourPoints(Structure theStructure, int startSlice, int stopSlice, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            int percentComplete = 0;
            int calcItems = stopSlice - startSlice + 1;
            VVector[][][] structurePoints = new VVector[stopSlice - startSlice + 1][][];
            for (int slice = startSlice; slice <= stopSlice; slice++)
            {
                structurePoints[percentComplete++] = theStructure.GetContoursOnImagePlane(slice);
                ProvideUIUpdate(100 * percentComplete / calcItems);
            }
            return structurePoints;
        }
    }
}
