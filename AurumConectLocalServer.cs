using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AurumConect
{
    internal sealed class AurumConectLocalServer : IDisposable
    {
        private static readonly string[] AllowedOrigins = { "https://tallerdeljoyero.com", "https://www.tallerdeljoyero.com" };
        private const long MaxFileBytes = 100L * 1024L * 1024L;
        private const int TransferBufferBytes = 64 * 1024;
        private readonly TcpListener _listener;
        private readonly string _filePath;
        private readonly string _token;
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly DateTime _expiresAt;
        private int _consumed;

        private AurumConectLocalServer(string filePath)
        {
            _filePath = Path.GetFullPath(filePath);
            var info = new FileInfo(_filePath);
            if (!info.Exists || !String.Equals(info.Extension, ".3dm", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Solo se puede transferir un archivo .3dm existente.");
            if (info.Length <= 0 || info.Length > MaxFileBytes)
                throw new InvalidOperationException("El archivo supera el límite seguro de 100 MB o está vacío.");

            byte[] random = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(random);
            _token = BitConverter.ToString(random).Replace("-", "").ToLowerInvariant();
            _expiresAt = DateTime.UtcNow.AddMinutes(3);
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start(1);
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        internal int Port { get; private set; }
        internal static AurumConectLocalServer Start(string filePath) { return new AurumConectLocalServer(filePath); }

        internal string CreateHandoffUrl()
        {
            return AllowedOrigins[0] + "/auth?returnTo=aurum-render#aurum-conect=" + Port.ToString() + "." + _token;
        }

        internal void Run() { Task.Run((Action)AcceptLoop); }

        private void AcceptLoop()
        {
            try
            {
                while (!_stop.IsCancellationRequested && DateTime.UtcNow < _expiresAt)
                {
                    if (!_listener.Pending()) { Thread.Sleep(80); continue; }
                    using (TcpClient client = _listener.AcceptTcpClient())
                    {
                        client.ReceiveTimeout = 5000;
                        client.SendTimeout = 10000;
                        using (NetworkStream stream = client.GetStream()) HandleRequest(stream);
                    }
                    if (Volatile.Read(ref _consumed) == 1) break;
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            finally
            {
                Dispose();
                try { if (File.Exists(_filePath)) File.Delete(_filePath); } catch { }
            }
        }

        private void HandleRequest(NetworkStream stream)
        {
            string requestLine;
            string origin = null;
            string contentLength = null;
            string transferEncoding = null;
            int headerBytes = 0;

            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                requestLine = reader.ReadLine();
                if (requestLine == null || requestLine.Length > 2048)
                {
                    WriteResponse(stream, 400, "Bad Request", "Solicitud inválida.");
                    return;
                }
                string line;
                while (!String.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    headerBytes += line.Length;
                    if (headerBytes > 8192)
                    {
                        WriteResponse(stream, 431, "Request Header Fields Too Large", "Cabeceras demasiado grandes.");
                        return;
                    }
                    int colon = line.IndexOf(':');
                    if (colon <= 0) continue;
                    string key = line.Substring(0, colon).Trim();
                    string value = line.Substring(colon + 1).Trim();
                    if (key.Equals("Origin", StringComparison.OrdinalIgnoreCase)) origin = value;
                    if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) contentLength = value;
                    if (key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) transferEncoding = value;
                }
            }

            string allowedOrigin = Array.Find(AllowedOrigins, value => String.Equals(value, origin, StringComparison.Ordinal));
            if (allowedOrigin == null)
            {
                WriteResponse(stream, 403, "Forbidden", "Origen no autorizado: " + (origin ?? "(sin Origin)"));
                return;
            }
            string[] parts = (requestLine ?? "").Split(' ');
            if (parts.Length != 3 || parts[2] != "HTTP/1.1")
            {
                WriteResponse(stream, 400, "Bad Request", "Solicitud HTTP inválida.");
                return;
            }

            string expectedPath = "/transfer?token=" + _token;
            if (!String.Equals(parts[1], expectedPath, StringComparison.Ordinal))
            {
                WriteResponse(stream, 401, "Unauthorized", "Token inválido.");
                return;
            }

            // Chromium puede exigir preflight para una solicitud desde HTTPS hacia loopback.
            // Se autoriza únicamente el origen oficial y el método GET, sin abrir otros métodos.
            if (parts[0] == "OPTIONS")
            {
                string preflight = "HTTP/1.1 204 No Content\r\n" +
                    "Access-Control-Allow-Origin: " + allowedOrigin + "\r\n" +
                    "Access-Control-Allow-Methods: GET, OPTIONS\r\n" +
                    "Access-Control-Allow-Headers: content-type\r\n" +
                    "Access-Control-Max-Age: 60\r\n" +
                    "Access-Control-Allow-Private-Network: true\r\n" +
                    "Vary: Origin\r\n" +
                    "Content-Length: 0\r\n" +
                    "Connection: close\r\n\r\n";
                byte[] preflightBytes = Encoding.ASCII.GetBytes(preflight);
                stream.Write(preflightBytes, 0, preflightBytes.Length);
                stream.Flush();
                return;
            }

            if (parts[0] != "GET")
            {
                WriteResponse(stream, 405, "Method Not Allowed", "Solo se admite GET.");
                return;
            }
            if (transferEncoding != null || (contentLength != null && contentLength != "0"))
            {
                WriteResponse(stream, 400, "Bad Request", "No se admite contenido en la solicitud.");
                return;
            }
            if (DateTime.UtcNow >= _expiresAt)
            {
                WriteResponse(stream, 410, "Gone", "La transferencia expiró. Actualiza el diseño desde Rhino.");
                return;
            }
            if (Volatile.Read(ref _consumed) == 1)
            {
                WriteResponse(stream, 410, "Gone", "Esta transferencia ya fue completada.");
                return;
            }

            bool responseStarted = false;
            try
            {
                // Transfiere por bloques para evitar cargar hasta 100 MB en memoria.
                // El archivo y el token solo se consumen cuando el envío completo termina.
                using (var file = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, TransferBufferBytes, FileOptions.SequentialScan))
                {
                    long fileLength = file.Length;
                    if (fileLength <= 0 || fileLength > MaxFileBytes)
                    {
                        WriteResponse(stream, 413, "Payload Too Large", "El archivo supera el límite seguro de 100 MB o está vacío.");
                        return;
                    }

                    string header = "HTTP/1.1 200 OK\r\n" +
                        "Access-Control-Allow-Origin: " + allowedOrigin + "\r\n" +
                        "Vary: Origin\r\n" +
                        "Content-Type: application/octet-stream\r\n" +
                        "Content-Disposition: attachment; filename=\"aurum-conect.3dm\"\r\n" +
                        "Content-Length: " + fileLength + "\r\n" +
                        "Cache-Control: no-store, no-cache, must-revalidate\r\n" +
                        "Pragma: no-cache\r\n" +
                        "X-Content-Type-Options: nosniff\r\n" +
                        "Connection: close\r\n\r\n";
                    byte[] headerBytesArray = Encoding.ASCII.GetBytes(header);
                    stream.Write(headerBytesArray, 0, headerBytesArray.Length);
                    responseStarted = true;

                    byte[] buffer = new byte[TransferBufferBytes];
                    long remaining = fileLength;
                    while (remaining > 0)
                    {
                        int read = file.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                        if (read <= 0) throw new EndOfStreamException("El archivo cambió durante la transferencia.");
                        stream.Write(buffer, 0, read);
                        remaining -= read;
                    }
                    stream.Flush();
                }

                Interlocked.Exchange(ref _consumed, 1);
            }
            catch (Exception)
            {
                // Si el cliente corta la conexión, no se consume el token ni se borra
                // el archivo: el navegador puede reintentar dentro de la ventana de 3 min.
                if (!responseStarted)
                {
                    try { WriteResponse(stream, 500, "Internal Server Error", "No se pudo leer el archivo temporal."); } catch { }
                }
            }
        }

        private static void WriteResponse(NetworkStream stream, int status, string reason, string message)
        {
            byte[] body = Encoding.UTF8.GetBytes(message);
            string header = "HTTP/1.1 " + status + " " + reason + "\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n";
            byte[] head = Encoding.ASCII.GetBytes(header);
            stream.Write(head, 0, head.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        public void Dispose()
        {
            if (!_stop.IsCancellationRequested) _stop.Cancel();
            try { _listener.Stop(); } catch { }
        }
    }
}
