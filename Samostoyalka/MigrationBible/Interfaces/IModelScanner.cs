using MigrationLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationLib.Interfaces
{
    public interface IModelScanner
    {
        List<Table> ScanModels();
    }
}
