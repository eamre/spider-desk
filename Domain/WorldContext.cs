using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Core;
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
        public Random Random { get; } = new();
    }
}
