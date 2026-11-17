using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.SilkText;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain
{
    public class WorldContext
    {
        public double ScreenWidth { get; set; } = 1920;
        public double ScreenHeight { get; set; } = 1080;
        public Point CursorPosition { get; set; }
        public Point PreviousCursorPosition { get; set; }
        public double CursorSpeed { get; set; }
        public bool IsNightTime { get; set; }
        public DewSpawnRatePreset DewSpawnRate { get; set; } = DewSpawnRatePreset.Normal;
        public List<SpiderWebEntity> Webs { get; } = new();
        public List<BugEntity> Bugs { get; } = new();

        /// <summary>Örümceğin açılışta ipekle yazdığı selam; yazı solup kaybolunca null olur.</summary>
        public SilkWriting? SilkMessage { get; set; }
        public Random Random { get; } = new();

        /// <summary>
        /// Yeni ağı ekler. Ekranda <see cref="WebTuning.MaxWebsOnScreen"/>'dan fazla ağ olursa en eski ağ silinir
        /// ve ona takılı böcekler serbest kalır.
        /// </summary>
        public void AddWeb(SpiderWebEntity web)
        {
            Webs.Add(web);
            if (Webs.Count > WebTuning.MaxWebsOnScreen)
            {
                SpiderWebEntity oldest = Webs[0];
                foreach (BugEntity bug in Bugs)
                {
                    if (bug.TrappedWeb == oldest) bug.ReleaseFromWeb(this);
                }
                Webs.RemoveAt(0);
            }
        }

        /// <summary>Bütün ağları siler; ağlara takılı ya da sarılmakta olan böcekler serbest kalıp uçar.</summary>
        public void ClearWebs()
        {
            foreach (BugEntity bug in Bugs)
            {
                if (bug.IsCaughtInWeb) bug.ReleaseFromWeb(this);
            }
            Webs.Clear();
        }
    }
}
