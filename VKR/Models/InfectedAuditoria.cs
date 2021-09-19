using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VKR.Models
{
    public class InfectedAuditoria
    {
        public int AuditoriaId { get; set; }
        public DateTime StartInfectionDate { get; set; }
        public List<string> ZeroPatientsData { get; set; }

        public bool _isInfectedNow;

        public List<Lecturers> InfectLecturers { get; set; }
        public List<Groups> InfectGroups { get; set; }

        public InfectedAuditoria(int audId, DateTime dateStartInfection, string patentData)
        {
            AuditoriaId = audId;
            StartInfectionDate = dateStartInfection;
            ZeroPatientsData = new List<string>();
            ZeroPatientsData.Add(patentData);
            _isInfectedNow = true;
            InfectLecturers = new List<Lecturers>();
            InfectGroups = new List<Groups>();
        }

        public void AddInfectedPeopleToia(Lecturers lecturer, Groups group)
        {
            if (InfectLecturers.FirstOrDefault(x => x.LecturerId == lecturer.LecturerId) == null)
                InfectLecturers.Add(lecturer);
            if (InfectGroups.FirstOrDefault(x => x.GroupId == group.GroupId) == null)
                InfectGroups.Add(group);
        }

        public void EnterNewInfectionEvent(DateTime dateStartInfect, string patientData)
        {
            //если это новый оборот заражения - новый нулевой пациент
            if (!IsStillInfected(dateStartInfect))
            {
                ZeroPatientsData.Add(patientData);
            }
            StartInfectionDate = dateStartInfect;
            _isInfectedNow = true;
        }

        public void CheckIsStillInfected(DateTime curDate)
        {
            if (!IsStillInfected(curDate))
                _isInfectedNow = false;
            else
                _isInfectedNow = true;
        }

        bool IsStillInfected(DateTime curDate)
        {
            return curDate > StartInfectionDate.AddDays(4) ? false : true;
        }
    }
}