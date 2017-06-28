using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using System.Web.Routing;

namespace TraceApplication
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            IHttpActionInvoker a;
            IHttpActionSelector f;
            IHttpControllerTypeResolver g;


            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
