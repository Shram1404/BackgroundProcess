using BackgroundService.Commands;
using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MyBackgroundService
{
    public class Worker : Microsoft.Extensions.Hosting.BackgroundService
    {
        const string ServiceName = "BackgroundWorkerService";
        private readonly ILogger<Worker> logger;

        public Worker(ILogger<Worker> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var server = NativePipeServer.CreatePipeServer(ServiceName))
                    {
                        await server.WaitForConnectionAsync(stoppingToken);

                        using (var reader = new StreamReader(server))
                        using (var writer = new StreamWriter(server) { AutoFlush = true })
                        {
                            string? request = await reader.ReadLineAsync();
                            if (!string.IsNullOrEmpty(request))
                            {
                                logger.LogInformation($"Received: {request}");

                                var command = ParseCommand(request);

                                string response = string.Empty;
                                if (command != null)
                                {
                                    response = await command.ExecuteAsync();
                                }

                                logger.LogInformation($"Command Response: {response}");

                                await writer.WriteLineAsync(response);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in background service execution.");
                }
            }
        }

        private IServiceCommand? ParseCommand(string request)
        {
            IServiceCommand command = null;

            var commandData = JsonSerializer.Deserialize<Dictionary<string, string>>(request);

            if (commandData != null)
            {
                if (commandData.ContainsKey("type") && !string.IsNullOrEmpty(commandData["type"]))
                {
                    command = commandData["type"] switch
                    {
                        "http" => new HttpRequestCommand(commandData["url"]),
                        "toast" => throw new NotImplementedException(),
                        "startApp" => throw new NotImplementedException(),
                        _ => throw new InvalidOperationException("Unknown command type")
                    };
                }
            }

            return command;
        }
    }

    public class NativePipeServer
    {
        // ���������� P/Invoke ��� CreateNamedPipe
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafePipeHandle CreateNamedPipe(
            string lpName,
            uint dwOpenMode,
            uint dwPipeMode,
            uint nMaxInstances,
            uint nOutBufferSize,
            uint nInBufferSize,
            uint nDefaultTimeOut,
            ref SECURITY_ATTRIBUTES lpSecurityAttributes);

        // ������� ��� ����������� SDDL � SECURITY_DESCRIPTOR
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string StringSecurityDescriptor,
            uint StringSDRevision,
            out IntPtr SecurityDescriptor,
            IntPtr SecurityDescriptorSize);

        // ��� ��������� ���'��, ������� ConvertStringSecurityDescriptorToSecurityDescriptor
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        // ��������� SECURITY_ATTRIBUTES
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        // ��������� ��� CreateNamedPipe
        private const uint PIPE_ACCESS_DUPLEX = 0x00000003;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const uint PIPE_TYPE_MESSAGE = 0x00000004;
        private const uint PIPE_READMODE_MESSAGE = 0x00000002;
        private const uint PIPE_WAIT = 0x00000000;

        /// <summary>
        /// ������� NamedPipeServerStream �� ������� ��'�� ������ �� ������������ �������� ��� ���.
        /// </summary>
        /// <param name="pipeName">��'� ������ (��� �������� "\\.\pipe\")</param>
        /// <returns>NamedPipeServerStream � ��������� ����������� �������</returns>
        public static NamedPipeServerStream CreatePipeServer(string pipeName)
        {
            // ������������� SDDL ��� ������� ������� ������� (Generic All) ��� ��� ������������
            string sddl = "D:(A;;FA;;;WD)";

            // ���������� SDDL � SECURITY_DESCRIPTOR
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, 1, out IntPtr pSecurityDescriptor, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, "ConvertStringSecurityDescriptorToSecurityDescriptor �� �������");
            }

            // ���������� SECURITY_ATTRIBUTES
            var sa = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
                lpSecurityDescriptor = pSecurityDescriptor,
                bInheritHandle = false
            };

            // ������� ����� ��'� ������
            string fullPipeName = @"\\.\pipe\" + pipeName;

            // ��������� ���������� ����� � ����������� ������� ������
            SafePipeHandle pipeHandle = CreateNamedPipe(
                fullPipeName,
                PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                1,        // ����������� ������� ���������
                4096,     // ����� ��������� ������ (����)
                4096,     // ����� �������� ������ (����)
                0,        // ������� (��)
                ref sa);

            // ��������� ���'���, ������� ������������� SECURITY_DESCRIPTOR
            LocalFree(pSecurityDescriptor);

            if (pipeHandle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, "CreateNamedPipe �� �������");
            }

            // ��������� SafePipeHandle � NamedPipeServerStream.
            // ������������� �����������: NamedPipeServerStream(PipeDirection, bool isAsync, bool isConnected, SafePipeHandle)
            return new NamedPipeServerStream(PipeDirection.InOut, true, false, pipeHandle);
        }
    }
}
