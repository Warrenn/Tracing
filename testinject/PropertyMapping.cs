using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testinject
{
    public class PropertyMapping
    {
        public Func<object, object, object> SetPropertyDelegate { get; set; }
        public Func<object, object> GetPropertyDelegate { get; set; }
        public Type PropertyType { get; set; }
        public Type CoreType { get; set; }
    }
}
