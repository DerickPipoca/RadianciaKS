using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RadianciaKS.PrintBridge
{
    internal class Program
    {
        // Altere para o nome exato que aparece no Windows ou passe por argumento
        private static string _printerName = "EPSON TM-T20X";
        private const int Port = 9100;

        private static async Task Main(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                _printerName = args[0];
            }

            Console.Title = "Radiância KS - Print Bridge";
            Console.WriteLine("==============================================");
            Console.WriteLine("    RADIANCIA KS - AGENTE DE IMPRESSAO        ");
            Console.WriteLine("==============================================");
            Console.WriteLine($"Impressora Alvo : {_printerName}");
            Console.WriteLine($"Porta de Escuta : {Port}");
            Console.WriteLine("Status          : Aguardando conexoes do Docker...");
            Console.WriteLine("----------------------------------------------\n");

            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            while (true)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO LISTENER] {ex.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                await using (var networkStream = client.GetStream())
                using (var memoryStream = new MemoryStream())
                {
                    // Copia os dados recebidos até a desconexão do cliente
                    await networkStream.CopyToAsync(memoryStream);
                    var rawBytes = memoryStream.ToArray();

                    if (rawBytes.Length == 0) return;

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Recebidos {rawBytes.Length} bytes do Docker. Enviando ao Spooler...");

                    bool success = RawPrinterHelper.SendBytesToPrinter(_printerName, rawBytes);

                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Impressão enviada com sucesso!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Falha ao enviar para o Spooler do Windows.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERRO CLIENTE] {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // Classe que conversa diretamente com a API nativa de Spooler do Windows
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName = "RadianciaKS_Receipt";
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile = null;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW"; // Modo RAW: não altera os bytes ESC/POS
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes)
        {
            IntPtr hPrinter = IntPtr.Zero;
            IntPtr pUnmanagedBytes = IntPtr.Zero;
            bool success = false;

            try
            {
                if (!OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
                    return false;

                var di = new DOCINFOA();
                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                if (!StartPagePrinter(hPrinter))
                    return false;

                pUnmanagedBytes = Marshal.AllocCoTaskMem(pBytes.Length);
                Marshal.Copy(pBytes, 0, pUnmanagedBytes, pBytes.Length);

                success = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out int bytesWritten);

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            finally
            {
                if (pUnmanagedBytes != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pUnmanagedBytes);

                if (hPrinter != IntPtr.Zero)
                    ClosePrinter(hPrinter);
            }

            return success;
        }
    }
}