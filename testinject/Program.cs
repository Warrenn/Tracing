using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace testinject
{
    class Program
    {

        static void Main(string[] args)
        {
            var mappings = PropertyFunctionProvider.CreateTypeMappings("", typeof(Main), typeof(Main));
            var main = new Main
            {
                Lots = new List<Sub2>
                {
                    new Sub2 {Field3 = "field3", Field4 = 44}
                }
            };

            
            PropertyFunctionProvider.SetProperty(main, "Lots_1_Subb_2_Int", "20", mappings);
            PropertyFunctionProvider.SetProperty(main, "Field7", "2017-11-13", mappings);

            var next2 = mappings["Main_Sub1_Sub2_Field4"].SetPropertyDelegate;

            main = (Main)next2(main, "22");

            var Sub2 = (ICollection<Sub2>)mappings["Main_Lots"].GetPropertyDelegate(main);
            //IEnumerable<int> j;
            var c = Sub2.Count();

            var next7 = PropertyFunctionProvider.CreateSetPropertyFunction<Main>("Main_Field8");

            main = next7(main, "129230,999");
        }


        public static void SetValueAtIndex<T>(object collection, int index, T value)
        {
            var type = typeof(T);
            if (typeof(IList).IsAssignableFrom(type))
            {
                var ilist = (IList)collection;
                ilist[index] = value;
            }

            var indexer = type
                .GetProperties()
                .FirstOrDefault(p => p.GetIndexParameters().Length != 0);

            if (indexer == null) return;

            object[] indexArgs = { index };
            indexer.SetValue(collection, value, indexArgs);
        }

    }
}
