using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BCXAPI.Exceptions
{
    public class RateLimitExceededException : GeneralAPIException
    {
        private int _retryInSeconds;
        public int RetryInSeconds
        {
            get
            {
                return _retryInSeconds;
            }
            
        }
        public RateLimitExceededException(int retry_in_seconds): base(string.Format("Rate limit exceeded, try again in {0}", retry_in_seconds), 429) {
            _retryInSeconds = retry_in_seconds;
        }
    }
}
