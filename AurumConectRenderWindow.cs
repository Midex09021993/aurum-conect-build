using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Rhino;

namespace AurumConect
{
    internal sealed class AurumConectRenderWindow : Form
    {
        private const string DefaultRenderUrl = "https://tallerdeljoyero.com/aurum-render";
        private static AurumConectRenderWindow _instance;
        private readonly string _initialUrl;
        private readonly Panel _browserHost;
        private readonly Panel _loadingPanel;
        private readonly Label _statusLabel;
        private readonly Button _retryButton;
        private readonly Button _openBrowserButton;
        private readonly System.Windows.Forms.Timer _startupTimer;
        private Task _initializationTask;
        private CoreWebView2Environment _environment;
        private CoreWebView2Controller _controller;
        private bool _startupRequested;
        private bool _browserReady;

        private static string LogPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AurumLab", "AurumConect", "Logs", "AurumConect-1.0.6.log");
            }
        }

        private AurumConectRenderWindow(string url)
        {
            WriteLog("1.0.6 constructor iniciado; integración CoreWebView2Controller nativa.");
            _initialUrl = String.IsNullOrWhiteSpace(url) ? DefaultRenderUrl : NormalizeUrl(url);
            Text = "AURUM RENDER · AURUM CONECT";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 246, 243);

            _browserHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 246, 243),
                Visible = true
            };
            _browserHost.Resize += (_, __) => UpdateBrowserBounds();
            Controls.Add(_browserHost);

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
                Text = "Preparando el navegador integrado…",
                AutoSize = false,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(75, 75, 75),
                Location = new Point(32, 82),
                Size = new Size(1000, 58)
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
            _retryButton.Click += async (_, __) =>
            {
                _startupRequested = false;
                _initializationTask = null;
                await RequestInitializationAsync("Reintento manual.");
            };

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

            _startupTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _startupTimer.Tick += async (_, __) =>
            {
                _startupTimer.Stop();
                await RequestInitializationAsync("Temporizador de arranque.");
            };

            Load += (_, __) =>
            {
                WriteLog("Load: handle=" + IsHandleCreated + ", hostHandle=" + _browserHost.IsHandleCreated);
                SetStatus("Ventana lista. Preparando WebView2…", false);
            };
            Shown += (_, __) =>
            {
                WriteLog("Shown: handle=" + IsHandleCreated);
                _startupTimer.Start();
            };
            FormClosed += (_, __) =>
            {
                try
                {
                    _startupTimer.Stop();
                    _startupTimer.Dispose();
                    if (_controller != null)
                    {
                        _controller.Close();
                        _controller = null;
                    }
                }
                catch (Exception exception) { WriteLog("Error cerrando controlador: " + exception); }

                if (ReferenceEquals(_instance, this)) _instance = null;
                WriteLog("Ventana cerrada.");
            };

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
                WriteLog("Error abriendo ventana: " + exception);
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
            if (_browserReady || (_initializationTask != null && !_initializationTask.IsCompleted))
            {
                WriteLog("Solicitud de inicialización omitida; ready=" + _browserReady
                    + ", task=" + (_initializationTask == null ? "null" : _initializationTask.Status.ToString()));
                return;
            }

            _startupRequested = true;
            _initializationTask = InitializeNativeControllerAsync(reason);
            await _initializationTask;
        }

        private async Task InitializeNativeControllerAsync(string reason)
        {
            WriteLog("INIT NATIVE ENTER: " + reason);
            try
            {
                SetStatus("Comprobando Microsoft Edge WebView2 Runtime…", false);
                await Task.Yield();

                string runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                WriteLog("Runtime detectado: " + (String.IsNullOrWhiteSpace(runtimeVersion) ? "(ninguno)" : runtimeVersion));
                if (String.IsNullOrWhiteSpace(runtimeVersion))
                    throw new InvalidOperationException("No se detectó Microsoft Edge WebView2 Runtime. Instala Evergreen Runtime x64.");

                SetStatus("Runtime detectado. Creando entorno Chromium…", false);
                string userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AurumLab", "AurumConect", "WebView2", "1.0.6");
                Directory.CreateDirectory(userData);
                WriteLog("Perfil de datos: " + userData);

                WriteLog("Creando CoreWebView2Environment…");
                _environment = await CoreWebView2Environment.CreateAsync(null, userData);
                WriteLog("CoreWebView2Environment creado.");

                _browserHost.CreateControl();
                IntPtr parentHandle = _browserHost.Handle;
                WriteLog("Creando controlador nativo; parent HWND=" + parentHandle);
                _controller = await _environment.CreateCoreWebView2ControllerAsync(parentHandle);
                if (_controller == null)
                    throw new InvalidOperationException("WebView2 no devolvió un controlador nativo.");

                WriteLog("CoreWebView2Controller creado correctamente.");
                _controller.Bounds = new Rectangle(0, 0, _browserHost.ClientSize.Width, _browserHost.ClientSize.Height);
                _controller.IsVisible = true;

                CoreWebView2 core = _controller.CoreWebView2;
                if (core == null)
                    throw new InvalidOperationException("El controlador no contiene CoreWebView2.");

                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsZoomControlEnabled = true;

                core.NavigationStarting += (_, args) =>
                {
                    WriteLog("Navegación iniciada: " + args.Uri);
                    SetStatus("Conectando con AURUM LAB…", false);
                };
                core.NavigationCompleted += (_, args) =>
                {
                    if (args.IsSuccess)
                    {
                        _browserReady = true;
                        _loadingPanel.Visible = false;
                        _controller.IsVisible = true;
                        WriteLog("Navegación completada: " + args.WebErrorStatus);
                        RhinoApp.WriteLine("AURUM CONECT 1.0.6: AURUM RENDER cargó en el controlador WebView2 nativo.");
                    }
                    else
                    {
                        string message = "No se pudo cargar AURUM RENDER. Estado: " + args.WebErrorStatus + ".";
                        WriteLog(message);
                        SetStatus(message + " Puedes reintentar o abrirlo en Chrome.", true);
                    }
                };
                core.ProcessFailed += (_, args) =>
                {
                    string message = "El proceso Chromium falló: " + args.ProcessFailedKind;
                    WriteLog(message);
                    SetStatus(message + " Puedes reintentar o abrirlo en Chrome.", true);
                };
                core.NewWindowRequested += (_, args) =>
                {
                    args.Handled = true;
                    AurumConectPanel.OpenUrl(args.Uri);
                };

                SetStatus("Abriendo AURUM RENDER…", false);
                WriteLog("Navegando a " + _initialUrl);
                core.Navigate(_initialUrl);
            }
            catch (Exception exception)
            {
                _browserReady = false;
                WriteLog("ERROR INIT NATIVE: " + exception);
                SetStatus("No se pudo iniciar el navegador integrado: " + exception.Message, true);
                RhinoApp.WriteLine("AURUM CONECT 1.0.6: error WebView2. Registro: {0}", LogPath);
                RhinoApp.WriteLine(exception.ToString());
            }
        }

        private void UpdateBrowserBounds()
        {
            if (_controller == null) return;
            try
            {
                _controller.Bounds = new Rectangle(0, 0, _browserHost.ClientSize.Width, _browserHost.ClientSize.Height);
            }
            catch (Exception exception) { WriteLog("Error actualizando tamaño del navegador: " + exception.Message); }
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
            if (_controller != null && _controller.CoreWebView2 != null)
            {
                _controller.CoreWebView2.Navigate(safeUrl);
                return;
            }

            SetStatus("Preparando el navegador integrado…", false);
            _startupRequested = false;
            _ = RequestInitializationAsync("Se solicitó navegación antes de inicializar el controlador.");
        }
    }
}