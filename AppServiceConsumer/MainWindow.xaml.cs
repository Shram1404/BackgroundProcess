using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;


namespace AppServiceConsumer
{
    public sealed partial class MainWindow : Window
    {
        private AppServiceConnection _connection;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";
            if (_connection == null)
            {
                _connection = new AppServiceConnection();

                _connection.AppServiceName = "com.greatfive.great";
                _connection.PackageFamilyName = "3bd3f21f-c50b-49b6-aecc-59ff846ae0d2_kqnxptwqxttew";

                var status = await _connection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    myTextBlock.Text = "Fail to Connect";
                    _connection = null;
                    return;
                }
            }

            int idx = int.Parse(myTextBox.Text);
            var msg = new ValueSet();
            msg.Add("Command", "Name");
            msg.Add("ID", idx);
            var response = await _connection.SendMessageAsync(msg);
            string result = "";
            if (response.Status == AppServiceResponseStatus.Success)
            {
                if (response.Message["Status"] as string == "ok")
                {
                    result = response.Message["Result"] as string;
                }
            }
            msg.Clear();
            msg.Add("Command", "Age");
            msg.Add("ID", idx);
            response = await _connection.SendMessageAsync(msg);

            if (response.Status == AppServiceResponseStatus.Success)
            {
                if (response.Message["Status"] as string == "ok")
                {
                    result += ":Age = " + response.Message["Result"] as string;
                }

            }

            myTextBox.Text = result;
        }
    }
}
