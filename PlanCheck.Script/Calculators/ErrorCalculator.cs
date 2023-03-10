using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PlanCheck
{
    public class ErrorCalculator
    {
        public ObservableCollection<ErrorViewModel> Calculate(PlanningItemViewModel planningItem)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            errorGrid = GetPlanningItemErrors(planningItem);
            var structureSet = planningItem.StructureSet;
            var structureSetErrors = GetStructureSetErrors(structureSet);
            foreach (var structureSetError in structureSetErrors)
                errorGrid.Add(structureSetError);
            return new ObservableCollection<ErrorViewModel>(errorGrid.OrderBy(x => x.Status));
        }

        public void AddNewRow(string description, string status, int severity, ObservableCollection<ErrorViewModel> errorGrid)
        {
            var errorColumns = new ErrorViewModel
            {
                Description = description,
                Status = status,
                Severity = severity
            };
            errorGrid.Add(errorColumns);
        }

        public ObservableCollection<ErrorViewModel> GetStructureSetErrors(StructureSetViewModel structureSet)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            //Structure Checks
            if (structureSet != null)
            {
                string description = string.Empty;
                int severity;
                string status = string.Empty;
                foreach (var structure in structureSet.Structures)
                {
                    //CT value check
                    if (structure.Code == "NormalTissue")
                    {
                        if (structure.AssignedHU != 0)
                        {
                            description = string.Format("Structure {0} has an assigned CT value of {1}.", structure.Id, structure.AssignedHU);
                            severity = 1;
                            status = "3 - OK";
                            AddNewRow(description, status, severity, errorGrid);
                        }
                        else
                        {
                            description = string.Format("Structure {0} does not have an assigned CT value.", structure.Id);
                            severity = 1;
                            status = "1 - Warning";
                            AddNewRow(description, status, severity, errorGrid);
                        }
                    }
                }
                foreach (var structure in structureSet.Structures)
                {
                    //Couch check
                    if (structure.Id.Contains("CouchSurface") == true)
                    {
                        double lowerLimitHU = -650;
                        double upperLimitHU = -425;
                        if (structure.AssignedHU <= upperLimitHU || structure.AssignedHU >= lowerLimitHU)
                        {
                            description = string.Format("Structure {0} has assigned HU of {1} and is within limit of {2} to {3}.",
                                structure, structure.AssignedHU, upperLimitHU, lowerLimitHU);
                            severity = 1;
                            status = "3 - OK";
                            AddNewRow(description, status, severity, errorGrid);
                        }
                        else
                        {
                            description = string.Format("Structure {0} has assigned HU of {1} and is outside limit of {2} to {3}.",
                                structure, structure.AssignedHU, upperLimitHU, lowerLimitHU);
                            severity = 1;
                            status = "1 - Warning";
                            AddNewRow(description, status, severity, errorGrid);
                        }
                        break;
                    }
                }
            }
            return errorGrid;
        }

        public ObservableCollection<ErrorViewModel> GetPlanningItemErrors(PlanningItemViewModel planningItem)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            string error;
            string errorStatus;
            int errorSeverity;


            bool couchFound = false;
            bool rtLatSetupFound = false;
            bool ltLatSetupFound = false;
            bool paSetupFound = false;
            bool apSetupFound = false;
            bool cbctSetupFound = false;

            if (planningItem.StructureSet != null)
            {
                var imageCreationDateTime = planningItem.StructureSet.ImageCreationDateTime;
                //ct age check
                if ((planningItem.CreationDateTime - imageCreationDateTime).TotalDays > 21)
                {
                    error = string.Format("CT and structure data ({0}) is {1} days older than plan creation date ({2}) and outside of 21 days.",
                        imageCreationDateTime, (planningItem.CreationDateTime - imageCreationDateTime).TotalDays.ToString("0"), planningItem.CreationDateTime);
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                else
                {
                    error = string.Format("CT and structure data ({0}) is {1} days older than plan creation date ({2}) and within 21 days.",
                        imageCreationDateTime, (planningItem.CreationDateTime - imageCreationDateTime).TotalDays.ToString("0"), planningItem.CreationDateTime);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }

            if (planningItem.IsDoseValid)
            {
                //dose max value check
                if (planningItem.DoseMax3D >= 115)
                {
                    error = string.Format("Dose maximum is {0}.", planningItem.DoseMax3D.ToString("0.0") + " %");
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (planningItem.DoseMax3D >= 110 && planningItem.DoseMax3D < 115)
                {
                    error = string.Format("Dose maximum is {0}.", planningItem.DoseMax3D.ToString("0.0") + " %");
                    errorStatus = "2 - Variation";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (planningItem.DoseMax3D >= 100 && planningItem.DoseMax3D < 110)
                {
                    error = string.Format("Dose maximum {0}.", planningItem.DoseMax3D.ToString("0.0") + " %");
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }



            //Target Volume check, does it exist, probably good
            if (planningItem.TargetVolumeId == "")
            {
                error = string.Format("Plan {0} does not have a target volume assigned.", planningItem.Id);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Plan {0} has a target volume assigned.", planningItem.Id);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            //insert new tests

            if (planningItem.TargetVolumeId == planningItem.PrimaryReferencePoint.Id)
            {
                error = string.Format("Target Volume {0} matches Primary Reference Point Id {1}.", planningItem.TargetVolumeId, planningItem.PrimaryReferencePoint.Id);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Target Volume {0} does not match Primary Reference Point Id {1}.", planningItem.TargetVolumeId, planningItem.PrimaryReferencePoint.Id);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            //foreach (Beam b in planningItem.Beams)
            //{

            //    if (b.MLCPlanType.ToString() == "VMAT")
            //    {
            //        //double[] CPList = new double[b.ControlPoints.Count];
            //        int CPsWithDoseRateMaxed = 0;
            //        int CPsWithLargeSpeedChange = 0;
            //        double mu = b.Meterset.Value;
            //        double doserateSum = 0;
            //        double maxGantrySpeed = 0.8 * 360.0 / 60.0; //default value is 0.8 RPM. There may be an operating limit for this.
            //        for (int n = 1; n < b.ControlPoints.Count - 1; n++)
            //        {
            //            var cpPrev = b.ControlPoints[n - 1];
            //            var cp = b.ControlPoints[n];
            //            var cpNext = b.ControlPoints[n + 1];
            //            double deltaAngle = GetDeltaAngle(cpPrev.GantryAngle, cp.GantryAngle);
            //            double deltaAngleNext = GetDeltaAngle(cp.GantryAngle, cpNext.GantryAngle);
            //            double deltaMU = mu * (cp.MetersetWeight - cpPrev.MetersetWeight);
            //            double deltaMUNext = mu * (cpNext.MetersetWeight - cp.MetersetWeight);
            //            double segmentDeliveryTime = CalculateSDT(deltaAngle, deltaMU, maxGantrySpeed, b.DoseRate / 60.0);
            //            double segmentDeliveryTimeNext = CalculateSDT(deltaAngleNext, deltaMUNext, maxGantrySpeed, b.DoseRate / 60.0);
            //            double doserate = deltaMU / segmentDeliveryTime * 60.0;
            //            doserateSum += doserate;
            //            double gantrySpeed = deltaAngle / segmentDeliveryTime;
            //            double gantrySpeedNext = deltaAngleNext / segmentDeliveryTimeNext;
            //            double gantrySpeedDelta = gantrySpeedNext - gantrySpeed;
            //            double muPerDeg = deltaMU / deltaAngle;

            //            if (doserate == b.DoseRate)
            //                CPsWithDoseRateMaxed += 1;
            //            if (gantrySpeedDelta > 0.1)
            //                CPsWithLargeSpeedChange += 1;

            //        }
            //        double doserateAvg = doserateSum / b.ControlPoints.Count;
            //        if (CPsWithDoseRateMaxed > 1)
            //        {
            //            error = string.Format("Field {0} has {1} of {2} control points with max dose rate {3} MU/min.  Average dose rate is {4} MU/min.", b.Id, CPsWithDoseRateMaxed, b.ControlPoints.Count, b.DoseRate, doserateAvg.ToString("0.0"));
            //            errorStatus = "2 - Variation";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //        else
            //        {
            //            error = string.Format("Field {0} has {1} of {2} control points with max dose rate {3} MU/min.  Average doserate is {4} MU/min.", b.Id, CPsWithDoseRateMaxed, b.ControlPoints.Count, b.DoseRate, doserateAvg.ToString("0.0"));
            //            errorStatus = "3 - OK";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //        if (CPsWithLargeSpeedChange > 1)
            //        {
            //            error = string.Format("Field {0} has {1} of {2} control points with gantry speed change > 0.1 deg/s.", b.Id, CPsWithLargeSpeedChange, b.ControlPoints.Count);
            //            errorStatus = "2 - Variation";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //        else
            //        {
            //            error = string.Format("Field {0} has {1} of {2} control points with gantry speed change > 0.1 deg/s.", b.Id, CPsWithLargeSpeedChange, b.ControlPoints.Count);
            //            errorStatus = "3 - OK";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //    }

            //    double gantryAngle = b.ControlPoints.First().GantryAngle;
            //    double tableAngle = 0;
            //    bool subfield1Found = false;
            //    bool subfield2Found = false;
            //    bool subfield3Found = false;
            //    if (b.ControlPoints.First().PatientSupportAngle == 0)
            //        tableAngle = b.ControlPoints.First().PatientSupportAngle;
            //    else
            //        tableAngle = 360 - b.ControlPoints.First().PatientSupportAngle;
            //    string fieldId = b.Id;
            //    fieldId = fieldId.Replace(" ", "");


            //    if (b.ReferenceImage == null)
            //    {
            //        error = string.Format("Field {0} does not have a DRR.", b.Id);
            //        errorStatus = "2 - Variation";
            //        errorSeverity = 1;
            //        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //    }

            //    else
            //    {
            //        error = string.Format("Field {0} has a DRR.", b.Id);
            //        errorStatus = "3 - OK";
            //        errorSeverity = 1;
            //        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //    }
            //    if (b.IsSetupField == true)
            //    {
            //        if (b.ControlPoints.First().PatientSupportAngle != 0)
            //        {
            //            error = string.Format("Setup field {0} is not at couch = 0.", b.Id);
            //            errorStatus = "1 - Warning";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //        else
            //        {
            //            error = string.Format("Setup field {0} is at couch = 0.", b.Id);
            //            errorStatus = "3 - OK";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //        if (b.IsSetupField && (90 - (b.ControlPoints.First().GantryAngle) <= 0.1))
            //            ltLatSetupFound = true;
            //        if (b.IsSetupField && ((0 - (b.ControlPoints.First().GantryAngle) <= 0.1)))
            //            apSetupFound = true;
            //        if (b.IsSetupField && (180 - (b.ControlPoints.First().GantryAngle) <= 0.1))
            //            paSetupFound = true;
            //        if (b.IsSetupField && (270 - (b.ControlPoints.First().GantryAngle) <= 0.1))
            //            rtLatSetupFound = true;
            //        if (b.IsSetupField && (b.ControlPoints.First().GantryAngle == 0.0) && b.Id == "CBCT")
            //            cbctSetupFound = true;
            //    }
            //    else    //if field is a treatment field
            //    {
            //        if (b.Wedges.Any())
            //        {
            //            foreach (var wedge in b.Wedges)
            //            {
            //                string wedgeTypeString = wedge.GetType().Name;
            //                if (wedgeTypeString.Contains("EDW") == true && b.Meterset.Value > 20)
            //                {
            //                    if (b.Meterset.Value > 20)
            //                    {
            //                        error = string.Format("EDW field {0} is more than 20 MU and should be deliverable.", b.Id);
            //                        errorStatus = "3 - OK";
            //                        errorSeverity = 1;
            //                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //                    }
            //                    else
            //                    {
            //                        error = string.Format("EDW field {0} is LESS than 20 MU and should not be deliverable.", b.Id);
            //                        errorStatus = "1 - Warning";
            //                        errorSeverity = 1;
            //                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //                    }
            //                }
            //            }
            //        }

            //        if (b.MLCPlanType.ToString() == "VMAT")
            //        {
            //            if (b.ToleranceTableLabel.ToString() == "IMRT")
            //            {
            //                error = string.Format("Field {0} is VMAT and the tolerance table is IMRT.", b.Id);
            //                errorStatus = "3 - OK";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //            else
            //            {
            //                error = string.Format("Field {0} is VMAT but the tolerance table is not IMRT.", b.Id);
            //                errorStatus = "1 - Warning";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //        }
            //        if (b.MLCPlanType.ToString() == "DoseDynamic")
            //        {
            //            if (b.ToleranceTableLabel.ToString() == "IMRT")
            //            {
            //                error = string.Format("Field {0} is IMRT and the tolerance table is IMRT.", b.Id);
            //                errorStatus = "3 - OK";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //            else
            //            {
            //                error = string.Format("Field {0} is IMRT but the tolerance table is not IMRT.", b.Id);
            //                error = "1 - Warning";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //        }
            //        if (b.MLCPlanType.ToString() == "Static")
            //        {
            //            if (b.ToleranceTableLabel.ToString() == "T1")
            //            {
            //                error = string.Format("Field {0} is conformal/3D and the tolerance table is T1.", b.Id);
            //                errorStatus = "3 - OK";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //            else
            //            {
            //                error = string.Format("Field {0} is conformal/3D but the tolerance table is not T1.", b.Id);
            //                error = "1 - Warning";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //        }
            //        if (b.EnergyModeDisplayName.ToString().Contains("E") == true)
            //        {
            //            if (b.ToleranceTableLabel.ToString().Contains("Electron") == true)
            //            {
            //                error = string.Format("Field {0} is an electron field and the tolerance table is 'Electron'.", b.Id);
            //                errorStatus = "3 - OK";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }
            //            else
            //            {
            //                error = string.Format("Field {0} is an electron field but the tolerance table is not 'Electron'.", b.Id);
            //                errorStatus = "1 - Warning";
            //                errorSeverity = 1;
            //                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //            }

            //        }
            //        if (b.Technique.ToString().Contains("STATIC") && b.Meterset.Value > 1000)
            //        {
            //            error = string.Format("Field {0} is Static, but the MUs are more than 1000.", b.Id);
            //            errorStatus = "1 - Warning";
            //            errorSeverity = 1;
            //            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //        }
            //    }
            //}



            //if (rtLatSetupFound == false)
            //{
            //    error = "Setup field Rt Lat not found.";
            //    errorStatus = "2 - Variation";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //else
            //{
            //    error = "Setup field Rt Lat found.";
            //    errorStatus = "3 - OK";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //if (ltLatSetupFound == false)
            //{
            //    error = "Setup field Lt Lat not found.";
            //    errorStatus = "2 - Variation";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //else
            //{
            //    error = "Setup field Lt Lat found.";
            //    errorStatus = "3 - OK";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //if (apSetupFound == false)
            //{
            //    error = "Setup field AP not found.";
            //    errorStatus = "2 - Variation";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //else
            //{
            //    error = "Setup field AP found.";
            //    errorStatus = "3 - OK";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //if (paSetupFound == false)
            //{
            //    error = "Setup field PA not found.";
            //    errorStatus = "2 - Variation";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //else
            //{
            //    error = "Setup field PA found.";
            //    errorStatus = "3 - OK";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //if (cbctSetupFound == false)
            //{
            //    error = "Setup field CBCT not found.";
            //    errorStatus = "2 - Variation";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}
            //else
            //{
            //    error = "Setup field CBCT found.";
            //    errorStatus = "3 - OK";
            //    errorSeverity = 1;
            //    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            //}







            try
            {
                if (planningItem.Id.StartsWith("R ") || planningItem.Id.Contains("_R") || planningItem.Name.Contains("RUL") || planningItem.Id.Contains("RML") || planningItem.Id.Contains("RLL") || planningItem.Id.Contains("RT"))
                {
                    if (planningItem.TreatmentOrientation.ToString() == "HeadFirstSupine")
                    {
                        if (planningItem.Beams.First().IsocenterPosition.x < 0)
                        {
                            error = string.Format("Plan {0} has a right shift of {1} mm and the plan name is labeled Right.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Plan {0} has a right shift of {1} mm but the plan name is not labeled Right.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                    if (planningItem.TreatmentOrientation.ToString() == "HeadFirstProne" || planningItem.TreatmentOrientation.ToString() == "FeetFirstSupine")
                    {
                        if (planningItem.Beams.First().IsocenterPosition.x > 0)
                        {
                            error = string.Format("Plan {0} has a right shift of {1} mm and the plan name is labeled Right.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Plan {0} has a right shift of {1} mm but the plan name is not labeled Right.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                }
                if (planningItem.Id.StartsWith("L ") || planningItem.Id.Contains("_L") || planningItem.Id.Contains("LUL") || planningItem.Id.Contains("LML") || planningItem.Id.Contains("LLL") || planningItem.Id.Contains("LT"))
                {
                    if (planningItem.TreatmentOrientation.ToString() == "HeadFirstSupine")
                    {
                        if (planningItem.Beams.First().IsocenterPosition.x > 0)
                        {
                            error = string.Format("Plan {0} has a left shift of {1} mm and the plan name is labeled Left.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Plan {0} has a left shift of {1} mm but the plan name is not labeled Left.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                    if (planningItem.TreatmentOrientation.ToString() == "HeadFirstProne" || planningItem.TreatmentOrientation.ToString() == "FeetFirstSupine")
                    {
                        if (planningItem.Beams.First().IsocenterPosition.x < 0)
                        {
                            error = string.Format("Plan {0} has a left shift of {1} mm and the plan name is labeled Left.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "3 - OK";
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Plan {0} has a left shift of {1} mm but the plan name is not labeled Left.", planningItem.Id, planningItem.Beams.First().IsocenterPosition.x.ToString("0.0"));
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                }
            }
            catch
            {

            }










            return errorGrid;
        }







        //math
        double GetDeltaAngle(double a1, double a2)
        {
            double diff = Math.Abs(a1 - a2);
            return diff > 180 ? 360 - diff : diff;
        }

        double CalculateSDT(double deltaAngle, double deltaMU, double maxGantrySpeed, double maxDoseRate)
        {
            double rotationTimeUsingMaxSpeed = deltaAngle / maxGantrySpeed;
            double deliveryTimeUsingMaxDoseRate = deltaMU / maxDoseRate;

            return Math.Max(deliveryTimeUsingMaxDoseRate, rotationTimeUsingMaxSpeed);
        }

    }
}
