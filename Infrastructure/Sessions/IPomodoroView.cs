using System;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Pomodoro;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// Pomodoro'nun gösterildiği ve yönetildiği yer (ayarlar penceresinin "Pomodoro" sekmesi).
    /// <see cref="PomodoroSessionController"/> yalnızca bu arayüzü tanır; testlerde yerine sahte bir görünüm verilir.
    /// </summary>
    public interface IPomodoroView
    {
        event Action? StartRequested;
        event Action? PauseRequested;
        event Action? ResumeRequested;
        event Action? FinishRequested;
        event Action<PomodoroPreset>? PresetSelected;

        /// <summary>Seçili süre ayarını gösterir. Bu çağrı <see cref="PresetSelected"/>'ı tetiklemez.</summary>
        void ShowPreset(PomodoroPreset preset);

        /// <summary>Geri sayımı, durum yazısını ve düğmeleri günceller. Her karede çağrılır.</summary>
        void ShowState(PomodoroDisplay display);
    }
}
