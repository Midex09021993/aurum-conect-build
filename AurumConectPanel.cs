using System;
using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;

namespace AurumConect
{
    [System.Runtime.InteropServices.Guid("8D7C9A11-4A2F-46D3-A80B-2D0D9B9A7102")]
    public sealed class AurumConectPanel : Panel
    {
        private static readonly Color Surface = Color.FromArgb(27, 30, 35);
        private static readonly Color SurfaceRaised = Color.FromArgb(39, 44, 52);
        private static readonly Color Gold = Color.FromArgb(215, 181, 111);
        private static readonly Color TextMain = Color.FromArgb(232, 234, 238);
        private static readonly Color TextMuted = Color.FromArgb(170, 176, 185);
        private readonly Label _status;

        public AurumConectPanel()
        {
            var layout = new DynamicLayout
            {
                Padding = new Padding(16),
                Spacing = new Size(0, 10),
                BackgroundColor = Surface
            };

            layout.AddRow(CreateLabel("AURUM CONECT", 16, true, Gold));
            layout.AddRow(CreateLabel("MATRIXGOLD 3.1  ·  RHINO 7", 9, true, TextMuted));
            layout.AddRow(CreateLabel("Envía tu diseño desde Rhino. AURUM RENDER lo recibe automáticamente.", 10, false, TextMain));

            var connectButton = CreateButton("CONECTAR A AURUM LAB", SurfaceRaised, TextMain, 38);
            connectButton.Click += (_, __) => OpenAurumLogin();
            layout.AddRow(connectButton);

            var renderButton = CreateButton("ABRIR AURUM RENDER", SurfaceRaised, Gold, 38);
            renderButton.Click += (_, __) => AurumConectRenderWindow.Open("https://tallerdeljoyero.com/aurum-render");
            layout.AddRow(renderButton);

            var updateButton = CreateButton("ENVIAR DISEÑO", Gold, Color.FromArgb(28, 28, 28), 42);
            updateButton.Click += (_, __) => RhinoApp.RunScript("_AurumConectActualizar", false);
            layout.AddRow(updateButton);

            _status = CreateLabel("Estado: esperando actualización", 9, false, TextMuted);
            layout.AddRow(_status);
            layout.AddRow(CreateLabel(
                "El acceso se realiza en la página oficial de AURUM LAB. Tus credenciales nunca se guardan en Rhino ni en este complemento.",
                9, false, TextMuted));
            layout.AddRow(CreateLabel(
                "AURUM RENDER se abre en una ventana Chromium vinculada a Rhino. Puedes mantenerla junto al área CAD o maximizarla.",
                9, false, TextMuted));

            Content = new Scrollable
            {
                Content = layout,
                ExpandContentWidth = true,
                BackgroundColor = Surface
            };
            BackgroundColor = Surface;
        }

        private static Label CreateLabel(string text, float size, bool bold, Color foreground)
        {
            return new Label
            {
                Text = text,
                Font = new Font(bold ? SystemFont.Bold : SystemFont.Default, size),
                TextColor = foreground,
                BackgroundColor = Surface,
                Wrap = WrapMode.Word
            };
        }

        private static Button CreateButton(string text, Color background, Color foreground, int height)
        {
            return new Button
            {
                Text = text,
                Height = height,
                BackgroundColor = background,
                TextColor = foreground
            };
        }

        internal static void OpenAurumLogin()
        {
            OpenUrl("https://tallerdeljoyero.com/aurum-render");
        }

        internal static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception exception) { RhinoApp.WriteLine("AURUM CONECT: no se pudo abrir el navegador: {0}", exception.Message); }
        }

        internal void SetStatus(string message)
        {
            if (_status == null) return;
            Application.Instance.AsyncInvoke(() => _status.Text = message);
        }
    }

    public sealed class AurumConectCommand : Rhino.Commands.Command
    {
        public override string EnglishName { get { return "AurumConect"; } }
        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            Panels.OpenPanel(typeof(AurumConectPanel).GUID);
            return Rhino.Commands.Result.Success;
        }
    }
}