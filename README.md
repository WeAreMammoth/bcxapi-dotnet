bcxapi-dotnet
=============

Wrapper for the new Basecamp API in .NET (https://github.com/37signals/bcx-api). Grab the package on nuget here: http://nuget.org/packages/BCXAPI

Only provides GET requests right now. Uses dynamic objects for all responses so you won't get intellisense.

We don't automatically refresh the token for you with Basecamp right now after the two week expiration period. Still getting the exact details of that figured out and will push an update soon.

Examples
=============
1. Register your app with 37signals at https://integrate.37signals.com/

2. In your app, construct an instance of the library like so


            BCXAPI.Service s = new BCXAPI.Service(**ClientID**,                 **ClientSecret**,                **RedirectURI**,                **YOUR_APP (YOUR_CONTACT@COMPANY.COM)**,                 cache: **OPTIONAL_CACHE**);3. Get the Request Authorization URL from basecamp if you've never authorized this user before. 
            s.GetRequestAuthorizationURL(new Dictionary<string, string>() { { **PARAM_KEY**, **PARAM_VALUE** } }));

where optionally **PARAM_KEY** is the querystring parameter and **PARAM_VALUE** is its value that you want 37s to pass through to your REDIRECT_URI

4. On your REDIRECT_URI, you will get a **code** from 37s. Again instantiate the service and  get the access token:
            BCXAPI.Service s = new BCXAPI.Service(DoneDone.Utilities.Config.BCXAPI_ClientID,                 DoneDone.Utilities.Config.BCXAPI_ClientSecret,                DoneDone.Utilities.Config.BCXAPI_RedirectURI,                DoneDone.Utilities.Config.BCXAPI_ApplicationNameAndContact,                cache: new DoneDone.Utilities.CacheForBCXAPI());            dynamic access_token = s.GetAccessToken(code); 

Store this entire access token somewhere (database, cache, memory, etc) - you will not have to request authorization for a user who has this access token until it expires (in which case you'll need to refresh the access token).

5. From here on out, when you create the service create it and pass along this access_token object like so:

            BCXAPI.Service s = new BCXAPI.Service    (DoneDone.Utilities.Config.BCXAPI_ClientID,                DoneDone.Utilities.Config.BCXAPI_ClientSecret,                DoneDone.Utilities.Config.BCXAPI_RedirectURI,                DoneDone.Utilities.Config.BCXAPI_ApplicationNameAndContact,                new DoneDone.Utilities.CacheForBCXAPI(),                access_token /*access token from above*/);

6. Make calls to the api -
             //returns ALL 37s accounts for this user this API only works 
             //with the new Basecamp for now as far as we can tell.
             dynamic accounts = s.GetAccounts(); 

That's all for now - this wrapper only implements the GET methods for the API. PUT/POST coming soon. Feel free to submit pull requests and I'll merge and repackage our NUGET package (http://nuget.org/packages/BCXAPI)
