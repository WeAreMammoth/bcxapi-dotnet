using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BCXAPI.Exceptions
{
    public class UnauthorizedException : GeneralAPIException
    {

        public UnauthorizedException()
            : base("You cannot be authenticated with Basecamp.", 401)
        {
           
        }
    }
}
