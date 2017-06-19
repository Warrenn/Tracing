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
        private static readonly MethodInfo setGenMethodInfo =
                typeof(PropertyFunctionProvider)
                .GetMethod("CreateSetPropertyFunctionGeneric");

        private static readonly MethodInfo getGenMethodInfo =
                typeof(PropertyFunctionProvider)
                .GetMethod("CreateGetPropertyFunctionGeneric");

        private static readonly MethodInfo addValuesTillIndexMethodInfo =
                typeof(Program)
                .GetMethod("AddValuesTillIndex");

        private static readonly MethodInfo GetValueAtIndexMethodInfo =
                typeof(Program)
                .GetMethod("GetValueAtIndex");

        private static readonly MethodInfo createDefaultMethodInfo =
                typeof(Program)
                .GetMethod("CreateDefault");

        static void Main(string[] args)
        {
            var mappings = CreateTypeMappings("Main", typeof(Main), typeof(Main));
            var main = new Main
            {
                Lots = new List<Sub2>
                {
                    new Sub2 {Field3 = "field3", Field4 = 44}
                }
            };

            main = (Main)SetProperty(main, "Main_Lots_1_Sub2_Subb_2_Sub3_Int", "20", mappings);
            var next2 = mappings["Main_Sub1_Sub2_Field4"].SetPropertyDelegate;

            main = (Main)next2(main, "22");

            var Sub2 = (ICollection<Sub2>)mappings["Main_Lots"].GetPropertyDelegate(main);
            //IEnumerable<int> j;
            var c = Sub2.Count();

            var next7 = PropertyFunctionProvider.CreateSetPropertyFunction<Main>("Main_Field8");

            main = next7(main, "129230,999");
        }

        private static object SetProperty(object instance, string path, object value, IDictionary<string, PropertyMapping> mappings)
        {
            var pattern = @"_(\d+)_";
            var parts = Regex.Split(path, pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var oddRange = parts.Length % 2 == 0;
            var endRange = oddRange ? parts.Length : parts.Length - 1;

            for (var i = 0; i < endRange; i++)
            {
                var part = parts[i];
                var getProperty = mappings[part].GetPropertyDelegate;
                var propType = mappings[part].PropertyType;
                var property = getProperty(instance);
                ++i;

                if (!typeof(IEnumerable).IsAssignableFrom(propType)) return null;

                var arrayType = propType.IsArray
                    ? propType.GetElementType()
                    : propType.GenericTypeArguments[0];

                var indexString = parts[i];
                if (!int.TryParse(indexString, out int arrayIndex)) return null;

                if (Count(instance) < arrayIndex)
                {
                    var addValuesTillIndexMethodInfoGen = addValuesTillIndexMethodInfo.MakeGenericMethod(arrayType);
                    addValuesTillIndexMethodInfoGen.Invoke(null, new[] { property, arrayIndex });
                }
                else
                {
                    var GetValueAtIndexMethodInfoGen = GetValueAtIndexMethodInfo.MakeGenericMethod(arrayType);
                    GetValueAtIndexMethodInfoGen.Invoke(null, new[] { property, arrayIndex });
                }
            }

            return instance;
        }

        public static T CreateDefault<T>()
        {
            var type = typeof(T);
            return type.GetConstructor(Type.EmptyTypes) == null
                ? default(T)
                : (T)Activator.CreateInstance(type);
        }

        public static T AddValuesTillIndex<T>(ICollection<T> instance, int index)
        {
            var defaultValue = CreateDefault<T>();
            for (var i = (instance.Count - 1); i < index; i++)
            {
                defaultValue = CreateDefault<T>();
                instance.Add(defaultValue);
            }
            return defaultValue;
        }

        public static T GetValueAtIndex<T>(object instance, int index)
        {
            var type = typeof(T);
            if (typeof(IList).IsAssignableFrom(type))
            {
                var ilist = (IList)instance;
                return (T)ilist[index];
            }

            var indexer = type
                .GetProperties()
                .FirstOrDefault(p => p.GetIndexParameters().Length != 0);

            if (indexer == null) return default(T);

            object[] indexArgs = { index };
            return (T)indexer.GetValue(instance, indexArgs);
        }

        public static int Count(object source)
        {
            var collection = source as ICollection;
            if (collection != null)
                return collection.Count;
            var num = 0;
            var enumerator = ((IEnumerable)source).GetEnumerator();
            while (enumerator.MoveNext())
                checked { ++num; }
            return num;
        }

        public static IDictionary<string, PropertyMapping> CreateTypeMappings(string path, Type coreType, Type type)
        {
            var dictionary = new Dictionary<string, PropertyMapping>();

            foreach (var propertyInfo in type.GetProperties())
            {
                var propType = propertyInfo.PropertyType;
                var propName = $"{path}_{propertyInfo.Name}";
                var setMethodInfo = setGenMethodInfo.MakeGenericMethod(coreType);
                var getMethodInfo = getGenMethodInfo.MakeGenericMethod(coreType, propType);

                dictionary.Add(propName, new PropertyMapping
                {
                    CoreType = coreType,
                    PropertyType = propType,
                    SetPropertyDelegate =
                        (Func<object, object, object>)setMethodInfo
                            .Invoke(null, new object[] { propName }),
                    GetPropertyDelegate =
                        (Func<object, object>)getMethodInfo
                            .Invoke(null, new object[] { propName })
                });

                var nextCoreType = coreType;

                if (propType.IsEnum ||
                    propType.IsValueType ||
                    typeof(string).IsAssignableFrom(propType))
                    continue;

                if (typeof(IEnumerable).IsAssignableFrom(propType))
                {
                    nextCoreType = propType.IsArray
                        ? propType.GetElementType()
                        : propType.GenericTypeArguments[0];
                    propName = nextCoreType.Name;
                    propType = nextCoreType;
                }

                foreach (var typeMapping in CreateTypeMappings(propName, nextCoreType, propType))
                {

                    dictionary[typeMapping.Key] = typeMapping.Value;
                }
            }

            return dictionary;
        }
    }
}
