using System.Net.Security;

namespace GritTrack.Services.Api
{
    // builds the HttpClientHandler for talking to the local API.
    // the API's https certificate is a self-signed development one. Windows
    // trusts it after "dotnet dev-certs https --trust", but the Android emulator
    // and iOS simulator never will, so in DEBUG builds only we accept a
    // certificate issued to localhost. release builds keep the normal checks
    // (this is the pattern from the Microsoft Learn local web services article)
    public static class DevelopmentHttpHandler
    {
        public static HttpClientHandler Create()
        {
            var handler = new HttpClientHandler();

#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) =>
            {
                if (certificate is not null && certificate.Issuer.Equals("CN=localhost"))
                    return true;

                return errors == SslPolicyErrors.None;
            };
#endif

            return handler;
        }
    }
}
