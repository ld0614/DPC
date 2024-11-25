using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ServiceIntegrationTests
{
    [TestClass]
    public class AssemblyTests
    {
        [AssemblyInitialize]
        public static void FirstRunInitialize(TestContext context)
        {
            HelperFunctions.StartETWMonitor(context);
        }

        [AssemblyCleanup]
        public static void TestingFinishedCleanup()
        {
            try
            {
                HelperFunctions.StopETWMonitor();
            }
            catch
            {
                //Silently handle failures in ETW Shutdown
            }
        }
    }
}
