using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BCXAPI.Exceptions
{
    public class GeneralAPIException : BaseException
    {
        private int _statusCode;
        public int StatusCode
        {
            get
            {
                return _statusCode;
            }
        }
        public GeneralAPIException(string message, int status_code = 500) : base(message) {
            _statusCode = status_code;
        }
    }
}
