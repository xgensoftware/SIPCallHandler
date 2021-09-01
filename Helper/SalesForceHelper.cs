using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIPCallHandler.Helper;
namespace SIPCallHandler
{
    public class SalesForceHelper
    {
        #region Member Variables
        RestClient _client = null;

        class AssetAttribute
        {
            public string type { get; set; }
            public string url { get; set; }
        }
        class Asset
        {
            public string Status { get; set; }

            public string Id { get; set; }

            public AssetAttribute attributes { get; set; }
        }
        class APIResponse
        {
            public bool done { get; set;}

            public int totalSize { get; set; }

            public List<Asset> records { get; set; }
        }
        #endregion

        #region Private Methods
        void GenerateAuthToken()
        {
            IRestResponse response = null;
            string loginUrl = $"https://login.salesforce.com/services/oauth2/token";

            RestRequest request = new RestRequest(loginUrl, Method.POST);
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_id", ConfigurationManager.AppSettings["SFClientId"].ToString());
            request.AddParameter("client_secret", ConfigurationManager.AppSettings["SFSecret"].ToString());
            request.AddParameter("username", ConfigurationManager.AppSettings["SFUser"].ToString());
            request.AddParameter("password", ConfigurationManager.AppSettings["SFPassword"].ToString());

            response = _client.Execute(request);
            if (response != null)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                    var token = jsonObj["access_token"];
                    var instance = jsonObj["instance_url"];
                    ConfigHelper.SetSetting("SFToken", token);
                    ConfigHelper.SetSetting("SalesforceInstance", instance);
                }
            }

        }

        string GetAuthToken()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["SFToken"]))
            {
                GenerateAuthToken();
            }
            return ConfigurationManager.AppSettings["SFToken"].ToString();
        }

        RestRequest FormRquest(Method method, string url)
        {
            RestRequest request = new RestRequest(url, method);
            request.RequestFormat = DataFormat.Json;
            return request;
        }

        IRestResponse ExecuteRequest(RestRequest request)
        { 
            request.AddHeader("Authorization", $"Bearer {GetAuthToken()}");
            var response =  _client.Execute(request);
            bool processToken = false;

            switch(response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    processToken = true;

                    break;
            }

            if(processToken)
            {
                try
                {
                    GenerateAuthToken();
                    if (request.Parameters.Exists(p => p.Name == "Authorization"))
                        request.Parameters.Find(p => p.Name == "Authorization").Value = $"Bearer {ConfigurationManager.AppSettings["SFToken"].ToString()}";
                    else
                        request.AddHeader("Authorization", $"Bearer {ConfigurationManager.AppSettings["SFToken"].ToString()}");
                }
                catch { }

                response = _client.Execute(request);
            }

            return response;
        }
        #endregion
        public SalesForceHelper()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12;

            _client = new RestClient(ConfigurationManager.AppSettings["SalesforceInstance"].ToString());
        }


        public bool AssetExists(string unitId)
        {
            bool doesExist = false;
            string url = $"{ConfigurationManager.AppSettings["SalesforceInstance"]}/services/data/v52.0/query";
            var request = FormRquest(Method.GET, url);
            request.AddParameter("q", $"SELECT ID, Status FROM Asset WHERE CSID__c = '{unitId}'");

            var response = ExecuteRequest(request);
            if(response != null)
            {
                var result = JsonConvert.DeserializeObject<APIResponse>(response.Content);
                if (result.totalSize > 0)
                    doesExist = true;
            }

            return doesExist;
        }
    }
}
