using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin hangi böceğin peşine düşeceğini ve aç kalınca hangi ağın kilerinden yiyeceğini seçer.
    /// Yalnızca dünyaya bakar, hiçbir şeyi değiştirmez; bu yüzden kolayca test edilebilir.
    /// </summary>
    public static class PreySearch
    {
        private const double EncounterRadiusSq = SpiderTuning.DesktopEncounterRadius * SpiderTuning.DesktopEncounterRadius;

        /// <summary>
        /// Örümceğin peşine düşeceği böceği seçer. Öncelik sırası:
        /// <list type="number">
        /// <item>Masaüstünde hemen yanından geçen uçan böcek (refleksle saldırır).</item>
        /// <item>Kendi ağına takılmış böcek. Kendi ağındaki avları bitirmeden ekranın öbür ucundaki bir ağa gitmez.</item>
        /// <item>Başka ağlara takılıp avlanmaya hazır olan böceklerden en yakını.</item>
        /// </list>
        /// Hiçbiri yoksa null döner.
        /// </summary>
        public static BugEntity? FindPrey(SpiderEntity spider, WorldContext world)
        {
            Point position = spider.Position;
            SpiderWebEntity? homeWeb = spider.ActiveWeb;

            // Koşullar "static": dışarıdan değişken yakalamadıkları için her karede yeni nesne oluşturulmaz
            return Nearest(world.Bugs, position, homeWeb, static (bug, _) => bug.CurrentState.Status == BugLifecycleStatus.ActiveInWorld, EncounterRadiusSq)
                ?? (homeWeb == null ? null : Nearest(world.Bugs, position, homeWeb, static (bug, web) => IsTrappedIn(bug, web)))
                ?? Nearest(world.Bugs, position, homeWeb, static (bug, _) => bug.IsReadyToBeHunted);
        }

        /// <summary>
        /// Örümceğin yemek için gideceği ağı seçer: kendi ağının kilerinde yiyecek varsa orası,
        /// yoksa kilerinde yiyecek olan en yakın ağ. Hiçbir ağda yiyecek yoksa null döner.
        /// </summary>
        public static SpiderWebEntity? FindWebWithStoredFood(SpiderEntity spider, WorldContext world)
        {
            if (spider.ActiveWeb is { Pantry.HasStoredFood: true } homeWeb)
            {
                return homeWeb;
            }

            SpiderWebEntity? nearest = null;
            double bestDistSq = double.MaxValue;

            foreach (var web in world.Webs)
            {
                if (!web.Pantry.HasStoredFood) continue;

                double distSq = (web.Center - spider.Position).LengthSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = web;
                }
            }

            return nearest;
        }

        private static bool IsTrappedIn(BugEntity bug, SpiderWebEntity? web) =>
            bug.CurrentState.Status == BugLifecycleStatus.TrappedInWeb && bug.TrappedWeb == web;

        /// <summary>
        /// Koşula uyan böceklerden konuma en yakın olanı verir; en fazla <paramref name="maxDistanceSq"/> uzaklıkta (karesi).
        /// Örümceğin kendi ağı, koşula parametre olarak verilir.
        /// </summary>
        private static BugEntity? Nearest(
            IReadOnlyList<BugEntity> bugs,
            Point position,
            SpiderWebEntity? homeWeb,
            Func<BugEntity, SpiderWebEntity?, bool> matches,
            double maxDistanceSq = double.MaxValue)
        {
            BugEntity? nearest = null;
            double bestDistSq = double.MaxValue;

            for (int i = 0; i < bugs.Count; i++)
            {
                var bug = bugs[i];
                if (!matches(bug, homeWeb)) continue;

                double distSq = (bug.Position - position).LengthSquared;
                if (distSq <= maxDistanceSq && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = bug;
                }
            }

            return nearest;
        }
    }
}
