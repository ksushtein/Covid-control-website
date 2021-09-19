using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace VKR.Models
{
    public class AuditoriaOnMap
    {
        public enum status { NotInSchedule, NotInfected, WasInfected, IsInfected }
        public InfectedAuditoria InfectedAuditoria { get; set; }
        public status Status { get; set; }
        public string Name { get; set; }

        public AuditoriaOnMap(string name, status st)
        {
            Name = name;
            Status = st;
        }

        public void AddInfectData(InfectedAuditoria ia)
        {
            InfectedAuditoria = ia;
            if (InfectedAuditoria._isInfectedNow)
                Status = status.IsInfected;
            else
                Status = status.WasInfected;
        }

    }
}