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
        private string haUrl = "";
        private string haToken = "";
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        // Nút Lưu và Kết Nối ở Tab Cài đặt
        private async void BtnSaveAndConnect_Click(object sender, RoutedEventArgs e)
        {
            // [Suy luận] Dùng FindName để né lỗi gạch đỏ trên Linux Codespaces cho anh hai
            var txtUrl = this.FindName("txtUrlSetting") as TextBox;
            var txtToken = this.FindName("txtTokenSetting") as TextBox;
            var lblUrl = this.FindName("lblCurrentUrl") as TextBlock;

            if (txtUrl == null || txtToken == null) return;

            haUrl = txtUrl.Text.Trim();
            haToken = txtToken.Text.Trim();

            if (string.IsNullOrEmpty(haUrl) || string.IsNullOrEmpty(haToken))
            {
                MessageBox.Show("Anh hai điền thiếu thông tin kìa!", "Tèo báo lỗi");
                return;
            }

            if (!haUrl.StartsWith("http")) haUrl = "http://" + haUrl;

            // Cấu hình Header cho HTTP Client
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);

            // Cập nhật trạng thái hiển thị
            if (lblUrl != null) lblUrl.Text = haUrl;

            // Tải trang Web
            try
            {
                await haWebView.EnsureCoreWebView2Async(null);
                haWebView.Source = new Uri(haUrl);
                MessageBox.Show("Đã lưu cấu hình và đang kết nối...", "Tèo thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private async void BtnToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(haUrl) || string.IsNullOrEmpty(haToken))
            {
                MessageBox.Show("Anh hai qua tab Cài đặt cấu hình trước đã nhé!", "Tèo nhắc nhở");
                return;
            }

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
                string domain = entityId.Split('.')[0];
                string apiUrl = $"{haUrl.TrimEnd('/')}/api/services/{domain}/toggle";

                string jsonPayload = $"{{\"entity_id\": \"{entityId}\"}}";
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Lỗi API: {response.StatusCode}", "Tèo báo lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có biến rồi anh hai: {ex.Message}");
            }
        }
    }
}