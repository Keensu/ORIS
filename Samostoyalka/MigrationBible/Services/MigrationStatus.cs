using MigrationLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationLib.Services
{
    public class MigrationStatus
    {
        public bool HasPendingChanges { get; set; }
        public List<Table> CurrentSchema { get; set; }
        public List<Table> TargetSchema { get; set; }
        public List<MigrationRec> MigrationHistory { get; set; }
    }
}
