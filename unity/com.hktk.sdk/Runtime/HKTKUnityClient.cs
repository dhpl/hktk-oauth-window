using System;
using System.Threading;
using System.Threading.Tasks;
using HKTK.Windows;

namespace HKTK.Unity
{
    public sealed class HKTKUnityClient : IDisposable
    {
        private readonly HKTKClient _client;

        public HKTKUnityClient(
            string clientId,
            HKTKEnvironment environment = HKTKEnvironment.Sandbox,
            string scope = "openid profile email phone",
            uint timeoutSeconds = 300,
            string authorizeUrl = null)
        {
            _client = new HKTKClient(new HKTKConfig
            {
                ClientId = clientId,
                Environment = environment,
                Scope = scope,
                TimeoutSeconds = timeoutSeconds,
                AuthorizeUrl = authorizeUrl,
            });
        }

        public Task<HKTKLoginResult> LoginAsync(
            CancellationToken cancellationToken = default)
        {
            return _client.LoginAsync(cancellationToken);
        }

        public void Cancel() => _client.Cancel();

        public void Dispose() => _client.Dispose();
    }
}
