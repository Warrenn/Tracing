using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace TraceApplication.Controllers
{
    public class CustomActionDescriptor<TService> : HttpActionDescriptor where TService : class
    {
        private static readonly object[] Empty = new object[0];

        private readonly Lazy<Collection<HttpParameterDescriptor>> parameters;
        private ParameterInfo[] parameterInfos;

        private Lazy<ActionExecutor> actionExecutor;
        private MethodInfo methodInfo;
        private Type returnType;
        private string actionName;
        private Collection<HttpMethod> supportedHttpMethods;

        // Getting custom attributes via reflection is slow. 
        // But iterating over a object[] to pick out specific types is fast. 
        // Furthermore, many different services may call to ask for different attributes, so we have multiple callers. 
        // That means there's not a single cache for the callers, which means there's some value caching here.
        // This cache can be a 2x speedup in some benchmarks.
        private object[] attributeCache;
        private object[] declaredOnlyAttributeCache;

        private static readonly HttpMethod[] SupportedHttpMethodsByConvention =
        {
            HttpMethod.Get,
            HttpMethod.Post,
            HttpMethod.Put,
            HttpMethod.Delete,
            HttpMethod.Head,
            HttpMethod.Options,
            new HttpMethod("PATCH")
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectedHttpActionDescriptor"/> class.
        /// </summary>
        /// <remarks>The default constructor is intended for use by unit testing only.</remarks>
        public CustomActionDescriptor()
        {
            parameters = new Lazy<Collection<HttpParameterDescriptor>>(InitializeParameterDescriptors);
            supportedHttpMethods = new Collection<HttpMethod>();
        }

        public CustomActionDescriptor(HttpControllerDescriptor controllerDescriptor, MethodInfo methodInfo)
            : base(controllerDescriptor)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            InitializeProperties(methodInfo);
            parameters = new Lazy<Collection<HttpParameterDescriptor>>(InitializeParameterDescriptors);
        }

        public override string ActionName => actionName;

        public override Collection<HttpMethod> SupportedHttpMethods => supportedHttpMethods;

        public MethodInfo MethodInfo
        {
            get { return methodInfo; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                InitializeProperties(value);
            }
        }

        private ParameterInfo[] ParameterInfos => parameterInfos ?? (parameterInfos = methodInfo.GetParameters());

        /// <inheritdoc/>
        public override Type ReturnType => returnType;

        /// <inheritdoc/>
        public override Collection<T> GetCustomAttributes<T>(bool inherit)
        {
            var attributes = inherit ? attributeCache : declaredOnlyAttributeCache;
            return new Collection<T>(TypeHelper.OfType<T>(attributes));
        }

        /// <inheritdoc/>
        public override Task<object> ExecuteAsync(HttpControllerContext controllerContext, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.Canceled<object>();
            }

            try
            {
                var argumentValues = PrepareParameters(arguments, controllerContext);
                return actionExecutor.Value.Execute(((CustomController<TService>)controllerContext.Controller).Instance, argumentValues);
            }
            catch (Exception e)
            {
                return TaskHelpers.FromError<object>(e);
            }
        }

        public override Collection<IFilter> GetFilters()
        {
            return new Collection<IFilter>(GetCustomAttributes<IFilter>().Concat(base.GetFilters()).ToList());
        }

        public override Collection<HttpParameterDescriptor> GetParameters()
        {
            return parameters.Value;
        }

        private void InitializeProperties(MethodInfo pmethodInfo)
        {
            methodInfo = pmethodInfo;
            parameterInfos = null;
            returnType = GetReturnType(pmethodInfo);
            actionExecutor = new Lazy<ActionExecutor>(() => InitializeActionExecutor(methodInfo));
            declaredOnlyAttributeCache = methodInfo.GetCustomAttributes(inherit: false);
            attributeCache = methodInfo.GetCustomAttributes(inherit: true);
            actionName = GetActionName(methodInfo, attributeCache);
            supportedHttpMethods = GetSupportedHttpMethods(methodInfo, attributeCache);
        }

        internal static Type GetReturnType(MethodInfo methodInfo)
        {
            var result = methodInfo.ReturnType;
            if (typeof(Task).IsAssignableFrom(result))
            {
                result = TypeHelper.GetTaskInnerTypeOrNull(methodInfo.ReturnType);
            }
            if (result == typeof(void))
            {
                result = null;
            }
            return result;
        }

        private Collection<HttpParameterDescriptor> InitializeParameterDescriptors()
        {
            Contract.Assert(methodInfo != null);

            var localInfos = ParameterInfos.Select(
                (item) => new ReflectedHttpParameterDescriptor(this, item)).ToList<HttpParameterDescriptor>();
            return new Collection<HttpParameterDescriptor>(localInfos);
        }

        private object[] PrepareParameters(IDictionary<string, object> @params, HttpControllerContext controllerContext)
        {
            // This is on a hotpath, so a quick check to avoid the allocation if we have no parameters. 
            if (parameters.Value.Count == 0)
            {
                return Empty;
            }

            var infos = ParameterInfos;
            var parameterCount = infos.Length;
            var parameterValues = new object[parameterCount];
            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                parameterValues[parameterIndex] = ExtractParameterFromDictionary(infos[parameterIndex], @params, controllerContext);
            }
            return parameterValues;
        }

        private object ExtractParameterFromDictionary(ParameterInfo parameterInfo, IDictionary<string, object> @params, HttpControllerContext controllerContext)
        {
            object value;
            if (!@params.TryGetValue(parameterInfo.Name, out value))
            {
                // the key should always be present, even if the parameter value is null
                throw new HttpResponseException(controllerContext.Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        $"Parameter Not In Dictionary {parameterInfo.Name}, {parameterInfo.ParameterType}, {MethodInfo}, {MethodInfo.DeclaringType}"));
            }

            if (value == null && !TypeHelper.TypeAllowsNullValue(parameterInfo.ParameterType))
            {
                // tried to pass a null value for a non-nullable parameter type
                throw new HttpResponseException(controllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                        $"Parameter Cannot Be Null {parameterInfo.Name}, {parameterInfo.ParameterType}, {MethodInfo}, {MethodInfo.DeclaringType}"));
            }

            if (value != null && !parameterInfo.ParameterType.IsInstanceOfType(value))
            {
                // value was supplied but is not of the proper type
                throw new HttpResponseException(controllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    $"Reflected Action Descriptor Parameter Value Has Wrong Type {parameterInfo.Name}, {MethodInfo}, {MethodInfo.DeclaringType}, {value.GetType()}, {parameterInfo.ParameterType}"));
            }

            return value;
        }

        private static string GetActionName(MethodInfo methodInfo, object[] actionAttributes)
        {
            var nameAttribute = TypeHelper.OfType<ActionNameAttribute>(actionAttributes).FirstOrDefault();
            return nameAttribute != null
                       ? nameAttribute.Name
                       : methodInfo.Name;
        }

        private static Collection<HttpMethod> GetSupportedHttpMethods(MethodInfo methodInfo, object[] actionAttributes)
        {
            var supportedHttpMethods = new Collection<HttpMethod>();
            ICollection<IActionHttpMethodProvider> httpMethodProviders = TypeHelper.OfType<IActionHttpMethodProvider>(actionAttributes);
            if (httpMethodProviders.Count > 0)
            {
                // Get HttpMethod from attributes
                foreach (var httpMethodSelector in httpMethodProviders)
                {
                    foreach (var httpMethod in httpMethodSelector.HttpMethods)
                    {
                        supportedHttpMethods.Add(httpMethod);
                    }
                }
            }
            else
            {
                // Get HttpMethod from method name convention 
                foreach (var t in SupportedHttpMethodsByConvention)
                {
                    if (!methodInfo.Name.StartsWith(t.Method, StringComparison.OrdinalIgnoreCase)) continue;
                    supportedHttpMethods.Add(t);
                    break;
                }
            }

            if (supportedHttpMethods.Count == 0)
            {
                // Use POST as the default HttpMethod
                supportedHttpMethods.Add(HttpMethod.Post);
            }

            return supportedHttpMethods;
        }

        // Implementing Equals and GetHashCode is needed here because when tracing is enabled, a different set of action descriptors
        // are available at configuration time for attribute routing and at runtime. This is because the default action selector
        // clears its action descriptor cache when the controller descriptor is different. And since tracing wraps the controller
        // descriptor for tracing, the cache gets cleared and new action descriptors get created for tracing. We need to compare
        // the action descriptors by method info to be able to correlate attribute routing actions to the tracing action descriptors.

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return methodInfo != null ? methodInfo.GetHashCode() : base.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (methodInfo == null) return base.Equals(obj);
            var otherDescriptor = obj as CustomActionDescriptor<TService>;
            return otherDescriptor != null && methodInfo.Equals(otherDescriptor.methodInfo);
        }

        private static ActionExecutor InitializeActionExecutor(MethodInfo methodInfo)
        {
            if (methodInfo.ContainsGenericParameters)
            {
                throw new InvalidOperationException();
            }

            return new ActionExecutor(methodInfo);
        }

        private sealed class ActionExecutor
        {
            private readonly Func<object, object[], Task<object>> executor;
            private static readonly MethodInfo ConvertOfTMethod = typeof(ActionExecutor).GetMethod("Convert", BindingFlags.Static | BindingFlags.NonPublic);

            public ActionExecutor(MethodInfo methodInfo)
            {
                Contract.Assert(methodInfo != null);
                executor = GetExecutor(methodInfo);
            }

            public Task<object> Execute(object instance, object[] arguments)
            {
                return executor(instance, arguments);
            }

            // Method called via reflection.
            private static Task<object> Convert<T>(object taskAsObject)
            {
                var task = (Task<T>)taskAsObject;
                return task.CastToObject<T>();
            }

            // Do not inline or optimize this method to avoid stack-related reflection demand issues when
            // running from the GAC in medium trust
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private static Func<object, Task<object>> CompileGenericTaskConversionDelegate(Type taskValueType)
            {
                Contract.Assert(taskValueType != null);

                return (Func<object, Task<object>>)Delegate.CreateDelegate(typeof(Func<object, Task<object>>), ConvertOfTMethod.MakeGenericMethod(taskValueType));
            }

            private static Func<object, object[], Task<object>> GetExecutor(MethodInfo methodInfo)
            {
                // Parameters to executor
                var instanceParameter = Expression.Parameter(typeof(object), "instance");
                var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

                // Build parameter list
                var parameters = new List<Expression>();
                var paramInfos = methodInfo.GetParameters();
                for (var i = 0; i < paramInfos.Length; i++)
                {
                    var paramInfo = paramInfos[i];
                    var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                    var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                    // valueCast is "(Ti) parameters[i]"
                    parameters.Add(valueCast);
                }

                // Call method
                var instanceCast = (!methodInfo.IsStatic) ? Expression.Convert(instanceParameter, methodInfo.ReflectedType) : null;
                MethodCallExpression methodCall = methodCall = Expression.Call(instanceCast, methodInfo, parameters);

                // methodCall is "((MethodInstanceType) instance).method((T0) parameters[0], (T1) parameters[1], ...)"
                // Create function
                if (methodCall.Type == typeof(void))
                {
                    // for: public void Action()
                    var lambda = Expression.Lambda<Action<object, object[]>>(methodCall, instanceParameter, parametersParameter);
                    var voidExecutor = lambda.Compile();
                    return (instance, methodParameters) =>
                    {
                        voidExecutor(instance, methodParameters);
                        return TaskHelpers.NullResult();
                    };
                }
                else
                {
                    // must coerce methodCall to match Func<object, object[], object> signature
                    var castMethodCall = Expression.Convert(methodCall, typeof(object));
                    var lambda = Expression.Lambda<Func<object, object[], object>>(castMethodCall, instanceParameter, parametersParameter);
                    var compiled = lambda.Compile();
                    if (methodCall.Type == typeof(Task))
                    {
                        // for: public Task Action()
                        return (instance, methodParameters) =>
                        {
                            var r = (Task)compiled(instance, methodParameters);
                            ThrowIfWrappedTaskInstance(methodInfo, r.GetType());
                            return r.CastToObject();
                        };
                    }
                    if (!typeof(Task).IsAssignableFrom(methodCall.Type))
                        return (instance, methodParameters) =>
                        {
                            var result = compiled(instance, methodParameters);
                            // Throw when the result of a method is Task. Asynchronous methods need to declare that they
                            // return a Task.
                            var resultAsTask = result as Task;
                            if (resultAsTask != null)
                            {
                                throw new InvalidOperationException();
                            }
                            return Task.FromResult(result);
                        };
                    // for: public Task<T> Action()
                    // constructs: return (Task<object>)Convert<T>(((Task<T>)instance).method((T0) param[0], ...))
                    var taskValueType = TypeHelper.GetTaskInnerTypeOrNull(methodCall.Type);
                    var compiledConversion = CompileGenericTaskConversionDelegate(taskValueType);

                    return (instance, methodParameters) =>
                    {
                        var callResult = compiled(instance, methodParameters);
                        var convertedResult = compiledConversion(callResult);
                        return convertedResult;
                    };
                    // for: public T Action()
                }
            }

            private static void ThrowIfWrappedTaskInstance(MethodInfo method, Type type)
            {
                // Throw if a method declares a return type of Task and returns an instance of Task<Task> or Task<Task<T>>
                // This most likely indicates that the developer forgot to call Unwrap() somewhere.
                Contract.Assert(method.ReturnType == typeof(Task));
                // Fast path: check if type is exactly Task first.
                if (type == typeof(Task)) return;
                var innerTaskType = TypeHelper.GetTaskInnerTypeOrNull(type);
                if (innerTaskType != null && typeof(Task).IsAssignableFrom(innerTaskType))
                {
                    throw new InvalidOperationException();
                }
            }
        }

    }
}