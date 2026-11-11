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
        private readonly Label _status;

        public AurumConectPanel()
        {
            var layout = new DynamicLayout
            {
                Padding = new Padding(16),
                Spacing = new Size(0, 10),
                BackgroundColor = Color.FromArgb(27, 30, 35)
            };

            layout.AddRow(new Label { Text = "AURUM CONECT", Font = new Font(SystemFont.Bold, 15), TextColor = Color.FromArgb(215, 181, 111) });
            layout.AddRow(new Label { Text = "MATRIXGOLD 3.1  ·  RHINO 7", TextColor = Color.FromArgb(165, 170, 178) });
            layout.AddRow(new Label
            {
                Text = "Envía una copia de la geometría seleccionada a AURUM Render.",
                TextColor = Color.FromArgb(205, 208, 213),
                Wrap = WrapMode.Word
            });

            var connectButton = new Button
            {
                Text = "CONECTAR A AURUM LAB", Height = 38,
                BackgroundColor = Color.FromArgb(48, 55, 64),
                TextColor = Color.FromArgb(235, 225, 202)
            };
            connectButton.Click += (_, __) => OpenAurumLogin();
            layout.AddRow(connectButton);

            var renderButton = new Button
            {
                Text = "ABRIR AURUM RENDER", Height = 38,
                BackgroundColor = Color.FromArgb(48, 55, 64),
                TextColor = Color.FromArgb(215, 181, 111)
            };
            renderButton.Click += (_, __) => AurumConectRenderWindow.Open("https://www.tallerdeljoyero.com/auth?returnTo=aurum-render");
            layout.AddRow(renderButton);

            var updateButton = new Button
            {
                Text = "ENVIAR DISEÑO", Height = 42,
                BackgroundColor = Color.FromArgb(184, 143, 63),
                TextColor = Color.FromArgb(25, 25, 25)
            };
            updateButton.Click += (_, __) => RhinoApp.RunScript("_AurumConectActualizar", false);
            layout.AddRow(updateButton);

            _status = new Label
            {
                Text = "Estado: esperando actualización",
                TextColor = Color.FromArgb(165, 170, 178),
                Wrap = WrapMode.Word
            };
            layout.AddRow(_status);
            layout.AddRow(new Label
            {
                Text = "El acceso se realiza en la página oficial de AURUM LAB. Tus credenciales nunca se guardan en Rhino ni en este complemento.",
                TextColor = Color.FromArgb(145, 150, 158), Wrap = WrapMode.Word
            });
            layout.AddRow(new Label
            {
                Text = "AURUM RENDER se abre en una ventana Chromium vinculada a Rhino. Puedes mantenerla junto al área CAD o maximizarla.",
                TextColor = Color.FromArgb(145, 150, 158), Wrap = WrapMode.Word
            });

            Content = new Scrollable { Content = layout, ExpandContentWidth = true };
        }

        internal static void OpenAurumLogin() { OpenUrl("https://www.tallerdeljoyero.com/auth?returnTo=aurum-render"); }

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
