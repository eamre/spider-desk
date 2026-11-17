using SpiderDesk.Infrastructure.Platform;

namespace SpiderDesk.Tests.Infrastructure.Platform
{
    /// <summary>
    /// "Açılışta çalıştır" kaydı için verilen kararları doğrular. Asıl kural: kaydın var olması yetmez,
    /// çalışan exe'yi göstermesi gerekir.
    /// </summary>
    public class StartupEntryPolicyTests
    {
        private const string ThisExe = @"C:\Program Files\SpiderDesk\SpiderDesk.exe";
        private const string OldExe = @"D:\Eski Sürüm\bin\Debug\SpiderDesk.exe";

        [Fact]
        public void On_WithoutAnEntry_WritesOne()
        {
            Assert.Equal(StartupEntryAction.Write, StartupEntryPolicy.Decide(null, ThisExe, shouldRunOnStartup: true));
        }

        [Fact]
        public void On_WithAnEntryForThisExe_DoesNothing()
        {
            Assert.Equal(StartupEntryAction.None, StartupEntryPolicy.Decide($"\"{ThisExe}\"", ThisExe, shouldRunOnStartup: true));
        }

        [Fact]
        public void On_WithAnEntryForAnOldExe_RewritesIt()
        {
            Assert.Equal(StartupEntryAction.Write, StartupEntryPolicy.Decide($"\"{OldExe}\"", ThisExe, shouldRunOnStartup: true));
        }

        [Fact]
        public void On_WithoutKnowingOurOwnPath_DoesNothing()
        {
            Assert.Equal(StartupEntryAction.None, StartupEntryPolicy.Decide(null, null, shouldRunOnStartup: true));
            Assert.Equal(StartupEntryAction.None, StartupEntryPolicy.Decide($"\"{OldExe}\"", "", shouldRunOnStartup: true));
        }

        [Fact]
        public void Off_WithAnyEntry_DeletesIt()
        {
            Assert.Equal(StartupEntryAction.Delete, StartupEntryPolicy.Decide($"\"{ThisExe}\"", ThisExe, shouldRunOnStartup: false));
            Assert.Equal(StartupEntryAction.Delete, StartupEntryPolicy.Decide($"\"{OldExe}\"", ThisExe, shouldRunOnStartup: false));
        }

        [Fact]
        public void Off_WithoutAnEntry_DoesNothing()
        {
            Assert.Equal(StartupEntryAction.None, StartupEntryPolicy.Decide(null, ThisExe, shouldRunOnStartup: false));
        }

        [Theory]
        [InlineData("\"C:\\Program Files\\SpiderDesk\\SpiderDesk.exe\"")]
        [InlineData("C:\\Program Files\\SpiderDesk\\SpiderDesk.exe")]
        [InlineData("\"c:\\program files\\spiderdesk\\SPIDERDESK.EXE\"")]
        [InlineData("  \"C:\\Program Files\\SpiderDesk\\SpiderDesk.exe\"  ")]
        public void AnEntryForThisExe_IsRecognised_WithOrWithoutQuotesAndInAnyCase(string registeredValue)
        {
            Assert.True(StartupEntryPolicy.PointsTo(registeredValue, ThisExe));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\"D:\\Eski Sürüm\\bin\\Debug\\SpiderDesk.exe\"")]
        [InlineData("\"C:\\Program Files\\SpiderDesk\\SpiderDesk.exe.old\"")]
        public void OtherEntries_AreNotThisExe(string? registeredValue)
        {
            Assert.False(StartupEntryPolicy.PointsTo(registeredValue, ThisExe));
        }

        [Fact]
        public void TheWrittenEntry_IsInQuotes_SoFoldersWithSpacesWork()
        {
            Assert.Equal("\"C:\\Program Files\\SpiderDesk\\SpiderDesk.exe\"", StartupEntryPolicy.EntryFor(ThisExe));
        }
    }
}
