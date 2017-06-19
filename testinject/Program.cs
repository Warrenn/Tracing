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

        private static readonly MethodInfo elementAtIndexMethodInfo =
                typeof(Program)
                .GetMethod("ElementAtIndex");

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

            SetProperty(main, "Main_Lots_1_Sub2_Subb_2_Sub3_Int", "20", mappings);
            var next2 = mappings["Main_Sub1_Sub2_Field4"].SetPropertyDelegate;

            main = (Main)next2(main, "22");

            var Sub2 = (ICollection<Sub2>)mappings["Main_Lots"].GetPropertyDelegate(main);
            //IEnumerable<int> j;
            var c = Sub2.Count();

            var next7 = PropertyFunctionProvider.CreateSetPropertyFunction<Main>("Main_Field8");

            main = next7(main, "129230,999");
        }

        private static object SetProperty(object instance, string path, object value,
            IDictionary<string, PropertyMapping> mappings)
        {
            var pattern = @"_(\d+)_";
            var parts = Regex.Split(path, pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var partCount = parts.Length/2;

            for (var pathIndex = 0; pathIndex < partCount; pathIndex++)
            {
                var propPart = (pathIndex*2);
                var indexPart = (pathIndex*2) + 1;
                var part = parts[propPart];
                var getProperty = mappings[part].GetPropertyDelegate;
                var setProperty = mappings[part].SetPropertyDelegate;
                var propType = mappings[part].PropertyType;

                var indexString = parts[indexPart];
                int arrayIndex;
                if (!int.TryParse(indexString, out arrayIndex)) return null;

                var collection = getProperty(instance);

                var arrayType = propType.IsArray
                    ? propType.GetElementType()
                    : propType.GenericTypeArguments[0];

                var genElementAtIndexMethodInfo = elementAtIndexMethodInfo.MakeGenericMethod(arrayType);
                var arguments = new[] {collection, arrayIndex};
                var element = genElementAtIndexMethodInfo.Invoke(null, arguments);

                collection = arguments[0];
                setProperty(instance, collection);

                instance = element;
            }

            var lastpart = parts[parts.Length - 1];

            var setPropertyFunc = mappings[lastpart].SetPropertyDelegate;

            return setPropertyFunc(instance, value);
        }

        public static T ElementAtIndex<T>(ref object instance, int index)
        {
            var num = 0;
            var collection = instance as ICollection<T> ?? new Collection<T>();

            using (var enumerator = collection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (num == index) return enumerator.Current;
                    checked { ++num; }
                }
            }
            var type = typeof(T);
            var createDefault = (type.GetConstructor(Type.EmptyTypes) == null)
                ? (Func<T>)(() => default(T))
                : () => (T)Activator.CreateInstance(type);
            var newItem = createDefault();
            while (num <= index)
            {
                newItem = createDefault();
                collection.Add(newItem);
                checked { ++num; }
            }
            instance = collection;
            return newItem;
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
