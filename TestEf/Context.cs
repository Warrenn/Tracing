using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using efplay;

namespace TestEf
{
    public class Context : DbContext
    {
        public Context() : base("EfTest")
        {

        }

        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<Lecture> Lectures { get; set; }

        #region Overrides of DbContext

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            modelBuilder
                .Properties<string>()
                .Configure(p => p.HasMaxLength(255));



        }

        #endregion
    }
}
