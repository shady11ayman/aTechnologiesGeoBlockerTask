using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions; 
using GeoBlocker.Application.Models;
using GeoBlocker.Infrastructure.Geolocation;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace GeoBlocker.Tests
{
    public class IpApiServiceTests
    {
        [Fact]
        public async Task LookupAsync_ShouldReturnGeoResult_WhenApiReturnsSuccess()
        {
            // Arrange
            var fakeJson = @"{
                ""ip"": ""1.2.3.4"",
                ""country_code2"": ""US"",
                ""country_name"": ""United States"",
                ""isp"": ""FakeISP""
            }";

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson)
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var ipApiConfig = Options.Create(new IpApiConfig
            {
                BaseUrl = "http://fake-api.com",
                ApiKey = "fake-api-key"
            });

            var service = new IpApiService(httpClient, ipApiConfig);

            // Act
            var result = await service.LookupAsync("1.2.3.4", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Ip.Should().Be("1.2.3.4");
            result.CountryCode.Should().Be("US");
            result.CountryName.Should().Be("United States");
            result.Org.Should().Be("FakeISP");
        }

        [Fact]
        public async Task LookupAsync_ShouldReturnNull_WhenApiCallIsNotSuccessful()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var ipApiConfig = Options.Create(new IpApiConfig
            {
                BaseUrl = "http://fake-api.com",
                ApiKey = "fake-api-key"
            });

            var service = new IpApiService(httpClient, ipApiConfig);

            // Act
            var result = await service.LookupAsync("8.8.8.8", CancellationToken.None);

            // Assert
            result.Should().BeNull("we return null if the upstream call fails (non-2xx status)");
        }

        [Fact]
        public async Task LookupAsync_ShouldRetryOn429AndEventuallyReturnSuccess()
        {
            // Arrange
            var attempt = 0;
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    attempt++;
                    if (attempt < 3)
                    {
                        // Return 429 on first two attempts
                        var response429 = new HttpResponseMessage((HttpStatusCode)429);
                        response429.Headers.Add("Retry-After", "1");
                        return response429;
                    }
                    else
                    {
                        // Third attempt success
                        var fakeJson = @"{
                            ""ip"": ""8.8.8.8"",
                            ""country_code2"": ""US"",
                            ""country_name"": ""United States"",
                            ""isp"": ""FakeISP""
                        }";
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(fakeJson)
                        };
                    }
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var ipApiConfig = Options.Create(new IpApiConfig
            {
                BaseUrl = "http://fake-api.com",
                ApiKey = "fake-api-key"
            });

            var service = new IpApiService(httpClient, ipApiConfig);

            // Act
            var result = await service.LookupAsync("8.8.8.8", CancellationToken.None);

            // Assert
            attempt.Should().Be(3, "the service should have retried up to the 3rd attempt");
            result.Should().NotBeNull("the 3rd attempt was finally successful");
            result!.Ip.Should().Be("8.8.8.8");
        }
    }
}