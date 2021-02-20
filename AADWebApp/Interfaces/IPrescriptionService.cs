﻿using System;
using System.Collections.Generic;
using AADWebApp.Models;

namespace AADWebApp.Interfaces
{
    public interface IPrescriptionService
    {
        public IEnumerable<Prescription> GetPrescriptions();
        public string CreatePrescription(int patientId, string medicationType, int dosage, DateTime prescriptionEnd);
        public string CancelPrescription(int prescriptionId);
        public string SetPrescriptionMedication(int prescriptionId, int dosage);
        public string SetPrescriptionCollectionDateTime(int prescriptionId, DateTime dateTime);
        public string SetPrescriptionStatus(int prescriptionId, string status);
    }
}