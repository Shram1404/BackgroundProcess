using Microsoft.UI.Xaml;


namespace AppServiceConsumer
{
    public sealed partial class MainWindow : Window
    {
        private BackgroundServiceController backgroundServiceController = new BackgroundServiceController();

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            await backgroundServiceController.SendRequestAsync("http", "https://www.google.com");
        }

        private void StartServiceClick(object sender, RoutedEventArgs e)
        {
            backgroundServiceController.CreateAndStartService(@"C:\Users\shram\Desktop\WASDKSample-master\src\AppService\AppServiceConsumer\BackgroundServices\BackgroundService.exe");
        }

        private void StopServiceClick(object sender, RoutedEventArgs e)
        {
            backgroundServiceController.StopService();
        }
    }
}
