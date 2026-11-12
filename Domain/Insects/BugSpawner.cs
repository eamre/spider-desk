using System.Linq;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects.Species;

namespace SpiderDesk.Domain.Insects
{
    /// <summary>
    /// Böceklerin ne zaman ve nereden doğacağını yönetir; sıklık ayarı dinamik değiştirilebilir.
    /// </summary>
    public class BugSpawner
    {
        private readonly IBugSpeciesProvider _speciesProvider;
        private double _spawnTimer = 15.0;
        private BugSpawnRatePreset _spawnRate = BugSpawnRatePreset.Low;

        public BugSpawnRatePreset SpawnRate
        {
            get => _spawnRate;
            set
            {
                _spawnRate = value;
                _spawnTimer = System.Math.Min(_spawnTimer, GetNextInterval(new System.Random()));
            }
        }

        public BugSpawner(IBugSpeciesProvider speciesProvider, BugSpawnRatePreset initialRate = BugSpawnRatePreset.Low)
        {
            _speciesProvider = speciesProvider;
            _spawnRate = initialRate;
        }

        public void Update(WorldContext world, double dt)
        {
            bool hasCompletedWeb = false;
            for (int i = 0; i < world.Webs.Count; i++)
            {
                if (world.Webs[i].IsCompleted)
                {
                    hasCompletedWeb = true;
                    break;
                }
            }

            if (!hasCompletedWeb)
            {
                return;
            }

            int maxBugs = _spawnRate switch
            {
                BugSpawnRatePreset.Low => 2,
                BugSpawnRatePreset.Normal => 3,
                _ => 5
            };

            _spawnTimer -= dt;
            if (_spawnTimer <= 0)
            {
                if (world.Bugs.Count < maxBugs)
                {
                    world.Bugs.Add(CreateRandomBug(world));
                }
                _spawnTimer = GetNextInterval(world.Random);
            }
        }

        public void SpawnBurst(WorldContext world, int count)
        {
            for (int i = 0; i < count; i++)
            {
                world.Bugs.Add(CreateRandomBug(world));
            }
        }

        public BugEntity CreateRandomBug(WorldContext world)
        {
            var rng = world.Random;
            IBugSpecies species = _speciesProvider.PickRandomSpecies(rng, world.IsNightTime);
            double size = species.BaseSize + (rng.NextDouble() - 0.5) * 1.2;

            int edge = rng.Next(4);
            Point startPos = edge switch
            {
                0 => new Point(rng.NextDouble() * world.ScreenWidth, -20),
                1 => new Point(world.ScreenWidth + 20, rng.NextDouble() * world.ScreenHeight),
                2 => new Point(rng.NextDouble() * world.ScreenWidth, world.ScreenHeight + 20),
                _ => new Point(-20, rng.NextDouble() * world.ScreenHeight)
            };

            Point target = new Point(
                world.ScreenWidth * (0.2 + rng.NextDouble() * 0.6),
                world.ScreenHeight * (0.2 + rng.NextDouble() * 0.6)
            );

            var completedWebs = world.Webs.Where(w => w.IsCompleted).ToList();
            if (completedWebs.Count > 0 && rng.NextDouble() < 0.65)
            {
                var web = completedWebs[rng.Next(completedWebs.Count)];
                Point hub = web.HubPosition;
                target = new Point(
                    hub.X + (rng.NextDouble() - 0.5) * web.Radius * 0.9,
                    hub.Y + (rng.NextDouble() - 0.5) * web.Radius * 0.9
                );
            }

            Vector dir = target - startPos;
            if (dir.Length > 0.01) dir.Normalize();
            double speed = species.MinSpeed + rng.NextDouble() * (species.MaxSpeed - species.MinSpeed) * 0.6;

            return new BugEntity(startPos, dir * speed, size, species);
        }

        private double GetNextInterval(System.Random rng)
        {
            return _spawnRate switch
            {
                BugSpawnRatePreset.Low => 45.0 + rng.NextDouble() * 30.0,    // 45 - 75 sn
                BugSpawnRatePreset.Normal => 20.0 + rng.NextDouble() * 18.0, // 20 - 38 sn
                _ => 8.0 + rng.NextDouble() * 9.0                            // 8 - 17 sn
            };
        }
    }
}
