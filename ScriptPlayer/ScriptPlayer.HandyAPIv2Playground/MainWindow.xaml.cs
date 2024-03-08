using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using ScriptPlayer.HandyAPIv2Playground.TheHandyV2;
using ScriptPlayer.Shared.TheHandyV2;

namespace ScriptPlayer.HandyAPIv2Playground
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HandyApiV2 _api;

        public MainWindow()
        {
            InitializeComponent();

            GridFunctions.IsEnabled = false;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            _api = new HandyApiV2(txtApiKey.Text);

            txtApiKey.IsEnabled = false;

            GridFunctions.IsEnabled = true;
        }

        private void btnSetSlideRange_Click(object sender, RoutedEventArgs e)
        {
            Execute(async ()=> await _api.PutSlide(new SlideSettingsMinMax
            {
                Min = sldRange.LowerValue,
                Max = sldRange.UpperValue,
            }));
        }

        private async void Execute<T>(Func<Task<Response<T>>> methodWithResponse, Action<T> onSuccess = null) where T : class
        {
            try
            {
                GridFunctions.IsEnabled = false;

                var response = await methodWithResponse();
                
                StringBuilder builder = new StringBuilder();

                if (response.Error?.Error != null)
                {
                    builder.AppendLine("Error!\r\n");
                    builder.AppendLine($"Name: {response.Error.Error.Name}");
                    builder.AppendLine($"Code: {response.Error.Error.Code}");
                    builder.AppendLine($"Connected: {response.Error.Error.Connected}");
                    builder.AppendLine($"Message: {response.Error.Error.Message}");
                }
                else if (response.RawData == null)
                {
                    builder.AppendLine("No Error, No Data ?!");
                }
                else
                {
                    builder.AppendLine("Response: \r\n");
                    builder.AppendLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));

                    onSuccess?.Invoke(response.Data);
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
            Execute(async () => await _api.PutMode(new ModeUpdate
            {
                Mode = Mode.Hamp
            }));
        }

        private void btnSetHampVelocity_Click(object sender, RoutedEventArgs e)
        {
            Execute(async () => await _api.HampPutVelocity(new HampVelocityPercent()
            {
                Velocity = sldVelocity.Value
            }));
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
            Execute(async () => await _api.HampGetVelocity(), (a) => sldVelocity.Value = a.Velocity);
        }
    }
}
