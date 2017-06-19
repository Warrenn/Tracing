using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testinject
{
    public class Sub2
    {
        public string Field3 { get; set; }
        public int Field4 { get; set; }
        public ICollection<Sub3> Subb { get; set; }
    }
}
