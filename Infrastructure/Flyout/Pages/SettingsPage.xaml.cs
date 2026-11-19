using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Infrastructure.Sessions;

namespace SpiderDesk.Infrastructure.Flyout.Pages
{
    /// <summary>
    /// Ayarlar penceresinin "Ayarlar" sekmesi: tema, gece/gündüz, hız ve sıklıklar, "Üstte göster" ve "Otomatik başlat".
    /// Yalnızca gösterir ve kullanıcının seçimini olay olarak bildirir; ayarı uygulamak ve kaydetmek
    /// <see cref="SettingsSessionController"/>'ın işidir.
    /// </summary>
    public partial class SettingsPage : UserControl, ISettingsView
    {
        private readonly Dictionary<string, RadioButton> _themeButtons = new();
        private readonly Dictionary<EnvironmentTimeMode, RadioButton> _timeModeButtons;
        private readonly Dictionary<SpiderSpeedPreset, RadioButton> _speedButtons;
        private readonly Dictionary<BugSpawnRatePreset, RadioButton> _spawnRateButtons;
        private readonly Dictionary<DewSpawnRatePreset, RadioButton> _dewButtons;

        // ShowSettings düğmeleri değiştirirken olay çıkmasın; olaylar yalnızca kullanıcının seçimleri içindir
        private bool _isShowingSettings;

        public event Action<ISpiderTheme>? ThemeSelected;
        public event Action<EnvironmentTimeMode>? TimeModeSelected;
        public event Action<SpiderSpeedPreset>? SpeedPresetSelected;
        public event Action<BugSpawnRatePreset>? SpawnRateSelected;
        public event Action<DewSpawnRatePreset>? DewSpawnRateSelected;
        public event Action<bool>? AlwaysOnTopChanged;
        public event Action<bool>? StartupChanged;

        public SettingsPage()
        {
            InitializeComponent();

            foreach (ISpiderTheme theme in SpiderThemeCatalog.All)
            {
                _themeButtons[theme.Id] = AddThemeChip(theme);
            }

            _timeModeButtons = AddSegments(TimeModeChoices, "TimeMode", new (string, EnvironmentTimeMode)[]
            {
                ("Otomatik", EnvironmentTimeMode.AutoBySystemClock),
                ("Gündüz", EnvironmentTimeMode.AlwaysDay),
                ("Gece", EnvironmentTimeMode.AlwaysNight)
            }, mode => TimeModeSelected?.Invoke(mode));

            _speedButtons = AddSegments(SpeedChoices, "Speed", new (string, SpiderSpeedPreset)[]
            {
                ("Sakin", SpiderSpeedPreset.Calm),
                ("Normal", SpiderSpeedPreset.Normal),
                ("Çevik", SpiderSpeedPreset.Agile)
            }, preset => SpeedPresetSelected?.Invoke(preset));

            _spawnRateButtons = AddSegments(SpawnRateChoices, "SpawnRate", new (string, BugSpawnRatePreset)[]
            {
                ("Seyrek", BugSpawnRatePreset.Low),
                ("Normal", BugSpawnRatePreset.Normal),
                ("Yoğun", BugSpawnRatePreset.High)
            }, rate => SpawnRateSelected?.Invoke(rate));

            _dewButtons = AddSegments(DewChoices, "Dew", new (string, DewSpawnRatePreset)[]
            {
                ("Kapalı", DewSpawnRatePreset.Off),
                ("Seyrek", DewSpawnRatePreset.Low),
                ("Normal", DewSpawnRatePreset.Normal),
                ("Yoğun", DewSpawnRatePreset.High)
            }, rate => DewSpawnRateSelected?.Invoke(rate));
        }

        public void ShowSettings(UserSettings settings, bool runsOnStartup)
        {
            _isShowingSettings = true;
            try
            {
                string themeId = SpiderThemeCatalog.GetById(settings.ThemeId).Id;
                _themeButtons[themeId].IsChecked = true;
                _timeModeButtons[settings.TimeMode].IsChecked = true;
                _speedButtons[settings.SpeedPreset].IsChecked = true;
                _spawnRateButtons[settings.SpawnRate].IsChecked = true;
                _dewButtons[settings.DewSpawnRate].IsChecked = true;
                AlwaysOnTopSwitch.IsChecked = settings.AlwaysOnTop;
                StartupSwitch.IsChecked = runsOnStartup;
            }
            finally
            {
                _isShowingSettings = false;
            }
        }

        public void ShowSaveFailed(bool failed) => SaveWarning.Visibility = failed ? Visibility.Visible : Visibility.Collapsed;

        private void OnAlwaysOnTopToggled(object sender, RoutedEventArgs e)
        {
            if (!_isShowingSettings) AlwaysOnTopChanged?.Invoke(AlwaysOnTopSwitch.IsChecked == true);
        }

        private void OnStartupToggled(object sender, RoutedEventArgs e)
        {
            if (!_isShowingSettings) StartupChanged?.Invoke(StartupSwitch.IsChecked == true);
        }

        /// <summary>Seçeneklerden yalnızca birinin seçili olabildiği parça düğmeleri ekler.</summary>
        private Dictionary<T, RadioButton> AddSegments<T>(Panel panel, string group, (string Label, T Value)[] options, Action<T> onChosen)
            where T : notnull
        {
            var buttons = new Dictionary<T, RadioButton>();
            foreach (var (label, value) in options)
            {
                var button = new RadioButton
                {
                    Content = label,
                    GroupName = group,
                    Style = (Style)FindResource("Segment")
                };
                button.Checked += (_, _) =>
                {
                    if (!_isShowingSettings) onChosen(value);
                };
                buttons[value] = button;
                panel.Children.Add(button);
            }
            return buttons;
        }

        /// <summary>Temanın renklerinden küçük bir örnek ve adıyla bir kutucuk ekler.</summary>
        private RadioButton AddThemeChip(ISpiderTheme theme)
        {
            var swatch = new Grid { Width = 18, Height = 18, Margin = new Thickness(0, 0, 8, 0) };
            swatch.Children.Add(new Ellipse
            {
                Fill = Frozen(theme.AbdomenColor),
                Stroke = Frozen(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1
            });
            swatch.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = Frozen(theme.MarkingColor) });

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(swatch);
            content.Children.Add(new TextBlock
            {
                Text = WithoutIcon(theme.DisplayName),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var chip = new RadioButton
            {
                Content = content,
                GroupName = "Theme",
                Style = (Style)FindResource("ThemeChip"),
                ToolTip = WithoutIcon(theme.DisplayName)
            };
            chip.Checked += (_, _) =>
            {
                if (!_isShowingSettings) ThemeSelected?.Invoke(theme);
            };
            ThemeChoices.Children.Add(chip);
            return chip;
        }

        // Tema adlarının başındaki emoji tepsi menüsü içindi; WPF emojileri renksiz çizdiği için burada gösterilmez
        private static string WithoutIcon(string displayName) =>
            new string(displayName.SkipWhile(c => !char.IsLetter(c)).ToArray());

        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
