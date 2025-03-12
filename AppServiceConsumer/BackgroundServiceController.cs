using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppServiceConsumer
{
    internal class BackgroundServiceController
    {
        const string ServiceName = "BackgroundWorkerService";

        public void CreateAndStartService(string servicePath, string serviceName = null)
        {
            if (serviceName == null)
            {
                serviceName = ServiceName;
            }

            if (!IsServiceInstalled(serviceName))
            {
                Debug.WriteLine($"Service '{serviceName}' is not installed. Installing and starting it now.");
                InstallAndStartService(servicePath, serviceName);
            }
            else
            {
                Debug.WriteLine($"Service '{serviceName}' is already installed and will be started if not running.");
                EnsureServiceIsRunning(serviceName);
            }
        }

        private void InstallAndStartService(string servicePath, string serviceName)
        {
            string createServiceCommand = $"New-Service -Name '{serviceName}' -BinaryPathName '{servicePath}' -StartupType Automatic; Start-Service -Name '{serviceName}'";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{createServiceCommand}\"",
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.WriteLine($"Error: {error}");
                    }
                    else
                    {
                        Debug.WriteLine($"Service '{serviceName}' created and started successfully: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to install and start service '{serviceName}': {ex.Message}");
            }
        }

        private void EnsureServiceIsRunning(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                        Debug.WriteLine($"Service '{serviceName}' started successfully.");
                    }
                    else
                    {
                        Debug.WriteLine($"Service '{serviceName}' is already running.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start service '{serviceName}': {ex.Message}");
            }
        }

        public async Task SendRequestAsync(string command)
        {
            Debug.WriteLine($"Sending message to service: {command}");

            using (var client = new NamedPipeClientStream(".", ServiceName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                try
                {
                    await client.ConnectAsync();
                    using (var reader = new StreamReader(client))
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    {
                        await writer.WriteLineAsync(command);
                        string response = await reader.ReadLineAsync();
                        Debug.WriteLine($"Response from service: {response}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to connect or communicate with the service: {ex.Message}");
                }
            }
        }

        private bool IsServiceInstalled(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    var status = sc.Status; // Перевірка стану дозволяє виявити, чи сервіс існує
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
