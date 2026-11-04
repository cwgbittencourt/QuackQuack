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
        [Test]
        public void ExistingConstructorArgumentsKeepTheirMeaningAndRelicsDefaultsOff()
        {
            var command = new PlayerCommand(Vector2.right, true, true, InputSource.Touch);
            Assert.That(command.Source, Is.EqualTo(InputSource.Touch));
            Assert.That(command.QuackPressed && command.PausePressed, Is.True); Assert.That(command.RelicsPressed, Is.False);
        }
        [Test]
        public void RelicsIsIndependentOfPauseAndDoesNotBypassMoveValidation()
        {
            var command = new PlayerCommand(new Vector2(float.NaN, 0), source: InputSource.Gamepad, relicsPressed: true);
            Assert.That(command.RelicsPressed, Is.True); Assert.That(command.PausePressed || command.QuackPressed, Is.False);
            Assert.That(command.Move, Is.EqualTo(Vector2.zero)); Assert.That(command.Source, Is.EqualTo(InputSource.Gamepad));
        }    }
}
