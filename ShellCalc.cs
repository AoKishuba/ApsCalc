using System;
using System.Collections.Generic;
using System.Text;

namespace ApsCalc
{
    public struct ModuleCount
    {
        public float Gauge;
        public int HeadIndex;
        public float Var0Count;
        public float Var1Count;
        public float GPCount;
        public float RGCount;
    }

    public class ShellCalc
    {
        /// <summary>
        /// Takes shell parameters and calculates performance of shell permutations.
        /// </summary>
        /// <param name="minGauge">Min desired gauge in mm</param>
        /// <param name="maxGauge">Max desired gauge in mm</param>
        /// <param name="headList">List of module indices for every module to be used as the head</param>
        /// <param name="baseModule">The special base module, if any</param>
        /// <param name="fixedModuleCounts">An array of integers representing the number of shells at that index in the module list</param>
        /// <param name="fixedModuleTotal">Minimum number of modules on every shell</param>
        /// <param name="variableModuleIndices">Module indices of the modules to be used in varying numbers in testing</param>
        /// <param name="maxGPInput">Max desired number of gunpowder casings</param>
        /// <param name="maxRGInput">Max desired number of railgun casings</param>
        /// <param name="maxShellLengthInput">Max desired shell length in mm</param>
        /// <param name="maxDrawInput">Max desired rail draw</param>
        /// <param name="minVelocityInput">Min required velocity</param>
        /// <param name="targetAC">Armor class of the target for kinetic damage calculations</param>
        /// <param name="damageType">0 for kinetic, 1 for chemical</param>
        public ShellCalc(
            float minGauge,
            float maxGauge,
            List<int> headList,
            Module baseModule,
            float[] fixedModuleCounts,
            float fixedModuleTotal,
            int[] variableModuleIndices,
            float maxGPInput,
            float maxRGInput,
            float maxShellLengthInput,
            float maxDrawInput,
            float minVelocityInput,
            float targetAC,
            float damageType)
        {
            MinGauge = minGauge;
            MaxGauge = maxGauge;
            HeadList = headList;
            BaseModule = baseModule;
            FixedModuleCounts = fixedModuleCounts;
            FixedModuleTotal = fixedModuleTotal;
            VariableModuleIndices = variableModuleIndices;
            MaxGPInput = maxGPInput;
            MaxRGInput = maxRGInput;
            MaxShellLength = maxShellLengthInput;
            MaxDrawInput = maxDrawInput;
            MinVelocityInput = minVelocityInput;
            TargetAC = targetAC;
            DamageType = damageType;
        }

        public float MinGauge { get; }
        public float MaxGauge { get; }
        public List<int> HeadList { get; }
        public Module BaseModule { get; }
        public float[] FixedModuleCounts { get; }
        public float FixedModuleTotal { get; }
        public int[] VariableModuleIndices { get; }
        public float MaxGPInput { get; }
        public float MaxRGInput { get; }
        public float MaxShellLength { get; }
        public float MaxDrawInput { get; }
        public float MinVelocityInput { get; }
        public float TargetAC { get; }
        public float DamageType { get; }

        // Testing data
        public float TestComparisons { get; set; } = 0;
        public float TestRejectLength { get; set; } = 0;
        public float TestRejectVelocity { get; set; } = 0;
        public float TestTotal { get; set; } = 0;

        // Store top-DPS shells by loader length
        public Shell TopDps1000 { get; set; } = new Shell();
        public Shell TopDpsBelt { get; set; } = new Shell();
        public Shell TopDps2000 { get; set; } = new Shell();
        public Shell TopDps4000 { get; set; } = new Shell();
        public Shell TopDps6000 { get; set; } = new Shell();
        public Shell TopDps8000 { get; set; } = new Shell();

        public Dictionary<string, Shell> TopDpsShells { get; set; } = new Dictionary<string, Shell>();

        /// <summary>
        /// The iterable generator for shells.  Generates all shell possible permutations of shell within the given parameters.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ModuleCount> GetModuleCounts()
        {
            float var0Max = 20f - FixedModuleTotal;
            float var1Max;
            float gpMax;
            float rgMax;

            foreach (int index in HeadList)
            {
                for (float gauge = MinGauge; gauge <= MaxGauge; gauge++)
                {
                    for (float var0Count = 0; var0Count <= var0Max; var0Count++)
                    {
                        if (VariableModuleIndices[0] == VariableModuleIndices[1])
                        {
                            var1Max = 0; // No need to add duplicates
                        }
                        else
                        {
                            var1Max = 20f - (FixedModuleTotal + var0Count);
                        }

                        for (float var1Count = 0; var1Count <= var1Max; var1Count++)
                        {
                            gpMax = Math.Min(20f - (FixedModuleTotal + var0Count + var1Count), MaxGPInput);

                            for (float gpCount = 0; gpCount <= gpMax; gpCount += 0.01f)
                            {
                                rgMax = Math.Min(20f - (FixedModuleTotal + var0Count + var1Count + gpCount), MaxRGInput);

                                for (float rgCount = 0; rgCount <= rgMax; rgCount++)
                                {
                                    yield return new ModuleCount
                                    {
                                        Gauge = gauge,
                                        HeadIndex = index,
                                        Var0Count = var0Count,
                                        Var1Count = var1Count,
                                        GPCount = gpCount,
                                        RGCount = rgCount
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The iterable for generating rail draw numbers.
        /// </summary>
        /// <param name="MaxDraw"></param>
        /// <returns></returns>
        public IEnumerable<float> GetRailDraw(float MaxDraw)
        {
            float maxDraw = Math.Min(MaxDraw, MaxDrawInput);

            for (float draw = 0; draw <= maxDraw; draw++)
            {
                yield return draw;
            }
        }


        /// <summary>
        /// Adds the current top-performing shells to the TopDpsShells list
        /// </summary>
        public void GetTopShells()
        {
            if (TopDpsBelt.KineticDPS > 0 || TopDpsBelt.ChemDPS > 0)
            {
                TopDpsShells.Add("1 m (belt)", TopDpsBelt);
            }

            if (TopDps1000.KineticDPS > 0 || TopDps1000.ChemDPS > 0)
            {
                TopDpsShells.Add("1 m", TopDps1000);
            }

            if (TopDps2000.KineticDPS > 0 || TopDps2000.ChemDPS > 0)
            {
                TopDpsShells.Add("2 m", TopDps2000);
            }

            if (TopDps4000.KineticDPS > 0 || TopDps4000.ChemDPS > 0)
            {
                TopDpsShells.Add("4 m", TopDps4000);
            }

            if (TopDps6000.KineticDPS > 0 || TopDps6000.ChemDPS > 0)
            {
                TopDpsShells.Add("6 m", TopDps6000);
            }

            if (TopDps8000.KineticDPS > 0 || TopDps8000.ChemDPS > 0)
            {
                TopDpsShells.Add("8 m", TopDps8000);
            }
        }


        /// <summary>
        /// The main test body.  Iterates over the IEnumerables to compare every permutation within the given parameters, then stores the results
        /// </summary>
        public void ShellTest()
        {
            float lastGauge = MinGauge;

            foreach (ModuleCount counts in GetModuleCounts())
            {
                if (counts.Gauge != lastGauge)
                {
                    Console.WriteLine("\nTesting " + Module.AllModules[counts.HeadIndex].Name + " " + counts.Gauge + " mm.  Max " + MaxGauge + " mm.");
                    lastGauge = counts.Gauge;
                }
                Shell ShellUnderTestingSetup = new Shell();
                ShellUnderTestingSetup.HeadModule = Module.AllModules[counts.HeadIndex];
                ShellUnderTestingSetup.BaseModule = BaseModule;
                FixedModuleCounts.CopyTo(ShellUnderTestingSetup.BodyModuleCounts, 0);

                ShellUnderTestingSetup.Gauge = counts.Gauge;
                ShellUnderTestingSetup.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                ShellUnderTestingSetup.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                ShellUnderTestingSetup.GPCasingCount = counts.GPCount;
                ShellUnderTestingSetup.RGCasingCount = counts.RGCount;

                ShellUnderTestingSetup.CalculateLengths();

                if (ShellUnderTestingSetup.TotalLength <= MaxShellLength)
                {
                    ShellUnderTestingSetup.CalculateMaxDraw();
                    float maxDraw = Math.Min(MaxDrawInput, ShellUnderTestingSetup.MaxDraw);
                    foreach (float draw in GetRailDraw(ShellUnderTestingSetup.MaxDraw))
                    {
                        // Reset shell
                        Shell ShellUnderTesting = new Shell();
                        ShellUnderTesting.HeadModule = Module.AllModules[counts.HeadIndex];
                        ShellUnderTesting.BaseModule = BaseModule;
                        FixedModuleCounts.CopyTo(ShellUnderTesting.BodyModuleCounts, 0);

                        ShellUnderTesting.Gauge = counts.Gauge;
                        ShellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                        ShellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                        ShellUnderTesting.GPCasingCount = counts.GPCount;
                        ShellUnderTesting.RGCasingCount = counts.RGCount;
                        ShellUnderTesting.CalculateLengths();
                        ShellUnderTesting.CalculateGPRecoil();
                        ShellUnderTesting.CalculateModifiers();

                        ShellUnderTesting.RailDraw = draw;
                        ShellUnderTesting.CalculateVelocity();
                        if (ShellUnderTesting.Velocity >= MinVelocityInput)
                        {
                            TestComparisons++;
                            ShellUnderTesting.CalculateReloadTime();
                            ShellUnderTesting.CalculateVolume();

                            if (DamageType == 0) // Kinetic
                            {
                                ShellUnderTesting.CalculateKineticDamage();
                                ShellUnderTesting.CalculateAP();
                                ShellUnderTesting.CalculateKineticDPS(TargetAC);

                                if (ShellUnderTesting.TotalLength <= 1000f)
                                {
                                    if (ShellUnderTesting.KineticDPSPerVolume > TopDps1000.KineticDPSPerVolume)
                                    {
                                        TopDps1000 = ShellUnderTesting;
                                    }
                                    if (ShellUnderTesting.KineticDPSPerVolumeBelt > TopDpsBelt.KineticDPSPerVolumeBelt)
                                    {
                                        TopDpsBelt = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 2000f)
                                {
                                    if (ShellUnderTesting.KineticDPSPerVolume > TopDps2000.KineticDPSPerVolume)
                                    {
                                        TopDps2000 = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 4000f)
                                {
                                    if (ShellUnderTesting.KineticDPSPerVolume > TopDps4000.KineticDPSPerVolume)
                                    {
                                        TopDps4000 = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 6000f)
                                {
                                    if (ShellUnderTesting.KineticDPSPerVolume > TopDps6000.KineticDPSPerVolume)
                                    {
                                        TopDps6000 = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 8000f)
                                {
                                    if (ShellUnderTesting.KineticDPSPerVolume > TopDps8000.KineticDPSPerVolume)
                                    {
                                        TopDps8000 = ShellUnderTesting;
                                    }
                                }
                            }

                            if (DamageType == 1) // Chem
                            {
                                ShellUnderTesting.CalculateChemDamage();
                                ShellUnderTesting.CalculateChemDPS();

                                if (ShellUnderTesting.TotalLength <= 1000f)
                                {
                                    if (ShellUnderTesting.ChemDPSPerVolume > TopDps1000.ChemDPSPerVolume)
                                    {
                                        TopDps1000 = ShellUnderTesting;
                                    }
                                    if (ShellUnderTesting.ChemDPSPerVolumeBelt > TopDpsBelt.ChemDPSPerVolumeBelt)
                                    {
                                        TopDpsBelt = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 2000f)
                                {
                                    if (ShellUnderTesting.ChemDPSPerVolume > TopDps2000.ChemDPSPerVolume)
                                    {
                                        TopDps2000 = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 4000f)
                                {
                                    if (ShellUnderTesting.ChemDPSPerVolume > TopDps4000.ChemDPSPerVolume)
                                    {
                                        TopDps4000 = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 6000f)
                                {
                                    if (ShellUnderTesting.ChemDPSPerVolume > TopDps6000.ChemDPSPerVolume)
                                    {
                                        TopDps6000 = ShellUnderTesting;
                                    }
                                }
                                else if (ShellUnderTesting.TotalLength <= 8000f)
                                {
                                    if (ShellUnderTesting.ChemDPSPerVolume > TopDps8000.ChemDPSPerVolume)
                                    {
                                        TopDps8000 = ShellUnderTesting;
                                    }
                                }
                            }
                        }
                        else
                        {
                            TestRejectVelocity++;
                        }

                    }

                }
                else
                {
                    TestRejectLength++;
                }

            }
            TestTotal = TestComparisons + TestRejectLength + TestRejectVelocity;
            Console.WriteLine(TestComparisons + " shells compared.");
            Console.WriteLine(TestRejectLength + " shells rejected due to length.");
            Console.WriteLine(TestRejectVelocity + " shells rejected due to velocity.");
            Console.WriteLine(TestTotal + " total.");
            Console.WriteLine("\n");

            GetTopShells();
        }
    }
}
