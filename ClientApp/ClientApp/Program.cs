using Microsoft.Identity.Client;
using AuthenticationResult = Microsoft.Identity.Client.AuthenticationResult;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;

namespace ApiSample
{
    class Program
    {


        static void Main(string[] args)
        {
            Task task = MainAsync();
            //終了を待つ
            task.Wait();
        }

        static async Task MainAsync()
        {

            string secretName = "secret-sauce";
            string keyVaultName = "aritest-keyvault";
            string keyVaultUrl = "https://" + keyVaultName + ".vault.azure.net";

            var secret = await new AzureService().GetKeyVaultSecret(secretName, keyVaultUrl);
            Console.WriteLine(secret);

        }
    }

    public class BearerTokenCredential : TokenCredential
    {
        private string _bearerToken;

        public BearerTokenCredential(string baererToken)
        {
            _bearerToken = baererToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_bearerToken, DateTimeOffset.UtcNow.AddHours(1));
        }
    }


    public class AzureService
    {

        private static async Task<string> AcquireMSALToken(string authority, string resource, string scope)
        {

            string clientID = "16bf7959-1b4e-4192-8e48-914cc4c9ac6d";
            string tenantID = "da4e5376-e590-44ac-b4f4-35c36df9aecb";
            //string[] ApiScopes = { $"https://vault.azure.net/user_impersonation", $"openid", $"User.Read", $"Group.Read.All" };
            string[] ApiScopes = { $"https://vault.azure.net/.default", $"openid" };
            string redirectUri = "http://localhost";

            AuthenticationResult authResult = null;
            var app = PublicClientApplicationBuilder.Create(clientID)
                   .WithTenantId(tenantID)
                   .WithRedirectUri(redirectUri)
                   .Build();

            try
            {
                authResult = await app.AcquireTokenInteractive(ApiScopes).ExecuteAsync();
                return authResult.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    //authResult = await app.AcquireTokenAsync(_scopes);
                    return authResult.AccessToken;
                }
                catch (MsalException) { throw; }
            }
            catch (Exception) { throw; }
        }


        public async Task<string> GetKeyVaultSecret(string secretKey, string vaultUri)
        {
            // KeyVaultClient(Microsoft.Azure.KeyVault) は deprecated
            // https://www.nuget.org/packages/Microsoft.Azure.KeyVault
            //KeyVaultClient kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(AcquireMSALToken));

            // MSALで取得したトークンを使う場合はこんな感じ
            var token = await AcquireMSALToken(null, null, null);
            var credential = new BearerTokenCredential(token);
            var kvClient = new SecretClient(new Uri(vaultUri), credential);

            // Azure.Core が提供するブラウザ対話認証を利用する場合はこんな感じ
            //var options = new InteractiveBrowserCredentialOptions
            //{
            //    TenantId = "da4e5376-e590-44ac-b4f4-35c36df9aecb",
            //    ClientId = "16bf7959-1b4e-4192-8e48-914cc4c9ac6d",
            //    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            //    RedirectUri = new Uri("http://localhost"),
            //};
            //var interactiveCredential = new InteractiveBrowserCredential(options);
            //var kvClient = new SecretClient(new Uri(vaultUri), interactiveCredential);

            try
            {
                var secretBundle = await kvClient.GetSecretAsync(secretKey);

                return secretBundle.Value.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}
