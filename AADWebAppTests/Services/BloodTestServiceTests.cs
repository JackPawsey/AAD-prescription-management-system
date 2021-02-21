﻿using System;
using System.Collections.Generic;
using System.Linq;
using AADWebApp.Interfaces;
using AADWebApp.Models;
using AADWebApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using static AADWebApp.Services.DatabaseService;

namespace AADWebAppTests.Services
{
    [TestClass]
    public class BloodTestServiceTests : TestBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IBloodTestService _bloodTestService;

        public BloodTestServiceTests()
        {
            _databaseService = Get<IDatabaseService>();
            _bloodTestService = new BloodTestService(_databaseService);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _databaseService.ConnectToMssqlServer(AvailableDatabases.program_data);

            // Populate a prescription for some tests, which means also creating a user due to the table constraints
            _databaseService.ExecuteNonQuery($"INSERT INTO patients (id, comm_preferences, nhs_number, general_practitioner) VALUES (1, 1, 1, 'gp-name');");
            _databaseService.ExecuteNonQuery($"INSERT INTO prescriptions (medication_id, patient_id, dosage, date_start, date_end, prescription_status, issue_frequency) VALUES (1, 1, 1, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', 'Approved', 'Weekly');");
            _databaseService.ExecuteNonQuery($"INSERT INTO prescriptions (medication_id, patient_id, dosage, date_start, date_end, prescription_status, issue_frequency) VALUES (1, 1, 1, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', 'Approved', 'Weekly');");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseService.ConnectToMssqlServer(AvailableDatabases.program_data);

            // Clear the tables
            _databaseService.ExecuteNonQuery($"DELETE FROM blood_test_results");
            _databaseService.ExecuteNonQuery($"DELETE FROM blood_test_requests");
            _databaseService.ExecuteNonQuery($"DELETE FROM prescriptions;");
            _databaseService.ExecuteNonQuery($"DELETE FROM patients;");

            // Reset auto increment values for next test
            _databaseService.ExecuteNonQuery($"DBCC CHECKIDENT (blood_test_results, RESEED, 0);");
            _databaseService.ExecuteNonQuery($"DBCC CHECKIDENT (blood_test_requests, RESEED, 0);");
            _databaseService.ExecuteNonQuery($"DBCC CHECKIDENT (prescriptions, RESEED, 0);");
        }

        [TestMethod]
        public void WhenThereAreBloodTests()
        {
            var results = _bloodTestService.GetBloodTests();

            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void WhenGettingBloodTestByAValidId()
        {
            // Prep Expected
            IEnumerable<BloodTest> expected = new List<BloodTest>
            {
                new BloodTest
                {
                    Id = 6,
                    AbbreviatedTitle = "FBC",
                    FullTitle = "Full Blood Count",
                    RestrictionLevel = 1
                }
            };

            var expectedSerialised = JsonConvert.SerializeObject(expected);

            // GetBloodTests with id 6
            var results = _bloodTestService.GetBloodTests(6);

            // Check results
            var resultSerialised = JsonConvert.SerializeObject(results);

            Assert.IsTrue(results.Count() == 1);
            Assert.AreEqual(expectedSerialised, resultSerialised);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(1000)]
        public void WhenGettingBloodTestByAnInvalidId(int id)
        {
            var results = _bloodTestService.GetBloodTests((short) id);

            Assert.IsTrue(!results.Any());
        }

        // GetBloodTestResults test methods
        [TestMethod]
        public void WhenThereAreNoBloodTestResults()
        {
            var results = _bloodTestService.GetBloodTestResults();

            Assert.IsTrue(!results.Any());
        }

        // GetBloodTestRequests test methods
        [TestMethod]
        public void WhenThereAreNoBloodTestRequests()
        {
            var results = _bloodTestService.GetBloodTestRequests();

            Assert.IsTrue(!results.Any());
        }

        // RequestBloodTest test method
        [TestMethod]
        public void WhenRequestingBloodTestsRowsAreAdded()
        {
            // Check prior to any setup - via the database
            var preSetupRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_requests");
            Assert.IsTrue(preSetupRows == 0);

            var beforeRequestResults = _bloodTestService.GetBloodTestRequests().ToList();
            Assert.IsTrue(!beforeRequestResults.Any());

            var now = DateTime.Now;

            // Prep expected
            IEnumerable<BloodTestRequest> allExpected = new List<BloodTestRequest>
            {
                new BloodTestRequest
                {
                    Id = 1,
                    PrescriptionId = 1,
                    BloodTestId = 1,
                    AppointmentTime = now
                },
                new BloodTestRequest
                {
                    Id = 2,
                    PrescriptionId = 2,
                    BloodTestId = 3,
                    AppointmentTime = now
                },
                new BloodTestRequest
                {
                    Id = 3,
                    PrescriptionId = 1,
                    BloodTestId = 1,
                    AppointmentTime = now
                }
            };

            IEnumerable<BloodTestRequest> expected1 = new List<BloodTestRequest>
            {
                new BloodTestRequest
                {
                    Id = 1,
                    PrescriptionId = 1,
                    BloodTestId = 1,
                    AppointmentTime = now
                },
                new BloodTestRequest
                {
                    Id = 3,
                    PrescriptionId = 1,
                    BloodTestId = 1,
                    AppointmentTime = now
                }
            };

            IEnumerable<BloodTestRequest> expected2 = new List<BloodTestRequest>
            {
                new BloodTestRequest
                {
                    Id = 2,
                    PrescriptionId = 2,
                    BloodTestId = 3,
                    AppointmentTime = now
                }
            };

            var allExpectedSerialised = JsonConvert.SerializeObject(allExpected, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            var expected1Serialised = JsonConvert.SerializeObject(expected1, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            var expected2Serialised = JsonConvert.SerializeObject(expected2, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            // Request blood tests
            var affectedRows1 = _bloodTestService.RequestBloodTest(1, 1, now);
            Assert.AreEqual(1, affectedRows1);

            var affectedRows2 = _bloodTestService.RequestBloodTest(2, 3, now);
            Assert.AreEqual(1, affectedRows2);

            var affectedRows3 = _bloodTestService.RequestBloodTest(1, 1, now);
            Assert.AreEqual(1, affectedRows3);

            // Check amount of database rows
            var databaseRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_requests");
            Assert.IsTrue(databaseRows == 3);

            // Check results - GetBloodTestRequests with no id
            var afterRequestResults = _bloodTestService.GetBloodTestRequests();
            var afterRequestResultSerialised = JsonConvert.SerializeObject(afterRequestResults);

            Assert.IsTrue(afterRequestResults.Count() == 3);
            Assert.AreEqual(allExpectedSerialised, afterRequestResultSerialised);

            // Check results - GetBloodTestRequests with id expecting 2 results
            var afterRequestResultsById1 = _bloodTestService.GetBloodTestRequests(1);
            var afterRequestResultsById1Serialised = JsonConvert.SerializeObject(afterRequestResultsById1);

            Assert.IsTrue(afterRequestResultsById1.Count() == 2);
            Assert.AreEqual(expected1Serialised, afterRequestResultsById1Serialised);

            // Check results - GetBloodTestRequests with id expecting 1 result
            var afterRequestResultsById2 = _bloodTestService.GetBloodTestRequests(2);
            var afterRequestResultsById2Serialised = JsonConvert.SerializeObject(afterRequestResultsById2);

            Assert.IsTrue(afterRequestResultsById2.Count() == 1);
            Assert.AreEqual(expected2Serialised, afterRequestResultsById2Serialised);
        }

        // SetBloodTestDateTime test method
        [TestMethod]
        public void WhenSettingsAnAppointmentTimeTheRowIsUpdated()
        {
            // Check prior to any setup - via the database
            var preSetupRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_requests");
            Assert.IsTrue(preSetupRows == 0);

            // Prerequisite setup
            var initialTime = DateTime.Now;
            var updatedTime = initialTime.AddDays(1);

            _bloodTestService.RequestBloodTest(1, 1, initialTime);

            // Begin actual test
            // Prep expected
            IEnumerable<BloodTestRequest> expectBeforeUpdate = new List<BloodTestRequest>
            {
                new BloodTestRequest
                {
                    Id = 1,
                    PrescriptionId = 1,
                    BloodTestId = 1,
                    AppointmentTime = initialTime
                }
            };

            var beforeExpectedSerialised = JsonConvert.SerializeObject(expectBeforeUpdate, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            IEnumerable<BloodTestRequest> expectedAfterUpdate = new List<BloodTestRequest>
            {
                new BloodTestRequest
                {
                    Id = 1,
                    PrescriptionId = 1,
                    BloodTestId = 1,
                    AppointmentTime = updatedTime
                }
            };

            var afterExpectedSerialised = JsonConvert.SerializeObject(expectedAfterUpdate, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            // Check prior to adding - via the database
            var priorAddingDatabaseRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_requests");
            Assert.IsTrue(priorAddingDatabaseRows == 1);

            // Check prior to adding
            var beforeUpdateResults = _bloodTestService.GetBloodTestRequests();
            var beforeUpdateResultSerialised = JsonConvert.SerializeObject(beforeUpdateResults);

            Assert.IsTrue(beforeUpdateResults.Count() == 1);
            Assert.AreEqual(beforeExpectedSerialised, beforeUpdateResultSerialised);
            Assert.AreEqual(initialTime, expectBeforeUpdate.First().AppointmentTime);

            // Update time
            var affectedRows = _bloodTestService.SetBloodTestDateTime(1, updatedTime);
            Assert.AreEqual(1, affectedRows);

            // Check results to adding - via the database
            var afterUpdateDatabaseRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_requests");
            Assert.IsTrue(afterUpdateDatabaseRows == 1);

            // Check results - GetBloodTestRequests with no id
            var afterUpdateResults = _bloodTestService.GetBloodTestRequests();
            var afterUpdateResultSerialised = JsonConvert.SerializeObject(afterUpdateResults);

            Assert.IsTrue(afterUpdateResults.Count() == 1);
            Assert.AreEqual(afterExpectedSerialised, afterUpdateResultSerialised);
            Assert.AreEqual(updatedTime, expectedAfterUpdate.First().AppointmentTime);

            // Check results - GetBloodTestRequests with id
            var afterUpdateResultsById = _bloodTestService.GetBloodTestRequests(1);
            var afterUpdateResultByIdSerialised = JsonConvert.SerializeObject(afterUpdateResultsById);

            Assert.IsTrue(afterUpdateResultsById.Count() == 1);
            Assert.AreEqual(afterExpectedSerialised, afterUpdateResultByIdSerialised);
            Assert.AreEqual(updatedTime, expectedAfterUpdate.First().AppointmentTime);
        }

        // SetBloodTestResults test method
        [TestMethod]
        public void WhenAddingBloodTestResultsRowsAreAdded()
        {
            // Check prior to any setup - via the database
            var preSetupRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_results");
            Assert.IsTrue(preSetupRows == 0);

            // Prerequisite setup
            var now = DateTime.Now;

            _bloodTestService.RequestBloodTest(1, 1, now);
            _bloodTestService.RequestBloodTest(1, 2, now);

            // Begin actual test
            var beforeResultResults = _bloodTestService.GetBloodTestResults().ToList();
            Assert.IsTrue(!beforeResultResults.Any());

            // Prep expected
            IEnumerable<BloodTestResult> allExpected = new List<BloodTestResult>
            {
                new BloodTestResult
                {
                    Id = 1,
                    BloodTestId = 1,
                    Result = false,
                    ResultTime = now
                },
                new BloodTestResult
                {
                    Id = 2,
                    BloodTestId = 2,
                    Result = true,
                    ResultTime = now
                }
            };

            IEnumerable<BloodTestResult> expected1 = new List<BloodTestResult>
            {
                new BloodTestResult
                {
                    Id = 2,
                    BloodTestId = 2,
                    Result = true,
                    ResultTime = now
                }
            };

            var allExpectedSerialised = JsonConvert.SerializeObject(allExpected, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            var expected1Serialised = JsonConvert.SerializeObject(expected1, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            });

            // Check prior to adding blood tests - via the database
            var beforeAnyResultsDatabaseRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_results");
            Assert.IsTrue(beforeAnyResultsDatabaseRows == 0);

            // Set blood test results
            var affectedRows1 = _bloodTestService.SetBloodTestResults(1, false, now);
            Assert.AreEqual(1, affectedRows1);

            var affectedRows2 = _bloodTestService.SetBloodTestResults(2, true, now);
            Assert.AreEqual(1, affectedRows2);

            // Check prior to adding blood tests - via the database
            var afterAddingResultsDatabaseRows = _databaseService.ExecuteScalarQuery("SELECT COUNT(*) FROM blood_test_results");
            Assert.IsTrue(afterAddingResultsDatabaseRows == 2);

            // Check results - GetBloodTestResults with no id
            var afterResultResults = _bloodTestService.GetBloodTestResults();
            var afterResultResultsSerialised = JsonConvert.SerializeObject(afterResultResults);

            Assert.IsTrue(afterResultResults.Count() == 2);
            Assert.AreEqual(allExpectedSerialised, afterResultResultsSerialised);

            // Check results - GetBloodTestResults with id
            var afterResultResultsById = _bloodTestService.GetBloodTestResults(2);
            var afterResultResultsByIdSerialised = JsonConvert.SerializeObject(afterResultResultsById);

            Assert.IsTrue(afterResultResultsById.Count() == 1);
            Assert.AreEqual(expected1Serialised, afterResultResultsByIdSerialised);
        }
    }
}