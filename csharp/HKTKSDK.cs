using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HKTK.Windows
{
    public enum HKTKEnvironment
    {
        Sandbox = 0,
        Production = 1,
        Local = 2,
    }

    public enum HKTKErrorCode
    {
        Ok = 0,
        InvalidArgument = 1,
        AlreadyRunning = 2,
        Network = 3,
        Browser = 4,
        Timeout = 5,
        Cancelled = 6,
        InvalidCallback = 7,
        StateMismatch = 8,
        OAuth = 9,
        Internal = 10,
    }

    public sealed class HKTKConfig
    {
        public HKTKEnvironment Environment { get; set; } = HKTKEnvironment.Sandbox;
        public string ClientId { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid profile email phone";
        public string? AuthorizeUrl { get; set; }
        public uint TimeoutSeconds { get; set; } = 300;
    }

    public sealed class HKTKLoginResult
    {
        public HKTKLoginResult(string code, string state, string redirectUri)
        {
            Code = code;
            State = state;
            RedirectUri = redirectUri;
        }

        public string Code { get; }
        public string State { get; }
        public string RedirectUri { get; }
    }

    public sealed class HKTKException : Exception
    {
        public HKTKException(HKTKErrorCode code, string message) : base(message)
        {
            Code = code;
        }

        public HKTKErrorCode Code { get; }
    }

    public sealed class HKTKClient : IDisposable
    {
        private IntPtr _handle;
        private bool _disposed;
        private int _activeLogins;
        private readonly object _gate = new object();

        public HKTKClient(HKTKConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.ClientId))
                throw new ArgumentException("ClientId is required.", nameof(config));

            var clientId = Utf8.Alloc(config.ClientId);
            var scope = Utf8.Alloc(config.Scope);
            var authorizeUrl = Utf8.Alloc(config.AuthorizeUrl);

            try
            {
                var nativeConfig = new NativeConfig
                {
                    Environment = config.Environment,
                    ClientId = clientId,
                    Scope = scope,
                    AuthorizeUrl = authorizeUrl,
                    TimeoutSeconds = config.TimeoutSeconds,
                };
                _handle = Native.hktk_client_create(ref nativeConfig, out var error);
                if (_handle == IntPtr.Zero)
                    throw CreateException(error);
            }
            finally
            {
                Utf8.Free(clientId);
                Utf8.Free(scope);
                Utf8.Free(authorizeUrl);
            }
        }

        ~HKTKClient() => Dispose(false);

        public Task<HKTKLoginResult> LoginAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                IntPtr handle;
                lock (_gate)
                {
                    if (_disposed) throw new ObjectDisposedException(nameof(HKTKClient));
                    handle = _handle;
                    _activeLogins++;
                }

                try
                {
                    using (cancellationToken.Register(Cancel))
                    {
                        var code = Native.hktk_client_login(
                            handle, out var result, out var error);
                        if (code != HKTKErrorCode.Ok)
                            throw CreateException(error);

                        return new HKTKLoginResult(
                            Utf8.Decode(result.Code),
                            Utf8.Decode(result.State),
                            Utf8.Decode(result.RedirectUri));
                    }
                }
                finally
                {
                    lock (_gate)
                    {
                        _activeLogins--;
                        Monitor.PulseAll(_gate);
                    }
                }
            }, cancellationToken);
        }

        public void Cancel()
        {
            IntPtr handle;
            lock (_gate) handle = _disposed ? IntPtr.Zero : _handle;
            if (handle != IntPtr.Zero) Native.hktk_client_cancel(handle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            IntPtr handle;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                handle = _handle;
                _handle = IntPtr.Zero;
            }

            if (handle == IntPtr.Zero) return;
            Native.hktk_client_cancel(handle);

            lock (_gate)
            {
                while (_activeLogins > 0) Monitor.Wait(_gate);
            }

            Native.hktk_client_destroy(handle);
        }

        private static HKTKException CreateException(NativeError error)
        {
            var message = Utf8.Decode(error.Message);
            return new HKTKException(error.Code,
                string.IsNullOrWhiteSpace(message) ? "HKTK login failed." : message);
        }
    }

    internal static class Utf8
    {
        internal static IntPtr Alloc(string? value)
        {
            if (string.IsNullOrEmpty(value)) return IntPtr.Zero;
            var bytes = Encoding.UTF8.GetBytes(value + "\0");
            var pointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            return pointer;
        }

        internal static void Free(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer);
        }

        internal static string Decode(byte[]? bytes)
        {
            if (bytes == null) return string.Empty;
            var length = Array.IndexOf(bytes, (byte)0);
            if (length < 0) length = bytes.Length;
            return Encoding.UTF8.GetString(bytes, 0, length);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeConfig
    {
        internal HKTKEnvironment Environment;
        internal IntPtr ClientId;
        internal IntPtr Scope;
        internal IntPtr AuthorizeUrl;
        internal uint TimeoutSeconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeLoginResult
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        internal byte[] Code;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        internal byte[] State;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal byte[] RedirectUri;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeError
    {
        internal HKTKErrorCode Code;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        internal byte[] Message;
    }

    internal static class Native
    {
        private const string Library = "HKTKSDK";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr hktk_client_create(
            ref NativeConfig config, out NativeError error);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void hktk_client_destroy(IntPtr client);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern HKTKErrorCode hktk_client_login(
            IntPtr client, out NativeLoginResult result, out NativeError error);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void hktk_client_cancel(IntPtr client);
    }
}
