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

namespace ScanProduct
{
    public partial class MainWindow : Window
    {
        private readonly ITextToSpeed _textToSpeed;
        private readonly ISheetService _sheetService;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isScanning = false;
        public MainWindow()
        {
            _textToSpeed = new TextToSpeedService();
            _sheetService = new SheetService();
            InitializeComponent();
            InitializeCamera();
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
                                getProduct(barcodeText);
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
        public async Task<Product> getProduct(string productId)
        {
            var listProduct = loadData("Product");
            var product = new Product();

            foreach (var Column in listProduct)
            {
                if (Column[0].ToString().Equals(productId))
                {
                    product.ProductId = (string)Column[0];
                    product.Price = (string)Column[1];
                    product.Productname = (string)Column[2];
                    product.Quanity = (string)Column[3];
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
    }
}
