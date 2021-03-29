using System;
using System.Collections.Generic;
using System.Text;
using ApsCalc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ApsCalcTests
{
    [TestClass]
    public class ShellCalcUnitTests
    {
        /// <summary>
        /// Tests the output of the ModuleCount IEnumerator
        /// </summary>
        [TestMethod]
        public void TestEnumOutput()
        {
            float[] testModCounts = new float[] { 16f, 0, 0, 0, 0 };
            int[] testVarModIndices = new int[] { 1, 2 };
            List<int> testHeadList = new List<int> { 7 };
            List<ModuleCount> moduleCounts = new List<ModuleCount>();
            ModuleCount expectedFirstCount = new ModuleCount { Gauge = 18f, HeadIndex = 7, Var0Count = 0, Var1Count = 0, GPCount = 0, RGCount = 0 };
            ModuleCount expectedLastCount = new ModuleCount { Gauge = 18f, HeadIndex = 7, Var0Count = 2, Var1Count = 0, GPCount = 0, RGCount = 0 };

            ShellCalc testCalc = new ShellCalc
                (
                18f,
                18f,
                testHeadList,
                Module.BaseBleeder,
                testModCounts,
                18,
                testVarModIndices,
                0,
                18,
                10000f,
                0,
                0,
                20,
                0
                );

            foreach (ModuleCount counts in testCalc.GetModuleCounts())
            {
                Console.WriteLine("Var0Count: " + counts.Var0Count);
                Console.WriteLine("Var1Count: " + counts.Var1Count);
                Console.WriteLine("GPCount: " + counts.GPCount);
                Console.WriteLine("RGCount: " + counts.RGCount);
                Console.WriteLine("\n");
                moduleCounts.Add(counts);
            }

            Assert.AreEqual(expectedFirstCount, moduleCounts.First());
            Assert.AreEqual(expectedLastCount, moduleCounts.Last());
        }


        /// <summary>
        /// Verifies the TopDpsShells list starts with all DPS values at 0
        /// </summary>
        [TestMethod]
        public void TopDps_Shells_Start_At_0()
        {
            // None of the test stats should matter for this test, but are needed in order to initialize the class
            float[] testModCounts = new float[] { 16f, 0, 0, 0, 0 };
            int[] testVarModIndices = new int[] { 1, 2 };
            List<int> testHeadList = new List<int> { 7 };
            ShellCalc testCalc = new ShellCalc
                (
                18, 
                18, 
                testHeadList, 
                Module.BaseBleeder, 
                testModCounts, 
                18f, 
                testVarModIndices, 
                0, 
                18, 
                10000f, 
                0, 
                0, 
                20, 
                0
                );

            foreach (KeyValuePair<string, Shell> entry in testCalc.TopDpsShells)
            {
                Assert.IsNull(entry.Value.HeadModule);
                Assert.IsNull(entry.Value.BaseModule);
                Assert.AreEqual(entry.Value.Velocity, 0);
            }
        }
    }
}
