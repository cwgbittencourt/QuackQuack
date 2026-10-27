using NUnit.Framework;
using QuackQuack.Presentation.UI;
namespace QuackQuack.Tests.EditMode
{
    public class ResultsContractTests
    {
        [Test] public void ProductionResultsViewExists() => Assert.That(typeof(GameplayHud).Assembly.GetType("QuackQuack.Presentation.UI.ResultsView"), Is.Not.Null);
    }
}
