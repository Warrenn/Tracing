using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace TraceApplication
{
    public interface IService
    {
        int GetDoSomething(int id);
    }

    public class Service : IService
    {
        #region Implementation of IService

        public int GetDoSomething(int id)
        {
            return id;
        }

        #endregion
    }
}
