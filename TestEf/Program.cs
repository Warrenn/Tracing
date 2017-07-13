using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using efplay;
using TestEf.Migrations;

namespace TestEf
{
    public static class DbContextExtensions
    {
        public static XDocument GetModel(this DbContext context)
        {
            return GetModel(w => EdmxWriter.WriteEdmx(context, w));
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static XDocument GetModel(Action<XmlWriter> writeXml)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(
                    memoryStream, new XmlWriterSettings
                    {
                        Indent = true
                    }))
                {
                    writeXml(xmlWriter);
                }

                memoryStream.Position = 0;

                return XDocument.Load(memoryStream);
            }
        }
    }

    class Program
    {
        public static XDocument Decompress(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    return XDocument.Load(gzipStream);
                }
            }
        }

        static void Main(string[] args)
        {

            //using (var ctx = new Context())
            //{
            //    var m = ctx.GetModel();
            //    string ConnectionString = ctx.Database.Connection.ConnectionString;
            //    var sqlToExecute = String.Format("select model from __MigrationHistory where migrationId like '%201707122148117_Initial'");

            //    using (var connection = new SqlConnection(ConnectionString))
            //    {
            //        connection.Open();

            //        var command = new SqlCommand(sqlToExecute, connection);

            //        var reader = command.ExecuteReader();
            //        if (!reader.HasRows)
            //        {
            //            throw new Exception("Now Rows to display. Probably migration name is incorrect");
            //        }

            //        while (reader.Read())
            //        {
            //            var model = (byte[])reader["model"];
            //            var decompressed = Decompress(model);
            //            Console.WriteLine(decompressed);
            //        }
            //    }
            //    ctx.SaveChanges();
            //}
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<Context, Configuration>());
            using (var ctx = new Context())
            {
                ctx.Database.Initialize(true);
            }
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<Context2, Configuration2>());
            using (var ctx2 = new Context2())
            {
                ctx2.Database.Initialize(true);
            }
        }
    }
}
