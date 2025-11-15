using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationLib.Models
{
    public class MigrationRec
    {
        public int Id { get; set; }
        public string MigrationName { get; set; }
        public DateTime AppliedAt { get; set; }
        public string ModelSnapshot { get; set; }
        public string UpSql { get; set; }
        public string DownSql { get; set; }
    }
}
