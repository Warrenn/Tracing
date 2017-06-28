using System.Linq;
using System.Web.Http.Controllers;

namespace TraceApplication.Controllers
{
    public class CustomActionSelector : IHttpActionSelector
    {
        private readonly IHttpActionSelector selector;

        public CustomActionSelector(IHttpActionSelector selector)
        {
            this.selector = selector;
        }

        #region Implementation of IHttpActionSelector

        public HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            var descriptor = new CustomActionDescriptor<IService>(
                controllerContext.ControllerDescriptor,
                typeof(IService).GetMethod("DoSomething"));
            return descriptor;
            //controllerContext.Request.
            // return new CustomActionDescriptor((ReflectedHttpActionDescriptor) descriptor);
        }

        public ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            return selector.GetActionMapping(controllerDescriptor);
        }

        #endregion
    }
}