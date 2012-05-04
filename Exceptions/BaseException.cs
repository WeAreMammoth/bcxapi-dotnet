using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BCXAPI.Exceptions
{
    public class BaseException : System.Exception
    {
       
        
        public BaseException(string message) : base(message) {
        }
    }
}
