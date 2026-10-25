using NUnit.Framework;
using QuackQuack.Core.Shared;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class PlayerCommandTests
    {
        [Test]
        public void DiagonalInputIsClampedAndAnalogMagnitudePreserved()
        {
            Assert.That(new PlayerCommand(Vector2.one).Move.magnitude, Is.EqualTo(1).Within(0.0001));
            Assert.That(new PlayerCommand(new Vector2(0.3f, 0.4f)).Move.magnitude, Is.EqualTo(0.5f).Within(0.0001));
            Assert.That(new PlayerCommand(new Vector2(float.NaN, 0)).Move, Is.EqualTo(Vector2.zero));
        }
    }
}
