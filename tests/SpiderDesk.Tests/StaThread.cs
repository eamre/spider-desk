using System.Runtime.ExceptionServices;

namespace SpiderDesk.Tests
{
    /// <summary>
    /// WPF pencereleri ve WinForms menüleri yalnızca STA iş parçacığında çalışır; xUnit testleri ise MTA'da çalışır.
    /// Bu yardımcı testin gövdesini ayrı bir STA iş parçacığında çalıştırır ve oradaki hatayı teste geri taşır.
    /// </summary>
    internal static class StaThread
    {
        public static void Run(Action test)
        {
            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    test();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            failure?.Throw();
        }
    }
}
