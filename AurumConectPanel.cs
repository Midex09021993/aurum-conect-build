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

            layout.AddRow(CreateLabel("A U R U M   C O N E C T", 16, true, Gold));
            layout.AddRow(CreateLabel("ESTUDIO PROFESIONAL DE JOYERÍA  ·  RHINO 7", 8, true, TextMuted));
            layout.AddRow(CreateLabel("Conecta tu flujo CAD con AURUM RENDER. Elige si deseas abrir el visor o transferir el documento Rhino activo.", 10, false, TextMain));

            layout.AddRow(CreateLabel("ACCIONES PRINCIPALES", 8, true, Gold));
            var renderButton = CreateButton("◇  ABRIR AURUM RENDER", SurfaceRaised, Gold, 42);
            renderButton.Click += (_, __) => AurumConectRenderWindow.Open("https://tallerdeljoyero.com/aurum-render");
            layout.AddRow(renderButton);

            var transferButton = CreateButton("↗  ENVIAR DOCUMENTO Y ABRIR RENDER", Gold, Surface, 44);
            transferButton.Click += (_, __) => RhinoApp.RunScript("_AurumConectActualizar", false);
            layout.AddRow(transferButton);

            layout.AddRow(CreateLabel("COMANDOS RHINO", 8, true, Gold));
            layout.AddRow(CreateLabel("AurumConect  ·  Abre este panel\nAurumRender  ·  Abre el visor integrado\nAurumConectActualizar  ·  Envía el documento activo", 9, false, TextMuted));

            _status = CreateLabel("●  LISTO  ·  Esperando una acción", 9, true, TextMuted);
            layout.AddRow(_status);
            layout.AddRow(CreateLabel(
                "La transferencia crea una copia temporal completa del documento activo. El archivo original permanece intacto.",
                9, false, TextMuted));
            layout.AddRow(CreateLabel(
                "TALLER DEL JOYERO  ·  AURUM LAB", 8, true, TextMuted));

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