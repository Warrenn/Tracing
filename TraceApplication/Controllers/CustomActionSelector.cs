using System;
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
            var actions =
                CustomControllerManager.GetActionDescriptors(controllerContext.ControllerDescriptor.ControllerType);
            if (actions == null)
            {
                return selector.SelectAction(controllerContext);
            }

            var actionName = (string)controllerContext.RouteData.Values["action"];

            foreach (var action in actions)
            {
                if (string.Equals(action.ActionName, actionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return action;
                }
            }

            return selector.SelectAction(controllerContext);
        }

        public ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            return selector.GetActionMapping(controllerDescriptor);
        }

        #endregion
    }
}