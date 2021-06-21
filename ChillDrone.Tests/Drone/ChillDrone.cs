using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chen.ChillDrone.Drone.Tests
{
    [TestClass]
    public class ModPlugin
    {
        [TestMethod]
        public void DebugCheck_Toggled_ReturnsFalse()
        {
            bool result = ChillDrone.DebugCheck();

            Assert.IsFalse(result);
        }
    }
}