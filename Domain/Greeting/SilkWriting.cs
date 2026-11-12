using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Domain.Greeting
{
    /// <summary>İpin yapıştığı düğüm noktası. Fırlatılan ipin ilk değdiği yerlerde küçük bir yapışma halkası belirir.</summary>
    public readonly record struct SilkKnot(Point Position, double TiedAt, bool IsLaunchPoint);

    /// <summary>Yazı bittiğinde kopan ve solarak kaybolan akış ipi.</summary>
    public readonly record struct SilkSnap(Point From, Point To, double SnappedAt);

    /// <summary>
    /// Örümceğin açılışta ipekle yazdığı selamın durumu: serilmiş teller, düğümler, örümcekten yazının ucuna
    /// uzanan kesintisiz akış ipi ve yazının solması. Yazının nasıl yazılacağına örümceğin durumu karar verir;
    /// bu sınıf yalnızca ortaya çıkan ipeği tutar.
    /// </summary>
    public sealed class SilkWriting
    {
        private readonly List<SilkThread> _threads = new();
        private readonly List<SilkKnot> _knots = new();

        public IReadOnlyList<SilkThread> Threads => _threads;
        public IReadOnlyList<SilkKnot> Knots => _knots;

        /// <summary>Yazının başlangıcından beri geçen süre; tel titreşimi gibi zamanlı görseller bu saate göre çizilir.</summary>
        public double Age { get; private set; }
        public double Opacity { get; private set; } = 1.0;
        public bool IsFading { get; private set; }
        public bool IsFinished => IsFading && Opacity <= 0;

        public Point? StreamOrigin { get; private set; }
        public Point? StreamTip { get; private set; }
        public bool IsStreamFlowing { get; private set; }

        /// <summary>Şu an yazılmakta olan harf çizgisinin, ipin ucuna kadar serilmiş kısmının başlangıcı.</summary>
        public Point? StrokeInProgressStart { get; private set; }
        public SilkSnap? Snap { get; private set; }

        public void Update(double dt)
        {
            Age += dt;
            if (IsFading)
            {
                Opacity = Math.Max(0, Opacity - dt / SpiderTuning.GreetingFadeSeconds);
            }
        }

        public void UpdateStream(Point origin, Point tip, bool isFlowing, Point? strokeInProgressStart)
        {
            StreamOrigin = origin;
            StreamTip = tip;
            IsStreamFlowing = isFlowing;
            StrokeInProgressStart = strokeInProgressStart;
        }

        /// <summary>Akış ipini koparır; kopan ip kısa sürede solarak kaybolur.</summary>
        public void SnapStream()
        {
            if (StreamOrigin is { } origin && StreamTip is { } tip)
            {
                Snap = new SilkSnap(origin, tip, Age);
            }

            StreamOrigin = null;
            StreamTip = null;
            IsStreamFlowing = false;
            StrokeInProgressStart = null;
        }

        public void LayThread(Point start, Point end, Random rng)
        {
            double length = (end - start).Length;
            if (length < 0.01) return;

            int beadCount = (int)(length / SpiderTuning.GreetingBeadSpacing);
            var beads = new SilkBead[beadCount];
            for (int i = 0; i < beadCount; i++)
            {
                double jitter = (rng.NextDouble() - 0.5) * 0.6;
                double radius = SpiderTuning.GreetingBeadMinRadius + rng.NextDouble() * SpiderTuning.GreetingBeadRadiusVariance;
                beads[i] = new SilkBead((i + 0.5 + jitter) / beadCount, radius);
            }

            _threads.Add(new SilkThread(start, end, Age, beads));
        }

        public void TieKnot(Point position, bool isLaunchPoint) =>
            _knots.Add(new SilkKnot(position, Age, isLaunchPoint));

        /// <summary>Yazıyı soldurmaya başlar; akış ipi hâlâ bağlıysa koparır.</summary>
        public void BeginFade()
        {
            if (IsFading) return;
            SnapStream();
            IsFading = true;
        }
    }
}
