using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AADWebApp.Interfaces;
using AADWebApp.Models;

namespace AADWebApp.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        public enum PrescriptionStatus
        {
            PendingApproval,
            AwaitingBloodWork,
            Approved,
            Declined,
            Terminated
        }

        public enum IssueFrequency
        {
            Minutely, // FOR DEMONSTRATION PURPOSES ONLY
            Weekly,
            BiWeekly,
            Monthly,
            BiMonthly
        }

        private readonly IDatabaseService _databaseService;
        private readonly INotificationScheduleService _notificationScheduleService;
        private readonly INotificationService _notificationService;
        private readonly IPrescriptionCollectionService _prescriptionCollectionService;
        private readonly IBloodTestService _bloodTestService;

        public PrescriptionService(IDatabaseService databaseService, INotificationScheduleService notificationScheduleService, INotificationService notificationService, IPrescriptionCollectionService prescriptionCollectionService, IBloodTestService bloodTestService)
        {
            _databaseService = databaseService;
            _notificationScheduleService = notificationScheduleService;
            _notificationService = notificationService;
            _prescriptionCollectionService = prescriptionCollectionService;
            _bloodTestService = bloodTestService;
        }

        public IEnumerable<Prescription> GetPrescriptions(short? id = null)
        {
            var prescriptions = new List<Prescription>();

            _databaseService.ConnectToMssqlServer(DatabaseService.AvailableDatabases.ProgramData);

            //GET prescriptions TABLE
            using var result = _databaseService.RetrieveTable("Prescriptions", "Id", id);

            while (result.Read())
            {
                prescriptions.Add(new Prescription
                {
                    Id = (short) result.GetValue(0),
                    MedicationId = (short) result.GetValue(1),
                    PatientId = (string) result.GetValue(2),
                    Dosage = (short) result.GetValue(3),
                    DateStart = (DateTime) result.GetValue(4),
                    DateEnd = (DateTime) result.GetValue(5),
                    PrescriptionStatus = (PrescriptionStatus) Enum.Parse(typeof(PrescriptionStatus), result.GetValue(6).ToString() ?? throw new InvalidOperationException()),
                    IssueFrequency = (IssueFrequency) Enum.Parse(typeof(IssueFrequency), result.GetValue(7).ToString() ?? throw new InvalidOperationException())
                });
            }

            return prescriptions.AsEnumerable();
        }

        public IEnumerable<Prescription> GetPrescriptionsByPatientId(string? id = null)
        {
            var prescriptions = new List<Prescription>();

            _databaseService.ConnectToMssqlServer(DatabaseService.AvailableDatabases.ProgramData);

            //GET prescriptions TABLE
            using var result = _databaseService.RetrieveTable("Prescriptions", "PatientId", id);

            while (result.Read())
            {
                prescriptions.Add(new Prescription
                {
                    Id = (short) result.GetValue(0),
                    MedicationId = (short) result.GetValue(1),
                    PatientId = (string) result.GetValue(2),
                    Dosage = (short) result.GetValue(3),
                    DateStart = (DateTime) result.GetValue(4),
                    DateEnd = (DateTime) result.GetValue(5),
                    PrescriptionStatus = (PrescriptionStatus) Enum.Parse(typeof(PrescriptionStatus), result.GetValue(6).ToString() ?? throw new InvalidOperationException()),
                    IssueFrequency = (IssueFrequency) Enum.Parse(typeof(IssueFrequency), result.GetValue(7).ToString() ?? throw new InvalidOperationException())
                });
            }

            return prescriptions.AsEnumerable();
        }

        public int CreatePrescription(int medicationId, string patientId, int dosage, DateTime dateStart, DateTime dateEnd, PrescriptionStatus prescriptionStatus, IssueFrequency issueFrequency)
        {
            if ((dateStart < dateEnd) && (prescriptionStatus != PrescriptionStatus.Approved)) // CHECK THAT dateEnd IS AFTER dateStart
            {
                _databaseService.ConnectToMssqlServer(DatabaseService.AvailableDatabases.ProgramData);

                //CREATE prescriptions TABLE ROW
                return _databaseService.ExecuteNonQuery($"INSERT INTO Prescriptions (MedicationId, PatientId, Dosage, DateStart, DateEnd, PrescriptionStatus, IssueFrequency) VALUES ('{medicationId}', '{patientId}', '{dosage}', '{dateStart:yyyy-MM-dd HH:mm:ss}', '{dateEnd:yyyy-MM-dd HH:mm:ss}', '{prescriptionStatus}', '{issueFrequency}')");
            }
            else
            {
                return 0;
            }
        }

        public async Task<int> CancelPrescriptionAsync(int id)
        {
            var prescription = GetPrescriptions((short?)id).ElementAt(0);

            if (prescription.PrescriptionStatus.ToString().Equals("Approved"))
            {
                _notificationScheduleService.CancelPrescriptionSchedule(id); // Cancel PrescriptionSchedule when prescription is cancelled and its has been approved
            }

            var result = _prescriptionCollectionService.GetPrescriptionCollectionsByPrescriptionId((short?)id);

            foreach (var item in result)
            {
                _prescriptionCollectionService.CancelPrescriptionCollection(item.Id); // Set any PrescriptionCollections to Cancelled
            }

            var bloodTestRequests = _bloodTestService.GetBloodTestRequests((short?) id);

            foreach (var bloodTestRequest in bloodTestRequests)
            {
                _bloodTestService.DeleteBloodTestRequest(bloodTestRequest.Id); // Delete bloodTestRequests for this prescription (would be better to set a status)
            }

            await _notificationService.SendCancellationNotification(prescription, DateTime.Now); // Send notifcation to patient

            return SetPrescriptionStatus(id, PrescriptionStatus.Terminated);
        }

        public int SetPrescriptionStatus(int id, PrescriptionStatus prescriptionStatus) // Setting an already approved prescription to 'Approved' will restart is presriptionSchedule!
        {
            _databaseService.ConnectToMssqlServer(DatabaseService.AvailableDatabases.ProgramData);

            if (prescriptionStatus.ToString().Equals("Approved"))
            {
                var prescription = GetPrescriptions((short?) id);

                _notificationScheduleService.CreatePrescriptionSchedule(prescription.ElementAt(0)); // Start PrescriptionSchedule when prescription is approved
            }

            //UPDATE prescriptions TABLE ROW prescription_status COLUMN
            return _databaseService.ExecuteNonQuery($"UPDATE Prescriptions SET PrescriptionStatus = '{prescriptionStatus}' WHERE Id = '{id}'");
        }
    }
}