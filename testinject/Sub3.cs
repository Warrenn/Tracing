using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testinject
{
    public class Sub3
    {
        public virtual ICollection<int> Ints { get; set; }
        public virtual int Int { get; set; }
        public virtual string String { get; set; }
    }
}
