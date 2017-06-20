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
            using (var ctx = new Context())
            {
                var lectures = ctx.Set<Lecture>();
                var students = ctx.Set<Student>();

                var lecture = lectures.Create();
                lecture.LectureName = "hello4";

                var student = new Student {Name = "student a4"};

                lecture.Students = new Collection<Student> {student};

                lectures.Add(lecture);
                ctx.SaveChanges();
            }
        }
    }
}
