using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using Moq;
using TraceApplication.Controllers;

namespace TraceApplication
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional },
                handler: new CustomControllerDispatcher<IService>(config),
                constraints: null
            );

            var dirmock = new Mock<IDependencyResolver>();
            var dismock = new Mock<IDependencyScope>();

            dismock
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(IService))))
                .Returns(new Service());

            dirmock
                .Setup(resolver => resolver.BeginScope())
                .Returns(dismock.Object);

            config.DependencyResolver = dirmock.Object;

            var p = config.Services.GetFilterProviders();

            var actionSelector = config.Services.GetActionSelector();
            config.Services.Replace(typeof(IHttpActionSelector), new CustomActionSelector(actionSelector));
            //config.Services
        }
    }
}
