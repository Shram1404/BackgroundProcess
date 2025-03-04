using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppServiceConsumer
{
    internal class BackgroundServiceController
    {
        const string ServiceName = "BackgroundWorkerService"; // TODO: IMPORTANT - Must be unique for each service or application // TODO: Move to shared constants

        public void CreateAndStartService(string servicePath, string serviceName = null)
        {
            if (serviceName == null)
            {
                serviceName = "BackgroundWorkerService";
            }

            if (!IsServiceInstalled(serviceName))
            {
                RunServiceAsAdministrator(servicePath, serviceName);
            }
            else
            {
                StartService(serviceName);
            }
        }

        private void RunServiceAsAdministrator(string servicePath, string serviceName)
        {
            string createServiceCommand = $"New-Service -Name '{serviceName}' -BinaryPathName '{servicePath}'";
            string combinedCommand = $"{createServiceCommand}; Start-Service -Name '{serviceName}'";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{combinedCommand}\"",
                UseShellExecute = true,
                Verb = "runas"
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
                        Debug.WriteLine($"Service created and started successfully: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create and start service: {ex.Message}");
            }
        }

        public void StopService(string serviceName = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                serviceName = ServiceName;
            }

            if (!IsAdministrator())
            {
                RunAsAdministrator("Stop-Service", serviceName);
                return;
            }

            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
        }

        private void StartService(string serviceName)
        {
            if (!IsAdministrator())
            {
                RunAsAdministrator("Start-Service", serviceName);
                return;
            }

            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start service: {ex.Message}");
            }
        }

        private void RunAsAdministrator(string command, string serviceName)
        {
            string psCommand = $"{command} -Name '{serviceName}'";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{psCommand}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to run as administrator: {ex.Message}");
            }
        }

        private bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public async Task SendRequestAsync(string type, string message)
        {
            string serviceMessage = JsonSerializer.Serialize(new Dictionary<string, string> { { "Type", type }, { "Message", message } });

            Debug.WriteLine($"Sending message to service: {serviceMessage}");

            using (var client = new NamedPipeClientStream(".", ServiceName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                try
                {
                    await client.ConnectAsync();
                    using (var reader = new StreamReader(client))
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    {
                        await writer.WriteLineAsync(serviceMessage);
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
                    var status = sc.Status;
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
