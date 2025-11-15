using MigrationLib.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration
{
    [Table("users")]
    class User
    {
        [Column]
        public int Id { get; set; }
        [PrimaryKey]
        public string Name { get; set; }
    }

}
