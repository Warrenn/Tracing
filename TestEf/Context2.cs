using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using efplay;

namespace TestEf
{
    public class Context2 :DbContext
    {
        public virtual DbSet<StudentPlus> Students { get; set; }

        public Context2() : base("EfTest")
        {
            
        }

        #region Overrides of DbContext

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<StudentPlus>()
                .Map(m => m.ToTable("Students"));
        }

        #endregion
    }
}
