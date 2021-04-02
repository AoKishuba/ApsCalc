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
        /// <param name="minVelocityInput">Min desired velocity</param>
        /// <param name="minEffectiveRangeInput">Min desired effective range</param>
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
            float minEffectiveRangeInput,
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
            MinEffectiveRangeInput = minEffectiveRangeInput;
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
        public float MinEffectiveRangeInput { get; }
        public float TargetAC { get; }
        public float DamageType { get; }

        // Testing data
        public float testComparisons { get; set; } = 0;
        public float testRejectLength { get; set; } = 0;
        public float testRejectVelocity { get; set; } = 0;
        public float testTotal { get; set; } = 0;

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
                Shell shellUnderTestingSetup = new Shell();
                shellUnderTestingSetup.HeadModule = Module.AllModules[counts.HeadIndex];
                shellUnderTestingSetup.BaseModule = BaseModule;
                FixedModuleCounts.CopyTo(shellUnderTestingSetup.BodyModuleCounts, 0);

                shellUnderTestingSetup.Gauge = counts.Gauge;
                shellUnderTestingSetup.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTestingSetup.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTestingSetup.GPCasingCount = counts.GPCount;
                shellUnderTestingSetup.RGCasingCount = counts.RGCount;

                shellUnderTestingSetup.CalculateLengths();

                if (shellUnderTestingSetup.TotalLength <= MaxShellLength)
                {
                    shellUnderTestingSetup.CalculateMaxDraw();
                    float maxDraw = Math.Min(MaxDrawInput, shellUnderTestingSetup.MaxDraw);
                    foreach (float draw in GetRailDraw(shellUnderTestingSetup.MaxDraw))
                    {
                        // Reset shell
                        Shell shellUnderTesting = new Shell();
                        shellUnderTesting.HeadModule = Module.AllModules[counts.HeadIndex];
                        shellUnderTesting.BaseModule = BaseModule;
                        FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                        shellUnderTesting.Gauge = counts.Gauge;
                        shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                        shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                        shellUnderTesting.GPCasingCount = counts.GPCount;
                        shellUnderTesting.RGCasingCount = counts.RGCount;
                        shellUnderTesting.CalculateLengths();
                        shellUnderTesting.CalculateGPRecoil();
                        shellUnderTesting.CalculateModifiers();

                        shellUnderTesting.RailDraw = draw;
                        shellUnderTesting.CalculateVelocity();
                        if (shellUnderTesting.Velocity >= MinVelocityInput)
                        {
                            testComparisons++;
                            shellUnderTesting.CalculateReloadTime();
                            shellUnderTesting.CalculateVolume();

                            if (DamageType == 0) // Kinetic
                            {
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDPS(TargetAC);

                                if (shellUnderTesting.TotalLength <= 1000f)
                                {
                                    if (shellUnderTesting.KineticDPSPerVolume > TopDps1000.KineticDPSPerVolume)
                                    {
                                        TopDps1000 = shellUnderTesting;
                                    }
                                    if (shellUnderTesting.KineticDPSPerVolumeBelt > TopDpsBelt.KineticDPSPerVolumeBelt)
                                    {
                                        TopDpsBelt = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 2000f)
                                {
                                    if (shellUnderTesting.KineticDPSPerVolume > TopDps2000.KineticDPSPerVolume)
                                    {
                                        TopDps2000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 4000f)
                                {
                                    if (shellUnderTesting.KineticDPSPerVolume > TopDps4000.KineticDPSPerVolume)
                                    {
                                        TopDps4000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 6000f)
                                {
                                    if (shellUnderTesting.KineticDPSPerVolume > TopDps6000.KineticDPSPerVolume)
                                    {
                                        TopDps6000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 8000f)
                                {
                                    if (shellUnderTesting.KineticDPSPerVolume > TopDps8000.KineticDPSPerVolume)
                                    {
                                        TopDps8000 = shellUnderTesting;
                                    }
                                }
                            }

                            if (DamageType == 1) // Chem
                            {
                                shellUnderTesting.CalculateChemDamage();
                                shellUnderTesting.CalculateChemDPS();

                                if (shellUnderTesting.TotalLength <= 1000f)
                                {
                                    if (shellUnderTesting.ChemDPSPerVolume > TopDps1000.ChemDPSPerVolume)
                                    {
                                        TopDps1000 = shellUnderTesting;
                                    }
                                    if (shellUnderTesting.ChemDPSPerVolumeBelt > TopDpsBelt.ChemDPSPerVolumeBelt)
                                    {
                                        TopDpsBelt = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 2000f)
                                {
                                    if (shellUnderTesting.ChemDPSPerVolume > TopDps2000.ChemDPSPerVolume)
                                    {
                                        TopDps2000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 4000f)
                                {
                                    if (shellUnderTesting.ChemDPSPerVolume > TopDps4000.ChemDPSPerVolume)
                                    {
                                        TopDps4000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 6000f)
                                {
                                    if (shellUnderTesting.ChemDPSPerVolume > TopDps6000.ChemDPSPerVolume)
                                    {
                                        TopDps6000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 8000f)
                                {
                                    if (shellUnderTesting.ChemDPSPerVolume > TopDps8000.ChemDPSPerVolume)
                                    {
                                        TopDps8000 = shellUnderTesting;
                                    }
                                }
                            }
                        }
                        else
                        {
                            testRejectVelocity++;
                        }

                    }

                }
                else
                {
                    testRejectLength++;
                }

            }
            testTotal = testComparisons + testRejectLength + testRejectVelocity;
            Console.WriteLine(testComparisons + " shells compared.");
            Console.WriteLine(testRejectLength + " shells rejected due to length.");
            Console.WriteLine(testRejectVelocity + " shells rejected due to velocity.");
            Console.WriteLine(testTotal + " total.");
            Console.WriteLine("\n");

            GetTopShells();
        }
    }
}