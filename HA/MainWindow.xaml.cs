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

        // Lưu cấu hình HA
        private async void BtnSaveAndConnect_Click(object sender, RoutedEventArgs e)
        {
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

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);

            if (lblUrl != null) lblUrl.Text = haUrl;

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

        // [Suy luận] Hàm xử lý thêm thiết bị mới vào Tab Điều khiển
        private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
        {
            var txtName = this.FindName("txtNewDeviceName") as TextBox;
            var txtId = this.FindName("txtNewEntityId") as TextBox;
            var pnl = this.FindName("pnlDevices") as WrapPanel;

            if (txtName == null || txtId == null || pnl == null) return;

            string name = txtName.Text.Trim();
            string entityId = txtId.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(entityId))
            {
                MessageBox.Show("Anh hai nhập đủ Tên và Entity ID giùm Tèo nha!", "Tèo nhắc nè");
                return;
            }

            // [Suy luận] Tạo Button động và gán Style từ Resources của Window
            Button newBtn = new Button
            {
                Content = $"💡 {name}",
                Tag = entityId,
                Width = 280,
                Height = 50,
                Margin = new Thickness(10),
                Style = (Style)this.Resources[typeof(Button)]
            };

            // Gán sự kiện Click dùng chung logic cũ
            newBtn.Click += BtnToggleDevice_Click;

            pnl.Children.Add(newBtn);

            // Xóa trắng để anh hai nhập cái tiếp theo cho nhanh
            txtName.Clear();
            txtId.Clear();
            MessageBox.Show($"Đã thêm '{name}' thành công rồi đó anh hai!", "Tèo xong việc");
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