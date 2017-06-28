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
        private static readonly MethodInfo AddConfigMethodInfo = typeof(ConfigurationRegistrar)
            .GetMethods()
            .First(m =>
                m.Name == "Add" &&
                m.GetParameters().Length > 0 &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>));

        public Context() : base("EfTest")
        {

        }

        #region Overrides of DbContext

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Properties<string>()
                .Configure(p => p.HasMaxLength(255));

            //modelBuilder.Properties<decimal>()
            //    .Configure(p=>);

            foreach (var type in typeof(Student).Assembly.GetTypes().Where(t=>t!=typeof(StudentPlus)))
            {
                var configType = typeof(EntityTypeConfiguration<>).MakeGenericType(type);
                var config = Activator.CreateInstance(configType);
                var genInfo = AddConfigMethodInfo.MakeGenericMethod(type);

                genInfo.Invoke(modelBuilder.Configurations, new[] { config });
            }

        }

        #endregion
    }
}
