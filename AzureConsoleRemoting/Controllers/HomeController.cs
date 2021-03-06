﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AzureConsoleRemoting.Models;
using System.Net;
using System.IO;
using System.Web;
using System.Text;

namespace AzureConsoleRemoting.Controllers
{
    public class HomeController : Controller
    {
        [HttpPost]
        public async Task<ActionResult> ProcessLogin(string login, string password)
        {
            bool isDiagnosticsEnabled;
            SignInEvents azureSignInEvents;
            string azureTokenForHiddenApi;
            string azureTokenForNormalApi;
            try
            {
                azureTokenForHiddenApi = await getAzureToken(login, password, "74658136-14ec-4630-ad9b-26e160ff0fc6");
                azureTokenForNormalApi = await getAzureToken(login, password, "https://management.core.windows.net/");
                isDiagnosticsEnabled = await checkIfdiagnosticsIsEnabled(azureTokenForNormalApi);
            }catch (Exception e)
            {
                return View("Error", (new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, description = "Failed to get Sign-in events, potential causes: MFA enabled on your account or tenant does not have premium licenses", debugData = e }));
            }
            GuestUsers guestUsers;
            try
            {
                guestUsers = await getTenantGuestUsers(azureTokenForHiddenApi);
            }
            catch (Exception e)
            {
                return View("Error", (new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, description = "Failed to get guest users", debugData = e }));
            }
            ViewData["diagnosticsEnabled"] = isDiagnosticsEnabled;
            GuestInfoRows reportRows = new GuestInfoRows {};
            reportRows.items = new List<GuestInfoRow>();
            foreach (Guest guestUser in guestUsers.items)
            {
                azureSignInEvents = await getSignInEvents(azureTokenForHiddenApi, guestUser.objectId, DateTime.Today.AddDays(-29), DateTime.Now);
                SigninEvent lastSignIn;
                GuestInfoRow newRow;
                if (azureSignInEvents.items.Count > 0)
                {
                    lastSignIn = azureSignInEvents.items[0]; //get most recent one?
                    reportRows.items.Add(new GuestInfoRow () { userPrincipalName = guestUser.userPrincipalName, objectId = guestUser.objectId, lastLogin = lastSignIn.createdDateTime, loginSucceeded = lastSignIn.loginSucceeded, application = lastSignIn.appDisplayName });
                }
                else
                {
                    reportRows.items.Add(new GuestInfoRow () { userPrincipalName = guestUser.userPrincipalName, objectId = guestUser.objectId, lastLogin = null, loginSucceeded = false, application = "none" });
                }

            }
            return View(reportRows);
        }
        [HttpGet]
        public async Task<ActionResult> Login()
        {
            return View();
        }

        private async Task<GuestUsers> getTenantGuestUsers(string azureToken)
        {
            var req = WebRequest.Create("https://main.iam.ad.ext.azure.com/api/Users?searchText=&top=500&orderByThumbnails=false&maxThumbnailCount=999&filterValue=Guest&state=Guest&adminUnit=");
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Headers["X-Requested-With"] = "XMLHttpRequest";
            req.Headers["x-ms-client-request-id"] = Guid.NewGuid().ToString();
            req.Headers["Authorization"] = "Bearer " + azureToken;
            string postData = "";
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] byte1 = encoding.GetBytes(postData);
            Stream newStream = req.GetRequestStream();
            req.ContentLength = byte1.Length;
            newStream.Write(byte1, 0, byte1.Length);
            newStream.Close();
            WebResponse response = await req.GetResponseAsync().ConfigureAwait(false);
            var responseReader = new StreamReader(response.GetResponseStream());
            var responseData = await responseReader.ReadToEndAsync();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<GuestUsers>(responseData);
            return data;
        }

        private async Task<string> getTenantTokenEndpoint(string username)
        {
            var domain = username.Split('@')[1];
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://login.microsoftonline.com/" + domain + "/.well-known/openid-configuration");

            HttpWebResponse response = (HttpWebResponse)req.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            var jsondata = reader.ReadToEnd();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<tenantInfoResponse>(jsondata).token_endpoint;
        }

        private async Task<bool> checkIfdiagnosticsIsEnabled(string azureToken)
        {
            var req = WebRequest.Create("https://management.azure.com/api/invoke");
            req.ContentType = "application/json";
            req.Method = "GET";
            req.Headers["Authorization"] = "Bearer " + azureToken;
            req.Headers["x-ms-path-query"] = "/providers/microsoft.aadiam/diagnosticSettings?api-version=2017-04-01-preview";

            HttpWebResponse response = (HttpWebResponse)req.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            var jsondata = reader.ReadToEnd();
            if(jsondata.Length > 15)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<SignInEvents> getSignInEvents(string azureToken, string userId, DateTime startDateTime, DateTime endDateTime)
        {
            var req = WebRequest.Create("https://main.iam.ad.ext.azure.com/api/Reports/SignInEventsV2");
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Headers["X-Requested-With"] = "XMLHttpRequest";
            req.Headers["Authorization"] = "Bearer " + azureToken;
            string postData = "{\"startDateTime\":\""+ startDateTime+"\",\"endDateTime\":\""+ endDateTime+"\",\"userId\":\""+ userId + "\"}";
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] byte1 = encoding.GetBytes(postData);
            Stream newStream = req.GetRequestStream();
            req.ContentLength = byte1.Length;
            newStream.Write(byte1, 0, byte1.Length);
            newStream.Close();
            WebResponse response = await req.GetResponseAsync().ConfigureAwait(false);
            var responseReader = new StreamReader(response.GetResponseStream());
            var responseData = await responseReader.ReadToEndAsync();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SignInEvents>(responseData);
            return data;
        }

        private async Task<String> getAzureToken(string login,string password,string audience)
        {

            var url = await getTenantTokenEndpoint(login);
            var req = WebRequest.Create(url);
            req.ContentType = "application/x-www-form-urlencoded ";
            req.Method = "POST";
            login = HttpUtility.UrlEncode(login);
            password = HttpUtility.UrlEncode(password);
            audience = HttpUtility.UrlEncode(audience);
            Guid clientId = Guid.NewGuid();
            string postData = "resource="+audience+"&client_id=1950a258-227b-4e31-a9cf-717495945fc2&grant_type=password&username=" + login + "&password=" + password;
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] byte1 = encoding.GetBytes(postData);
            Stream newStream = req.GetRequestStream();
            req.ContentLength = byte1.Length;
            newStream.Write(byte1, 0, byte1.Length);
            newStream.Close();
            WebResponse response = await req.GetResponseAsync().ConfigureAwait(false);
            var responseReader = new StreamReader(response.GetResponseStream());
            var responseData = await responseReader.ReadToEndAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<azureTokenResponse>(responseData).access_token;
        }

        public class azureTokenResponse
        {
            public string access_token { get; set; }
        }

        public class Location
        {
            public string city { get; set; }
            public string state { get; set; }
            public string country { get; set; }
        }

        public class ConditionalAccessPolicy
        {
            public string id { get; set; }
            public string displayName { get; set; }
            public List<object> enforcedAccessControls { get; set; }
            public List<object> enforcedSessionControls { get; set; }
            public int result { get; set; }
        }

        public class SigninEvent
        {
            public string id { get; set; }
            public DateTime createdDateTime { get; set; }
            public string userDisplayName { get; set; }
            public string userPrincipalName { get; set; }
            public string userId { get; set; }
            public string appId { get; set; }
            public string appDisplayName { get; set; }
            public string ipAddress { get; set; }
            public bool loginSucceeded { get; set; }
            public int errorCode { get; set; }
            public object failureReason { get; set; }
            public string clientAppUsed { get; set; }
            public string deviceId { get; set; }
            public string deviceBrowser { get; set; }
            public string operatingSystem { get; set; }
            public bool? deviceCompliant { get; set; }
            public bool? deviceManaged { get; set; }
            public string deviceTrustType { get; set; }
            public Location location { get; set; }
            public bool mfaRequired { get; set; }
            public object mfaAuthMethod { get; set; }
            public object mfaAuthDetail { get; set; }
            public object mfaResult { get; set; }
            public string correlationId { get; set; }
            public int conditionalAccessStatus { get; set; }
            public List<object> conditionalAccessPolicies { get; set; }
            public bool isRisky { get; set; }
        }

        public class SignInEvents
        {
            public List<SigninEvent> items { get; set; }
            public string nextLink { get; set; }
        }

        public class tenantInfoResponse
        {
            public string authorization_endpoint { get; set; }
            public string token_endpoint { get; set; }
            public List<string> token_endpoint_auth_methods_supported { get; set; }
            public string jwks_uri { get; set; }
            public List<string> response_modes_supported { get; set; }
            public List<string> subject_types_supported { get; set; }
            public List<string> id_token_signing_alg_values_supported { get; set; }
            public bool http_logout_supported { get; set; }
            public bool frontchannel_logout_supported { get; set; }
            public string end_session_endpoint { get; set; }
            public List<string> response_types_supported { get; set; }
            public List<string> scopes_supported { get; set; }
            public string issuer { get; set; }
            public List<string> claims_supported { get; set; }
            public bool microsoft_multi_refresh_token { get; set; }
            public string check_session_iframe { get; set; }
            public string userinfo_endpoint { get; set; }
            public string tenant_region_scope { get; set; }
            public string cloud_instance_name { get; set; }
            public string cloud_graph_host_name { get; set; }
            public string msgraph_host { get; set; }
            public string rbac_url { get; set; }
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
