using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace testinject
{
    public class PropertyFunctionProvider
    {
        private static readonly MethodInfo SetGenMethodInfo =
                typeof(PropertyFunctionProvider)
                .GetMethod("CreateSetPropertyFunctionGeneric");

        private static readonly MethodInfo GetGenMethodInfo =
                typeof(PropertyFunctionProvider)
                .GetMethod("CreateGetPropertyFunctionGeneric");

        private static readonly MethodInfo ElementAtIndexMethodInfo =
                typeof(PropertyFunctionProvider)
                .GetMethod("ElementAtIndex");

        private static IList<Expression> CreateMemberExpression<TClass>(string path, Expression parameter)
        {
            var expressions = new List<Expression>();

            var valueExpression = parameter;
            var nullBaseExpression = Expression.Constant(null, typeof(TClass));
            var newExpression = Expression.New(typeof(TClass));
            var conditionExpression = Expression.Condition(Expression.Equal(nullBaseExpression, parameter),
                newExpression, valueExpression);
            expressions.Add(Expression.Assign(valueExpression, conditionExpression));

            var parts = path.Split('_');

            for (var i = 0; i < parts.Length - 1; i++)
            {
                valueExpression = Expression.PropertyOrField(valueExpression, parts[i]);
                newExpression = Expression.New(valueExpression.Type);
                nullBaseExpression = Expression.Constant(null, valueExpression.Type);
                conditionExpression = Expression.Condition(Expression.Equal(nullBaseExpression, valueExpression),
                    newExpression, valueExpression);

                expressions.Add(Expression.Assign(valueExpression, conditionExpression));
            }

            expressions.Add(Expression.PropertyOrField(valueExpression, parts[parts.Length - 1]));

            return expressions;
        }

        public static Func<object, object, object> CreateSetPropertyFunctionGeneric<T>(string path)
        {
            var propFunc = CreateSetPropertyFunction<T>(path);
            return (o, o1) => propFunc((T)o, o1);
        }

        public static Func<object, object> CreateGetPropertyFunctionGeneric<T, TProp>(string path)
        {
            var propFunc = CreateGetPropertyFunction<T, TProp>(path);
            return o => propFunc((T)o);
        }

        public static Func<TClass, TProp> CreateGetPropertyFunction<TClass, TProp>(string path)
        {
            var parameter = Expression.Parameter(typeof(TClass), "classparam");
            var labelTarget = Expression.Label(typeof(TProp));
            var expressions = CreateMemberExpression<TClass>(path, parameter);
            var valueExpression = expressions.Last();

            expressions.RemoveAt(expressions.Count - 1);

            expressions.Add(Expression.Return(labelTarget, valueExpression));
            expressions.Add(Expression.Label(labelTarget, Expression.Constant(default(TProp), typeof(TProp))));

            var body = Expression.Block(expressions);
            var expressionTree = Expression.Lambda<Func<TClass, TProp>>(body, parameter);
            return expressionTree.Compile();
        }

        public static Func<TClass, object, TClass> CreateSetPropertyFunction<TClass>(string path)
        {
            var createConvertFunctionExpressionMethodInfo = typeof(TryParser)
                .GetMethod("CreateConvertFunctionExpression");
            var parameters = new[]
            {
                Expression.Parameter(typeof(TClass), "classparam"),
                Expression.Parameter(typeof(object), "propertyparam")
            };
            var expressions = CreateMemberExpression<TClass>(path, parameters[0]);
            var valueExpression = expressions.Last();

            expressions.RemoveAt(expressions.Count - 1);

            var genericConvertFunctionMethodInfo = createConvertFunctionExpressionMethodInfo.MakeGenericMethod(valueExpression.Type);
            var convertFunction = (Expression)genericConvertFunctionMethodInfo.Invoke(null, null);
            var invokeConvertExpression = Expression.Invoke(convertFunction, parameters[1]);
            var labelTarget = Expression.Label(typeof(TClass));

            expressions.Add(Expression.Assign(valueExpression, invokeConvertExpression));
            expressions.Add(Expression.Return(labelTarget, parameters[0]));
            expressions.Add(Expression.Label(labelTarget, Expression.Constant(null, typeof(TClass))));

            var body = Expression.Block(expressions);
            var expressionTree = Expression.Lambda<Func<TClass, object, TClass>>(body, parameters);
            return expressionTree.Compile();
        }

        public static void SetProperty(object instance, string path, object value,
            IDictionary<string, PropertyMapping> mappings)
        {
            var pattern = @"_(\d+)_";
            var parts = Regex.Split(path, pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var partCount = parts.Length / 2;

            for (var pathIndex = 0; pathIndex < partCount; pathIndex++)
            {
                var propPart = (pathIndex * 2);
                var indexPart = (pathIndex * 2) + 1;
                var part = parts[propPart];

                if (!mappings.ContainsKey(part)) return;
                var getProperty = mappings[part].GetPropertyDelegate;
                var setProperty = mappings[part].SetPropertyDelegate;
                var propType = mappings[part].PropertyType;

                var indexString = parts[indexPart];
                int arrayIndex;
                if (!int.TryParse(indexString, out arrayIndex)) return;
                arrayIndex--;

                var collection = getProperty(instance);

                var arrayType = propType.IsArray
                    ? propType.GetElementType()
                    : propType.GenericTypeArguments[0];

                var genElementAtIndexMethodInfo = ElementAtIndexMethodInfo.MakeGenericMethod(arrayType);
                var arguments = new[] { collection, arrayIndex };
                var element = genElementAtIndexMethodInfo.Invoke(null, arguments);

                collection = arguments[0];
                setProperty(instance, collection);

                instance = element;
            }

            var lastpart = parts[parts.Length - 1];
            if (!mappings.ContainsKey(lastpart)) return;

            var setPropertyFunc = mappings[lastpart].SetPropertyDelegate;

            setPropertyFunc(instance, value);
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

        public static IDictionary<string, PropertyMapping> CreateTypeMappings(string path, Type coreType, Type type)
        {
            var dictionary = new Dictionary<string, PropertyMapping>();
            path = string.IsNullOrEmpty(path) ? string.Empty : $"{path}_";

            foreach (var propertyInfo in type.GetProperties())
            {
                var propType = propertyInfo.PropertyType;
                var propName = $"{path}{propertyInfo.Name}";
                var setMethodInfo = SetGenMethodInfo.MakeGenericMethod(coreType);
                var getMethodInfo = GetGenMethodInfo.MakeGenericMethod(coreType, propType);

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
                    propName = string.Empty;
                    propType = nextCoreType;
                }

                foreach (var typeMapping in CreateTypeMappings(propName, nextCoreType, propType))
                {
                    dictionary[typeMapping.Key] = typeMapping.Value;
                }
            }

            return dictionary;
        }

        public class PropertyMapping
        {
            public Func<object, object, object> SetPropertyDelegate { get; set; }
            public Func<object, object> GetPropertyDelegate { get; set; }
            public Type PropertyType { get; set; }
            public Type CoreType { get; set; }
        }
    }
}
