﻿using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Contrib.HttpClientService.Infrastructure;
using IdentityServer4.Contrib.HttpClientService.Models;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace IdentityServer4.Contrib.HttpClientService
{
    /// <summary>
    /// The request service implemantation
    /// </summary>
    public class HttpClientService
    {
        private readonly ITokenResponseService _accessTokenService;
        private readonly IConfiguration _configuration;
        private HttpRequestMessage httpRequestMessage;

        private readonly ICoreHttpClient _coreHttpClient;
        private IOptions<DefaultClientCredentialOptions> _options;

        internal HttpClientService(IConfiguration configuration, ICoreHttpClient coreHttpClient, IHttpRequestMessageFactory requestMessageFactory, ITokenResponseService accessTokenService)
        {
            _configuration = configuration;
            _coreHttpClient = coreHttpClient;
            _accessTokenService = accessTokenService;
            httpRequestMessage = requestMessageFactory.CreateRequestMessage();
        }

        /// <summary>
        /// Sets the IdentityServer4 options for retrieving an access token using client credentials by passing the appsettings configuration section 
        /// that contain the necessary configuration keys.
        /// </summary>
        /// <param name="configurationSection">
        /// The name of the configuration section that contains the information for requesting an access token. 
        /// See <see cref="DefaultClientCredentialOptions"/> for the property names.
        /// </param>
        /// <returns></returns>
        public HttpClientService SetIdentityServerOptions(string configurationSection)
        {
            if (_configuration == null)
                throw new InvalidOperationException("String configuration can only be used with the HttpClientService(IConfiguration configuration,...) constructors.");

            var sectionExists = _configuration.GetChildren().Any(item => item.Key == configurationSection);
            if (!sectionExists)
                throw new ArgumentException("The configuration section '" + configurationSection + "' cannot be found!", nameof(configurationSection));

            var options = _configuration.GetSection(configurationSection).Get(typeof(DefaultClientCredentialOptions)) as DefaultClientCredentialOptions;
            SetIdentityServerOptions(Options.Create(options));

            return this;
        }

        /// <summary>
        /// Sets the IdentityServer4 options for retrieving an access token using client credentials.
        /// </summary>
        /// <typeparam name="TIdentityServerOptions">A type that inherits from the <see cref="DefaultClientCredentialOptions"/> onject.</typeparam>
        /// <param name="options">The token service options.</param>
        /// <returns>Returns the current instance of <see cref="HttpClientService"/> for method chaining.</returns>
        public HttpClientService SetIdentityServerOptions<TIdentityServerOptions>(IOptions<TIdentityServerOptions> options) where TIdentityServerOptions : DefaultClientCredentialOptions, new()
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Value.Address == null)
            {
                throw new ArgumentNullException(nameof(options.Value.Address), "Is there an " + typeof(TIdentityServerOptions).Name + " section in the appsettings.json?");
            }
            if (options.Value.ClientId == null)
            {
                throw new ArgumentNullException(nameof(options.Value.ClientId), "Is there a " + typeof(TIdentityServerOptions).Name + " section in the appsettings.json?");
            }
            if (options.Value.ClientSecret == null)
            {
                throw new ArgumentNullException(nameof(options.Value.ClientSecret), "Is there a " + typeof(TIdentityServerOptions).Name + " section in the appsettings.json?");
            }

            _options = options;

            return this;
        }

        /// <summary>
        /// Sets a collection of headers to the request.
        /// </summary>
        /// <param name="headers">A <see cref="Dictionary{TKey, TValue}"/> with the key representing the name of the header, and the value representing the value of the header.</param>
        /// <returns>Returns the current instance of <see cref="HttpClientService"/> for method chaining.</returns>
        public HttpClientService SetHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            foreach (var kv in headers)
            {
                httpRequestMessage.Headers.Add(kv.Key, kv.Value);
            }

            return this;
        }


        /// <summary>
        /// Adds a header to the request.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        /// <returns>Returns the current instance of <see cref="HttpClientService"/> for method chaining.</returns>
        public HttpClientService AddHeader(string name, string value)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            httpRequestMessage.Headers.Add(name, value);

            return this;
        }

        /// <summary>
        /// Creates and sends a request to the <paramref name="requestUri"/> using the HTTP verb from <paramref name="httpMethod"/> and the request body from <paramref name="requestBody"/>. 
        /// If <see cref="SetIdentityServerOptions{TTokenServiceOptions}(IOptions{TTokenServiceOptions})" /> is called before the <c>SendAsync</c>, 
        /// then a valid access token will be fetched by the <see cref="ITokenResponseService"/> and attached on this request. 
        /// </summary>
        /// <typeparam name="TResponseBody">
        ///     The type of the property <see cref="ResponseObject{TResponseBody}.BodyAsType"/> of the <see cref="ResponseObject{TResponseBody}"/> object,
        ///     that will contain the body of the response deserialized or casted to type <typeparamref name="TResponseBody"/>. 
        ///     The type used can be one of the following:
        ///     <list type="bullet">
        ///         <item>
        ///             <term><see cref="StringContent"/></term>
        ///             <description>Use <see cref="StringContent"/> to define Encoding and/or ContentType for an HTTP content based on string.</description>
        ///         </item>
        ///         <item>
        ///             <term><see cref="StreamContent"/></term>
        ///             <description>Use <see cref="StreamContent"/> to provide HTTP content based on a stream.</description>
        ///         </item>
        ///         <item>
        ///             <term>A serializable complex type</term>
        ///             <description>Any serializable object to attempt to deserialize the body of the response to it.</description>
        ///         </item>
        ///         <item>
        ///             <term>A simple type</term>
        ///             <description>Any other simple type to try convert the body of the response to it.</description>
        ///         </item>
        ///     </list>
        /// </typeparam>
        /// <typeparam name="TRequestBody">
        ///     The type of the request body. The type used can be one of the following:
        ///     <list type="bullet">
        ///         <item>
        ///             <term><see cref="StringContent"/></term>
        ///             <description>Use <see cref="StringContent"/> to define Encoding and/or ContentType for an HTTP content based on string.</description>
        ///         </item>
        ///         <item>
        ///             <term><see cref="StreamContent"/></term>
        ///             <description>Use <see cref="StreamContent"/> to provide HTTP content based on a stream.</description>
        ///         </item>
        ///         <item>
        ///             <term>A serializable complex type</term>
        ///             <description>Any serializable object that will be serialized and sent in the body of the request.</description>
        ///         </item>
        ///         <item>
        ///             <term>A simple type</term>
        ///             <description>Any other simple type that will be sent in the body of the request.</description>
        ///         </item>
        ///     </list>
        /// </typeparam>
        /// <param name="requestUri">The <see cref="Uri"/> of the request.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> of the request.</param>
        /// <param name="requestBody">The body of the request (available only in POST, PUT and PATCH).</param>
        /// <returns>
        /// A <see cref="ResponseObject{TResponseBody}"/> containing the body of the response 
        /// as <c>String</c> in the <see cref="ResponseObject{TResponseBody}.BodyAsString"/> property,
        /// as <typeparamref name="TResponseBody"/> in the <see cref="ResponseObject{TBody}.BodyAsType"/> and,
        /// as <c>Stream</c> in the <see cref="ResponseObject{TResponseBody}.BodyAsStream"/> property.
        /// The <typeparamref name="TResponseBody"/> can be of the following:
        ///     <list type="bullet">
        ///         <item>
        ///             <term><see cref="StringContent"/></term>
        ///             <description>Use <see cref="StringContent"/> to define Encoding and/or ContentType for an HTTP content based on string.</description>
        ///         </item>
        ///         <item>
        ///             <term><see cref="StreamContent"/></term>
        ///             <description>Use <see cref="StreamContent"/> to provide HTTP content based on a stream.</description>
        ///         </item>
        ///         <item>
        ///             <term>A serializable complex type</term>
        ///             <description>Any serializable object to attempt to deserialize the body of the response to it.</description>
        ///         </item>
        ///         <item>
        ///             <term>A simple type</term>
        ///             <description>Any other simple type to try convert the body of the response to it.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public async Task<ResponseObject<TResponseBody>> SendAsync<TRequestBody, TResponseBody>(Uri requestUri, HttpMethod httpMethod, TRequestBody requestBody)
        {
            // var request = _requestMessageFactory.CreateRequestMessage();
            httpRequestMessage.Method = httpMethod;
            httpRequestMessage.RequestUri = requestUri;

            //todo: unit test that
            if ((httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Head || httpMethod == HttpMethod.Delete)
                && requestBody != null)
                throw new ArgumentException(nameof(requestBody), "HTTP method " + httpMethod.Method + " does not support a request body.");

            //handle request body
            if (requestBody != null)
            {
                if (typeof(TRequestBody) == typeof(StringContent))
                {
                    httpRequestMessage.Content = requestBody as StringContent;
                }
                else if (typeof(TRequestBody) == typeof(StreamContent))
                {
                    httpRequestMessage.Content = requestBody as StreamContent;
                }
                else if (IsSimpleType(typeof(TRequestBody)))
                {
                    httpRequestMessage.Content = new StringContent(requestBody.ToString());
                }
                else
                {
                    httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                }
            }

            //handle authentication
            if (_options != default)
            {
                var tokenResponse = await _accessTokenService.GetTokenResponseAsync(_options);
                if (tokenResponse.IsError)
                {
                    return new ResponseObject<TResponseBody>
                    {
                        StatusCode = tokenResponse.HttpStatusCode,
                        HttpResponseMessage = tokenResponse.HttpResponse,
                        HttpRequestMessge = null,
                        HasError = true,
                        Error = tokenResponse.Error + Environment.NewLine + tokenResponse.ErrorDescription,
                        BodyAsString = await tokenResponse.HttpResponse.Content.ReadAsStringAsync()
                    };
                }

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
            }

            //make the call            
            var response = await _coreHttpClient.SendAsync(httpRequestMessage);
            var apiResponse = new ResponseObject<TResponseBody>
            {
                Headers = response.Headers,
                StatusCode = response.StatusCode,
                HttpResponseMessage = response,
                HttpRequestMessge = httpRequestMessage
            };

            //handle response body
            if (response.IsSuccessStatusCode)
            {
                apiResponse.BodyAsStream = await response.Content.ReadAsStreamAsync();
                if (!typeof(TResponseBody).IsAssignableFrom(typeof(Stream)))
                {
                    apiResponse.BodyAsString = await response.Content.ReadAsStringAsync();
                    //try to convert to the requested type
                    if (IsSimpleType(typeof(TResponseBody)))
                    {
                        apiResponse.BodyAsType = (TResponseBody)Convert.ChangeType(apiResponse.BodyAsString, typeof(TResponseBody));
                    }
                    else
                    {
                        apiResponse.BodyAsType = JsonConvert.DeserializeObject<TResponseBody>(apiResponse.BodyAsString);
                    }
                }
            }
            else
            {
                apiResponse.HasError = true;
                apiResponse.Error = response.ReasonPhrase;
                apiResponse.BodyAsString = await response.Content.ReadAsStringAsync();
            }

            return apiResponse;
        }

        /// <summary>
        /// Disposes the HTTP request message used for the request.
        /// </summary>
        /// <remarks>
        /// The <see cref="ResponseObject{TResponseBody}.HttpRequestMessge"/> will not be available after disposing.
        /// </remarks>
        public void Dispose()
        {
            httpRequestMessage.Dispose();
        }


        private static bool IsSimpleType(Type type)
        {
            return
                type.IsPrimitive ||
                new Type[] {
                    typeof(Enum),
                    typeof(String),
                    typeof(Decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }
    }
}