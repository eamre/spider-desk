using System;
using System.Windows.Forms;
using SpiderDesk.Core.Settings;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Tepsideki "🍅 Pomodoro" alt menüsü: Başlat / Duraklat / Devam Et, Bitir, süre seçimi ve başlıkta geri sayım.
    /// </summary>
    public sealed class PomodoroMenu
    {
        private const string Title = "🍅 Pomodoro";

        private readonly ToolStripMenuItem _primaryItem;
        private readonly ToolStripMenuItem _finishItem;
        private bool _isRunning;
        private bool _isPaused;

        /// <summary>Menüye eklenecek "🍅 Pomodoro" maddesi.</summary>
        public ToolStripMenuItem Root { get; }

        public event Action? StartRequested;
        public event Action? PauseRequested;
        public event Action? ResumeRequested;
        public event Action? FinishRequested;
        public event Action<PomodoroPreset>? PresetSelected;

        /// <summary>Geri sayım değişince (ya da Pomodoro bitince, null) haber verir; tepsi ikonunun ipucu buna göre güncellenir.</summary>
        public event Action<string?>? CountdownChanged;

        public PomodoroMenu(PomodoroPreset currentPreset, KeepMenuOpen keepOpen)
        {
            Root = new ToolStripMenuItem(Title);

            // Bu düğmeler menüyü açık tutar: kullanıcı yazının değiştiğini ve geri sayımı hemen görür.
            // İlk düğme duruma göre Başlat, Duraklat ya da Devam Et olur.
            _primaryItem = keepOpen.Mark(new ToolStripMenuItem("▶️ Başlat"));
            _primaryItem.Click += (_, _) => OnPrimaryClicked();
            Root.DropDownItems.Add(_primaryItem);

            _finishItem = keepOpen.Mark(new ToolStripMenuItem("⏹️ Bitir") { Available = false });
            _finishItem.Click += (_, _) => FinishRequested?.Invoke();
            Root.DropDownItems.Add(_finishItem);
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

        private void OnPrimaryClicked()
        {
            if (!_isRunning) StartRequested?.Invoke();
            else if (_isPaused) ResumeRequested?.Invoke();
            else PauseRequested?.Invoke();
        }

        /// <summary>
        /// Pomodoro'nun durumunu menüye yansıtır. <paramref name="countdown"/> null ise Pomodoro çalışmıyordur.
        /// </summary>
        public void ShowState(string? countdown, bool isPaused)
        {
            _isRunning = countdown != null;
            _isPaused = _isRunning && isPaused;

            string primaryText = !_isRunning ? "▶️ Başlat" : _isPaused ? "▶️ Devam Et" : "⏸️ Duraklat";
            TrayMenuItems.SetTextIfChanged(_primaryItem, primaryText);
            // Visible yerine Available: menü kapalıyken Visible her zaman false okunur, Available ise ayarlanan değeri verir
            if (_finishItem.Available != _isRunning)
            {
                _finishItem.Available = _isRunning;
            }

            TrayMenuItems.SetTextIfChanged(Root, _isRunning ? $"{Title} · {countdown}" : Title);
            CountdownChanged?.Invoke(countdown);
        }
    }
}
