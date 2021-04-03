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
        /// <param name="labels">True if row headers should be printed on every line</param>
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
            float damageType,
            bool labels
            )
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
            Labels = labels;
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
        public bool Labels { get; }

        // Testing data
        public int TestComparisons { get; set; } = 0;
        public int TestRejectLength { get; set; } = 0;
        public int TestRejectVelocity { get; set; } = 0;
        public int TestRejectRange { get; set; } = 0;
        public int TestTotal { get; set; } = 0;
        public List<int> TestCounts { get; set; }


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
        /// Divides a range of integers into even groups.  Used by the railgun testing method.
        /// </summary>
        /// <param name="start">Beginning of the range to be divided</param>
        /// <param name="end">End of the range to be divided</param>
        /// <param name="numGroups">Number of groups into which the range is divided</param>
        /// <returns></returns>
        public static IEnumerable<float> DistributeRange(float start, float end, float numGroups)
        {
            if (numGroups == 0)
            {
                yield return 0;
            }
            else
            {
                float total = Math.Abs(end - start);
                float value;
                float groupSize = (float)Math.Floor(total / numGroups);

                for (float i = 0; i < numGroups; i++)
                {
                    value = i * groupSize;
                    yield return value;
                }

                yield return end;
            }
        }



        /// <summary>
        /// Efficiently tests railgun shells to find optimal draw without testing every integer value
        /// </summary>
        public void RailTest()
        {
            float lastGauge = MinGauge;

            foreach (ModuleCount counts in GetModuleCounts())
            {
                if (counts.Gauge != lastGauge)
                {
                    Console.WriteLine("\nTesting " + Module.AllModules[counts.HeadIndex].Name + " " + counts.Gauge + " mm.  Max " + MaxGauge + " mm.");
                    lastGauge = counts.Gauge;
                }
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

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateMaxDraw();
                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);

                    // Test shell at max draw to see if it meets velocity and effective range requirements
                    shellUnderTesting.RailDraw = maxDraw;
                    shellUnderTesting.CalculateModifiers();
                    shellUnderTesting.CalculateGPRecoil();
                    shellUnderTesting.CalculateVelocity();

                    if (shellUnderTesting.Velocity >= MinVelocityInput)
                    {
                        shellUnderTesting.CalculateEffectiveRange();

                        if (shellUnderTesting.EffectiveRange >= MinEffectiveRangeInput)
                        {
                            shellUnderTesting.CalculateReloadTime();
                            TestComparisons++;

                            // Test rail draw at even intervals over entire range to bound peak of the curve
                            float drawInterval = (float)Math.Min(1f, Math.Floor(maxDraw / 1000f));
                            Shell bestShellInterval = new Shell();
                            Shell bestShellIntervalBelt = new Shell();
                            Shell secondBestShellInterval = new Shell();
                            Shell secondBestShellIntervalBelt = new Shell();

                            foreach (float drawTest in DistributeRange(0, maxDraw, drawInterval))
                            {
                                shellUnderTesting.RailDraw = drawTest;
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateVolume();

                                if (DamageType == 0) // kinetic
                                {
                                    shellUnderTesting.CalculateAP();
                                    shellUnderTesting.CalculateKineticDamage();
                                    shellUnderTesting.CalculateKineticDPS(TargetAC);

                                    if (shellUnderTesting.KineticDPSPerVolume > bestShellInterval.KineticDPSPerVolume)
                                    {
                                        bestShellInterval = shellUnderTesting;
                                    }

                                    if (shellUnderTesting.KineticDPSPerVolume < bestShellInterval.KineticDPSPerVolume
                                        && shellUnderTesting.KineticDPSPerVolume > secondBestShellInterval.KineticDPSPerVolume)
                                    {
                                        secondBestShellInterval = shellUnderTesting;
                                    }

                                    if (shellUnderTesting.TotalLength <= 1000f)
                                    {
                                        if (shellUnderTesting.KineticDPSPerVolumeBelt > bestShellIntervalBelt.KineticDPSPerVolumeBelt)
                                        {
                                            bestShellIntervalBelt = shellUnderTesting;
                                        }

                                        if (shellUnderTesting.KineticDPSPerVolumeBelt < bestShellIntervalBelt.KineticDPSPerVolumeBelt
                                            && shellUnderTesting.KineticDPSPerVolumeBelt > secondBestShellIntervalBelt.KineticDPSPerVolumeBelt)
                                        {
                                            secondBestShellIntervalBelt = shellUnderTesting;
                                        }
                                    }
                                }
                                else if (DamageType == 1) // chem
                                {
                                    shellUnderTesting.CalculateChemDamage();
                                    shellUnderTesting.CalculateChemDPS();

                                    if (shellUnderTesting.ChemDPSPerVolume > bestShellInterval.ChemDPSPerVolume)
                                    {
                                        bestShellInterval = shellUnderTesting;
                                    }

                                    if (shellUnderTesting.ChemDPSPerVolumeBelt > bestShellIntervalBelt.ChemDPSPerVolumeBelt)
                                    {
                                        bestShellIntervalBelt = shellUnderTesting;
                                    }

                                    if (shellUnderTesting.ChemDPSPerVolume < bestShellInterval.ChemDPSPerVolume
                                        && shellUnderTesting.ChemDPSPerVolume > secondBestShellInterval.ChemDPSPerVolume)
                                    {
                                        secondBestShellInterval = shellUnderTesting;
                                    }

                                    if (shellUnderTesting.ChemDPSPerVolumeBelt < bestShellIntervalBelt.ChemDPSPerVolumeBelt
                                        && shellUnderTesting.ChemDPSPerVolumeBelt > secondBestShellIntervalBelt.ChemDPSPerVolumeBelt)
                                    {
                                        secondBestShellIntervalBelt = shellUnderTesting;
                                    }
                                }

                                // Test from best towards second best until peak draw is found
                                float bestDraw = bestShellInterval.RailDraw;
                                float bestDrawBelt = bestShellIntervalBelt.RailDraw;
                                float secondBestDraw = secondBestShellInterval.RailDraw;
                                float secondBestDrawBelt = secondBestShellIntervalBelt.RailDraw;

                                Shell peakShell = new Shell();
                                Shell peakShellBelt = new Shell();

                                if (bestDraw > secondBestDraw)
                                {
                                    // Draw is decreasing
                                    for (float draw = bestDraw; draw >= secondBestDraw; draw--)
                                    {
                                        shellUnderTesting.RailDraw = draw;
                                        shellUnderTesting.CalculateVelocity();
                                        shellUnderTesting.CalculateVolume();

                                        if (DamageType == 0)
                                        {
                                            shellUnderTesting.CalculateAP();
                                            shellUnderTesting.CalculateKineticDamage();
                                            shellUnderTesting.CalculateKineticDPS(TargetAC);

                                            // Keep reducing draw until just before effectiveness begins to decrease, to get minimum required draw
                                            if (shellUnderTesting.KineticDPSPerVolume < peakShell.KineticDPSPerVolume)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                peakShell = shellUnderTesting;
                                            }

                                            if (shellUnderTesting.TotalLength <= 1000f)
                                            {
                                                if (shellUnderTesting.KineticDPSPerVolumeBelt < peakShellBelt.KineticDPSPerVolumeBelt)
                                                {
                                                    break;
                                                }
                                                else
                                                {
                                                    peakShellBelt = shellUnderTesting;
                                                }
                                            }                                            
                                        }
                                        else if (DamageType == 1)
                                        {
                                            // Keep reducing draw until just before effectiveness begins to decrease, to get minimum required draw
                                            if (shellUnderTesting.ChemDPSPerVolume < peakShell.ChemDPSPerVolume)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                peakShell = shellUnderTesting;
                                            }

                                            if (shellUnderTesting.TotalLength <= 1000f)
                                            {
                                                if (shellUnderTesting.ChemDPSPerVolumeBelt < peakShellBelt.ChemDPSPerVolumeBelt)
                                                {
                                                    break;
                                                }
                                                else
                                                {
                                                    peakShellBelt = shellUnderTesting;
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (bestDraw < secondBestDraw)
                                {
                                    // Draw is increasing
                                    for (float draw = bestDraw; draw <= secondBestDraw; draw--)
                                    {
                                        shellUnderTesting.RailDraw = draw;
                                        shellUnderTesting.CalculateVelocity();
                                        shellUnderTesting.CalculateVolume();
                                        shellUnderTesting.GetModuleCounts();

                                        if (DamageType == 0)
                                        {
                                            shellUnderTesting.CalculateAP();
                                            shellUnderTesting.CalculateKineticDamage();
                                            shellUnderTesting.CalculateKineticDPS(TargetAC);

                                            // Keep increasing draw until just before effectiveness peaks, to get minimum required draw
                                            if (shellUnderTesting.KineticDPSPerVolume <= peakShell.KineticDPSPerVolume)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                peakShell = shellUnderTesting;
                                            }

                                            if (shellUnderTesting.TotalLength <= 1000f)
                                            {
                                                if (shellUnderTesting.KineticDPSPerVolumeBelt <= peakShellBelt.KineticDPSPerVolumeBelt)
                                                {
                                                    break;
                                                }
                                                else
                                                {
                                                    peakShellBelt = shellUnderTesting;
                                                }
                                            }
                                        }
                                        else if (DamageType == 1)
                                        {
                                            shellUnderTesting.CalculateChemDPS();

                                            // Keep increasing draw until just before effectiveness peaks, to get minimum required draw
                                            if (shellUnderTesting.ChemDPSPerVolume <= peakShell.ChemDPSPerVolume)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                peakShell = shellUnderTesting;
                                            }

                                            if (shellUnderTesting.TotalLength <= 1000f)
                                            {
                                                if (shellUnderTesting.ChemDPSPerVolumeBelt <= peakShellBelt.ChemDPSPerVolumeBelt)
                                                {
                                                    break;
                                                }
                                                else
                                                {
                                                    peakShellBelt = shellUnderTesting;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Final damage comparison
                                if (DamageType == 0) // Kinetic
                                {
                                    if (peakShell.TotalLength <= 1000f)
                                    {
                                        if (peakShell.KineticDPSPerVolume > TopDps1000.KineticDPSPerVolume)
                                        {
                                            TopDps1000 = peakShell;
                                        }
                                        if (peakShellBelt.KineticDPSPerVolumeBelt > TopDpsBelt.KineticDPSPerVolumeBelt)
                                        {
                                            TopDpsBelt = peakShellBelt;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 2000f)
                                    {
                                        if (peakShell.KineticDPSPerVolume > TopDps2000.KineticDPSPerVolume)
                                        {
                                            TopDps2000 = peakShell;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 4000f)
                                    {
                                        if (peakShell.KineticDPSPerVolume > TopDps4000.KineticDPSPerVolume)
                                        {
                                            TopDps4000 = peakShell;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 6000f)
                                    {
                                        if (peakShell.KineticDPSPerVolume > TopDps6000.KineticDPSPerVolume)
                                        {
                                            TopDps6000 = peakShell;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 8000f)
                                    {
                                        if (peakShell.KineticDPSPerVolume > TopDps8000.KineticDPSPerVolume)
                                        {
                                            TopDps8000 = peakShell;
                                        }
                                    }
                                }

                                if (DamageType == 1) // Chem
                                {
                                    if (peakShell.TotalLength <= 1000f)
                                    {
                                        if (peakShell.ChemDPSPerVolume > TopDps1000.ChemDPSPerVolume)
                                        {
                                            TopDps1000 = peakShell;
                                        }
                                        if (peakShellBelt.ChemDPSPerVolumeBelt > TopDpsBelt.ChemDPSPerVolumeBelt)
                                        {
                                            TopDpsBelt = peakShellBelt;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 2000f)
                                    {
                                        if (peakShell.ChemDPSPerVolume > TopDps2000.ChemDPSPerVolume)
                                        {
                                            TopDps2000 = peakShell;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 4000f)
                                    {
                                        if (peakShell.ChemDPSPerVolume > TopDps4000.ChemDPSPerVolume)
                                        {
                                            TopDps4000 = peakShell;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 6000f)
                                    {
                                        if (peakShell.ChemDPSPerVolume > TopDps6000.ChemDPSPerVolume)
                                        {
                                            TopDps6000 = peakShell;
                                        }
                                    }
                                    else if (peakShell.TotalLength <= 8000f)
                                    {
                                        if (peakShell.ChemDPSPerVolume > TopDps8000.ChemDPSPerVolume)
                                        {
                                            TopDps8000 = peakShell;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            TestRejectRange++;
                        }
                    }
                    else
                    {
                        TestRejectVelocity++;
                    }
                }
                else
                {
                    TestRejectLength++;
                }
            }
        }


        /// <summary>
        /// Calculates damage output for GP only shell configurations
        /// </summary>
        public void GPTest()
        {
            float lastGauge = MinGauge;
            Console.WriteLine("Testing " + Module.AllModules[HeadList[0]].Name + " " + MinGauge + " mm.  Max " + MaxGauge + " mm.");

            foreach (ModuleCount counts in GetModuleCounts())
            {
                if (counts.Gauge != lastGauge)
                {
                    Console.WriteLine("\nTesting " + Module.AllModules[counts.HeadIndex].Name + " " + counts.Gauge + " mm.  Max " + MaxGauge + " mm.");
                    lastGauge = counts.Gauge;
                }
                Shell shellUnderTesting = new Shell();
                shellUnderTesting.HeadModule = Module.AllModules[counts.HeadIndex];
                shellUnderTesting.BaseModule = BaseModule;
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                shellUnderTesting.Gauge = counts.Gauge;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTesting.GPCasingCount = counts.GPCount;
                shellUnderTesting.RGCasingCount = counts.RGCount;
                shellUnderTesting.RailDraw = 0;

                shellUnderTesting.CalculateLengths();

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateModifiers();
                    shellUnderTesting.CalculateGPRecoil();
                    shellUnderTesting.CalculateVelocity();
                    
                    if (shellUnderTesting.Velocity >= MinVelocityInput)
                    {
                        shellUnderTesting.CalculateEffectiveRange();

                        if (shellUnderTesting.EffectiveRange >= MinEffectiveRangeInput)
                        {
                            TestComparisons++;
                            shellUnderTesting.CalculateReloadTime();
                            shellUnderTesting.CalculateVolume();
                            shellUnderTesting.GetModuleCounts();

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
                            TestRejectRange++;
                        }
                    }
                    else
                    {
                        TestRejectVelocity++;
                    }
                }
                else
                {
                    TestRejectLength++;
                }
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
        /// Write to the console the statistics of the top shells
        /// </summary>
        public void WriteTopShells()
        {
            Console.WriteLine("Test Parameters");
            if (MinGauge == MaxGauge)
            {
                Console.WriteLine("Gauge: " + MinGauge);
            }
            else
            {
                Console.WriteLine("Gauge: " + MinGauge + " mm thru " + MaxGauge + " mm");
            }


            if (HeadList.Count == 1)
            {
                Console.WriteLine("Head: " + Module.AllModules[HeadList[0]].Name);
            }
            else
            {
                Console.WriteLine("Heads: ");
                foreach (int headIndex in HeadList)
                {
                    Console.WriteLine(Module.AllModules[headIndex].Name);
                }
            }

            Console.WriteLine("Base: " + BaseModule.Name);
            Console.WriteLine("Fixed modules: ");

            int modIndex = 0;
            foreach (float modCount in FixedModuleCounts)
            {
                Console.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                modIndex++;
            }

            if (VariableModuleIndices[0] == VariableModuleIndices[1])
            {
                Console.WriteLine("Variable module: " + Module.AllModules[VariableModuleIndices[0]].Name);
            }
            else
            {
                Console.WriteLine("Variable modules: ");
                foreach (int varModIndex in VariableModuleIndices)
                {
                    Console.WriteLine(Module.AllModules[varModIndex].Name);
                }
            }

            Console.WriteLine("Max GP casings: " + MaxGPInput);
            Console.WriteLine("Max RG casings: " + MaxRGInput);
            Console.WriteLine("Max draw: " + MaxDrawInput);
            Console.WriteLine("Max length: " + MaxShellLength);
            Console.WriteLine("Min velocity: " + MinVelocityInput);
            Console.WriteLine("Min effective range: " + MinEffectiveRangeInput);

            if (DamageType == 0)
            {
                Console.WriteLine("Damage type: kinetic");
                Console.WriteLine("Target AC: " + TargetAC);
            }
            else if (DamageType == 1)
            {
                Console.WriteLine("Damage type: chemical");
            }
            Console.WriteLine("\n");


            if (!Labels)
            {
                Console.WriteLine("\n");
                Console.WriteLine("Row Headers:");
                Console.WriteLine("Gauge (mm)");
                Console.WriteLine("Total ength (mm)");
                Console.WriteLine("Length without casings (mm)");
                Console.WriteLine("Total modules");
                Console.WriteLine("GP casings");
                Console.WriteLine("RG casings");

                for (int i = 0; i < FixedModuleCounts.Length; i++)
                {
                    Console.WriteLine(Module.AllModules[i].Name);
                }

                Console.WriteLine("Head");
                Console.WriteLine("Draw");
                Console.WriteLine("Recoil");
                Console.WriteLine("Velocity (m/s)");
                Console.WriteLine("Effective range (m)");

                if (DamageType == 0)
                {
                    Console.WriteLine("Raw KD");
                    Console.WriteLine("AP");
                    Console.WriteLine("Eff. KD");
                }
                else if (DamageType == 1)
                {
                    Console.WriteLine("Chemical payload strength");
                }

                Console.WriteLine("Reload (s)");
                Console.WriteLine("DPS");
                Console.Write("DPS per volume");
            }


            if (DamageType == 0)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    Console.WriteLine("\n");
                    Console.WriteLine(topShell.Key);
                    topShell.Value.GetShellInfoKinetic(Labels);
                }
            }
            else if (DamageType == 1)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    Console.WriteLine("\n");
                    Console.WriteLine(topShell.Key);
                    topShell.Value.GetShellInfoChem(Labels);
                }
            }
        }


        /// <summary>
        /// The main test body.  Iterates over the IEnumerables to compare every permutation within the given parameters, then stores the results
        /// </summary>
        public void ShellTest()
        {
            if (MaxDrawInput > 0)
            {
                RailTest();
            }
            else
            {
                GPTest();
            }

            TestTotal = TestComparisons + TestRejectLength + TestRejectVelocity;
            Console.WriteLine(TestComparisons + " shells compared.");
            Console.WriteLine(TestRejectLength + " shells rejected due to length.");
            Console.WriteLine(TestRejectVelocity + " shells rejected due to velocity.");
            Console.WriteLine(TestTotal + " total.");
            Console.WriteLine("\n");
        }
    }
}
