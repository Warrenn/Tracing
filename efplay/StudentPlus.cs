using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace efplay
{
    public class StudentPlus
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public decimal PlusPlus { get; set; }
        public virtual List<Pluz> Pluzes { get; set; }
    }
}
