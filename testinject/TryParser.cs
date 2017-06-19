using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace testinject
{
    public static class TryParser
    {
        private delegate bool TryParseDelegate<T>(string stringValue, out T instance) where T : struct;

        private static readonly ConcurrentDictionary<Type, Lazy<Delegate>> TryParseMethods =
            new ConcurrentDictionary<Type, Lazy<Delegate>>();

        private static readonly string[] Formats = {
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy hh:mm:ss tt",
            "dd-MM-yyyy",
            "dd-MM-yyyy hh:mm:ss tt",
            "dd-MM-yyyy HH:mm:ss",
            "yyyy/MM/dd",
            "yyyy/MM/dd hh:mm:ss tt",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy-MM-dd hh:mm:ss tt",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "o",
        };

        public static DateTime? DateTime(object value)
        {
            if (value is DateTime) return (DateTime?)value;
            var stringValue = $"{value}";
            if (string.IsNullOrEmpty(stringValue)) return null;

            if (System.DateTime.TryParseExact(
                stringValue, Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime date))
                return date;

            date = new DateTime(1899, 12, 30);

            if (double.TryParse(stringValue, out double doubleValue) &&
                (doubleValue <= 2958465) &&
                (doubleValue >= -693593))
                return date.AddDays(doubleValue);
            return null;
        }

        public static T? Nullable<T>(object value)
            where T : struct
        {
            if (value is T) return (T)value;
            var stringValue = $"{value}";
            if (string.IsNullOrEmpty(stringValue)) return null;
            var tryParse = GetDelegate<T>(TryParseMethods);
            if ((tryParse != null) && (tryParse(stringValue, out T returnvalue))) return returnvalue;
            return null;
        }

        public static Expression<Func<object, TProp>> CreateConvertFunctionExpression<TProp>()
        {
            var nullableInfo = typeof(TryParser).GetMethod("Nullable");
            var memberType = typeof(TProp);
            MethodInfo convertMethod;

            if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var genericType = memberType.GenericTypeArguments[0];
                if (genericType == typeof(DateTime)) return o => (TProp)((object)DateTime(o));

                convertMethod = nullableInfo.MakeGenericMethod(genericType);
                return o => (TProp)convertMethod.Invoke(null, new[] { o });
            }

            if (!memberType.IsValueType) return o => (TProp)o;

            convertMethod = nullableInfo.MakeGenericMethod(memberType);
            if (memberType != typeof(DateTime))
                return o => (TProp)(convertMethod.Invoke(null, new[] { o }) ?? default(TProp));

            return o => (TProp)((object)DateTime(o) ?? default(TProp));
        }

        private static TryParseDelegate<T> GetDelegate<T>(ConcurrentDictionary<Type, Lazy<Delegate>> dictionary)
            where T : struct
        {
            var type = typeof(T);

            if (type.IsEnum) return Enum.TryParse<T>;

            var lazy = dictionary
                .GetOrAdd(type, new Lazy<Delegate>(() =>
                {
                    var method = type
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m =>
                            (m.Name == "TryParse") &&
                            (m.GetParameters().Length == 2) &&
                            (m.GetParameters()[0].ParameterType == typeof(string)) &&
                            (m.GetParameters()[1].IsOut));
                    if (method == null) return null;
                    var returnValue =
                        Delegate.CreateDelegate(typeof(TryParseDelegate<T>), method);
                    return returnValue;
                }));
            return (TryParseDelegate<T>)lazy.Value;
        }
    }
}
