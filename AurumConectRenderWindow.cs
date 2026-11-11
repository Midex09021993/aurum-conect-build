using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Rhino;

namespace AurumConect
{
    internal sealed class AurumConectRenderWindow : Form
    {
        private const string DefaultRenderUrl = "https://tallerdeljoyero.com/aurum-render";
        private static AurumConectRenderWindow _instance;
        private readonly WebView2 _browser;
        private readonly string _initialUrl;
        private readonly Panel _loadingPanel;
        private readonly Label _statusLabel;
        private readonly Button _retryButton;
        private bool _browserReady;
        private bool _navigationFailed;

        private AurumConectRenderWindow(string url)
        {
            _initialUrl = String.IsNullOrWhiteSpace(url) ? DefaultRenderUrl : NormalizeUrl(url);
            Text = "AURUM RENDER · AURUM CONECT";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 246, 243);
            ShowIcon = true;

            _browser = new WebView2 { Dock = DockStyle.Fill, Visible = false };
            Controls.Add(_browser);

            _loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 246, 243)
            };
            var title = new Label
            {
                Text = "AURUM RENDER",
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 49, 39),
                Location = new Point(30, 30)
            };
            _statusLabel = new Label
            {
                Text = "Iniciando navegador integrado…",
                AutoSize = false,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(105, 105, 105),
                Location = new Point(32, 82),
                Size = new Size(780, 54)
            };
            _retryButton = new Button
            {
                Text = "REINTENTAR",
                Visible = false,
                Location = new Point(32, 150),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(215, 181, 111),
                FlatStyle = FlatStyle.Flat
            };
            _retryButton.Click += async (_, __) => await InitializeBrowserAsync();
            var openBrowserButton = new Button
            {
                Text = "ABRIR EN CHROME / NAVEGADOR",
                Location = new Point(195, 150),
                Size = new Size(245, 38),
                BackColor = Color.FromArgb(39, 44, 52),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            openBrowserButton.Click += (_, __) => AurumConectPanel.OpenUrl(_initialUrl);
            _loadingPanel.Controls.Add(title);
            _loadingPanel.Controls.Add(_statusLabel);
            _loadingPanel.Controls.Add(_retryButton);
            _loadingPanel.Controls.Add(openBrowserButton);
            Controls.Add(_loadingPanel);
            _loadingPanel.BringToFront();

            FormClosed += (_, __) => { if (ReferenceEquals(_instance, this)) _instance = null; };
            Shown += async (_, __) => await InitializeBrowserAsync();
        }

        internal static void Open(string url)
        {
            if (String.IsNullOrWhiteSpace(url)) url = DefaultRenderUrl;
            try
            {
                if (_instance != null && !_instance.IsDisposed)
                {
                    if (_instance.InvokeRequired)
                        _instance.BeginInvoke(new Action(() => _instance.Navigate(url)));
                    else
                        _instance.Navigate(url);

                    if (_instance.WindowState == FormWindowState.Minimized)
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
                RhinoApp.WriteLine("AURUM CONECT: no se pudo abrir la ventana integrada: {0}", exception);
                AurumConectPanel.OpenUrl(url);
            }
        }

        private static string NormalizeUrl(string url)
        {
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp))
                return DefaultRenderUrl;
            if (parsed.Host.Equals("www.tallerdeljoyero.com", StringComparison.OrdinalIgnoreCase))
                return "https://tallerdeljoyero.com" + parsed.PathAndQuery + parsed.Fragment;
            return parsed.ToString();
        }

        private async System.Threading.Tasks.Task InitializeBrowserAsync()
        {
            if (_browserReady) return;
            SetStatus("Comprobando Microsoft Edge WebView2 Runtime…", false);
            _retryButton.Visible = false;

            try
            {
                string runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (String.IsNullOrWhiteSpace(runtimeVersion))
                    throw new InvalidOperationException("No se detectó Microsoft Edge WebView2 Runtime. Instala o actualiza el Runtime Evergreen.");

                string userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AurumLab", "AurumConect", "WebView2", "1.0.2");
                Directory.CreateDirectory(userData);

                SetStatus("Preparando Chromium (" + runtimeVersion + ")…", false);
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userData);
                await _browser.EnsureCoreWebView2Async(environment);

                CoreWebView2 core = _browser.CoreWebView2;
                if (core == null) throw new InvalidOperationException("WebView2 no creó el núcleo del navegador.");

                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsZoomControlEnabled = true;
                core.NavigationStarting += (_, args) =>
                {
                    _navigationFailed = false;
                    SetStatus("Conectando con AURUM LAB…", false);
                };
                core.NavigationCompleted += (_, args) =>
                {
                    if (args.IsSuccess)
                    {
                        _navigationFailed = false;
                        _browser.Visible = true;
                        _loadingPanel.Visible = false;
                        _browser.BringToFront();
                        RhinoApp.WriteLine("AURUM CONECT: AURUM RENDER cargó correctamente en WebView2.");
                    }
                    else
                    {
                        _navigationFailed = true;
                        SetStatus("No se pudo cargar AURUM RENDER. Código WebErrorStatus: " + args.WebErrorStatus + ". Puedes reintentar o abrirlo en tu navegador.", true);
                        RhinoApp.WriteLine("AURUM CONECT: fallo de navegación WebView2. Estado: {0}. URL: {1}", args.WebErrorStatus, _initialUrl);
                    }
                };
                core.ProcessFailed += (_, args) =>
                {
                    _navigationFailed = true;
                    SetStatus("El proceso Chromium se interrumpió (" + args.ProcessFailedKind + "). Reintenta o abre AURUM RENDER en tu navegador.", true);
                    RhinoApp.WriteLine("AURUM CONECT: proceso WebView2 fallido: {0}", args.ProcessFailedKind);
                };
                core.NewWindowRequested += (_, args) =>
                {
                    args.Handled = true;
                    AurumConectPanel.OpenUrl(args.Uri);
                };

                SetStatus("Abriendo AURUM RENDER…", false);
                _browser.Source = new Uri(_initialUrl);
                _browserReady = true;
            }
            catch (Exception exception)
            {
                _browserReady = false;
                SetStatus("No se pudo iniciar el navegador integrado: " + exception.Message, true);
                RhinoApp.WriteLine("AURUM CONECT: error al inicializar WebView2: {0}", exception);
            }
        }

        private void SetStatus(string message, bool showRetry)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetStatus(message, showRetry)));
                return;
            }
            _statusLabel.Text = message;
            _retryButton.Visible = showRetry;
            _loadingPanel.Visible = true;
            _loadingPanel.BringToFront();
        }

        private sealed class RhinoWindowOwner : IWin32Window
        {
            internal RhinoWindowOwner(IntPtr handle) { Handle = handle; }
            public IntPtr Handle { get; private set; }
        }

        private void Navigate(string url)
        {
            string safeUrl = NormalizeUrl(url);
            if (_browser.CoreWebView2 != null)
            {
                _browser.CoreWebView2.Navigate(safeUrl);
                return;
            }
            _statusLabel.Text = "Preparando el navegador integrado…";
            _loadingPanel.Visible = true;
            _retryButton.Visible = false;
            _ = InitializeBrowserAsync();
        }
    }
}