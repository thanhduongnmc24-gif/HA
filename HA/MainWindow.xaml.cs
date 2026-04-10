using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace HA
{
    public partial class MainWindow : Window
    {
        // Anh hai điền đúng link và Token của anh hai vào đây nhé
        private readonly string haUrl = "https://ten-mien-cua-anh-hai.com";
        private readonly string haToken = "DÁN_LONG_LIVED_ACCESS_TOKEN_VÀO_ĐÂY"; 

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebViewAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
        }

        private async void InitializeWebViewAsync()
        {
            await haWebView.EnsureCoreWebView2Async(null);
            haWebView.Source = new Uri(haUrl);
        }

        private async void BtnToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string entityId)
            {
                btn.IsEnabled = false; 
                await ToggleDeviceAsync(entityId);
                btn.IsEnabled = true;
            }
        }

        private async Task ToggleDeviceAsync(string entityId)
        {
            try
            {
                // Đã vá sẵn lỗi lấy nhầm mảng chuỗi ở đây rồi nghen anh hai
                string domain = entityId.Split('.')[0]; 
                string apiUrl = $"{haUrl}/api/services/{domain}/toggle";

                string jsonPayload = $"{{\"entity_id\": \"{entityId}\"}}";
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Lỗi gọi API. Mã lỗi: {response.StatusCode}", "Tèo báo lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mất kết nối hoặc có biến: {ex.Message}", "Tèo báo lỗi");
            }
        }
    }
}