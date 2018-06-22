using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Communication;
using Communication.Middleware;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Communication.Tests
{
    public class TestServiceMiddleWare
    {

        private Mock<HttpContext> PrepareHttpContext(IDictionary<string, string> headers, object service, string path, string querystring, string method, string request)
        {
            var headersMock = new Mock<IHeaderDictionary>();
            foreach (var headerKeyVal in headers)
            {
                headersMock.SetupGet(p => p[It.Is<string>(q => q == headerKeyVal.Key)]).Returns(headerKeyVal.Value);
            }

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString(path));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns(method);
            requestMock.Setup(x => x.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(request)));
            requestMock.Setup(x => x.QueryString).Returns(new QueryString(querystring));
            requestMock.Setup(x => x.Headers).Returns(headersMock.Object);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(It.IsAny<Type>())).Returns(service);

            var responseMock = new Mock<HttpResponse>();
            var responseStream = new MemoryStream();
            responseMock.Setup(x => x.Body).Returns(responseStream);
            responseMock.SetupProperty(x => x.StatusCode);

            var contextMock = new Mock<HttpContext>();
            contextMock.Setup(x => x.Request).Returns(requestMock.Object);
            contextMock.Setup(x => x.Response).Returns(responseMock.Object);
            contextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);

            return contextMock;
        }

        [Fact]
        public async Task ServiceMiddleWare_PerformGetAndPostRequests()
        {
            var loggerMock = new Mock<ILoggerFactory>();
            var coll = new MetadataCollection(new[] { typeof(ITestService) }, "bla", 1, 5);
            int version = 1;
            var service = new TestService();

            var headers = new Dictionary<string, string>
            {
                { "ServiceVersion", version.ToString() },
                { "Content-Type", "application/json" },
            };

            var middleware = new ServiceMiddleware(null, loggerMock.Object);

            // Add three persons
            var contextMock = PrepareHttpContext(headers, service, "/testservice/addperson", "", "POST",
                "{ person: { Name: 'Piet', Gender: 0, Age: 33 } }");
            await middleware.Invoke(contextMock.Object, coll[version]);

            contextMock = PrepareHttpContext(headers, service, "/testservice/addperson", "", "POST",
                "{ person: { Name: 'Jannie', Gender: 1, Age: 13 } }");
            await middleware.Invoke(contextMock.Object, coll[version]);

            contextMock = PrepareHttpContext(headers, service, "/testservice/addperson", "", "POST",
                 "{ person: { Name: 'Jan', Gender: 0, Age: 35 } }");
            await middleware.Invoke(contextMock.Object, coll[version]);

            // Find all persons starting with 'ja'
            contextMock = PrepareHttpContext(headers, service, "/testservice/FindPersons", "?name=ja", "GET", "");
            await middleware.Invoke(contextMock.Object, coll[version]);

            // Read the result
            string json = Encoding.UTF8.GetString((contextMock.Object.Response.Body as MemoryStream).ToArray());
            var result = JToken.Parse(json) as JArray;
            Assert.Equal(2, result.Count);
            Assert.All(result, it => Assert.StartsWith("ja", it["name"].Value<string>(), StringComparison.InvariantCultureIgnoreCase));

            // Find all persons starting with 'ja' asynchronously
            contextMock = PrepareHttpContext(headers, service, "/testservice/FindPersons2", "?name=ja", "GET", "");
            await middleware.Invoke(contextMock.Object, coll[version]);

            // Read the result
            json = Encoding.UTF8.GetString((contextMock.Object.Response.Body as MemoryStream).ToArray());
            result = JToken.Parse(json) as JArray;
            Assert.Equal(2, result.Count);
            Assert.All(result, it => Assert.StartsWith("ja", it["name"].Value<string>(), StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public async Task ServiceMiddleWare_CanHandleEmptyQuery()
        {
            var loggerMock = new Mock<ILoggerFactory>();
            var coll = new MetadataCollection(new[] { typeof(ITestService) }, "bla", 1, 5);
            int version = 1;
            var service = new TestService();

            var headers = new Dictionary<string, string>
            {
                { "ServiceVersion", version.ToString() },
                { "Content-Type", "application/json" },
            };

            var middleware = new ServiceMiddleware(null, loggerMock.Object);
            // Get request with an empty query
            var contextMock = PrepareHttpContext(headers, service, "/testservice/FindPersons", "", "GET", "");
            await middleware.Invoke(contextMock.Object, coll[version]);

            string json = Encoding.UTF8.GetString((contextMock.Object.Response.Body as MemoryStream).ToArray());
            Assert.Equal("[]", json);
        }

        [Fact]
        public async Task ServiceMiddleWare_ShouldRaiseDescriptiveError()
        {
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var coll = new MetadataCollection(new[] { typeof(ITestService) }, "bla", 1, 5);
            int version = 1;
            var service = new TestService();

            var headers = new Dictionary<string, string>
            {
                { "ServiceVersion", version.ToString() },
                { "Content-Type", "application/json" },
            };

            var middleware = new ServiceMiddleware(null, loggerFactoryMock.Object);

            // Missing required property Name
            var contextMock = PrepareHttpContext(headers, service, "/testservice/addperson", "", "POST",
                "{ person: { Gender: 0, Age: 33 } }");
            await middleware.Invoke(contextMock.Object, coll[version]);
            string message = Encoding.UTF8.GetString((contextMock.Object.Response.Body as MemoryStream).ToArray());
            Assert.StartsWith("Missing required property", message);
            Assert.Equal(400, contextMock.Object.Response.StatusCode);
        }
    }
}
