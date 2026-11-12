using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Greeting
{
    /// <summary>Bir ipek telinin üzerindeki çiy damlası: tel boyunca konumu (0..1) ve yarıçapı.</summary>
    public readonly record struct SilkBead(double Position, double Radius);

    /// <summary>Yazının harflerini oluşturan, iki nokta arasına gerilmiş tek bir ipek teli.</summary>
    public sealed class SilkThread
    {
        public Point Start { get; }
        public Point End { get; }
        public double Length { get; }
        public double LaidAt { get; }
        public IReadOnlyList<SilkBead> Beads { get; }

        public SilkThread(Point start, Point end, double laidAt, IReadOnlyList<SilkBead> beads)
        {
            Start = start;
            End = end;
            Length = (end - start).Length;
            LaidAt = laidAt;
            Beads = beads;
        }
    }
}
