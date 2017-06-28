using System.Net.Http;
using System.Web.Http.Controllers;

namespace TraceApplication.Controllers
{
    public class CustomControllerDescriptor<T> : HttpControllerDescriptor where T : class
    {
        public CustomControllerDescriptor()
        {
            ControllerType = typeof(T);
        }

        public override IHttpController CreateController(HttpRequestMessage request)
        {
            var resolver = Configuration.DependencyResolver;
            var scope = resolver.BeginScope();
            var service = scope.GetService(typeof(T)) as T;
            var controller = new CustomController<T>(service);
            
            return controller;
        }
    }
}