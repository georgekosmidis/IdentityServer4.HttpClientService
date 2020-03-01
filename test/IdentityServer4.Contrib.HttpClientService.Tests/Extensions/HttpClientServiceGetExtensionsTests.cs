using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityServer4.Contrib.HttpClientService.Models;
using Microsoft.Extensions.Options;
using IdentityServer4.Contrib.HttpClientService;
using IdentityServer4.Contrib.HttpClientService.Tests.Helpers;
using IdentityServer4.Contrib.HttpClientService.Infrastructure;
using System.Net;
using IdentityModel.Client;
using System;
using System.IO;
using System.Collections.Generic;
using IdentityServer4.Contrib.HttpClientService.Extensions;

namespace IdentityServer4.Contrib.HttpClientService.Test
{
    [TestClass]
    public class HttpClientServiceGetExtensionsTests : TestBase
    {

        [TestMethod]
        public async Task HttpClientServiceGet_NoTypedResponse()
        {

            var httpClientService = new HttpClientServiceFactory(
                IConfigurationMocks.Get("section_data"),
                IHttpClientFactoryMocks.Get(HttpStatusCode.OK, this.ComplexTypeResponseString),
                new HttpRequestMessageFactory(
                    IHttpContextAccessorMocks.Get()
                ),
                new TokenResponseService(
                    IHttpClientFactoryMocks.Get(HttpStatusCode.OK),
                    IAccessTokenCacheManagerMocks.Get(
                        await TokenResponseMock.GetValidResponseAsync("access_token", 3600)
                    )
                )
            ).CreateHttpClientService();

            var result = await httpClientService.GetAsync("http://localhost");

            httpClientService.Dispose();

            //Status/HttpResponseMessage
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpResponseMessage.StatusCode);

            //HttpRequestMessage
            Assert.AreEqual(HttpMethod.Get, result.HttpRequestMessge.Method);
            Assert.IsNull(result.HttpRequestMessge.Content);

            //Body
            Assert.AreEqual(this.ComplexTypeResponseString, result.BodyAsString);
            var sr = new StreamReader(result.BodyAsStream);
            Assert.AreEqual(this.ComplexTypeResponseString, sr.ReadToEnd());
            Assert.AreEqual(this.ComplexTypeResponseString, result.BodyAsType);
        }

        [TestMethod]
        public async Task HttpClientServiceGet_TypedResponse()
        {

            var httpClientService = new HttpClientServiceFactory(
                IConfigurationMocks.Get("section_data"),
                IHttpClientFactoryMocks.Get(HttpStatusCode.OK, this.ComplexTypeResponseString),
                new HttpRequestMessageFactory(
                    IHttpContextAccessorMocks.Get()
                ),
                new TokenResponseService(
                    IHttpClientFactoryMocks.Get(HttpStatusCode.OK),
                    IAccessTokenCacheManagerMocks.Get(
                        await TokenResponseMock.GetValidResponseAsync("access_token", 3600)
                    )
                )
            ).CreateHttpClientService();

            var result = await httpClientService.GetAsync<ComplexTypeResponse>("http://localhost");

            httpClientService.Dispose();

            //Status/HttpResponseMessage
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpResponseMessage.StatusCode);

            //HttpRequestMessage
            Assert.AreEqual(HttpMethod.Get, result.HttpRequestMessge.Method);
            Assert.IsNull(result.HttpRequestMessge.Content);

            //Body
            Assert.AreEqual(this.ComplexTypeResponseString, result.BodyAsString);
            var sr = new StreamReader(result.BodyAsStream);
            Assert.AreEqual(this.ComplexTypeResponseString, sr.ReadToEnd());

            Assert.IsInstanceOfType(result.BodyAsType.TestInt, typeof(int));
            Assert.AreEqual(new ComplexTypeResponse().TestInt, result.BodyAsType.TestInt);

            Assert.IsInstanceOfType(result.BodyAsType.TestBool, typeof(bool));
            Assert.AreEqual(new ComplexTypeResponse().TestBool, result.BodyAsType.TestBool);
        }

    }
}