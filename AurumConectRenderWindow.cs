using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
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
        private readonly Button _openBrowserButton;
        private Task _initializationTask;
        private readonly System.Windows.Forms.Timer _startupTimer;
        private bool _startupRequested;
        private bool _browserReady;
        private bool _navigationFailed;

        private static string LogPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AurumLab", "AurumConect", "Logs", "AurumConect-1.0.4.log");
            }
        }

        private AurumConectRenderWindow(string url)
        {
            WriteLog("Constructor iniciado.");
            _initialUrl = String.IsNullOrWhiteSpace(url) ? DefaultRenderUrl : NormalizeUrl(url);
            Text = "AURUM RENDER · AURUM CONECT";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 246, 243);
            ShowIcon = true;

            WriteLog("Configurando formulario y controles.");
            _browser = new WebView2 { Dock = DockStyle.Fill, Visible = false };
            Controls.Add(_browser);

            _loadingPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 246, 243) };
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
                Text = "Preparando el navegador integrado…",
                AutoSize = false,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(75, 75, 75),
                Location = new Point(32, 82),
                Size = new Size(900, 54)
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
            _retryButton.Click += async (_, __) => { _startupRequested = false; await RequestInitializationAsync("Reintento solicitado por el usuario."); };

            _openBrowserButton = new Button
            {
                Text = "ABRIR EN CHROME / NAVEGADOR",
                Location = new Point(195, 150),
                Size = new Size(245, 38),
                BackColor = Color.FromArgb(39, 44, 52),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _openBrowserButton.Click += (_, __) => AurumConectPanel.OpenUrl(_initialUrl);

            _loadingPanel.Controls.Add(title);
            _loadingPanel.Controls.Add(_statusLabel);
            _loadingPanel.Controls.Add(_retryButton);
            _loadingPanel.Controls.Add(_openBrowserButton);
            Controls.Add(_loadingPanel);
            _loadingPanel.BringToFront();

            FormClosed += (_, __) =>
            {
                if (_startupTimer != null) _startupTimer.Dispose();
                if (ReferenceEquals(_instance, this)) _instance = null;
                WriteLog("Ventana cerrada por el usuario.");
            };

            _startupTimer = new System.Windows.Forms.Timer { Interval = 350 };
            _startupTimer.Tick += async (_, __) =>
            {
                _startupTimer.Stop();
                WriteLog("Temporizador de arranque ejecutado; handle=" + IsHandleCreated + ", visible=" + Visible + ".");
                await RequestInitializationAsync("Temporizador de arranque.");
            };
            Load += (_, __) =>
            {
                WriteLog("Evento Load ejecutado; handle=" + IsHandleCreated + ".");
                SetStatus("Ventana lista. Preparando WebView2…", false);
            };
            Shown += (_, __) =>
            {
                WriteLog("Evento Shown ejecutado; handle=" + IsHandleCreated + ".");
                _startupTimer.Start();
            };
            HandleCreated += (_, __) => WriteLog("HandleCreated ejecutado.");
            HandleDestroyed += (_, __) => WriteLog("HandleDestroyed ejecutado.");
            WriteLog("Constructor completado.");
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
                WriteLog("Error al abrir la ventana: " + exception);
                RhinoApp.WriteLine("AURUM CONECT: no se pudo abrir la ventana integrada: {0}", exception.Message);
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

        private async Task RequestInitializationAsync(string reason)
        {
            if (_startupRequested && _initializationTask != null && !_initializationTask.IsCompleted)
            {
                WriteLog("Solicitud duplicada ignorada: " + reason);
                return;
            }
            _startupRequested = true;
            await StartInitializationAsync(reason);
        }

        private async Task StartInitializationAsync(string reason)
        {
            WriteLog("START ENTER | reason=" + reason
                + " | browserReady=" + _browserReady
                + " | taskNull=" + (_initializationTask == null)
                + " | taskStatus=" + (_initializationTask == null ? "null" : _initializationTask.Status.ToString()));

            if (_browserReady)
            {
                WriteLog("START EXIT | motivo=browserReady ya es true.");
                return;
            }

            if (_initializationTask != null && !_initializationTask.IsCompleted)
            {
                WriteLog("START EXIT | ya existe tarea activa: " + _initializationTask.Status);
                return;
            }

            try
            {
                SetStatus("Iniciando diagnóstico de WebView2…", false);
                WriteLog("START | antes de invocar InitializeBrowserAsync.");
                Task initialization = InitializeBrowserAsync(reason);
                WriteLog("START | InitializeBrowserAsync devolvió una tarea: " + initialization.Status);
                _initializationTask = initialization;
                await initialization;
                WriteLog("START | tarea finalizada: " + initialization.Status);
            }
            catch (Exception exception)
            {
                WriteLog("START EXCEPTION | " + exception);
                SetStatus("Error al iniciar WebView2: " + exception.Message, true);
            }
        }

        private async Task InitializeBrowserAsync(string reason)
        {
            WriteLog("INIT ENTER | " + reason);
            try
            {
                SetStatus("Comprobando Microsoft Edge WebView2 Runtime…", false);
                WriteLog("INIT | estado inicial actualizado; antes de ceder el hilo.");
                await Task.Yield();
                WriteLog("INIT | reanudación después de Task.Yield.");
                WriteLog("Proceso: " + Environment.Is64BitProcess + " bits=" + (Environment.Is64BitProcess ? "x64" : "x86"));
                WriteLog("SO: " + Environment.OSVersion.VersionString);
                WriteLog("Directorio de la aplicación: " + AppDomain.CurrentDomain.BaseDirectory);

                string runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (String.IsNullOrWhiteSpace(runtimeVersion))
                    throw new InvalidOperationException("No se detectó Microsoft Edge WebView2 Runtime. Instala Evergreen Runtime x64 y reinicia Rhino.");

                WriteLog("Runtime detectado: " + runtimeVersion);
                SetStatus("Runtime detectado (" + runtimeVersion + "). Preparando Chromium…", false);

                WriteLog("Biblioteca WinForms WebView2 cargada: " + typeof(WebView2).Assembly.FullName);
                WriteLog("Inicializando WebView2 con la sobrecarga compatible EnsureCoreWebView2Async()…");
                SetStatus("Inicializando el motor Chromium…", false);

                // Usar la sobrecarga sin parámetros evita depender de un overload
                // que no está presente en la versión de WebView2.WinForms que Rhino carga.
                await _browser.EnsureCoreWebView2Async();
                WriteLog("EnsureCoreWebView2Async() completado.");

                CoreWebView2 core = _browser.CoreWebView2;
                if (core == null) throw new InvalidOperationException("WebView2 no creó el núcleo del navegador.");
                WriteLog("CoreWebView2 inicializado correctamente.");

                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsZoomControlEnabled = true;

                core.NavigationStarting += (_, args) =>
                {
                    _navigationFailed = false;
                    WriteLog("Navegación iniciada: " + args.Uri);
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
                        WriteLog("Navegación completada correctamente: " + _initialUrl);
                        RhinoApp.WriteLine("AURUM CONECT 1.0.4: AURUM RENDER cargó correctamente en WebView2.");
                    }
                    else
                    {
                        _navigationFailed = true;
                        string message = "No se pudo cargar AURUM RENDER. Estado: " + args.WebErrorStatus + ".";
                        WriteLog(message + " URL: " + _initialUrl);
                        SetStatus(message + " Reintenta o abre la plataforma en tu navegador.", true);
                    }
                };
                core.ProcessFailed += (_, args) =>
                {
                    _navigationFailed = true;
                    string message = "El proceso Chromium falló: " + args.ProcessFailedKind;
                    WriteLog(message);
                    SetStatus(message + ". Reintenta o abre AURUM RENDER en tu navegador.", true);
                };
                core.NewWindowRequested += (_, args) =>
                {
                    args.Handled = true;
                    AurumConectPanel.OpenUrl(args.Uri);
                };

                SetStatus("Abriendo AURUM RENDER…", false);
                WriteLog("Solicitando navegación a " + _initialUrl);
                _browser.Source = new Uri(_initialUrl);
                _browserReady = true;
            }
            catch (Exception exception)
            {
                _browserReady = false;
                WriteLog("EXCEPCIÓN DE INICIALIZACIÓN: " + exception);
                SetStatus("No se pudo iniciar WebView2: " + exception.Message, true);
                RhinoApp.WriteLine("AURUM CONECT 1.0.4: error WebView2. Revisa el log en: {0}", LogPath);
                RhinoApp.WriteLine(exception.ToString());
            }
        }

        private void SetStatus(string message, bool showRetry)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => SetStatus(message, showRetry))); }
                catch (InvalidOperationException) { }
                return;
            }
            _statusLabel.Text = message;
            _retryButton.Visible = showRetry;
            _loadingPanel.Visible = true;
            _loadingPanel.BringToFront();
        }

        private static void WriteLog(string message)
        {
            try
            {
                string directory = Path.GetDirectoryName(LogPath);
                Directory.CreateDirectory(directory);
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + message + Environment.NewLine);
            }
            catch { }
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
            SetStatus("Preparando el navegador integrado…", false);
            _ = RequestInitializationAsync("Se solicitó navegación antes de inicializar WebView2.");
        }
    }
}