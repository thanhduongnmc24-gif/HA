using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HA
{
    public partial class MainWindow : Window
    {
        // Danh sách thiết bị hiển thị trên Canvas
        public ObservableCollection<DeviceModel> Devices { get; set; } = new ObservableCollection<DeviceModel>();
        
        private bool _isEditMode = false;
        private DeviceModel _draggingDevice = null;
        private Point _lastMousePosition;
        private string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dashboard_config.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadDashboardData();
            icDashboard.ItemsSource = Devices;
        }

        #region LOGIC DASHBOARD & KÉO THẢ

        private void BtnToggleEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = btnToggleEdit.IsChecked ?? false;
            pnlEditTools.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
            
            // Cập nhật con trỏ chuột cho các thiết bị
            foreach (var d in Devices) d.UpdateCursor(_isEditMode);

            if (!_isEditMode)
            {
                // Bỏ chọn tất cả khi thoát chế độ sửa
                foreach (var d in Devices) d.IsSelected = false;
            }
        }

        private void Device_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isEditMode)
            {
                // Chế độ điều khiển: Bật/Tắt thiết bị (giả lập logic cũ)
                var device = (sender as ContentControl)?.DataContext as DeviceModel;
                if (device != null)
                {
                    // Gọi hàm xử lý API HA của anh hai ở đây
                    MessageBox.Show($"Đang gửi lệnh tới: {device.EntityId}");
                }
                return;
            }

            // Chế độ sửa: Xử lý kéo thả và chọn
            var element = sender as ContentControl;
            var deviceData = element?.DataContext as DeviceModel;

            if (deviceData != null)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    deviceData.IsSelected = !deviceData.IsSelected;
                }
                else
                {
                    if (!deviceData.IsSelected)
                    {
                        foreach (var d in Devices) d.IsSelected = false;
                        deviceData.IsSelected = true;
                    }
                }

                _draggingDevice = deviceData;
                _lastMousePosition = e.GetPosition(this);
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Device_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isEditMode && _draggingDevice != null && (sender as ContentControl).IsMouseCaptured)
            {
                Point currentPos = e.GetPosition(this);
                double diffX = currentPos.X - _lastMousePosition.X;
                double diffY = currentPos.Y - _lastMousePosition.Y;

                // Nếu đang chọn nhiều, kéo cả đám đi cùng
                var selectedItems = Devices.Where(d => d.IsSelected).ToList();
                foreach (var item in selectedItems)
                {
                    item.X += diffX;
                    item.Y += diffY;
                }

                _lastMousePosition = currentPos;
            }
        }

        private void Device_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isEditMode)
            {
                (sender as ContentControl)?.ReleaseMouseCapture();
                _draggingDevice = null;
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Click ra vùng trống Canvas để bỏ chọn
            if (_isEditMode)
            {
                foreach (var d in Devices) d.IsSelected = false;
            }
        }

        #endregion

        #region CĂN CHỈNH & LƯU TRỮ

        private void BtnAlignHorizontal_Click(object sender, RoutedEventArgs e)
        {
            var selected = Devices.Where(d => d.IsSelected).ToList();
            if (selected.Count < 2) return;

            double targetY = selected[0].Y;
            foreach (var item in selected) item.Y = targetY;
        }

        private void BtnAlignVertical_Click(object sender, RoutedEventArgs e)
        {
            var selected = Devices.Where(d => d.IsSelected).ToList();
            if (selected.Count < 2) return;

            double targetX = selected[0].X;
            foreach (var item in selected) item.X = targetX;
        }

        private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewDeviceName.Text) || string.IsNullOrWhiteSpace(txtNewDeviceEntity.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin thiết bị!");
                return;
            }

            Devices.Add(new DeviceModel 
            { 
                Name = txtNewDeviceName.Text, 
                EntityId = txtNewDeviceEntity.Text,
                X = 50, Y = 50 
            });

            txtNewDeviceName.Clear();
            txtNewDeviceEntity.Clear();
            SaveDashboardData();
            MessageBox.Show("Đã thêm thiết bị! Qua tab Dashboard để sắp xếp.");
        }

        private void BtnSaveLayout_Click(object sender, RoutedEventArgs e)
        {
            SaveDashboardData();
            MessageBox.Show("Đã lưu vị trí các nút bấm!");
        }

        private void SaveDashboardData()
        {
            try
            {
                string json = JsonSerializer.Serialize(Devices);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void LoadDashboardData()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    var list = JsonSerializer.Deserialize<List<DeviceModel>>(json);
                    if (list != null)
                    {
                        Devices.Clear();
                        foreach (var item in list) Devices.Add(item);
                    }
                }
                catch { }
            }
        }

        // Logic cũ của anh hai
        private void BtnSaveAndConnect_Click(object sender, RoutedEventArgs e)
        {
            lblCurrentUrl.Text = txtUrlSetting.Text;
            if (!string.IsNullOrEmpty(txtUrlSetting.Text))
                haWebView.Source = new Uri(txtUrlSetting.Text);
        }

        private void BtnToggleDevice_Click(object sender, RoutedEventArgs e) { /* Giữ lại logic API cũ nếu cần */ }

        #endregion
    }

    // Model dữ liệu thiết bị
    public class DeviceModel : INotifyPropertyChanged
    {
        private string _name;
        private string _entityId;
        private double _x;
        private double _y;
        private bool _isSelected;
        private Cursor _cursorType = Cursors.Hand;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string EntityId { get => _entityId; set { _entityId = value; OnPropertyChanged(); } }
        public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
        public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
        
        public Cursor CursorType { get => _cursorType; set { _cursorType = value; OnPropertyChanged(); } }
        public SolidColorBrush StatusColor => new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Màu mặc định

        public void UpdateCursor(bool editMode)
        {
            CursorType = editMode ? Cursors.SizeAll : Cursors.Hand;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}