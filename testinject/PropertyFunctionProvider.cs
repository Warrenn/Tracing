using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace testinject
{
    public class PropertyFunctionProvider
    {
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

            for (var i = 1; i < parts.Length - 1; i++)
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
            return (o, o1) => propFunc((T) o, o1);
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
    }
}
