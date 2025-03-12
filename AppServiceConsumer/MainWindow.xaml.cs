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
            await backgroundServiceController.SendRequestAsync(myTextBox.Text);
        }

        private void StartServiceClick(object sender, RoutedEventArgs e)
        {
            backgroundServiceController.CreateAndStartService(@"E:\Main\WASDKSample-master\src\AppService\AppServiceConsumer\BackgroundServices\BackgroundService.exe");
        }

        private void StopServiceClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
