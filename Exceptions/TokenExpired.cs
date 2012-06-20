using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BCXAPI.Exceptions
{
    public class TokenExpired : GeneralAPIException
    {

        public TokenExpired()
            : base("You must refresh your token or request authentication from the user.",401)
        {
           
        }
    }
}
