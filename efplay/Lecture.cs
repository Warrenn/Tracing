using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace efplay
{
    public class Lecture
    {
        public int Id { get; set; }
        public string LectureName { get; set; }
        public virtual ICollection<Student> Students { get; set; }
    }
}
