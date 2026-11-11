using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Rhino;

namespace AurumConect
{
    internal sealed class AurumConectRenderWindow : Form
    {
        private static AurumConectRenderWindow _instance;
        private readonly WebView2 _browser;
        private readonly string _initialUrl;

        private AurumConectRenderWindow(string url)
        {
            _initialUrl = url;
            Text = "AURUM RENDER · AURUM CONECT";
            Width = 1280;
            Height = 820;
            MinimumSize = new System.Drawing.Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            ShowIcon = true;

            _browser = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_browser);
            FormClosed += (_, __) => { if (ReferenceEquals(_instance, this)) _instance = null; };
            Shown += InitializeBrowser;
        }

        internal static void Open(string url)
        {
            if (String.IsNullOrWhiteSpace(url)) return;
            try
            {
                if (_instance != null && !_instance.IsDisposed)
                {
                    if (_instance.InvokeRequired) _instance.BeginInvoke(new Action(() => _instance.Navigate(url)));
                    else _instance.Navigate(url);
                    _instance.WindowState = FormWindowState.Normal;
                    _instance.BringToFront();
                    _instance.Activate();
                    return;
                }

                _instance = new AurumConectRenderWindow(url);
                IntPtr rhinoHandle = RhinoApp.MainWindowHandle();
                if (rhinoHandle != IntPtr.Zero) _instance.Show(new RhinoWindowOwner(rhinoHandle));
                else _instance.Show();
            }
            catch (Exception exception)
            {
                RhinoApp.WriteLine("AURUM CONECT: no se pudo abrir la ventana integrada: {0}", exception.Message);
                AurumConectPanel.OpenUrl(url);
            }
        }

        private async void InitializeBrowser(object sender, EventArgs e)
        {
            try
            {
                string userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AURUM-LAB", "AurumConect", "WebView2");
                Directory.CreateDirectory(userData);
                // WebView2Loader.dll ya se extrajo desde el recurso privado del .RHP al iniciar el plugin.
                // El Runtime Evergreen de Microsoft Edge sigue siendo un requisito del sistema.
                string runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (String.IsNullOrWhiteSpace(runtimeVersion))
                    throw new InvalidOperationException("No se detectó Microsoft Edge WebView2 Runtime. Instala el Runtime Evergreen y vuelve a abrir Rhino.");
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userData);
                await _browser.EnsureCoreWebView2Async(environment);
                _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                _browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _browser.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    AurumConectPanel.OpenUrl(args.Uri);
                };
                _browser.Source = new Uri(_initialUrl);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this,
                    "AURUM CONECT está instalado correctamente, pero no se pudo iniciar el visor integrado. Comprueba que Microsoft Edge WebView2 Runtime esté instalado y actualizado.\r\n\r\n" + exception.Message,
                    "AURUM CONECT · WebView2", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AurumConectPanel.OpenUrl(_initialUrl);
            }
        }

        private sealed class RhinoWindowOwner : IWin32Window
        {
            internal RhinoWindowOwner(IntPtr handle) { Handle = handle; }
            public IntPtr Handle { get; private set; }
        }

        private void Navigate(string url)
        {
            if (_browser.CoreWebView2 != null) _browser.CoreWebView2.Navigate(url);
            else _browser.Source = new Uri(url);
        }
    }
}
