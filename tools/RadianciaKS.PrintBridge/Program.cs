using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RadianciaKS.PrintBridge
{
    internal class Program
    {
        // Altere para o nome exato que aparece no Windows ou passe por argumento
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "printer.txt");
        private static string _printerName = string.Empty;
        private const int Port = 9100;

        private static async Task Main(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                _printerName = args[0];
            }
            /**
             *       ___          ___               _          ___      _    __        
             *      / _ \___ ____/ (_)__ ____  ____(_)__ _____/ _ )____(_)__/ /__ ____ 
             *     / , _/ _ `/ _  / / _ `/ _ \/ __/ / _ `/___/ _  / __/ / _  / _ `/ -_)
             *    /_/|_|\_,_/\_,_/_/\_,_/_//_/\__/_/\_,_/   /____/_/ /_/\_,_/\_, /\__/ 
             *                                                              /___/      
             */
            Console.Title = "Radiância KS - Print Bridge";
            Console.WriteLine("    ____            ___                  _       __ _______");
            Console.WriteLine("   / __ \\____ _____/ (_)___ _____  _____(_)___ _/ //_/ ___/");
            Console.WriteLine("  / /_/ / __ `/ __  / / __ `/ __ \\/ ___/ / __ `/ ,<  \\__ \\ ");
            Console.WriteLine(" / _, _/ /_/ / /_/ / / /_/ / / / / /__/ / /_/ / /| |___/ / ");
            Console.WriteLine("/_/ |_|\\__,_/\\__,_/_/\\__,_/_/ /_/\\___/_/\\__,_/_/ |_/____/  ");
            Console.WriteLine("    ____       _       __     ____       _     __         ");
            Console.WriteLine("   / __ \\_____(_)___  / /_   / __ )_____(_)___/ /___ ____ ");
            Console.WriteLine("  / /_/ / ___/ / __ \\/ __/  / __  / ___/ / __  / __ `/ _ \\");
            Console.WriteLine(" / ____/ /  / / / / / /_   / /_/ / /  / / /_/ / /_/ /  __/");
            Console.WriteLine("/_/   /_/  /_/_/ /_/\\__/  /_____/_/  /_/\\__,_/\\__, /\\___/ ");
            Console.WriteLine("                                             /____/       ");
            Console.WriteLine("----------------------------------------------");
            string? candidatePrinter = null;

            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                candidatePrinter = args[0].Trim();
            }
            else if (File.Exists(ConfigPath))
            {
                try
                {
                    candidatePrinter = (await File.ReadAllTextAsync(ConfigPath)).Trim();
                }
                catch
                {
                    candidatePrinter = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(candidatePrinter))
            {
                Console.Write($"Verificando impressora '{candidatePrinter}'... ");
                if (RawPrinterHelper.PrinterExists(candidatePrinter))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[OK]");
                    Console.ResetColor();
                    _printerName = candidatePrinter;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[FALHA OU INACESSÍVEL]");
                    Console.ResetColor();
                }
            }

            if (string.IsNullOrWhiteSpace(_printerName))
            {
                _printerName = SelectPrinterInteractive();
                await File.WriteAllTextAsync(ConfigPath, _printerName);
            }

            Console.WriteLine($"Impressora Ativa : {_printerName}");
            Console.WriteLine($"Porta de Escuta  : {Port}");
            Console.WriteLine($"Configuração     : {ConfigPath}");
            Console.WriteLine("Status           : Aguardando conexões do Docker...");
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

        private static string SelectPrinterInteractive()
        {
            while (true)
            {
                Console.WriteLine("\nDetectando impressoras instaladas no Windows...");
                var printers = RawPrinterHelper.GetInstalledPrinters();

                if (printers.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERRO] Nenhuma impressora foi encontrada no Windows!");
                    Console.ResetColor();
                    Console.WriteLine("Conecte o cabo USB da Epson e instale o driver oficial.");
                    Console.WriteLine("Pressione ENTER para tentar detectar novamente...");
                    Console.ReadLine();
                    continue;
                }

                Console.WriteLine("\nSelecione a impressora desejada para o Radiancia KS:");
                Console.WriteLine("----------------------------------------------");
                for (int i = 0; i < printers.Count; i++)
                {
                    Console.WriteLine($" [{i + 1}] {printers[i]}");
                }
                Console.WriteLine("----------------------------------------------");
                Console.Write($"Digite o número da impressora (1 a {printers.Count}): ");

                var input = Console.ReadLine();
                if (int.TryParse(input, out int selectedIndex) && selectedIndex >= 1 && selectedIndex <= printers.Count)
                {
                    var chosen = printers[selectedIndex - 1];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n[SUCESSO] Impressora '{chosen}' selecionada e salva!\n");
                    Console.ResetColor();
                    return chosen;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Opção inválida. Tente novamente.");
                Console.ResetColor();
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

    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName = "RadianciaKS_Receipt";
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile = null;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PRINTER_INFO_4
        {
            public IntPtr pPrinterName;
            public IntPtr pServerName;
            public uint Attributes;
        }

        private const int PRINTER_ENUM_LOCAL = 0x00000002;
        private const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

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

        [DllImport("winspool.Drv", EntryPoint = "EnumPrintersA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum, int cbBuf, out int pcbNeeded, out int pcReturned);

        public static bool PrinterExists(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;

            IntPtr hPrinter = IntPtr.Zero;
            try
            {
                return OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero);
            }
            finally
            {
                if (hPrinter != IntPtr.Zero)
                    ClosePrinter(hPrinter);
            }
        }

        public static List<string> GetInstalledPrinters()
        {
            var printers = new List<string>();
            int flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;

            EnumPrinters(flags, null, 4, IntPtr.Zero, 0, out int pcbNeeded, out _);
            if (pcbNeeded <= 0) return printers;

            IntPtr pBuffer = Marshal.AllocHGlobal(pcbNeeded);
            try
            {
                if (EnumPrinters(flags, null, 4, pBuffer, pcbNeeded, out _, out int pcReturned))
                {
                    int structSize = Marshal.SizeOf<PRINTER_INFO_4>();
                    for (int i = 0; i < pcReturned; i++)
                    {
                        IntPtr currentPtr = IntPtr.Add(pBuffer, i * structSize);
                        var info = Marshal.PtrToStructure<PRINTER_INFO_4>(currentPtr);
                        if (info.pPrinterName != IntPtr.Zero)
                        {
                            string? name = Marshal.PtrToStringAnsi(info.pPrinterName);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                printers.Add(name);
                            }
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuffer);
            }

            return printers;
        }

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

                success = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out int _);

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