using Newtonsoft.Json;
using Communication.Exceptions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Communication.Client
{
    public class PcaHttpClient: HttpClient
    {
        private readonly int version;
        private readonly Uri baseUri;
        private readonly JsonSerializerSettings serializerSettings;

        public PcaHttpClient(Uri baseUri, int version, TypeMapper typeMapper = null)
            : base()
        {
            this.version = version;
            this.baseUri = baseUri;
            this.serializerSettings = new JsonSerializerSettings();
            if (typeMapper != null)
                serializerSettings.Converters.Add(new DerivedEntityJsonConverter(typeMapper));
        }

        private async Task<string> Execute(HttpRequestMessage requestMessage)
        {
            var rsp = await SendAsync(requestMessage);
            string result = await rsp.Content.ReadAsStringAsync();
            if (rsp.StatusCode != HttpStatusCode.OK)
            {
                throw new ServiceException(rsp.StatusCode, result);
            }
            return result;
        }

        public HttpRequestMessage BuildGetRequest(string service, string command, Action<UrlBuilder> queryOptions = null)
        {
            var uriBuilder = new UrlBuilder(baseUri, $"{service}/{command}");
            queryOptions?.Invoke(uriBuilder);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            requestMessage.Headers.Add("ServiceVersion", version.ToString());
            return requestMessage;
        }

        public HttpRequestMessage BuildPostRequest(string service, string command, object requestBody = null)
        {
            var uri = new Uri(baseUri, $"{service}/{command}");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Headers.Add("ServiceVersion", version.ToString());
            requestMessage.Content = new StringContent(requestBody == null ? null : JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            return requestMessage;
        }

        public async Task Get(string service, string command, Action<UrlBuilder> queryOptions = null)
        {
            await Execute(BuildGetRequest(service, command, queryOptions));
        }

        public async Task<T> Get<T>(string service, string command, Action<UrlBuilder> queryOptions = null)
        {
            string json = await Execute(BuildGetRequest(service, command, queryOptions));
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }

        public async Task Post(string service, string command, object requestBody = null)
        {
            await Execute(BuildPostRequest(service, command, requestBody));
        }

        public async Task<T> Post<T>(string service, string command, object requestBody = null)
        {
            string json = await Execute(BuildPostRequest(service, command, requestBody));
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }
    }
}
