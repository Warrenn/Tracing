using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using ExceptionCatchBlocks = System.Web.Http.ExceptionHandling.ExceptionCatchBlocks;
using ExceptionContext = System.Web.Http.ExceptionHandling.ExceptionContext;

namespace TraceApplication.Controllers
{
    public class CustomControllerDispatcher<T> : HttpMessageHandler where T : class
    {
        private readonly HttpConfiguration configuration;

        private IExceptionLogger exceptionLogger;
        private IExceptionHandler exceptionHandler;
        private IHttpControllerSelector controllerSelector;

        public CustomControllerDispatcher(HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
        }

        /// <summary>
        /// Gets the <see cref="HttpConfiguration"/>.
        /// </summary>
        public HttpConfiguration Configuration => configuration;

        /// <remarks>This property is internal and settable only for unit testing purposes.</remarks>
        internal IExceptionLogger ExceptionLogger
        {
            get { return exceptionLogger ?? (exceptionLogger = ExceptionServices.GetLogger(configuration)); }
            set { exceptionLogger = value; }
        }

        /// <remarks>This property is internal and settable only for unit testing purposes.</remarks>
        internal IExceptionHandler ExceptionHandler
        {
            get { return exceptionHandler ?? (exceptionHandler = ExceptionServices.GetHandler(configuration)); }
            set { exceptionHandler = value; }
        }

        private IHttpControllerSelector ControllerSelector
            => controllerSelector ?? (controllerSelector = configuration.Services.GetHttpControllerSelector());

        /// <summary>
        /// Dispatches an incoming <see cref="HttpRequestMessage"/> to an <see cref="IHttpController"/>.
        /// </summary>
        /// <param name="request">The request to dispatch</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task{HttpResponseMessage}"/> representing the ongoing operation.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ExceptionDispatchInfo exceptionInfo;
            HttpControllerContext controllerContext = null;

            try
            {
                var controllerDescriptor = new CustomControllerDescriptor<T>
                {
                    Configuration = request.GetConfiguration()
                };

                var controller = controllerDescriptor.CreateController(request);
                if (controller == null)
                {
                    return request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        $"Resource Not Found {request.RequestUri} No Controller Selected");
                }

                controllerContext = CreateControllerContext(request, controllerDescriptor, controller);
                return await controller.ExecuteAsync(controllerContext, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Propogate the canceled task without calling exception loggers or handlers.
                throw;
            }
            catch (HttpResponseException httpResponseException)
            {
                return httpResponseException.Response;
            }
            catch (Exception exception)
            {
                exceptionInfo = ExceptionDispatchInfo.Capture(exception);
            }

            Debug.Assert(exceptionInfo.SourceException != null);

            var exceptionContext = new ExceptionContext(
                exceptionInfo.SourceException,
                ExceptionCatchBlocks.HttpControllerDispatcher,
                request)
            {
                ControllerContext = controllerContext,
            };

            await ExceptionLogger.LogAsync(exceptionContext, cancellationToken);
            var response = await ExceptionHandler.HandleAsync(exceptionContext, cancellationToken);

            if (response == null)
            {
                exceptionInfo.Throw();
            }

            return response;
        }

        private static HttpControllerContext CreateControllerContext(
            HttpRequestMessage request,
            HttpControllerDescriptor controllerDescriptor,
            IHttpController controller)
        {
            Contract.Assert(request != null);
            Contract.Assert(controllerDescriptor != null);
            Contract.Assert(controller != null);

            var controllerConfiguration = controllerDescriptor.Configuration;

            // Set the controller configuration on the request properties
            var requestConfig = request.GetConfiguration();
            if (requestConfig == null)
            {
                request.SetConfiguration(controllerConfiguration);
            }
            else
            {
                if (requestConfig != controllerConfiguration)
                {
                    request.SetConfiguration(controllerConfiguration);
                }
            }

            var requestContext = request.GetRequestContext();

            return new HttpControllerContext(requestContext, request, controllerDescriptor, controller);
        }

    }
}