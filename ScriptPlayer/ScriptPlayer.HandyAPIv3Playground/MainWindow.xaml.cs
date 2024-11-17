using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScriptPlayer.HandyAPIv3Playground.TheHandyV3;

namespace ScriptPlayer.HandyAPIv3Playground
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HandyApiV3 _api;

        public MainWindow()
        {
            InitializeComponent();

            txtApiKey.Text = LoadKey();
            GridFunctions.IsEnabled = false;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            SaveKey(txtApiKey.Text);

            _api = new HandyApiV3(txtApiKey.Text);

            txtApiKey.IsEnabled = false;

            GridFunctions.IsEnabled = true;
        }

        private void SaveKey(string value)
        {
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\ScriptPlayer", "ConnectionKey", value);
        }

        private string LoadKey()
        {
            return (string) Registry.GetValue("HKEY_CURRENT_USER\\Software\\ScriptPlayer", "ConnectionKey", "");
        }

        private void btnSetSlideRange_Click(object sender, RoutedEventArgs e)
        {
            Execute(async ()=> await _api.PutSliderStroke(new SliderSettings
            {
                Min = sldRange.LowerValue / 100.0,
                Max = sldRange.UpperValue / 100.0,
            }));
        }

        private async void Execute<T>(Func<Task<Response<T>>> methodWithResponse, Action<T> onSuccess = null) where T : class
        {
            try
            {
                GridFunctions.IsEnabled = false;

                var response = await methodWithResponse();
                
                StringBuilder builder = new StringBuilder();

                if (response.Error != null)
                {
                    builder.AppendLine("Error!\r\n");
                    builder.AppendLine($"Name: {response.Error.Name}");
                    builder.AppendLine($"Code: {response.Error.Code}");
                    builder.AppendLine($"Connected: {response.Error.Connected}");
                    builder.AppendLine($"Message: {response.Error.Message}");
                }
                else
                {
                    builder.AppendLine("Response: \r\n");
                    builder.AppendLine(JsonConvert.SerializeObject(response.Result, Formatting.Indented));

                    onSuccess?.Invoke(response.Result);
                }

                builder.AppendLine();
                builder.AppendLine($"RateLimit = {response.RateLimitPerMinute}/m");
                builder.AppendLine($"Remaining = {response.RateLimitRemaining}");
                builder.AppendLine($"Reset in {response.MsUntilRateLimitReset} ms");
                

                txtResponse.Text = builder.ToString();
            }
            catch (Exception e)
            {
                txtResponse.Text = "Exception!\r\n" + e.Message;
            }
            finally
            {
                GridFunctions.IsEnabled = true;
            }
        }

        private void btnGetConnected_Click(object sender, RoutedEventArgs e)
        {
            Execute(async ()=> await _api.GetConnected());
        }

        private void btnSetHampMode_Click(object sender, RoutedEventArgs e)
        {
            Execute(async () => await _api.PutMode(TheHandyV3.Messages.Info.HandyModes.Hamp));
        }

        private void btnSetHampVelocity_Click(object sender, RoutedEventArgs e)
        {
            Execute(async () => await _api.HampPutVelocity(sldVelocity.Value / 100.0));
        }

        private void btnStartHamp_Click(object sender, RoutedEventArgs e)
        {
            Execute(async () => await _api.HampStart());
        }

        private void btnStopHamp_Click(object sender, RoutedEventArgs e)
        {
            Execute(async () => await _api.HampStop());
        }

        private void btnGetHampVelocity_Click(object sender, RoutedEventArgs e)
        {
            Execute(async () => await _api.HampGetState(), (a) => sldVelocity.Value = a.Velocity * 100.0);
        }
    }
}
