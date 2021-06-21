using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chen.ChillDrone.Tests
{
    [TestClass]
    public class ModPlugin
    {
        [TestMethod]
        public void DebugCheck_Toggled_ReturnsFalse()
        {
            bool result = MyModPluginPlugin.DebugCheck();

            Assert.IsFalse(result);
        }
    }
}