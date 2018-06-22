using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Communication;
using Communication.Middleware;
using Communication.Versioning;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Communication.Tests
{
    public class TestMetadataMiddleWare
    {
        [ExternalName("MyService")]
        private interface ITestService
        {
            [ExternalName("MyMethod")]
            IEnumerable<int> TestMethod();
        }

        [Fact]
        public async Task MetadataMiddleware_ReturnsMetadata_WhenRequested()
        {
            var coll = new MetadataCollection(new[] { typeof(ITestService) }, "bla", 1, 5);
            int version = 2;

            var loggerMock = new Mock<ILoggerFactory>();

            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.SetupGet(p => p[It.Is<string>(q => q == "ServiceVersion")]).Returns(version.ToString());

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/metadata"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());
            requestMock.Setup(x => x.QueryString).Returns(new QueryString(""));
            requestMock.Setup(x => x.Headers).Returns(headersMock.Object);

            var responseMock = new Mock<HttpResponse>();
            var responseStream = new MemoryStream();
            responseMock.Setup(x => x.Body).Returns(responseStream);

            var contextMock = new Mock<HttpContext>();
            contextMock.Setup(x => x.Request).Returns(requestMock.Object);
            contextMock.Setup(x => x.Response).Returns(responseMock.Object);

            // Create the middleware to test
            var middleware = new MetadataEndpointMiddleware(
                (innerHttpContext) => Task.FromResult(0),
                loggerMock.Object
            );

            // Invoke the middleware
            await middleware.Invoke(contextMock.Object, coll);

            // Parse the result
            string json = Encoding.UTF8.GetString(responseStream.ToArray());
            var metadata = Metadata.FromJson(json);
            var myService = metadata.Services.Values.First();
            var myCommand = myService.Commands.Values.First();

            // Checks
            Assert.Equal(version, metadata.CurVersion);
            Assert.Equal("MyService", myService.Name);
            Assert.Equal("myMethod", myCommand.Name);
        }
    }
}
