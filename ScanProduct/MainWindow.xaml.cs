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

        private void Btn_Done_Click(object sender, RoutedEventArgs e)
        {
            string textToSpeak = TotalValue.Text;
            _textToSpeed.SpeedGoogle("Tổng Hóa đơn của quý khách là" + textToSpeak + "Đồng");
        }

        private bool ProcessPayment()
        {
            bool paymentSuccess = false;


            return paymentSuccess;
        }

        private async void btn_PayMono_Click(object sender, RoutedEventArgs e)
        {
            string Phone = "0349470340";
            string Name = "Hoàng Chí Dương";
            string Email = "";
            string PayNumber = TotalValue.Text.Trim();
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
                failButton.Content = "Thất bại";
                failButton.Width = 100; 
                failButton.Margin = new Thickness(10, 0, 10, 0);
                failButton.Click += (sender, e) =>
                {
                    qrCodeWindow.Close();
                    MessageBox.Show("QR code quá thời gian. Vui lòng thanh toán lại !!!", "Error", MessageBoxButton.OK);
                };
                Button successButton = new Button();
                successButton.Content = "Thành công";
                successButton.Width = 100; 
                successButton.Margin = new Thickness(10, 0, 10, 0); 
                successButton.Click += (sender, e) =>
                {
                    qrCodeWindow.Close(); 
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
                MessageBox.Show("Please enter a valid total price.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }
    }
}
