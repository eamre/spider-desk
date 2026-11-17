using SpiderDesk.Domain.Greeting;

namespace SpiderDesk.Tests.Domain.Greeting
{
    public class GreetingPolicyTests
    {
        // Her selamın ilk ve son dakikası: sınırlardaki küçük bir hata (< yerine <=) burada hemen yakalanır
        [Theory]
        [InlineData("00:00", GreetingPolicy.Night)]
        [InlineData("04:59", GreetingPolicy.Night)]
        [InlineData("05:00", GreetingPolicy.Morning)]
        [InlineData("11:59", GreetingPolicy.Morning)]
        [InlineData("12:00", GreetingPolicy.Day)]
        [InlineData("17:59", GreetingPolicy.Day)]
        [InlineData("18:00", GreetingPolicy.Evening)]
        [InlineData("21:59", GreetingPolicy.Evening)]
        [InlineData("22:00", GreetingPolicy.Night)]
        [InlineData("23:59", GreetingPolicy.Night)]
        public void ChooseGreeting_PicksTheGreetingForTheTimeOfDay(string time, string expected)
        {
            Assert.Equal(expected, GreetingPolicy.ChooseGreeting(TimeSpan.Parse(time)));
        }
    }
}
