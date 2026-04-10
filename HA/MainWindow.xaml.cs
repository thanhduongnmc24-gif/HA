using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HA
{
    // [Suy luận] Sửa lỗi CS8618 và CS8612 bằng cách khởi tạo giá trị mặc định và dùng dấu '?' cho event
    public class DeviceItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _entityId = string.Empty;
        private string _room = string.Empty;

        public string Name { get => _name; set { _name = value; OnPropertyChanged("Name"); } }
        public string EntityId { get => _entityId; set { _entityId = value; OnPropertyChanged("EntityId"); } }
        public string Room { get => _room; set { _room = value; OnPropertyChanged("Room"); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MainWindow : Window
    {
        private string haUrl = "";
        private string haToken = "";
        private static readonly HttpClient client = new HttpClient();
        
        public ObservableCollection<DeviceItem> Devices { get; set; } = new ObservableCollection<DeviceItem>();

        public MainWindow()
        {
            InitializeComponent();
            
            // [Suy luận] Dùng FindName để tìm icDeviceGroups và dgDevices tránh lỗi CS0103
            ICollectionView view = CollectionViewSource.GetDefaultView(Devices);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Room"));
            
            var icGroups = this.FindName("icDeviceGroups") as ItemsControl;
            var dg = this.FindName("dgDevices") as DataGrid;

            if (icGroups != null) icGroups.ItemsSource = view;
            if (dg != null) dg.ItemsSource = Devices;
        }

        private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
        {
            // [Suy luận] Tìm các TextBox bằng FindName
            var txtName = this.FindName("txtNewName") as TextBox;
            var txtId = this.FindName("txtNewId") as TextBox;
            var txtRoom = this.FindName("txtNewRoom") as TextBox;

            if (txtName == null || txtId == null || string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtId.Text)) return;

            Devices.Add(new DeviceItem { 
                Name = txtName.Text.Trim(), 
                EntityId = txtId.Text.Trim(), 
                Room = (txtRoom == null || string.IsNullOrEmpty(txtRoom.Text)) ? "Khác" : txtRoom.Text.Trim() 
            });

            txtName.Clear();
            txtId.Clear();
        }

        private void BtnDeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceItem item)
            {
                Devices.Remove(item);
            }
        }

        private async void BtnSaveAndConnect_Click(object sender, RoutedEventArgs e)
        {
            var txtUrl = this.FindName("txtUrlSetting") as TextBox;
            var txtToken = this.FindName("txtTokenSetting") as TextBox;
            var lblUrl = this.FindName("lblCurrentUrl") as TextBlock;

            if (txtUrl == null || txtToken == null) return;

            haUrl = txtUrl.Text.Trim();
            haToken = txtToken.Text.Trim();
            
            if (string.IsNullOrEmpty(haUrl)) return;
            if (!haUrl.StartsWith("http")) haUrl = "http://" + haUrl;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
            if (lblUrl != null) lblUrl.Text = haUrl;

            try {
                await haWebView.EnsureCoreWebView2Async(null);
                haWebView.Source = new Uri(haUrl);
            } catch (Exception ex) { MessageBox.Show("Lỗi kết nối: " + ex.Message); }
        }

        private async void BtnToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string entityId)
            {
                if (string.IsNullOrEmpty(haUrl)) { MessageBox.Show("Anh hai chưa cấu hình URL kìa!"); return; }
                btn.IsEnabled = false;
                await ToggleDeviceAsync(entityId);
                btn.IsEnabled = true;
            }
        }

        private async Task ToggleDeviceAsync(string entityId)
        {
            try {
                string domain = entityId.Split('.')[0];
                string apiUrl = $"{haUrl.TrimEnd('/')}/api/services/{domain}/toggle";
                var content = new StringContent($"{{\"entity_id\": \"{entityId}\"}}", Encoding.UTF8, "application/json");
                await client.PostAsync(apiUrl, content);
            } catch (Exception ex) { MessageBox.Show($"Có biến rồi anh hai: {ex.Message}"); }
        }
    }
}