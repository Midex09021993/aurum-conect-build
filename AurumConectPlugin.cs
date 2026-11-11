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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        public AurumConectPlugin() { Instance = this; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                RegisterLocalWebView2Dependencies();
                Panels.RegisterPanel(this, typeof(AurumConectPanel), "AURUM CONECT", null);
                RhinoApp.WriteLine("AURUM CONECT 1.0 cargado. Ejecuta AurumConect para abrir el panel.");
                return LoadReturnCode.Success;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return LoadReturnCode.ErrorShowDialog;
            }
        }

        private static void RegisterLocalWebView2Dependencies()
        {
            string pluginDirectory = Path.GetDirectoryName(typeof(AurumConectPlugin).Assembly.Location);
            if (String.IsNullOrWhiteSpace(pluginDirectory))
                throw new InvalidOperationException("No se pudo determinar la carpeta de instalación de AURUM CONECT.");

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveWebView2Assembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveWebView2Assembly;

            string nativeLoader = Path.Combine(pluginDirectory, "WebView2Loader.dll");
            if (!File.Exists(nativeLoader))
            {
                RhinoApp.WriteLine("AURUM CONECT: falta WebView2Loader.dll junto al complemento.");
                return;
            }

            if (_webView2LoaderHandle == IntPtr.Zero)
            {
                _webView2LoaderHandle = LoadLibrary(nativeLoader);
                if (_webView2LoaderHandle == IntPtr.Zero)
                    throw new InvalidOperationException("No se pudo cargar WebView2Loader.dll. Error de Windows: " + Marshal.GetLastWin32Error() + ".");
            }
        }

        private static Assembly ResolveWebView2Assembly(object sender, ResolveEventArgs args)
        {
            AssemblyName requested = new AssemblyName(args.Name);
            if (!requested.Name.StartsWith("Microsoft.Web.WebView2.", StringComparison.OrdinalIgnoreCase))
                return null;

            string pluginDirectory = Path.GetDirectoryName(typeof(AurumConectPlugin).Assembly.Location);
            string candidate = Path.Combine(pluginDirectory, requested.Name + ".dll");
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        }
    }
}
