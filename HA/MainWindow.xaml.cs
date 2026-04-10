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
        // Link không còn cứng nữa, sẽ linh hoạt lấy từ ô Text
        private string haUrl = ""; 
        
        // Token thì anh hai vẫn dán vào đây nghen
        private readonly string haToken = "DÁN_LONG_LIVED_ACCESS_TOKEN_VÀO_ĐÂY"; 

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
        }

       // Sự kiện khi bấm nút Dán (Paste)
        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                // Mò tìm ô textbox bằng code thay vì gọi trực tiếp
                if (this.FindName("txtUrl") is TextBox txtBox)
                {
                    txtBox.Text = Clipboard.GetText();
                }
            }
        }

        // Sự kiện khi bấm Kết Nối
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("txtUrl") is TextBox txtBox)
            {
                string urlInput = txtBox.Text.Trim();
                
                if (string.IsNullOrEmpty(urlInput))
                {
                    MessageBox.Show("Anh hai chưa nhập link kìa!", "Tèo báo lỗi");
                    return;
                }

                // Tự động thêm http nếu anh hai copy thiếu
                if (!urlInput.StartsWith("http"))
                {
                    urlInput = "http://" + urlInput;
                    txtBox.Text = urlInput; // Cập nhật lại giao diện
                }

                // Cập nhật lại đường dẫn cho API
                haUrl = urlInput;

                // Chạy WebView
                await haWebView.EnsureCoreWebView2Async(null);
                haWebView.Source = new Uri(haUrl);
            }
        }
        private async void BtnToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(haUrl))
            {
                MessageBox.Show("Anh hai phải dán link và bấm 'Kết Nối' trước đã nghen!", "Tèo nhắc nhở");
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