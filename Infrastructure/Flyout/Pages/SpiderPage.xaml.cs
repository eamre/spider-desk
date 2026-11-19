using System;
using System.Windows;
using System.Windows.Controls;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Infrastructure.Sessions;

namespace SpiderDesk.Infrastructure.Flyout.Pages
{
    /// <summary>
    /// Ayarlar penceresinin "Örümcek" sekmesi: seviye ve ilerleme, tokluk, toplam av, şu an ne yaptığı ve komutlar.
    /// Yalnızca gösterir ve düğmeleri olay olarak bildirir; komutları örümceğe <see cref="SpiderSessionController"/> iletir.
    /// </summary>
    public partial class SpiderPage : UserControl, ISpiderView
    {
        public event Action? NewWebRequested;
        public event Action? RappelRequested;
        public event Action? RainRequested;
        public event Action? SpawnBugsRequested;
        public event Action? ClearWebsRequested;

        public SpiderPage()
        {
            InitializeComponent();
            SpawnBugsText.Text = $"{SpiderSessionController.BugsPerSpawn} böcek gönder";
        }

        public void ShowStatus(SpiderStatusDisplay status)
        {
            LevelBadgeText.Text = status.Level.ToString();
            LevelText.Text = $"Seviye {status.Level}";
            NextLevelText.Text = status.NextLevelText;
            LevelFillScale.ScaleX = status.LevelProgress;

            SatietyText.Text = $"%{status.SatietyPercent}";
            BugsText.Text = status.BugsCaught.ToString();
            ActivityText.Text = status.Activity;
        }

        private void OnNewWebClicked(object sender, RoutedEventArgs e) => NewWebRequested?.Invoke();
        private void OnRappelClicked(object sender, RoutedEventArgs e) => RappelRequested?.Invoke();
        private void OnRainClicked(object sender, RoutedEventArgs e) => RainRequested?.Invoke();
        private void OnSpawnBugsClicked(object sender, RoutedEventArgs e) => SpawnBugsRequested?.Invoke();
        private void OnClearWebsClicked(object sender, RoutedEventArgs e) => ClearWebsRequested?.Invoke();
    }
}
