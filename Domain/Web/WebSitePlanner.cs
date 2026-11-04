using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Yeni örülecek ağın tipini (%50 Köşe Yelpaze / %50 Dairesel Orta) ve mevcut ağlarla
    /// kesişmeyen en uygun ekran konumunu belirler.
    /// </summary>
    public static class WebSitePlanner
    {
        private readonly record struct WebSiteCandidate(
            Point Vertex,
            double Radius,
            bool IsCornerFan,
            double StartAngle,
            double EndAngle);

        public static SpiderWebEntity CreateNextWeb(WorldContext world)
        {
            WebSiteCandidate candidate = PickBestWebSite(world);
            SpiderWebEntity web;

            if (candidate.IsCornerFan)
            {
                double midAngle = (candidate.StartAngle + candidate.EndAngle) * 0.5;
                Point restingHub = WebBlueprintGenerator.PolarToCartesian(candidate.Vertex, midAngle, 58.0);

                Queue<WebBuildStep> plan = WebBlueprintGenerator.CreateCornerFanPlan(
                    candidate.Vertex,
                    restingHub,
                    candidate.Radius,
                    candidate.StartAngle,
                    candidate.EndAngle,
                    world.Random
                );

                web = new SpiderWebEntity(
                    candidate.Vertex,
                    restingHub,
                    candidate.Radius,
                    plan,
                    candidate.StartAngle,
                    candidate.EndAngle
                );
            }
            else
            {
                Queue<WebBuildStep> plan = WebBlueprintGenerator.CreateOrbPlan(candidate.Vertex, candidate.Radius, world.Random);
                web = new SpiderWebEntity(candidate.Vertex, candidate.Vertex, candidate.Radius, plan);
            }

            web.Droplets.SpawnRate = world.DewSpawnRate;
            world.Webs.Add(web);

            if (world.Webs.Count > 6)
            {
                world.Webs.RemoveAt(0);
            }

            return web;
        }

        private static WebSiteCandidate PickBestWebSite(WorldContext world)
        {
            var rng = world.Random;
            double w = world.ScreenWidth;
            double h = world.ScreenHeight;

            const double cornerMargin = 3.0;
            const double taskbarPad = 48.0;

            Point[] cornerVertices =
            {
                new(cornerMargin, cornerMargin),
                new(w - cornerMargin, cornerMargin),
                new(w - cornerMargin, h - taskbarPad),
                new(cornerMargin, h - taskbarPad)
            };

            double cornerRadius = 215.0 + rng.NextDouble() * 30.0;
            WebSiteCandidate[] allCorners =
            {
                new(cornerVertices[0], cornerRadius, true, 0.0, Math.PI * 0.5),
                new(cornerVertices[1], cornerRadius, true, Math.PI * 0.5, Math.PI),
                new(cornerVertices[2], cornerRadius, true, Math.PI, Math.PI * 1.5),
                new(cornerVertices[3], cornerRadius, true, Math.PI * 1.5, Math.PI * 2.0)
            };

            var validCorners = allCorners
                .Where(c => !OverlapsAnyExistingWeb(world, c))
                .OrderBy(_ => rng.Next())
                .ToList();

            WebSiteCandidate? bestOrbCandidate = FindNonOverlappingOrbCandidate(world, cornerVertices, rng);

            bool preferCorner;
            if (world.Webs.Count > 0 && world.Webs.All(existing => existing.IsCornerFan))
            {
                preferCorner = false;
            }
            else if (world.Webs.Count > 0 && world.Webs.All(existing => !existing.IsCornerFan))
            {
                preferCorner = true;
            }
            else
            {
                preferCorner = rng.NextDouble() < 0.50;
            }

            if (preferCorner)
            {
                if (validCorners.Count > 0) return validCorners[0];
                if (bestOrbCandidate is { } orbFallback) return orbFallback;
            }
            else
            {
                if (bestOrbCandidate is { } orb) return orb;
                if (validCorners.Count > 0) return validCorners[0];
            }

            return allCorners[rng.Next(allCorners.Length)];
        }

        private static WebSiteCandidate? FindNonOverlappingOrbCandidate(
            WorldContext world,
            Point[] cornerVertices,
            Random rng)
        {
            double w = world.ScreenWidth;
            double h = world.ScreenHeight;

            double[] radiusAttempts = { 105.0 + rng.NextDouble() * 25.0, 88.0 + rng.NextDouble() * 14.0 };

            foreach (double orbRadius in radiusAttempts)
            {
                double marginX = orbRadius * 1.25 + 25.0;
                double marginY = orbRadius * 1.25 + 55.0;

                WebSiteCandidate? best = null;
                double bestClearance = -1;

                for (int i = 0; i < 45; i++)
                {
                    Point pt = new Point(
                        marginX + rng.NextDouble() * Math.Max(80.0, w - marginX * 2),
                        marginY + rng.NextDouble() * Math.Max(80.0, h - marginY * 2)
                    );

                    bool tooCloseToAnyCorner = false;
                    double minCornerDist = 250.0 + orbRadius * 1.15;
                    for (int c = 0; c < cornerVertices.Length; c++)
                    {
                        if ((cornerVertices[c] - pt).Length < minCornerDist)
                        {
                            tooCloseToAnyCorner = true;
                            break;
                        }
                    }

                    if (tooCloseToAnyCorner) continue;

                    var candidate = new WebSiteCandidate(pt, orbRadius, false, 0.0, Math.PI * 2.0);
                    if (OverlapsAnyExistingWeb(world, candidate)) continue;

                    double minClearance = double.MaxValue;
                    for (int e = 0; e < world.Webs.Count; e++)
                    {
                        double d = (world.Webs[e].HubPosition - pt).Length;
                        if (d < minClearance) minClearance = d;
                    }

                    if (minClearance > bestClearance)
                    {
                        bestClearance = minClearance;
                        best = candidate;
                    }
                }

                if (best != null)
                {
                    return best;
                }
            }

            return null;
        }

        private static bool OverlapsAnyExistingWeb(WorldContext world, WebSiteCandidate candidate)
        {
            const double safetyGap = 38.0;
            double candidateExtent = candidate.IsCornerFan
                ? candidate.Radius * 1.18
                : candidate.Radius * 1.15;

            for (int i = 0; i < world.Webs.Count; i++)
            {
                var existing = world.Webs[i];
                double existingExtent = existing.IsCornerFan
                    ? existing.Radius * 1.18
                    : existing.Radius * 1.15;

                double centerDist = (existing.Center - candidate.Vertex).Length;
                if (centerDist < existingExtent + candidateExtent + safetyGap)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
