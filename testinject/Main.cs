using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace testinject
{
    public class Main
    {
        public Sub1 Sub1 { get; set; }
        public IList<Sub2> Lots { get; set; }
        public int Field6 { get; set; }
        public DateTime? Field7 { get; set; }
        public decimal? Field8 { get; set; }
    }
}
