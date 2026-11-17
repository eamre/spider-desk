using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Web
{
    /// <summary>
    /// Ağın bugünkü davranışını kayıt altına alır: örülme, kopma, tamir ve böcek yakalama.
    /// Ağın iç yapısı değişse bile bu testler aynı sonuçları vermelidir.
    /// </summary>
    public class SpiderWebEntityTests
    {
        private static readonly Point Center = new(100, 100);
        private const double Radius = 50;

        // Merkezden geçen bir bağlantı teli, merkezden geçen iki normal tel ve ipek bırakmayan bir yürüme adımı
        private static readonly WebBuildStep Anchor = new(new Point(50, 100), new Point(150, 100), DrawSilk: true, IsAnchor: true);
        private static readonly WebBuildStep Vertical = new(new Point(100, 50), new Point(100, 150), DrawSilk: true, IsAnchor: false);
        private static readonly WebBuildStep Walk = new(new Point(100, 150), new Point(60, 60), DrawSilk: false, IsAnchor: false);
        private static readonly WebBuildStep Diagonal = new(new Point(60, 60), new Point(140, 140), DrawSilk: true, IsAnchor: false);

        private static SpiderWebEntity CreateWeb(params WebBuildStep[] steps) =>
            new(Center, Center, Radius, new Queue<WebBuildStep>(steps));

        private static SpiderWebEntity CreateFinishedWeb()
        {
            var web = CreateWeb(Anchor, Vertical, Walk, Diagonal);
            BuildAllSteps(web);
            return web;
        }

        // Ağ örme durumunun (SpinningWebState) yaptığını taklit eder: adımları sırayla bitirir
        private static void BuildAllSteps(SpiderWebEntity web)
        {
            while (web.BuildQueue.Count > 0)
            {
                WebBuildStep step = web.BuildQueue.Dequeue();
                if (step.DrawSilk)
                {
                    web.CompletedSegments.Add(new WebSegment(step.From, step.To, step.IsAnchor));
                }
            }
        }

        // Tamir durumunun (RepairingWebState) yaptığını taklit eder: kopuk telleri sırayla yerine takar
        private static void RepairAll(SpiderWebEntity web)
        {
            while (web.DamagedSegments.Count > 0)
            {
                web.CompletedSegments.Add(web.DamagedSegments.Dequeue());
            }
        }

        // ---------- Örülme ----------

        [Fact]
        public void NewWebWithPlan_IsNotCompleted()
        {
            var web = CreateWeb(Anchor, Vertical);

            Assert.False(web.IsCompleted);
            Assert.Empty(web.CompletedSegments);
        }

        [Fact]
        public void StepInProgress_KeepsWebIncomplete_EvenWhenNoStepsAreLeft()
        {
            var web = CreateWeb();
            web.IsStepInProgress = true;

            Assert.False(web.IsCompleted);
        }

        [Fact]
        public void AfterAllStepsAreBuilt_WebIsCompleted_AndOnlySilkStepsBecomeThreads()
        {
            var web = CreateFinishedWeb();

            Assert.True(web.IsCompleted);
            Assert.Equal(3, web.CompletedSegments.Count);
            Assert.False(web.HasDamage);
        }

        [Fact]
        public void OrbPlan_BuildsIntoACompletedWebWithAnchors()
        {
            var plan = WebBlueprintGenerator.CreateOrbPlan(Center, Radius, new Random(42));
            var web = new SpiderWebEntity(Center, Center, Radius, plan);

            BuildAllSteps(web);

            Assert.True(web.IsCompleted);
            Assert.Contains(web.CompletedSegments, s => s.IsAnchor);
            Assert.Contains(web.CompletedSegments, s => !s.IsAnchor);
        }

        // ---------- Kopma ----------

        [Fact]
        public void DamageNear_OnUnfinishedWeb_DoesNothing()
        {
            var web = CreateWeb(Anchor, Vertical);
            web.CompletedSegments.Add(new WebSegment(Vertical.From, Vertical.To, IsAnchor: false));

            web.DamageNear(Center, damageRadius: 10, maxBrokenSegments: 5, new Random(1));

            Assert.Single(web.CompletedSegments);
            Assert.False(web.HasDamage);
        }

        [Fact]
        public void DamageNear_BreaksNearbyThreads_AndQueuesThemForRepair()
        {
            var web = CreateFinishedWeb();

            web.DamageNear(Center, damageRadius: 10, maxBrokenSegments: 5, new Random(1));

            Assert.Single(web.CompletedSegments);
            Assert.Equal(2, web.DamagedSegments.Count);
            Assert.True(web.HasDamage);
            Assert.True(web.IsVibrating);
        }

        [Fact]
        public void DamageNear_NeverBreaksAnchorThreads()
        {
            var web = CreateFinishedWeb();

            web.DamageNear(Center, damageRadius: 10, maxBrokenSegments: 5, new Random(1));

            Assert.All(web.CompletedSegments, s => Assert.True(s.IsAnchor));
            Assert.DoesNotContain(web.DamagedSegments, s => s.IsAnchor);
        }

        [Fact]
        public void DamageNear_BreaksNoMoreThanTheLimit()
        {
            var web = CreateFinishedWeb();

            web.DamageNear(Center, damageRadius: 10, maxBrokenSegments: 1, new Random(1));

            Assert.Single(web.DamagedSegments);
            Assert.Equal(2, web.CompletedSegments.Count);
        }

        [Fact]
        public void DamageNear_FarFromAnyThread_BreaksNothing()
        {
            var web = CreateFinishedWeb();

            web.DamageNear(new Point(300, 300), damageRadius: 10, maxBrokenSegments: 5, new Random(1));

            Assert.Equal(3, web.CompletedSegments.Count);
            Assert.False(web.HasDamage);
            Assert.False(web.IsVibrating);
        }

        // ---------- Tamir ----------

        [Fact]
        public void RepairingEveryBrokenThread_RestoresTheWeb()
        {
            var web = CreateFinishedWeb();
            var original = web.CompletedSegments.ToHashSet();

            web.DamageNear(Center, damageRadius: 10, maxBrokenSegments: 5, new Random(1));
            RepairAll(web);

            Assert.False(web.HasDamage);
            Assert.Equal(original, web.CompletedSegments.ToHashSet());
        }

        // ---------- Böcek yakalama ----------

        [Fact]
        public void CanCatchPoint_OnUnfinishedWeb_IsFalse()
        {
            var web = CreateWeb(Vertical);

            Assert.False(web.CanCatchPoint(new Point(100, 120)));
        }

        [Fact]
        public void CanCatchPoint_OnAThread_IsTrue()
        {
            var web = CreateFinishedWeb();

            Assert.True(web.CanCatchPoint(new Point(100, 120)));
        }

        [Fact]
        public void CanCatchPoint_OutsideTheWeb_IsFalse()
        {
            var web = CreateFinishedWeb();

            Assert.False(web.CanCatchPoint(new Point(100, 200)));
        }
    }
}
