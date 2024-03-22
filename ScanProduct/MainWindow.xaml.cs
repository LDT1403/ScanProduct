using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ScanProduct.TextToSpeedGoogle;
using ScanProduct.Services;
using System.Windows.Input;
using System.Threading.Tasks;
using ScanProduct.Interfaces;
using ScanProduct.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace ScanProduct
{
    public partial class MainWindow : Window
    {
        private readonly ITextToSpeed _textToSpeed;
        private readonly ISheetService _sheetService;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isScanning = false;
        int total = 0;
        private ObservableCollection<Product> productList = new ObservableCollection<Product>();
        public MainWindow()
        {
            _textToSpeed = new TextToSpeedService();
            _sheetService = new SheetService();
            InitializeComponent();
            InitializeCamera();
            listView.ItemsSource = productList;
        }
        //ok
        // Khởi động camera
        private void InitializeCamera()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += VideoSource_NewFrame;
                videoSource.Start();
            }
            else
            {
                MessageBox.Show("No video devices found.");
            }
        }
        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        // Khởi động camera
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!isScanning)
            {
                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    Dispatcher.Invoke(async () =>
                    {
                        webcamImage.Source = ConvertBitmapToBitmapSource(bitmap);
                        BarcodeReader reader = new BarcodeReader();
                        reader.Options = new DecodingOptions
                        {
                            TryHarder = true,
                            PossibleFormats = new BarcodeFormat[] { BarcodeFormat.CODE_39, BarcodeFormat.EAN_13 }
                        };
                        Result result = reader.Decode(bitmap);

                        if (result != null)
                        {
                            string barcodeText = result.Text;

                            if (barcodeText.Length == 8)
                            {
                                isScanning = true;
                                inputTextBox.Text = barcodeText;
                                await _textToSpeed.PlayMp3("../../../SoundScan.mp3");
                                var product = await getProduct(barcodeText);
                                
                                UpdateListViewWithProduct(product);

                                // productList.Add(product);
                                isScanning = false;

                            }
                            else
                            {
                                isScanning = false;
                            }
                        }
                    });
                }
            }
            else
            {
                // Nếu quá trình quét đang diễn ra, không thực hiện gì cả
            }
        }
        private async void UpdateListViewWithProduct(Product product)
        {

            bool isProductExist = false;
            foreach (Product item in productList)
            {



                if (item.ProductId == product.ProductId)
                {

                    int quan = int.Parse(item.Quantity) + 1;
                    item.Quantity = quan.ToString();
                    isProductExist = true;
                    break;
                }
            }
            TotalUpdate();
            listView.Items.Refresh();
            if (!isProductExist)
            {
                product.Quantity = "1";
                productList.Add(product);
                TotalUpdate();


            }
        }
        private void TotalUpdate()
        {
            total = 0;
            foreach (Product item in productList)
            {
                total += int.Parse(item.Price) * int.Parse(item.Quantity);
            }
            TotalValue.Text = total.ToString();
        }
        public async Task<Product> getProduct(string productId)
        {

            var listProduct = loadData("Product");
            var product = new Product();

            foreach (var Column in listProduct)
            {

                if (Column[0].ToString().Equals(productId))
                {
                    product.ProductId = (string)Column[0];
                    product.ProductName = (string)Column[1];
                    product.Price = (string)Column[2];
                }

            }
            if (listProduct.Count == 0) {
                return null;
            }



            return product;
        }

        public IList<IList<object>> loadData(string tableId)
        {

            var service = _sheetService.API();
            var values = service.Spreadsheets.Values.Get("1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", $"{tableId}!A:F").Execute().Values;
            return values;

        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.Stop();
            }
            base.OnClosing(e);
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
        string userNamemm = "";
        private void Btn_Done_Click(object sender, RoutedEventArgs e)
        {
            string textToSpeak = TotalValue.Text;
            if(labelUserName.Content == "")
            {
                
                _textToSpeed.SpeedGoogle("Tổng Hóa đơn của bạn là" + textToSpeak + "Đồng");
            }
            else
            {
                _textToSpeed.SpeedGoogle("Tổng Hóa đơn của"+ userNamemm + " là" + textToSpeak + "Đồng");
            }
            
        }

        private bool ProcessPayment()
        {
            bool paymentSuccess = false;


            return paymentSuccess;
        }
        private bool IsListViewEmpty()
        {
            return productList.Count == 0;
        }

        private async void btn_PayMono_Click(object sender, RoutedEventArgs e)
        {
            //if (IsListViewEmpty())
            //{
            //    MessageBox.Show("Vui Lòng Thêm Sản Phẩm", "Giỏ Hàng", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
            //else
            //{
            //    AddOrder();
            //    string Phone = "0349470340";
            //    string Name = "Hoàng Chí Dương";
            //    string Email = "";
            //    string PayNumber = TotalValue.Text.Trim();
            //    string Description = "";
            //    MomoQRCodeGenerator momoGenerator = new MomoQRCodeGenerator();
            //    string merchantCode = $"2|99|{Phone}|{Name}|{Email}|0|0|{PayNumber}|{Description}";
            //    Bitmap momoQRCode = momoGenerator.GenerateMomoQRCode(merchantCode);
            //    if (!string.IsNullOrWhiteSpace(TotalValue.Text))
            //    {
            //        ScanMoMoQR scanQR = new ScanMoMoQR();
            //        scanQR.UpdateQRCode(momoQRCode);
            //        double windowWidth = 500;
            //        double windowHeight = 600; // Tăng chiều cao để chứa nút

            //        // Tạo Grid để chứa cảnh báo và StackPanel chứa nút
            //        Grid grid = new Grid();
            //        grid.Width = windowWidth;
            //        grid.Height = windowHeight;
            //        Window qrCodeWindow = new Window
            //        {
            //            Width = windowWidth,
            //            Height = windowHeight,
            //            WindowStyle = WindowStyle.None,
            //            ResizeMode = ResizeMode.NoResize,
            //            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            //            Title = "ScanQR",
            //            Content = grid
            //        };

            //        // Thêm ScanMoMoQR vào Grid
            //        grid.Children.Add(scanQR);

            //        // Tạo Grid để chứa nút
            //        Grid buttonGrid = new Grid();
            //        buttonGrid.HorizontalAlignment = HorizontalAlignment.Center;
            //        buttonGrid.VerticalAlignment = VerticalAlignment.Bottom;
            //        buttonGrid.Height = 50; // Đặt chiều cao của lưới nút

            //        ColumnDefinition column1 = new ColumnDefinition();
            //        ColumnDefinition column2 = new ColumnDefinition();
            //        column1.Width = new GridLength(1, GridUnitType.Star);
            //        column2.Width = new GridLength(1, GridUnitType.Star);
            //        buttonGrid.ColumnDefinitions.Add(column1);
            //        buttonGrid.ColumnDefinitions.Add(column2);


            //        Button failButton = new Button();
            //        failButton.Content = "Hủy";
            //        failButton.Width = 100;
            //        failButton.Margin = new Thickness(10, 0, 10, 0);
            //        failButton.Click += (sender, e) =>
            //        {
            //            AddPayment("fail", "MOMO");
            //            qrCodeWindow.Close();
            //            Reset();
            //        };
            //        Button successButton = new Button();
            //        successButton.Content = "Thành công";
            //        successButton.Width = 100;
            //        successButton.Margin = new Thickness(10, 0, 10, 0);
            //        successButton.Click += (sender, e) =>
            //        {
            //            AddPayment("success", "MOMO");
            //            qrCodeWindow.Close();
            //            Reset();
            //        };
            //        Grid.SetColumn(successButton, 1);
            //        Grid.SetColumn(failButton, 0);

            //        buttonGrid.Children.Add(failButton);
            //        buttonGrid.Children.Add(successButton);
            //        grid.Children.Add(buttonGrid);

            //        qrCodeWindow.Show();
            //    }
            //    else
            //    {
            //        MessageBox.Show("Sai Tổng Tiền", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }

            //}



        }
        public async Task<List<string>> GetAllPhoneNumbers()
        {
            List<string> phoneNumbers = new List<string>();

            var listProduct = loadData("User");

            foreach (var Column in listProduct)
            {
                // Thêm số điện thoại từ cột thích hợp vào danh sách
                string phoneNumber = (string)Column[0];
                phoneNumbers.Add(phoneNumber);
            }

            return phoneNumbers;
        }

        string porit = "";
       
        private async void txt_Phone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string phoneNumber = txt_Phone.Text.Trim();
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    // Lấy danh sách tất cả các số điện thoại từ Google Sheet
                    List<string> allPhoneNumbers = await GetAllPhoneNumbers();

                    if (allPhoneNumbers.Contains(phoneNumber))
                    {
                        // Nếu số điện thoại đã tồn tại, hiển thị tên người dùng tương ứng
                        string userName = await GetUserNameByPhoneNumber(phoneNumber);
                        string userPoint = await GetPointByPhoneNumber(phoneNumber);
                        porit = userPoint;
                        userNamemm = userName;
                        labelUserName.Content = userName + "      |      Điểm: " + userPoint;
                    }
                    else
                    {
                        // Nếu số điện thoại không tồn tại, hiển thị thông báo
                        MessageBox.Show("Số điện thoại chưa được đăng ký", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        labelUserName.Content = ""; // Xóa nội dung của label nếu không có số điện thoại tương ứng
                    }
                }
                else
                {
                    // Hiển thị thông báo nếu số điện thoại trống
                    MessageBox.Show("Vui lòng nhập số điện thoại", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    labelUserName.Content = ""; // Xóa nội dung của label nếu số điện thoại trống
                }
            }
        }

        private async Task<string> GetUserNameByPhoneNumber(string phoneNumber)
        {
            var listUser = loadData("User");

            foreach (var column in listUser)
            {
                // Tìm và trả về tên người dùng dựa trên số điện thoại
                if (column[0].ToString().Equals(phoneNumber))
                {
                    return column[1].ToString();
                }
            }
            return "Không tìm thấy";
        }
        private async Task<string> GetPointByPhoneNumber(string phoneNumber)
        {
            var listUser = loadData("User");

            foreach (var column in listUser)
            {
                // Tìm và trả về tên người dùng dựa trên số điện thoại
                if (column[0].ToString().Equals(phoneNumber))
                {
                    return column[2].ToString();
                }
            }
            return "Không tìm thấy";
        }
        private async void txt_PhoneRegist(object sender, KeyEventArgs e)
        {

        }

        private async void btn_Regist(object sender, RoutedEventArgs e)
        {
            var phoneNumber = txt_RegistPhone.Text;
            var userName = txt_Name.Text;
            try
            {
                var listuser = loadData("User");

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    MessageBox.Show("Vui lòng nhập số điện thoại", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(userName))
                {
                    MessageBox.Show("Vui lòng nhập tên người dùng", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Kiểm tra số điện thoại đã tồn tại hay chưa
                bool phoneNumberExists = await CheckPhoneNumberExists(phoneNumber);

                if (phoneNumberExists)
                {
                    MessageBox.Show("Số điện thoại đã tồn tại", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    var service = _sheetService.API();
                    SpreadsheetsResource.ValuesResource.GetRequest getRequest = service.Spreadsheets.Values.Get("1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", "User!A:C");
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender2, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) { return true; };
                    ValueRange getRespone = getRequest.Execute();
                    IList<IList<Object>> valuess = getRespone.Values;
                    var range = $"{"User"}!A" + (valuess.Count + 1) + ":C" + (valuess.Count + 1);
                    var valueRange = new ValueRange();

                    valueRange.Values = new List<IList<object>> { new List<object>() { phoneNumber, userName , "0"} };
                    var updateRequest = service.Spreadsheets.Values.Update(valueRange, "1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var AddResponse = updateRequest.Execute();
                    txt_RegistPhone.Text = "";
                    txt_Name.Text = "";

                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        private async Task<bool> CheckPhoneNumberExists(string phoneNumber)
        {
            // Lấy danh sách tất cả các số điện thoại từ Google Sheet
            List<string> allPhoneNumbers = await GetAllPhoneNumbers();

            // Kiểm tra số điện thoại có tồn tại trong danh sách hay không
            return allPhoneNumbers.Contains(phoneNumber);
        }

        private void AddOrder()
        {
            try
            {
                var service = _sheetService.API();
                SpreadsheetsResource.ValuesResource.GetRequest getRequest = service.Spreadsheets.Values.Get("1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", "Orders!A:C");
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender2, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) { return true; };
                ValueRange getRespone = getRequest.Execute();
                IList<IList<Object>> valuess = getRespone.Values;
                var range = $"{"Orders"}!B" + (valuess.Count + 1) + ":C" + (valuess.Count + 1);
                var valueRange = new ValueRange();
                var date = DateTime.Now;
                valueRange.Values = new List<IList<object>> { new List<object>() { date, TotalValue.Text } };
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, "1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var AddResponse = updateRequest.Execute();
                AddOrderDetail();


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        private void AddOrderDetail()
        {
            try
            {
                var listOrder = loadData("Orders").LastOrDefault();
                var orderid = listOrder[0];
                foreach (var item in productList)
                {
                    var service = _sheetService.API();
                    SpreadsheetsResource.ValuesResource.GetRequest getRequest = service.Spreadsheets.Values.Get("1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", "OrderDetail!A:E");
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender2, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) { return true; };
                    ValueRange getRespone = getRequest.Execute();
                    IList<IList<Object>> valuess = getRespone.Values;
                    var range = $"{"OrderDetail"}!B" + (valuess.Count + 1) + ":E" + (valuess.Count + 1);
                    var valueRange = new ValueRange();
                    var date = DateTime.Now;
                    valueRange.Values = new List<IList<object>> { new List<object>() { orderid, item.Price, item.ProductId, item.Quantity } };
                    var updateRequest = service.Spreadsheets.Values.Update(valueRange, "1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var AddResponse = updateRequest.Execute();
                    //ok
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        int priceFinal = 0;
        private void AddPayment(string status, string method)
        {
            try
            {
                var listOrder = loadData("Orders").LastOrDefault();
                var orderid = listOrder[0];
               
                if (txt_Point.Text != "")
                {
                    priceFinal = int.Parse(TotalValue.Text) - int.Parse(txt_Point.Text);
                }
                else
                {
                    priceFinal = int.Parse(TotalValue.Text);
                }
                var service = _sheetService.API();
                SpreadsheetsResource.ValuesResource.GetRequest getRequest = service.Spreadsheets.Values.Get("1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", "Payment!A:F");
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender2, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) { return true; };
                ValueRange getRespone = getRequest.Execute();
                IList<IList<Object>> valuess = getRespone.Values;
                var range = $"{"Payment"}!B" + (valuess.Count + 1) + ":F" + (valuess.Count + 1);
                var valueRange = new ValueRange();
                var date = DateTime.Now;
                valueRange.Values = new List<IList<object>> { new List<object>() { date, orderid, status, priceFinal.ToString(), method } };
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, "1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var AddResponse = updateRequest.Execute();
                if (status.Equals("success"))
                {
                    if (txt_Phone.Text !=null)
                    {
                        var listUser = loadData("User");
                        var user = new Users();
                        int i = 0;
                        
                        int totalPoint = 0;
                        foreach (var list in listUser)
                        {
                            i++;
                            if (list[0].Equals(txt_Phone.Text))
                            {
                                var numberRow = i;
                                if(txt_Point.Text != "")
                                {
                                    totalPoint = int.Parse(porit) - int.Parse(txt_Point.Text) +( priceFinal * 5 / 100);
                                }
                                else
                                {
                                    totalPoint = int.Parse(porit) + (int.Parse(TotalValue.Text) * 5 / 100);
                                }
                                
                                /* var totalPoint = int.Parse(TotalValue.Text) * 5 / 100;*/
                                var rangeUser = $"{"User"}!A" + (numberRow) + ":C" + (numberRow);

                                var userValue = new ValueRange();
                                userValue.Values = new List<IList<object>> { new List<object>() { list[0].ToString(), list[1].ToString(), totalPoint.ToString() } };
                                var updatePoint = service.Spreadsheets.Values.Update(userValue, "1NdaV8vr3yAyZUX5TstgNLB5UIVngZ8wBNVgvpYqnA3g", rangeUser);
                                updatePoint.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                                var AddPoint = updatePoint.Execute();
                            }
                        }
                    }
                    Reset();



                }
                else
                {
                    Reset();
                }



            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        private void Reset()
        {
            productList.Clear();
            listView.Items.Refresh();
            TotalValue.Text = "";
            inputTextBox.Text = "";
            paymentMethodComboBox.SelectedIndex = 0;
            txt_Phone.Text = "";
            txt_RegistPhone.Text = "";
            txt_Name.Text = "";
            labelUserName.Content = "";
            txt_Point.Text = "";

        }
        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBoxItem selectedItem = (ComboBoxItem)paymentMethodComboBox.SelectedItem;
            if (selectedItem != null)
            {

                string selectedPaymentMethod = selectedItem.Content.ToString();
                if (IsListViewEmpty() && selectedPaymentMethod != "Chọn Thanh Toán")
                {
                    MessageBox.Show("Vui Lòng Thêm Sản Phẩm", "Giỏ Hàng", MessageBoxButton.OK, MessageBoxImage.Information);
                    paymentMethodComboBox.SelectedIndex = 0;
                }
                else
                {
                    switch (selectedPaymentMethod)
                    {
                        case "Momo":
                            priceFinal = int.Parse(TotalValue.Text) - int.Parse(txt_Point.Text);
                            AddOrder();
                            string Phone = "0349470340";
                            string Name = "Hoàng Chí Dương";
                            string Email = "";
                            string PayNumber = priceFinal.ToString().Trim();
                            string Description = "";
                            MomoQRCodeGenerator momoGenerator = new MomoQRCodeGenerator();
                            string merchantCode = $"2|99|{Phone}|{Name}|{Email}|0|0|{PayNumber}|{Description}";
                            Bitmap momoQRCode = momoGenerator.GenerateMomoQRCode(merchantCode);
                            if (!string.IsNullOrWhiteSpace(TotalValue.Text))
                            {
                                ScanMoMoQR scanQR = new ScanMoMoQR();
                                scanQR.UpdateQRCode(momoQRCode);
                                double windowWidth = 500;
                                double windowHeight = 600; // Tăng chiều cao để chứa nút

                                // Tạo Grid để chứa cảnh báo và StackPanel chứa nút
                                Grid grid = new Grid();
                                grid.Width = windowWidth;
                                grid.Height = windowHeight;
                                Window qrCodeWindow = new Window
                                {
                                    Width = windowWidth,
                                    Height = windowHeight,
                                    WindowStyle = WindowStyle.None,
                                    ResizeMode = ResizeMode.NoResize,
                                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                                    Title = "ScanQR",
                                    Content = grid
                                };

                                // Thêm ScanMoMoQR vào Grid
                                grid.Children.Add(scanQR);

                                // Tạo Grid để chứa nút
                                Grid buttonGrid = new Grid();
                                buttonGrid.HorizontalAlignment = HorizontalAlignment.Center;
                                buttonGrid.VerticalAlignment = VerticalAlignment.Bottom;
                                buttonGrid.Height = 50; // Đặt chiều cao của lưới nút

                                ColumnDefinition column1 = new ColumnDefinition();
                                ColumnDefinition column2 = new ColumnDefinition();
                                column1.Width = new GridLength(1, GridUnitType.Star);
                                column2.Width = new GridLength(1, GridUnitType.Star);
                                buttonGrid.ColumnDefinitions.Add(column1);
                                buttonGrid.ColumnDefinitions.Add(column2);


                                Button failButton = new Button();
                                failButton.Content = "Hủy";
                                failButton.Width = 100;
                                failButton.Margin = new Thickness(10, 0, 10, 0);
                                failButton.Click += (sender, e) =>
                                {
                                    AddPayment("fail", "MOMO");
                                    qrCodeWindow.Close();
                                    Reset();
                                };
                                Button successButton = new Button();
                                successButton.Content = "Thành công";
                                successButton.Width = 100;
                                successButton.Margin = new Thickness(10, 0, 10, 0);
                                successButton.Click += (sender, e) =>
                                {
                                    AddPayment("success", "MOMO");
                                    qrCodeWindow.Close();
                                    Reset();
                                };
                                Grid.SetColumn(successButton, 1);
                                Grid.SetColumn(failButton, 0);

                                buttonGrid.Children.Add(failButton);
                                buttonGrid.Children.Add(successButton);
                                grid.Children.Add(buttonGrid);

                                qrCodeWindow.Show();
                            }
                            else
                            {
                                MessageBox.Show("Sai Tổng Tiền", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            break;
                        case "Cash":
                            AddOrder();
                            AddPayment("success", "Cash");
                            Reset();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private async void btn_UserPoint(object sender, RoutedEventArgs e)
        {
            string phoneNumber = txt_Phone.Text.Trim();
            string userPoint = await GetPointByPhoneNumber(phoneNumber);
            var total = 0;
            if (txt_Point.Text != null && int.Parse(txt_Point.Text) <= int.Parse(userPoint) )
            {
               total = int.Parse(TotalValue.Text) - int.Parse(txt_Point.Text);
            }
            TotalValue.Text = total.ToString();

        }
    }
}
