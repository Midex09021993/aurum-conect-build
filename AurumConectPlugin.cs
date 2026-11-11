using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;

namespace AurumConect
{
    [Guid("C8F4D8A1-5A2B-4D8E-9C31-7B7E2A4D9016")]
    public sealed class AurumConectPlugin : PlugIn
    {
        public static AurumConectPlugin Instance { get; private set; }
        private static IntPtr _webView2LoaderHandle = IntPtr.Zero;
        private const string NativeLoaderResource = "AurumConect.Embedded.WebView2Loader.dll";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        public AurumConectPlugin() { Instance = this; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveEmbeddedDependency;
                AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedDependency;
                PrepareEmbeddedWebView2Loader();
                Panels.RegisterPanel(this, typeof(AurumConectPanel), "AURUM CONECT", null);
                RhinoApp.WriteLine("AURUM CONECT 1.0.8 cargado desde un único .RHP. Ejecuta AurumConect para abrir el panel.");
                return LoadReturnCode.Success;
            }
            catch (Exception exception)
            {
                errorMessage = "AURUM CONECT no pudo inicializar sus componentes integrados: " + exception.Message;
                return LoadReturnCode.ErrorShowDialog;
            }
        }

        private static Assembly ResolveEmbeddedDependency(object sender, ResolveEventArgs args)
        {
            AssemblyName requested = new AssemblyName(args.Name);
            if (String.IsNullOrEmpty(requested.Name) ||
                !requested.Name.StartsWith("Microsoft.Web.WebView2.", StringComparison.OrdinalIgnoreCase))
                return null;

            string resourceName = "AurumConect.Embedded." + requested.Name + ".dll";
            Assembly owner = typeof(AurumConectPlugin).Assembly;
            using (Stream resource = owner.GetManifestResourceStream(resourceName))
            {
                if (resource == null) return null;
                using (var buffer = new MemoryStream())
                {
                    resource.CopyTo(buffer);
                    return Assembly.Load(buffer.ToArray());
                }
            }
        }

        private static void PrepareEmbeddedWebView2Loader()
        {
            if (_webView2LoaderHandle != IntPtr.Zero) return;

            Assembly owner = typeof(AurumConectPlugin).Assembly;
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AurumLab", "AurumConect", "Runtime", "1.0.8");
            Directory.CreateDirectory(root);
            string loaderPath = Path.Combine(root, "WebView2Loader.dll");

            using (Stream resource = owner.GetManifestResourceStream(NativeLoaderResource))
            {
                if (resource == null)
                    throw new InvalidOperationException("El archivo nativo WebView2Loader no está integrado en el complemento.");

                bool writeFile = !File.Exists(loaderPath) || new FileInfo(loaderPath).Length != resource.Length;
                if (writeFile)
                {
                    string temporaryPath = loaderPath + ".tmp";
                    using (var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        resource.CopyTo(output);
                    if (File.Exists(loaderPath)) File.Delete(loaderPath);
                    File.Move(temporaryPath, loaderPath);
                }
            }

            _webView2LoaderHandle = LoadLibrary(loaderPath);
            if (_webView2LoaderHandle == IntPtr.Zero)
                throw new InvalidOperationException("Windows no pudo cargar WebView2Loader x64. Código Win32: " + Marshal.GetLastWin32Error() + ".");
            RhinoApp.WriteLine("AURUM CONECT 1.0.8: WebView2Loader x64 preparado.");
        }
    }
}