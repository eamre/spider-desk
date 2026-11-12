using System;
using System.Collections.Generic;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
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
