using System.IO;
using SpiderDesk.Domain.Reminders;
using SpiderDesk.Infrastructure.Storage;

namespace SpiderDesk.Tests.Infrastructure.Storage
{
    public class JsonReminderRepositoryTests : IDisposable
    {
        private readonly TempFolder _folder = new();
        private readonly string _path;

        public JsonReminderRepositoryTests()
        {
            _path = _folder.File("reminders.json");
        }

        public void Dispose() => _folder.Dispose();

        [Fact]
        public void Load_WhenThereIsNoFileYet_GivesAnEmptyList()
        {
            Assert.Empty(new JsonReminderRepository(_path).Load());
        }

        [Fact]
        public void SavedReminders_ComeBackTheSame_AfterTheAppRestarts()
        {
            var reminders = new[]
            {
                Reminder.Create("Annemi ara", new DateTime(2026, 1, 1, 14, 30, 0)),
                Reminder.Create("Saat 15:00'te toplantı, sunumu unutma", DateTime.Now.AddMinutes(15))
            };

            Assert.True(new JsonReminderRepository(_path).Save(reminders));
            // Yeni bir depo nesnesi, uygulamanın kapanıp yeniden açılması gibidir
            var loaded = new JsonReminderRepository(_path).Load();

            Assert.Equal(reminders, loaded);
        }

        [Fact]
        public void Save_ReplacesTheEarlierList()
        {
            var repository = new JsonReminderRepository(_path);
            var first = Reminder.Create("Su iç", new DateTime(2026, 1, 1, 10, 0, 0));
            var second = Reminder.Create("Esne", new DateTime(2026, 1, 1, 11, 0, 0));
            repository.Save(new[] { first, second });

            repository.Save(new[] { second });

            Assert.Equal(new[] { second }, repository.Load());
        }

        [Fact]
        public void Save_LeavesNoTemporaryFileBehind()
        {
            new JsonReminderRepository(_path).Save(new[] { Reminder.Create("Su iç", DateTime.Now) });

            Assert.Equal(new[] { "reminders.json" }, Directory.GetFiles(_folder.Path).Select(Path.GetFileName));
        }

        [Fact]
        public void Load_WhenTheFileIsBroken_GivesAnEmptyList_InsteadOfCrashing()
        {
            File.WriteAllText(_path, "{ bu json değil");

            Assert.Empty(new JsonReminderRepository(_path).Load());
        }

        [Fact]
        public void Load_SkipsEntriesWithoutAValidText()
        {
            File.WriteAllText(_path, """
                [
                  { "Id": "6f0c1a2b-0000-0000-0000-000000000001", "Text": "Annemi ara", "DueAt": "2026-01-01T14:30:00" },
                  { "Id": "6f0c1a2b-0000-0000-0000-000000000002", "Text": "   ", "DueAt": "2026-01-01T15:00:00" },
                  { "Id": "6f0c1a2b-0000-0000-0000-000000000003", "DueAt": "2026-01-01T16:00:00" },
                  null
                ]
                """);

            var loaded = new JsonReminderRepository(_path).Load();

            Assert.Equal("Annemi ara", Assert.Single(loaded).Text);
        }
    }
}
