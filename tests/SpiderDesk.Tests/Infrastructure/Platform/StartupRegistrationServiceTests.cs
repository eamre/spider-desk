using Microsoft.Win32;
using SpiderDesk.Infrastructure.Platform;

namespace SpiderDesk.Tests.Infrastructure.Platform
{
    /// <summary>
    /// Başlangıç kaydının kayıt defterine gerçekten doğru yazıldığını doğrular.
    /// Windows'un gerçek "açılışta çalıştır" listesine dokunulmaz: her test kendi geçici anahtarını kullanır ve sonunda siler.
    /// </summary>
    public sealed class StartupRegistrationServiceTests : IDisposable
    {
        private const string TestsRoot = @"Software\SpiderDesk.Tests";
        private const string EntryName = "SpiderDesk";
        private const string ThisExe = @"C:\Program Files\SpiderDesk\SpiderDesk.exe";
        private const string OldExe = @"D:\Eski Sürüm\bin\Debug\SpiderDesk.exe";

        private readonly string _runKeyPath = $@"{TestsRoot}\Run-{Guid.NewGuid():N}";
        private readonly StartupRegistrationService _service;

        public StartupRegistrationServiceTests()
        {
            _service = new StartupRegistrationService(_runKeyPath, EntryName, () => ThisExe);
        }

        public void Dispose()
        {
            Registry.CurrentUser.DeleteSubKeyTree(_runKeyPath, throwOnMissingSubKey: false);
            using RegistryKey? root = Registry.CurrentUser.OpenSubKey(TestsRoot, writable: true);
            if (root != null && root.SubKeyCount == 0 && root.ValueCount == 0)
            {
                Registry.CurrentUser.DeleteSubKey(TestsRoot, throwOnMissingSubKey: false);
            }
        }

        private string? Entry()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_runKeyPath);
            return key?.GetValue(EntryName) as string;
        }

        private void PutEntry(string value)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(_runKeyPath);
            key.SetValue(EntryName, value);
        }

        [Fact]
        public void TurningOn_WritesThisExe_InQuotes()
        {
            _service.SetEnabled(true);

            Assert.Equal($"\"{ThisExe}\"", Entry());
            Assert.True(_service.IsEnabled());
        }

        [Fact]
        public void TurningOn_WhenTheEntryPointsToAnOldExe_FixesIt()
        {
            PutEntry($"\"{OldExe}\"");
            Assert.False(_service.IsEnabled());

            _service.SetEnabled(true);

            Assert.Equal($"\"{ThisExe}\"", Entry());
            Assert.True(_service.IsEnabled());
        }

        [Fact]
        public void TurningOn_WhenTheEntryIsAlreadyRight_LeavesItAsItIs()
        {
            // Aynı exe, farklı yazılmış: yeniden yazılsaydı tırnaklı ve büyük harfli olurdu
            string sameExeWrittenDifferently = ThisExe.ToLowerInvariant();
            PutEntry(sameExeWrittenDifferently);

            _service.SetEnabled(true);

            Assert.Equal(sameExeWrittenDifferently, Entry());
        }

        [Fact]
        public void TurningOff_DeletesTheEntry_EvenIfItPointsToAnotherExe()
        {
            PutEntry($"\"{OldExe}\"");

            _service.SetEnabled(false);

            Assert.Null(Entry());
            Assert.False(_service.IsEnabled());
        }

        [Fact]
        public void TurningOff_WithoutAnEntry_DoesNotFail()
        {
            _service.SetEnabled(false);

            Assert.Null(Entry());
            Assert.Null(_service.LastError);
        }

        [Fact]
        public void WithoutKnowingItsOwnPath_TurningOnWritesNothing()
        {
            var service = new StartupRegistrationService(_runKeyPath, EntryName, () => null);

            service.SetEnabled(true);

            Assert.Null(Entry());
        }
    }
}
