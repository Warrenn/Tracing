using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using efplay;

namespace TestEf
{
    class Program
    {
        static void Main(string[] args)
        {
            //using (var ctx = new Context())
            //{
            //    var lectures = ctx.Set<Lecture>();
            //    var students = ctx.Set<Student>();

            //    var lecture = lectures.Find(1);
            //    var studentp = students.Create();

            //    lecture.Students.Add(studentp);
            //    studentp.Name = "Studentp";
            //    studentp.Plus = "Plus";

            //    ctx.SaveChanges();
            //}
            using (var ctx = new Context2())
            {
                var students = ctx.Students;

                var studentp = students.Find(1007);

                studentp.Name = "Student check";
                studentp.Age = 33;

                ctx.SaveChanges();
            }
        }
    }
}
