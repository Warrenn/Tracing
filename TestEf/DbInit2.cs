using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEf.Migrations;

namespace TestEf
{
    public class DbInit2: MigrateDatabaseToLatestVersion<Context2, Configuration2>
    {
    }
}
