using System;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Sadece örümceği, 2-Segmentli IK bacaklarını ve aktif ipek ipini seçili temaya (ISpiderTheme) göre çizer (SRP & OCP).
    /// </summary>
    public class SpiderRenderer : IRenderer<SpiderEntity>
    {
        private readonly Pen _activeSilkPen = DrawingResources.CreateFrozenPen(Color.FromArgb(220, 255, 255, 255), 1.25);
        private readonly Pen _draglinePen = DrawingResources.CreateFrozenPen(Color.FromArgb(75, 240, 248, 255), 0.8);
        private readonly SolidColorBrush _carriedCocoonBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(235, 245, 250, 255));

        private string _cachedThemeId = string.Empty;
        private Pen _legUpperPen = null!;
        private Pen _legLowerPen = null!;
        private SolidColorBrush _jointBrush = null!;
        private SolidColorBrush _abdomenBrush = null!;
        private SolidColorBrush _thoraxBrush = null!;
        private SolidColorBrush _markingBrush = null!;
        private SolidColorBrush _eyeBrush = null!;

        public SpiderRenderer()
        {
            UpdateThemeBrushes(SpiderThemeCatalog.All[0]);
        }

        public void Render(DrawingContext dc, SpiderEntity spider)
        {
            if (_cachedThemeId != spider.Theme.Id)
            {
                UpdateThemeBrushes(spider.Theme);
            }

            // 1. Örülmekte olan aktif ip veya güvenlik ipi
            if (spider.ActiveSilkLine is { } silk)
            {
                dc.DrawLine(_activeSilkPen, silk.Start, silk.End);
            }
            else if (spider.DraglineAnchor is { } anchor)
            {
                dc.DrawLine(_draglinePen, anchor, spider.GetSpinneretPosition());
            }

            // 2. Sabit uzunluklu 2-Kemikli Ters Kinematik (IK) Bacaklar
            foreach (var leg in spider.Legs)
            {
                Point hip = spider.LocalToWorld(leg.LocalAttach);
                LegJointPositions joints = leg.SolveJoints(hip);

                dc.DrawLine(_legUpperPen, joints.Hip, joints.Knee);
                dc.DrawLine(_legLowerPen, joints.Knee, joints.Foot);
                dc.DrawEllipse(_jointBrush, null, joints.Knee, 1.5, 1.5);
            }

            // 3. Gövde (Seviyeye göre çok hafif olgunlaşma ölçeği)
            double bodyScale = Math.Min(1.25, 1.0 + (spider.Level - 1) * 0.03);

            var bodyMatrix = Matrix.Identity;
            bodyMatrix.Scale(bodyScale, bodyScale);
            bodyMatrix.Rotate(spider.Angle * 180.0 / Math.PI);
            bodyMatrix.Translate(spider.Position.X, spider.Position.Y);
            var bodyTransform = new MatrixTransform(bodyMatrix);
            bodyTransform.Freeze();

            dc.PushTransform(bodyTransform);

            dc.DrawEllipse(_thoraxBrush, null, new Point(12, -2.6), 3.2, 1.6);
            dc.DrawEllipse(_thoraxBrush, null, new Point(12, 2.6), 3.2, 1.6);

            // Eğer örümcek ağzında (kıskaçlarında) kilere götürmek üzere bir koza taşıyorsa onu çiz
            if (spider.CarriedCocoonSize is { } cocoonSize)
            {
                dc.DrawEllipse(_carriedCocoonBrush, _activeSilkPen, new Point(16.5, 0), cocoonSize * 1.1, cocoonSize * 0.8);
            }

            dc.DrawEllipse(_abdomenBrush, null, new Point(-10, 0), 11.5, 9.2);
            dc.DrawEllipse(_markingBrush, null, new Point(-11, 0), 4.2, 2.5);
            dc.DrawEllipse(_markingBrush, null, new Point(-6.5, 0), 2.2, 1.8);

            dc.DrawEllipse(_thoraxBrush, null, new Point(4, 0), 7.2, 6.2);

            dc.DrawEllipse(_eyeBrush, null, new Point(9.2, -2.0), 1.2, 1.2);
            dc.DrawEllipse(_eyeBrush, null, new Point(9.2, 2.0), 1.2, 1.2);
            dc.DrawEllipse(_eyeBrush, null, new Point(8.0, -3.8), 0.9, 0.9);
            dc.DrawEllipse(_eyeBrush, null, new Point(8.0, 3.8), 0.9, 0.9);

            dc.Pop();
        }

        private void UpdateThemeBrushes(ISpiderTheme theme)
        {
            _cachedThemeId = theme.Id;
            _legUpperPen = DrawingResources.CreateFrozenPen(theme.LegUpperColor, 2.8);
            _legLowerPen = DrawingResources.CreateFrozenPen(theme.LegLowerColor, 1.9);
            _jointBrush = DrawingResources.CreateFrozenBrush(theme.JointColor);
            _abdomenBrush = DrawingResources.CreateFrozenBrush(theme.AbdomenColor);
            _thoraxBrush = DrawingResources.CreateFrozenBrush(theme.ThoraxColor);
            _markingBrush = DrawingResources.CreateFrozenBrush(theme.MarkingColor);
            _eyeBrush = DrawingResources.CreateFrozenBrush(theme.EyeColor);
        }
    }
}
