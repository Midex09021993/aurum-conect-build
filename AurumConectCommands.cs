using System;
using System.IO;
using Rhino;
using Rhino.FileIO;

namespace AurumConect
{
    public sealed class AurumConectActualizarCommand : Rhino.Commands.Command
    {
        private static AurumConectLocalServer _activeTransfer;
        public override string EnglishName { get { return "AurumConectActualizar"; } }

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            if (doc == null)
            {
                RhinoApp.WriteLine("AURUM CONECT: no hay un documento activo.");
                return Rhino.Commands.Result.Failure;
            }

            string tempDirectory = Path.Combine(Path.GetTempPath(), "AURUM-LAB", "AURUM-CONECT");
            Directory.CreateDirectory(tempDirectory);
            string documentName = Path.GetFileNameWithoutExtension(doc.Name);
            if (String.IsNullOrWhiteSpace(documentName)) documentName = "diseno";
            foreach (char invalid in Path.GetInvalidFileNameChars()) documentName = documentName.Replace(invalid, '_');
            string fileName = documentName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".3dm";
            string outputPath = Path.Combine(tempDirectory, fileName);

            // Escribe una copia completa del documento activo, sin alterar su nombre,
            // estado de guardado ni selección. Incluye objetos, capas, bloques,
            // unidades, atributos, datos de usuario y mallas de render.
            using (var options = new FileWriteOptions())
            {
                options.WriteSelectedObjectsOnly = false;
                options.WriteGeometryOnly = false;
                options.WriteUserData = true;
                options.IncludeRenderMeshes = true;
                options.IncludeBitmapTable = true;
                options.IncludeHistory = true;
                options.SuppressDialogBoxes = true;
                options.UseCompression = true;

                if (!doc.Write3dmFile(outputPath, options) || !File.Exists(outputPath))
                {
                    RhinoApp.WriteLine("AURUM CONECT: Rhino no pudo generar la copia completa del documento.");
                    return Rhino.Commands.Result.Failure;
                }
            }

            try
            {
                if (_activeTransfer != null) _activeTransfer.Dispose();
                _activeTransfer = AurumConectLocalServer.Start(outputPath);
                _activeTransfer.Run();
                AurumConectRenderWindow.Open(_activeTransfer.CreateHandoffUrl());
            }
            catch (Exception exception)
            {
                RhinoApp.WriteLine("AURUM CONECT: no se pudo iniciar el canal local seguro: {0}", exception.Message);
                return Rhino.Commands.Result.Failure;
            }

            RhinoApp.WriteLine("AURUM CONECT: documento Rhino completo preparado para AURUM RENDER ({0}).", Path.GetFileName(outputPath));
            return Rhino.Commands.Result.Success;
        }
    }
}
