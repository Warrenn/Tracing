using System.Web.Http;

namespace TraceApplication.Controllers
{
    public class CustomController<T> : ApiController where T : class
    {
        public readonly T Instance;

        public CustomController(T instance)
        {
            Instance = instance;
        }
    }
}