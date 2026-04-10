using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HA
{
    // Cấu trúc để lưu vào file JSON
    public class AppData
    {
        public string Url { get; set; } = "";
        public string Token { get; set; } = "";
        public List<DeviceItem> SavedDevices { get; set; } = new List<DeviceItem>();
    }

    public class DeviceItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _entityId = string.Empty;
        private string _room = string.Empty;
        private string _colorOn = "Green";
        private string _colorOff = "Gray";
        private bool _isOn = false;
        private int _roomOrder = 0;

        public string Name { get => _name; set { _name = value; OnPropertyChanged("Name"); } }
        public string EntityId { get => _entityId; set { _entityId = value; OnPropertyChanged("EntityId"); } }
        
        public string Room { get => _room; set { _room = value; OnPropertyChanged("Room"); } }
        public int RoomOrder { get => _roomOrder; set { _roomOrder = value; OnPropertyChanged("RoomOrder"); } }

        public string ColorOn { get => _colorOn; set { _colorOn = value; OnPropertyChanged("ColorOn"); OnPropertyChanged("CurrentColor"); } }
        public string ColorOff { get => _colorOff; set { _colorOff = value; OnPropertyChanged("ColorOff"); OnPropertyChanged("CurrentColor"); } }
        public bool IsOn { get => _isOn; set { _isOn = value; OnPropertyChanged("IsOn"); OnPropertyChanged("CurrentColor"); } }

        // System.Text.Json sẽ tự động bỏ qua thuộc tính chỉ có Get
        public string CurrentColor => IsOn ? ColorOn : ColorOff;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class DeviceOrderComparer : System.Collections.IComparer
    {
        public int Compare(object? x, object? y)
        {
            var d1 = x as DeviceItem;
            var d2 = y as DeviceItem;
            if (d1 == null || d2 == null) return 0;
            
            int roomCompare = d1.RoomOrder.CompareTo(d2.RoomOrder);
            if (roomCompare != 0) return roomCompare;
            return string.Compare(d1.Name, d2.Name);
        }
    }

    public partial class MainWindow : Window
    {
        private string haUrl = "";
        private string haToken = "";
        private static readonly HttpClient client = new HttpClient();
        private readonly string dataFilePath = "ha_database.json"; // [Suy luận] Tên file lưu dữ liệu
        
        public ObservableCollection<DeviceItem> Devices { get; set; } = new ObservableCollection<DeviceItem>();
        public ObservableCollection<string> RoomsList { get; set; } = new ObservableCollection<string>();
        
        public string[] AvailableColors { get; } = new string[] 
        { 
            "Gray", "Green", "Blue", "Red", "Orange", 
            "Purple", "Teal", "DodgerBlue", "HotPink", "Black" 
        };

        public MainWindow()
        {
            InitializeComponent();
            
            var icGroups = this.FindName("icDeviceGroups") as ItemsControl;
            var dg = this.FindName("dgDevices") as DataGrid;

            ICollectionView view = CollectionViewSource.GetDefaultView(Devices);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Room"));
            
            if (icGroups != null) icGroups.ItemsSource = view;
            if (dg != null) dg.ItemsSource = Devices;

            // [TRỌNG TÂM] Load dữ liệu ngay khi vừa mở app
            LoadData();
        }

        // --- CÁC HÀM LƯU / ĐỌC DỮ LIỆU ---
        private void SaveData()
        {
            try
            {
                var data = new AppData
                {
                    Url = haUrl,
                    Token = haToken,
                    SavedDevices = Devices.ToList()
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dataFilePath, json);
            }
            catch { /* Lỗi ghi file thì lờ đi tạm thời để không crash app */ }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath);
                    var data = JsonSerializer.Deserialize<AppData>(json);
                    
                    if (data != null)
                    {
                        haUrl = data.Url;
                        haToken = data.Token;

                        // Đổ dữ liệu ra các ô text bên Tab Cài đặt
                        var txtUrl = this.FindName("txtUrlSetting") as TextBox;
                        var txtToken = this.FindName("txtTokenSetting") as TextBox;
                        var lblUrl = this.FindName("lblCurrentUrl") as TextBlock;

                        if (txtUrl != null) txtUrl.Text = haUrl;
                        if (txtToken != null) txtToken.Text = haToken;
                        if (lblUrl != null && !string.IsNullOrEmpty(haUrl)) lblUrl.Text = haUrl;

                        // Gắn lại token nếu có sẵn
                        if (!string.IsNullOrEmpty(haToken))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
                        }

                        // Đổ danh sách thiết bị ra bảng
                        foreach (var item in data.SavedDevices)
                        {
                            item.PropertyChanged += DeviceItem_PropertyChanged;
                            Devices.Add(item);
                        }
                        UpdateDevicesRoomOrder();
                    }
                }
            }
            catch { /* Nếu file lỗi format thì mở trắng như bình thường */ }
        }

        private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
        {
            var txtName = this.FindName("txtNewName") as TextBox;
            var txtId = this.FindName("txtNewId") as TextBox;
            var txtRoom = this.FindName("txtNewRoom") as TextBox;
            var cbOn = this.FindName("cbColorOn") as ComboBox;
            var cbOff = this.FindName("cbColorOff") as ComboBox;

            if (txtName == null || txtId == null || string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtId.Text)) return;

            var newDevice = new DeviceItem { 
                Name = txtName.Text.Trim(), 
                EntityId = txtId.Text.Trim(), 
                Room = (txtRoom == null || string.IsNullOrEmpty(txtRoom.Text)) ? "Khác" : txtRoom.Text.Trim(),
                ColorOn = cbOn?.SelectedItem?.ToString() ?? "Green",
                ColorOff = cbOff?.SelectedItem?.ToString() ?? "Gray"
            };

            newDevice.PropertyChanged += DeviceItem_PropertyChanged;
            Devices.Add(newDevice);
            
            UpdateDevicesRoomOrder();
            SaveData(); // [Suy luận] Ghi lại ngay khi có thiết bị mới

            txtName.Clear();
            txtId.Clear();
        }

        private void DeviceItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Room") {
                UpdateDevicesRoomOrder();
            }
            // [Suy luận] Lưu lại dữ liệu khi anh hai đổi tên/phòng/màu trong bảng
            if (e.PropertyName != "IsOn" && e.PropertyName != "CurrentColor") {
                SaveData(); 
            }
        }

        private void BtnMoveGroupUp_Click(object sender, RoutedEventArgs e)
        {
            var lst = this.FindName("lstGroups") as ListBox;
            if (lst == null || lst.SelectedIndex <= 0) return;

            int index = lst.SelectedIndex;
            string item = RoomsList[index];
            RoomsList.RemoveAt(index);
            RoomsList.Insert(index - 1, item);
            lst.SelectedIndex = index - 1;
            
            UpdateDevicesRoomOrder();
            SaveData();
        }

        private void BtnMoveGroupDown_Click(object sender, RoutedEventArgs e)
        {
            var lst = this.FindName("lstGroups") as ListBox;
            if (lst == null || lst.SelectedIndex < 0 || lst.SelectedIndex >= RoomsList.Count - 1) return;

            int index = lst.SelectedIndex;
            string item = RoomsList[index];
            RoomsList.RemoveAt(index);
            RoomsList.Insert(index + 1, item);
            lst.SelectedIndex = index + 1;

            UpdateDevicesRoomOrder();
            SaveData();
        }

        private void UpdateDevicesRoomOrder()
        {
            foreach (var d in Devices) {
                if (!RoomsList.Contains(d.Room)) {
                    RoomsList.Add(d.Room);
                }
                d.RoomOrder = RoomsList.IndexOf(d.Room);
            }

            if (CollectionViewSource.GetDefaultView(Devices) is ListCollectionView view) {
                view.CustomSort = new DeviceOrderComparer();
                view.Refresh();
            }
        }

        private void BtnDeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceItem item) {
                item.PropertyChanged -= DeviceItem_PropertyChanged;
                Devices.Remove(item);
                SaveData(); // Xóa xong phải ghi lại
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

            SaveData(); // Bấm lưu kết nối là ghi lại liền

            try {
                await haWebView.EnsureCoreWebView2Async(null);
                haWebView.Source = new Uri(haUrl);
            } catch (Exception ex) { MessageBox.Show("Lỗi kết nối: " + ex.Message); }
        }

        private async void BtnToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceItem item)
            {
                if (string.IsNullOrEmpty(haUrl)) { MessageBox.Show("Anh hai chưa cấu hình URL kìa!"); return; }
                
                btn.IsEnabled = false; 
                bool success = await ToggleDeviceAsync(item.EntityId);
                
                if (success) {
                    item.IsOn = !item.IsOn;
                    // Chỗ này không cần SaveData() để tránh ghi file quá nhiều lần lúc bật tắt đèn
                }
                btn.IsEnabled = true;
            }
        }

        private async Task<bool> ToggleDeviceAsync(string entityId)
        {
            try {
                string domain = entityId.Split('.')[0];
                string apiUrl = $"{haUrl.TrimEnd('/')}/api/services/{domain}/toggle";
                var content = new StringContent($"{{\"entity_id\": \"{entityId}\"}}", Encoding.UTF8, "application/json");
                
                var res = await client.PostAsync(apiUrl, content);
                return res.IsSuccessStatusCode;
            } catch (Exception ex) { 
                MessageBox.Show($"Có biến rồi anh hai: {ex.Message}"); 
                return false; 
            }
        }
    }
}