using System;
using System.Windows.Forms;
using SpiderDesk.Core.Settings;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Tepsideki "🍅 Pomodoro" alt menüsü: Başlat/Durdur, süre seçimi ve başlıkta geri sayım.
    /// </summary>
    public sealed class PomodoroMenu
    {
        private const string Title = "🍅 Pomodoro";

        private readonly ToolStripMenuItem _toggleItem;

        /// <summary>Menüye eklenecek "🍅 Pomodoro" maddesi.</summary>
        public ToolStripMenuItem Root { get; }

        public event Action? ToggleRequested;
        public event Action<PomodoroPreset>? PresetSelected;

        /// <summary>Geri sayım değişince (ya da Pomodoro durunca, null) haber verir; tepsi ikonunun ipucu buna göre güncellenir.</summary>
        public event Action<string?>? CountdownChanged;

        public PomodoroMenu(PomodoroPreset currentPreset, KeepMenuOpen keepOpen)
        {
            Root = new ToolStripMenuItem(Title);

            // Başlat/Durdur menüyü açık tutar: kullanıcı yazının "Durdur"a dönüştüğünü ve geri sayımı hemen görür
            _toggleItem = keepOpen.Mark(new ToolStripMenuItem("▶️ Başlat"));
            _toggleItem.Click += (_, _) => ToggleRequested?.Invoke();
            Root.DropDownItems.Add(_toggleItem);
            Root.DropDownItems.Add(new ToolStripSeparator());

            Root.DropDownItems.Add(TrayMenuItems.Header("⏱️ Süre"));
            TrayMenuItems.AddRadioOptions(
                Root,
                new (string, PomodoroPreset)[]
                {
                    ("Klasik (25 dk odak / 5 dk mola)", PomodoroPreset.Classic),
                    ("Uzun (50 dk odak / 10 dk mola)", PomodoroPreset.Extended)
                },
                preset => preset == currentPreset,
                preset => PresetSelected?.Invoke(preset),
                keepOpen);

            keepOpen.AttachTo(Root.DropDown);
        }

        /// <summary>
        /// Pomodoro'nun durumunu menüye yansıtır. <paramref name="countdown"/> null ise Pomodoro çalışmıyordur.
        /// </summary>
        public void ShowState(string? countdown)
        {
            bool isRunning = countdown != null;
            TrayMenuItems.SetTextIfChanged(_toggleItem, isRunning ? "⏹️ Durdur" : "▶️ Başlat");
            TrayMenuItems.SetTextIfChanged(Root, isRunning ? $"{Title} · {countdown}" : Title);
            CountdownChanged?.Invoke(countdown);
        }
    }
}
