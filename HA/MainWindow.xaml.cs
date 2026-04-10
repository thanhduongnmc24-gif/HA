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
    // Cập nhật DeviceItem để hỗ trợ đổi màu và Trạng thái (Bật/Tắt)
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
        
        // Khi đổi phòng trong DataGrid, tự động cập nhật lại danh sách sắp xếp
        public string Room { get => _room; set { _room = value; OnPropertyChanged("Room"); } }
        public int RoomOrder { get => _roomOrder; set { _roomOrder = value; OnPropertyChanged("RoomOrder"); } }

        public string ColorOn { get => _colorOn; set { _colorOn = value; OnPropertyChanged("ColorOn"); OnPropertyChanged("CurrentColor"); } }
        public string ColorOff { get => _colorOff; set { _colorOff = value; OnPropertyChanged("ColorOff"); OnPropertyChanged("CurrentColor"); } }
        public bool IsOn { get => _isOn; set { _isOn = value; OnPropertyChanged("IsOn"); OnPropertyChanged("CurrentColor"); } }

        // [Suy luận] Thuộc tính này quyết định nút sẽ hiện màu gì
        public string CurrentColor => IsOn ? ColorOn : ColorOff;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // [Suy luận] Bộ so sánh giúp sắp xếp các nhóm theo ý đồ của anh hai thay vì alphabet
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
        
        public ObservableCollection<DeviceItem> Devices { get; set; } = new ObservableCollection<DeviceItem>();
        public ObservableCollection<string> RoomsList { get; set; } = new ObservableCollection<string>();
        
        // Cung cấp danh sách màu sắc đẹp mắt cho anh hai chọn
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

            // Lắng nghe sự kiện nếu anh hai sửa tên phòng trong DataGrid
            newDevice.PropertyChanged += DeviceItem_PropertyChanged;
            Devices.Add(newDevice);
            
            UpdateDevicesRoomOrder();

            txtName.Clear();
            txtId.Clear();
        }

        private void DeviceItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Cập nhật lại danh sách nếu anh hai sửa tên phòng trong bảng
            if (e.PropertyName == "Room") {
                UpdateDevicesRoomOrder();
            }
        }

        // --- CÁC HÀM XỬ LÝ SẮP XẾP NHÓM LÊN/XUỐNG ---
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
        }

        private void UpdateDevicesRoomOrder()
        {
            // Bổ sung các phòng mới vào RoomsList nếu chưa có
            foreach (var d in Devices) {
                if (!RoomsList.Contains(d.Room)) {
                    RoomsList.Add(d.Room);
                }
                d.RoomOrder = RoomsList.IndexOf(d.Room);
            }

            // Áp dụng thuật toán tự động sắp xếp lên giao diện (Tab 2)
            if (CollectionViewSource.GetDefaultView(Devices) is ListCollectionView view) {
                view.CustomSort = new DeviceOrderComparer();
                view.Refresh();
            }
        }

        // --- CÁC HÀM GIAO TIẾP VỚI HOME ASSISTANT ---
        private void BtnDeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceItem item) {
                item.PropertyChanged -= DeviceItem_PropertyChanged;
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
            if (sender is Button btn && btn.DataContext is DeviceItem item)
            {
                if (string.IsNullOrEmpty(haUrl)) { MessageBox.Show("Anh hai chưa cấu hình URL kìa!"); return; }
                
                btn.IsEnabled = false; // Khoá tạm thời tránh click nhầm 2 lần
                bool success = await ToggleDeviceAsync(item.EntityId);
                
                // Nếu gọi API thành công thì Tèo mới đổi trạng thái & màu sắc
                if (success) {
                    item.IsOn = !item.IsOn;
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