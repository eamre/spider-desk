using System;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Rendering;

namespace SpiderDesk
{
    /// <summary>
    /// Yalnızca şeffaf masaüstü katman penceresinin görsel yapılandırmasını ve işletim sistemi
    /// pencere tanıtıcısı (HWND) yaşam döngüsünü yöneten ana pencere sınıfı.
    /// </summary>
    public class MainWindow : Window
    {
        private readonly IWindowLayerService _windowLayerService;

        public MainWindow(
            SimulationCanvas canvas,
            IWindowLayerService windowLayerService)
        {
            _windowLayerService = windowLayerService;

            ConfigureOverlayWindow();
            Content = canvas;
        }

        private void ConfigureOverlayWindow()
        {
            Title = "SpiderDesk";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = false;
            ShowInTaskbar = false;
            WindowState = WindowState.Normal;
            Left = 0;
            Top = 0;
            Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/spider.ico"));
            Width = SystemParameters.PrimaryScreenWidth;
            // Tam ekran (Maximized / 100% boy) pencereler Windows Explorer'da ABN_FULLSCREENAPP bildirimi
            // tetikleyip masaüstü ikonlarının F5 yapılmış gibi yenilenmesine yol açtığı için 1 piksel kısa tutulur.
            Height = Math.Max(100, SystemParameters.PrimaryScreenHeight - 1);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowLayerService.Attach(this);
        }
    }
}
