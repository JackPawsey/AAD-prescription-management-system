﻿using System;
using System.Collections.Generic;
using AADWebApp.Models;

namespace AADWebApp.Interfaces
{
    public interface IBloodTestService
    {
        public IEnumerable<BloodTest> GetBloodTests(short? id = null);
        public IEnumerable<BloodTestResult> GetBloodTestResults(short? bloodTestRequestId = null);
        public IEnumerable<BloodTestRequest> GetBloodTestRequests(short? prescriptionId = null);
        public int RequestBloodTest(int prescriptionId, int bloodTestId, DateTime appointmentTime);
        public int SetBloodTestDateTime(int id, DateTime appointmentTime);
        public int SetBloodTestResults(int bloodRequestTestId, bool result, DateTime resultTime);
        public int DeleteBloodTestRequest(int id);
    }
}