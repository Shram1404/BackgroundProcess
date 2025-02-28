using System.IO.Pipes;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace AppServiceConsumer
{
    internal class BackgroundServiceController
    {
        const string ServiceName = "MyTestBackgroundService"; // TODO: IMPORTANT - Must be unique for each service or application // TODO: Move to shared constants

        bool isServiceRunning = false;

        public void StartService(string serviceName)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);

                    isServiceRunning = true;
                }
            }
        }

        public void StopService(string serviceName)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);

                    isServiceRunning = false;
                }
            }
        }

        public async Task SendRequestAsync(string message)
        {
            using (var client = new NamedPipeClientStream(".", ServiceName, PipeDirection.InOut))
            {
                await client.ConnectAsync();

                using (var reader = new StreamReader(client))
                using (var writer = new StreamWriter(client) { AutoFlush = true })
                {
                    await writer.WriteLineAsync(message);
                    string response = await reader.ReadLineAsync();
                }
            }
        }
    }
}
