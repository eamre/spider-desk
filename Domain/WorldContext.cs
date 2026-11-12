using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Greeting;
using SpiderDesk.Domain.Insects;
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
        public SilkWriting? Greeting { get; set; }
        public Random Random { get; } = new();
    }
}
