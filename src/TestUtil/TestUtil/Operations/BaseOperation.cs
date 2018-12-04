using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using WomPlatform.Web.Api;

namespace TestUtil.Operations {

    public abstract class BaseOperation {

        public string Host { get; set; }
        public int Port { get; set; }

        protected RestClient Client {
            get {
                return new RestClient($"http://{Host}:{Port}/api/v1");
            }
        }

        public CryptoProvider Crypto { get; set; }

        public abstract void Execute(string[] args);

        protected RestRequest CreateJsonRequest(string urlPath, object jsonBody) {
            var request = new RestRequest(urlPath, Method.POST) {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Accept", "application/json");
            if (jsonBody != null) {
                request.AddJsonBody(jsonBody);
            }
            return request;
        }

        protected T PerformRequest<T>(RestRequest request) {
            var response = Client.Execute(request);
            Console.Error.WriteLine("HTTP {0}, {1} bytes, content type: {2}", response.StatusCode, response.ContentLength, response.ContentType);
            if(response.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new InvalidOperationException("API did not return 200 OK");
            }

            return JsonConvert.DeserializeObject<T>(response.Content);
        }

    }

}
