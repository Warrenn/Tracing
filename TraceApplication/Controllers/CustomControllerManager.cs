using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace TraceApplication.Controllers
{
    public class CustomControllerManager
    {
        private static readonly ConcurrentDictionary<Type, HttpControllerDescriptor>
            ControllerDescriptors = new ConcurrentDictionary<Type, HttpControllerDescriptor>();

        private static readonly ConcurrentDictionary<Type, IList<HttpActionDescriptor>>
            ActionDescriptors = new ConcurrentDictionary<Type, IList<HttpActionDescriptor>>();

        public static HttpControllerDescriptor CreateController<T>(HttpConfiguration configuration) where T : class
        {
            var type = typeof(T);
            var descriptor = new CustomControllerDescriptor<T>(configuration);
            var descriptors = type
                .GetMethods()
                .Select(method => new CustomActionDescriptor<T>(descriptor, method))
                .Cast<HttpActionDescriptor>()
                .ToList();

            ActionDescriptors.TryAdd(type, descriptors);
            ControllerDescriptors.TryAdd(type, descriptor);

            return descriptor;
        }

        public static HttpControllerDescriptor GetDescriptor<T>() where T : class
        {
            return GetDescriptor(typeof(T));
        }

        public static IEnumerable<HttpActionDescriptor> GetActionDescriptors<T>(Type type) where T : class
        {
            return GetActionDescriptors(typeof(T));
        }

        public static IEnumerable<HttpActionDescriptor> GetActionDescriptors(Type type)
        {
            IList<HttpActionDescriptor> actions;
            if (ActionDescriptors.TryGetValue(type, out actions))
            {
                return actions;
            }
            return null;
        }

        public static HttpControllerDescriptor GetDescriptor(Type type)
        {
            HttpControllerDescriptor descriptor;
            if (ControllerDescriptors.TryGetValue(type, out descriptor))
            {
                return descriptor;
            }
            return null;
        }
    }
}