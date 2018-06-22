using Communication.Client;
using System;
using System.Globalization;
using Xunit;

namespace Communication.Tests
{
    public class TestUrlBuilder
    {
        [Fact]
        public void UrlBuilder_BuildAnUrl_ByCallingAddMethods()
        {
            var ci = CultureInfo.InvariantCulture;
            var uri = new UrlBuilder(new Uri("http://bla:8080"), "<service>/<command>")
                .Add("boolOption", true)
                .Add("byteOption", (byte)123)
                .Add("intOption", 12345)
                .Add("longOption", 12345L)
                .Add("floatOption", 10.0f)
                .Add("doubleOption", 123.100)
                .Add("decimalOption", 1500.00m)
                .Add("stringOption", @"a&b=c/\дц")
                .Add("GuidOption", Guid.Parse("{C0DEC0DE-C0DE-C0DE-C0DE-C0DEC0DEC0DE}"))
                .Add("DateTimeoption", DateTime.Parse("2018-02-21 13:45:21.032", ci))
                .Add("DateTimeOffsetOption", DateTimeOffset.Parse("2018-02-21 13:45:21.032+2:00", ci))
                .Add("TimeSpanOption", TimeSpan.Parse(@"13:45:21.032", ci))
                .Uri;
            Assert.Equal(
                "http://bla:8080/<service>/<command>?" +
                "boolOption=True&" +
                "byteOption=123&" +
                "intOption=12345&" +
                "longOption=12345&" +
                "floatOption=10&" +
                "doubleOption=123.1&" +
                "decimalOption=1500.00&" +
                "stringOption=a%26b%3Dc%2F%5Cдц&" +
                "GuidOption=c0dec0de-c0de-c0de-c0de-c0dec0dec0de&" +
                "DateTimeoption=2018-02-21T13:45:21&" +
                "DateTimeOffsetOption=2018-02-21T13:45:21&" +
                "TimeSpanOption=13:45:21.0320000",

                uri.ToString()
            );
            
        }
    }
}
