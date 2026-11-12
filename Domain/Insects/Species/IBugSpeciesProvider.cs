using System;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    public interface IBugSpeciesProvider
    {
        IBugSpecies PickRandomSpecies(Random rng, bool isNightTime);
    }
}
