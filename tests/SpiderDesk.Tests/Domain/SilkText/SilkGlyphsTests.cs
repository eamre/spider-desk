using System.Linq;
using SpiderDesk.Domain.SilkText;

namespace SpiderDesk.Tests.Domain.SilkText
{
    public class SilkGlyphsTests
    {
        // Örümceğin yazabilmesi gereken bütün kelimeler; yeni bir kelime eklenirse buraya da eklenmeli
        public static TheoryData<string> Words => new()
        {
            GreetingPolicy.Morning,
            GreetingPolicy.Day,
            GreetingPolicy.Evening,
            GreetingPolicy.Night,
            "FOCUS",
            "BREAK"
        };

        [Theory]
        [MemberData(nameof(Words))]
        public void EveryLetterOfEveryWord_HasSilkStrokes(string word)
        {
            foreach (char letter in word.Where(char.IsLetter))
            {
                Assert.True(SilkGlyphs.Get(letter).Strokes.Count > 0, $"'{letter}' harfi tanımlı değil");
            }
        }

        [Theory]
        [MemberData(nameof(Words))]
        public void EveryLetter_StaysInsideItsBox(string word)
        {
            const double tolerance = 0.001;
            foreach (char letter in word.Where(char.IsLetter))
            {
                SilkGlyph glyph = SilkGlyphs.Get(letter);
                foreach (var point in glyph.Strokes.SelectMany(stroke => stroke))
                {
                    Assert.InRange(point.X, -tolerance, glyph.Width + tolerance);
                    Assert.InRange(point.Y, -tolerance, 1 + tolerance);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Words))]
        public void EveryStroke_HasAtLeastTwoDistinctPoints(string word)
        {
            foreach (char letter in word.Where(char.IsLetter))
            {
                foreach (var stroke in SilkGlyphs.Get(letter).Strokes)
                {
                    Assert.True(stroke.Count >= 2, $"'{letter}' harfinde tek noktalı bir çizgi var");
                    for (int i = 1; i < stroke.Count; i++)
                    {
                        Assert.True((stroke[i] - stroke[i - 1]).Length > 1e-6, $"'{letter}' harfinde sıfır uzunlukta tel var");
                    }
                }
            }
        }

        [Fact]
        public void LowercaseLetters_UseTheUppercaseShape()
        {
            Assert.Same(SilkGlyphs.Get('F'), SilkGlyphs.Get('f'));
        }

        [Theory]
        [InlineData(' ')]
        [InlineData('?')]
        [InlineData('7')]
        public void UnknownCharacters_AreWrittenAsSpace(char character)
        {
            SilkGlyph glyph = SilkGlyphs.Get(character);

            Assert.Empty(glyph.Strokes);
            Assert.True(glyph.Width > 0);
        }
    }
}
