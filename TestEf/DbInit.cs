using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEf.Migrations;

namespace TestEf
{
    public class DbInit: MigrateDatabaseToLatestVersion<Context, Configuration>
    {
    }
}
