using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Helpers;
using BCXAPI.Extensions;

namespace BCXAPI
{
    public class Service
    {
        private const string _BaseCampAPIURL = "https://basecamp.com/{0}/api/v1/{1}.json";
        private const string _AccountsURL = "https://launchpad.37signals.com/authorization.json";
        private const string _AuthorizationURL = "https://launchpad.37signals.com/authorization/new?type=web_server&client_id={0}&redirect_uri={1}{2}";
        private const string _AccessTokenURL = "https://launchpad.37signals.com/authorization/token?type=web_server&client_id={0}&redirect_uri={1}&client_secret={2}&code={3}";

        private readonly string _clientID;//the client id given to you by basecamp
        private readonly string _clientSecret;//the client secret given to you by basecamp
        private readonly string _redirectURI; //this must match what you've set up in your basecamp integration page
        private readonly string _appNameAndContact; //this will go in your User-Agent header when making requests. 37s recommends you add your app name and a contact URL or email.

        
        private static BCXAPI.Providers.IResponseCache _cache;
        private dynamic _accessToken;
        //get or set the access token here - this way if you just got it back from basecamp you dont need to reconstruct to entire object
        public dynamic AccessToken
        {
            get
            {
                return _accessToken;
            }
            set
            {
                _accessToken = value;
            }
        }

        /// <summary>
        /// create a service class with an authorization token retrieved from GetAuthToken (if you have it). 
        /// If you do not provide one then you will only be able to get the URL to the 
        /// basecamp authorization requested page and to validate a code returned to you by that authorization.
        /// parameters come from the app you set up at integrate.37signals.com
        /// </summary>
        /// <param name="clientID">your client id from 37s</param>
        /// <param name="clientSecret">your client secret from 37s</param>
        /// <param name="redirectURI">the redirect URI you set up with 37s - this must match</param>
        /// <param name="appNameAndContact">your application name and contact info - added to your request header</param>
        /// <param name="cache">an optional cache to use for caching responses from 37s. if you don't provide one, it'll use the System.Runtime.Caching.MemoryCache.Default cache</param>
        /// <param name="accessToken">if you have an access token, provide it here. this is the entire json object returned from the call to GetAccessToken</param>
        public Service(string clientID, 
            string clientSecret, 
            string redirectURI, 
            string appNameAndContact, 
            BCXAPI.Providers.IResponseCache cache = null, 
            dynamic accessToken = null)
        {
            if (cache == null)
            {
                _cache = new BCXAPI.Providers.DefaultMemoryCache();
            }
            else
            {
                _cache = cache;
            }
            
            _clientID = clientID;
            _clientSecret = clientSecret;
            _redirectURI = redirectURI;
            _appNameAndContact = appNameAndContact;
            _accessToken = accessToken;

            if (string.IsNullOrWhiteSpace(clientID) ||
                string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(redirectURI) ||
               string.IsNullOrWhiteSpace(_appNameAndContact))
            {
                throw new Exceptions.BaseException("You must provide the client id, client secret, redirect uri, and your app name and contact information to use the API.");
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                try
                {
                    return _accessToken != null && !string.IsNullOrWhiteSpace(_accessToken.access_token);

                }
                catch
                {
                    return false;
                }
            }
        }
        

        /// <summary>
        /// step 1: get the URL to redirect your users to
        /// </summary>
        /// <param name="optionalArguments">pass in this optional parameter to get these key value pairs passed back to your redirect URL in the query string</param>
        /// <returns>string of the URL to redirect to - since basecamp requires user authentication then you cannot make this request on the backend</returns>
        public string GetRequestAuthorizationURL(Dictionary<string, string> optionalArguments = null)
        {
            string additionalParams = string.Empty;

            if (optionalArguments != null)
            {
                System.Text.StringBuilder optionalParams = new StringBuilder();
                foreach (var kv in optionalArguments)
                {
                    optionalParams = optionalParams.AppendFormat("&{0}={1}", System.Web.HttpUtility.UrlEncode(kv.Key), System.Web.HttpUtility.UrlEncode(kv.Value));
                }
                additionalParams = optionalParams.ToString();
            }

            return string.Format(_AuthorizationURL, _clientID, _redirectURI, additionalParams);
        }
        
        /// <summary>
        ///step 2: Given a code that the url from GetRequestAuthorizationURL eventually redirects back to and the clientsecret you can get an access token. store this token somewhere 
        /// as you need to provide it to this wrapper to make calls.
        /// </summary>
        /// <param name="code">the code given to you by basecamp</param>
        /// <returns>the access token</returns>
        public dynamic GetAccessToken(string code)
        {
            try
            {
                string url = string.Format(_AccessTokenURL, _clientID, _redirectURI, _clientSecret, code);
                var wr = System.Net.HttpWebRequest.Create(url);
                wr.Method = "POST";
                var resp = (System.Net.HttpWebResponse)wr.GetResponse();
                using (var sw = new System.IO.StreamReader(resp.GetResponseStream()))
                {
                    _accessToken = Json.Decode(sw.ReadToEnd());
                }
                return _accessToken;
            }
            catch
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        /// <summary>
        /// helper method to make a get request to basecamp. checks cache first if you've already received that response and checks with basecamp if you
        /// need to update your cache.
        /// </summary>
        /// <param name="url">the api method endpoint being called</param>
        /// <returns>a dynamic object - matches the json from basecamp exactly</returns>
        private dynamic _getJSONFromURL(string url)
        {
            // ensure url ends with .json or .json?xxx
            if (!url.ToLower().EndsWith(".json") && 
                !(url.Contains("?") && url.ToLower().Substring(0, url.IndexOf("?")).EndsWith(".json")))
            {
                throw new ArgumentException("Invalid URL. URLs must end in .json", url);
            }

            string unique_id_to_hash = (_accessToken.access_token + url.ToLower());
            var cacheKey = unique_id_to_hash.CalculateMD5();
            try
            {
                //if in cache, check with server and if not modified then return original results
                string cached_results = (string)_cache.Get(cacheKey);
                if (cached_results != null)
                {
                    string if_none_match = (string)_cache.Get(cacheKey + "etag");
                    string if_modified_since = (string)_cache.Get(cacheKey + "lastModified");

                    System.Net.HttpWebRequest wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                    wr.Method = "HEAD";
                    wr.Headers.Add(System.Net.HttpRequestHeader.Authorization, string.Format("Bearer {0}", _accessToken.access_token));
                    wr.UserAgent = _appNameAndContact;
                    if (!string.IsNullOrWhiteSpace(if_modified_since))
                    {
                        wr.IfModifiedSince = DateTime.Parse(if_modified_since);
                    }
                    if (!string.IsNullOrWhiteSpace(if_none_match))
                    {
                        wr.Headers["If-None-Match"] = if_none_match;
                    }
                    var resp = (System.Net.HttpWebResponse)wr.BetterGetResponse();//use extension to properly handle 304
                    if (resp.StatusCode == System.Net.HttpStatusCode.NotModified)
                    {
                        return Json.Decode(cached_results);
                    }
                }
            }
            catch
            {
                //if cache check fails just make the real request to basecamp
            }

            try
            {
                System.Net.HttpWebRequest wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                wr.Method = "GET";
                wr.Headers.Add(System.Net.HttpRequestHeader.Authorization, string.Format("Bearer {0}", _accessToken.access_token));
                wr.UserAgent = _appNameAndContact;

                var resp = (System.Net.HttpWebResponse)wr.BetterGetResponse();
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    using (var sw = new System.IO.StreamReader(resp.GetResponseStream()))
                    {
                        var strResp = sw.ReadToEnd();
                        var json_results = Json.Decode(strResp);
                        var resp_etag = resp.Headers["ETag"] != null ? resp.Headers["ETag"] : null;
                        var resp_last_modified = resp.Headers["Last-Modified"] != null ? resp.Headers["Last-Modified"] : null;

                        if (resp_etag != null || resp_last_modified != null)
                        {
                            //cache it
                            if (!string.IsNullOrWhiteSpace(resp_etag))
                            {
                                _cache.Set(cacheKey + "etag", resp_etag);
                            }
                            if (!string.IsNullOrWhiteSpace(resp_last_modified))
                            {
                                _cache.Set(cacheKey + "lastModified", resp_last_modified);
                            }
                            if (!string.IsNullOrWhiteSpace(strResp))
                            {
                                _cache.Set(cacheKey, strResp);
                            }
                        }
                        return json_results;
                    }
                }
                else if (resp.StatusCode == (System.Net.HttpStatusCode)429)//too many requests
                {
                    throw new Exceptions.RateLimitExceededException(int.Parse(resp.Headers["Retry-After"]));
                }
                else
                {
                    throw new Exceptions.GeneralAPIException("Try again later. Status code returned was " + (int)resp.StatusCode, (int)resp.StatusCode);
                }
            }
            catch
            {
                return null;
            }
        }

        public dynamic GetAccounts()
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(_AccountsURL);
                
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetProjects(int accountID, bool archived = false)
        {
            if (IsAuthenticated)
            {
                if (archived)
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, "projects/archived"));
                }
                else
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, "projects"));
                }
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetProject(int accountID, int projectID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}", projectID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetAccessesForProject(int accountID, int projectID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/accesses",projectID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetAccessesForCalendar(int accountID, int calendarID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("calendars/{0}/accesses", calendarID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetCalendars(int accountID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, "calendars"));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetCalendar(int accountID, int calendarID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("calendars/{0}", calendarID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetPeople(int accountID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, "people"));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetPerson(int accountID, int personID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("people/{0}", personID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetTodo(int accountID, int projectID, int todoID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/todos/{1}", projectID, todoID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetDocuments(int accountID, int projectID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/documents", projectID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetDocument(int accountID, int projectID, int documentID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/documents/{1}", projectID, documentID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetTopics(int accountID, int projectID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/topics", projectID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetAttachments(int accountID, int projectID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/attachments", projectID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetUpload(int accountID, int projectID, int uploadID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/uploads/{1}", projectID, uploadID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }
        
        public dynamic GetTodoLists(int accountID, int projectID, bool completed=false)
        {
            if (IsAuthenticated)
            {
                if (completed)
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/todolists/completed", projectID)));
                }
                else
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/todolists", projectID)));
                }
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetTodoListsWithAssignedTodos(int accountID, int personID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("people/{0}/assigned_todos", personID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetTodoList(int accountID, int projectID, int todoListID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/todolists/{1}", projectID, todoListID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetMessage(int accountID, int projectID, int messageID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/messages/{1}", projectID, messageID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetGlobalEvents(int accountID, DateTime? since = null, int page = 1)
        {
            if (IsAuthenticated)
            {
                since = since ?? DateTime.MinValue;
                string string_since = since.Value.ToString("yyyy-MM-ddTHH:mmzzz");
                string url = string.Format("{0}?since={1}",
                    string.Format(_BaseCampAPIURL, accountID, "events"),
                   string_since);
                if (page != 1)
                {
                    url = string.Format("&page={1}", url, page);
                }
                
                return _getJSONFromURL(url);
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetProjectEvents(int accountID, int projectID, DateTime? since = null, int page = 1)
        {
            if (IsAuthenticated)
            {
                since = since ?? DateTime.MinValue;
                string string_since = since.Value.ToString("yyyy-MM-ddTHH:mmzzz");
                string url = string.Format("{0}?since={1}",
                    string.Format(_BaseCampAPIURL, accountID, string.Format( "projects/{0}/events", projectID)),
                   string_since);
                if (page != 1)
                {
                    url = string.Format("&page={1}", url, page);
                }

                return _getJSONFromURL(url);
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetPersonEvents(int accountID, int personID, DateTime? since = null, int page = 1)
        {
            if (IsAuthenticated)
            {
                since = since ?? DateTime.MinValue;
                string string_since = since.Value.ToString("yyyy-MM-ddTHH:mmzzz");
                string url = string.Format("{0}?since={1}",
                    string.Format(_BaseCampAPIURL, accountID, string.Format("people/{0}/events", personID)),
                   string_since);
                if (page != 1)
                {
                    url = string.Format("&page={1}", url, page);
                }

                return _getJSONFromURL(url);
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetCalendarEventsForProject(int accountID, int projectID, bool past = false)
        {
            if (IsAuthenticated)
            {
                if (!past)
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/calendar_events", projectID)));
                }
                else
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/calendar_events/past", projectID)));
                }
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetCalendarEvents(int accountID, int calendarID, bool past = false)
        {
            if (IsAuthenticated)
            {
                if (!past)
                {
                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("calendars/{0}/calendar_events", calendarID)));
                }
                else
                {

                    return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("calendars/{0}/calendar_events/past", calendarID)));
                }
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetCalendarEventForProject(int accountID, int projectID, int calendarEventID)
        {
            if (IsAuthenticated)
            {
                  return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("projects/{0}/calendar_events/{1}", projectID, calendarEventID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }

        public dynamic GetCalendarEvent(int accountID, int calendarID, int calendarEventID)
        {
            if (IsAuthenticated)
            {
                return _getJSONFromURL(string.Format(_BaseCampAPIURL, accountID, string.Format("calendars/{0}/calendar_events/{1}", calendarID, calendarEventID)));
            }
            else
            {
                throw new Exceptions.UnauthorizedException();
            }
        }
    }
}
