using System;
using System.Collections.Generic;
using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    public interface IBugSpeciesProvider
    {
        IBugSpecies PickRandomSpecies(Random rng, bool isNightTime);
    }

    public sealed class HouseFlySpecies : IBugSpecies
    {
        public string Name => "Karasinek";
        public Color BodyColor => Color.FromRgb(45, 55, 65);
        public Color? GlowColor => null;
        public double BaseSize => 5.4;
        public double MinSpeed => 105.0;
        public double MaxSpeed => 185.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.0;
        public bool CanPerchOnDesktop => true;
    }

    public sealed class EmeraldFlySpecies : IBugSpecies
    {
        public string Name => "Yeşil Sinek";
        public Color BodyColor => Color.FromRgb(30, 145, 95);
        public Color? GlowColor => null;
        public double BaseSize => 5.8;
        public double MinSpeed => 115.0;
        public double MaxSpeed => 200.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.18;
        public bool CanPerchOnDesktop => true;
    }

    public sealed class GoldenMothSpecies : IBugSpecies
    {
        public string Name => "Altın Güve";
        public Color BodyColor => Color.FromRgb(190, 145, 55);
        public Color? GlowColor => null;
        public double BaseSize => 6.4;
        public double MinSpeed => 95.0;
        public double MaxSpeed => 165.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.0;
        public bool CanPerchOnDesktop => true;
    }

    /// <summary>
    /// Ateş Böceği (Firefly): Etrafına sarı-yeşil biyolüminesans ışık saçarak uçar.
    /// </summary>
    public sealed class FireflySpecies : IBugSpecies
    {
        public string Name => "Ateş Böceği";
        public Color BodyColor => Color.FromRgb(70, 90, 45);
        public Color? GlowColor => Color.FromRgb(195, 255, 75);
        public double BaseSize => 5.6;
        public double MinSpeed => 85.0;
        public double MaxSpeed => 150.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.0;
        public bool CanPerchOnDesktop => true;
    }

    /// <summary>
    /// Güçlü İri Böcek / Yaban Arısı: Sadece bu tür ağa çarptığında telleri koparabilir ve ağı yırtıp kurtulabilir!
    /// </summary>
    public sealed class HornetSpecies : IBugSpecies
    {
        public string Name => "Yaban Arısı";
        public Color BodyColor => Color.FromRgb(215, 130, 20);
        public Color? GlowColor => null;
        public double BaseSize => 7.4;
        public double MinSpeed => 125.0;
        public double MaxSpeed => 215.0;
        public int WebDamageOnImpact => 2;
        public double EscapeChance => 0.35;
        public bool CanPerchOnDesktop => false;
    }

    public class BugSpeciesCatalog : IBugSpeciesProvider
    {
        private readonly IReadOnlyList<(IBugSpecies Species, double Weight)> _dayEntries = new (IBugSpecies, double)[]
        {
            (new HouseFlySpecies(), 0.36),
            (new EmeraldFlySpecies(), 0.28),
            (new HornetSpecies(), 0.16),
            (new GoldenMothSpecies(), 0.12),
            (new FireflySpecies(), 0.08)
        };

        private readonly IReadOnlyList<(IBugSpecies Species, double Weight)> _nightEntries = new (IBugSpecies, double)[]
        {
            (new FireflySpecies(), 0.52),
            (new GoldenMothSpecies(), 0.26),
            (new HouseFlySpecies(), 0.10),
            (new EmeraldFlySpecies(), 0.06),
            (new HornetSpecies(), 0.06)
        };

        public IBugSpecies PickRandomSpecies(Random rng, bool isNightTime)
        {
            var table = isNightTime ? _nightEntries : _dayEntries;
            double roll = rng.NextDouble();
            double cumulative = 0.0;

            foreach (var entry in table)
            {
                cumulative += entry.Weight;
                if (roll <= cumulative)
                {
                    return entry.Species;
                }
            }

            return table[0].Species;
        }
    }
}
