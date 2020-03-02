## Getting started
Getting started with IdentityServer4.Contrib.HttpClientService is rather easy, you only need three things:
1. Install the nuget package [IdentityServer4.Contrib.HttpClientService](https://www.nuget.org/packages/IdentityServer4.Contrib.HttpClientService)
2. Provide the options to authenticate in `appsettings.json`
3. Register the service in `Startup.cs`


### It's a nuget package!
Install the [IdentityServer4.Contrib.HttpClientService](https://www.nuget.org/packages/IdentityServer4.Contrib.HttpClientService) nuget package, using your favorite way.

### IdentityServer4 client credentials options
Add the IdentityServer4 client credentials options to your appsettings.json 
```
"ProtectedResourceClientCredentialsOptions": {
    "Address": "https://demo.identityserver.io/connect/token",
    "ClientId": "m2m",
    "ClientSecret": "secret",
    "Scopes": "api"
  }
```
*The values above are part of the demo offered in https://demo.identityserver.io/*

### Register the service 
Register the service In `StartUp.cs` in `ConfigureServices(IServiceCollection services)`:
```csharp
services.AddHttpClientService();
```
If you want to use the [Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options), create a `ProtectedResourceClientCredentialsOptions` class with the client credential options or inherits from `DefaultClientCredentialOptions`:
```csharp
public class ProtectedResourceClientCredentialsOptions : DefaultClientCredentialOptions
{
}
```
and then use `.Configure<TOptions>(...)`:
```csharp
services.AddHttpClientService()
        .Configure<ProtectedResourceClientCredentialsOptions>(Configuration.GetSection(nameof(ProtectedResourceClientCredentialsOptions)));
```   

### You are done!
Inject the `IHttpClientServiceFactory` wherever you want to make the an authenticated requests:
```csharp
public class ProtectedResourceService {

  private readonly IHttpClientServiceFactory _requestServiceFactory;
  
  public ProtectedResourceService(IHttpClientServiceFactory requestServiceFactory)
  {
    _requestServiceFactory = requestServiceFactory;
  }  
  
  public async Task<IEnumerable<Customer>> GetCustomers(){
    var response = await _requestServiceFactory
      .CreateHttpClientService(nameof(ProtectedResourceService))
      .SetIdentityServerOptions("ProtectedResourceClientCredentialsOptions")
      .GetAsync<IEnumerable<Customer>>("https://protected_resource_that_returns_customers_in_json"); 
  }
}
```
Or if you used the [Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options) approach
```csharp
public class ProtectedResourceService {

  private readonly IHttpClientServiceFactory _requestServiceFactory;
  private readonly IOptions<ProtectedResourceClientCredentialsOptions> _identityServerOptions;
  
  public ProtectedResourceService(IHttpClientServiceFactory requestServiceFactory, IOptions<ProtectedResourceClientCredentialsOptions> identityServerOptions)
  {
    _requestServiceFactory = requestServiceFactory;
    _identityServerOptions = identityServerOptions;
  }  
  public async Task<IEnumerable<Customer>> GetCustomers(){
    var response = await _requestServiceFactory
      .CreateHttpClientService(nameof(ProtectedResourceService))
      .SetIdentityServerOptions(_identityServerOptions)
      .GetAsync<IEnumerable<Customer>>("https://protected_resource_that_returns_customers_in_json"); 
  }
}
```