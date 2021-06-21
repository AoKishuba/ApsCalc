using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using PenCalc;

namespace ApsCalc
{
    public struct ModuleCount
    {
        public float Gauge;
        public int HeadIndex;
        public float Var0Count;
        public float Var1Count;
        public float Var2Count;
        public float GPCount;
        public float RGCount;
    }


    public class ShellCalc
    {
        /// <summary>
        /// Takes shell parameters and calculates performance of shell permutations.
        /// </summary>
        /// <param name="barrelCount">Number of barrels</param>
        /// <param name="minGauge">Min desired gauge in mm</param>
        /// <param name="maxGauge">Max desired gauge in mm</param>
        /// <param name="headList">List of module indices for every module to be used as head</param>
        /// <param name="baseModule">The special base module, if any</param>
        /// <param name="fixedModuleCounts">An array of integers representing number of shells at that index in module list</param>
        /// <param name="fixedModuleTotal">Minimum number of modules on every shell</param>
        /// <param name="variableModuleIndices">Module indices of modules to be used in varying numbers in testing</param>
        /// <param name="maxGPInput">Max desired number of gunpowder casings</param>
        /// <param name="boreEvacuator">True if bore evacuator is used</param>
        /// <param name="maxRGInput">Max desired number of railgun casings</param>
        /// <param name="maxShellLengthInput">Max desired shell length in mm</param>
        /// <param name="maxDrawInput">Max desired rail draw</param>
        /// <param name="minVelocityInput">Min desired velocity</param>
        /// <param name="minEffectiveRangeInput">Min desired effective range</param>
        /// <param name="targetAC">Armor class of target for kinetic damage calculations</param>
        /// <param name="damageType">0 for kinetic, 1 for chemical, 2 for pendepth, 3 for disruptor</param>
        /// <param name="targetArmorScheme">Target armor scheme, from Pencalc namespace</param>
        /// <param name="testType">0 for DPS per volume, 1 for DPS per cost</param>
        /// <param name="labels">True if row headers should be printed on every line</param>
        public ShellCalc(
            int barrelCount,
            float minGauge,
            float maxGauge,
            List<int> headList,
            Module baseModule,
            float[] fixedModuleCounts,
            float fixedModuleTotal,
            int[] variableModuleIndices,
            float maxGPInput,
            bool boreEvacuator,
            float maxRGInput,
            float maxShellLengthInput,
            float maxDrawInput,
            float minVelocityInput,
            float minEffectiveRangeInput,
            float targetAC,
            float damageType,
            Scheme targetArmorScheme,
            int testType,
            bool labels
            )
        {
            BarrelCount = barrelCount;
            MinGauge = minGauge;
            MaxGauge = maxGauge;
            HeadList = headList;
            BaseModule = baseModule;
            FixedModuleCounts = fixedModuleCounts;
            FixedModuleTotal = fixedModuleTotal;
            VariableModuleIndices = variableModuleIndices;
            MaxGPInput = maxGPInput;
            BoreEvacuator = boreEvacuator;
            MaxRGInput = maxRGInput;
            MaxShellLength = maxShellLengthInput;
            MaxDrawInput = maxDrawInput;
            MinVelocityInput = minVelocityInput;
            MinEffectiveRangeInput = minEffectiveRangeInput;
            TargetAC = targetAC;
            DamageType = damageType;
            TargetArmorScheme = targetArmorScheme;
            TestType = testType;
            Labels = labels;
        }


        public int BarrelCount { get; }
        public float MinGauge { get; }
        public float MaxGauge { get; }
        public List<int> HeadList { get; }
        public Module BaseModule { get; }
        public float[] FixedModuleCounts { get; }
        public float FixedModuleTotal { get; }
        public int[] VariableModuleIndices { get; }
        public float MaxGPInput { get; }
        public bool BoreEvacuator { get; }
        public float MaxRGInput { get; }
        public float MaxShellLength { get; }
        public float MaxDrawInput { get; }
        public float MinVelocityInput { get; }
        public float MinEffectiveRangeInput { get; }
        public float TargetAC { get; }
        public float DamageType { get; }
        public Scheme TargetArmorScheme { get; }
        public int TestType { get; }
        public bool Labels { get; }

        // Testing data
        public int TestComparisons { get; set; }
        public int TestRejectLength { get; set; }
        public int TestRejectVelocityOrRange { get; set; }
        public int TestRejectPen { get; set; }
        public int TestTotal { get; set; }
        public List<int> TestCounts { get; set; }


        // Store top-DPS shells by loader length
        public Shell Top1000 { get; set; } = new Shell();
        public Shell TopBelt { get; set; } = new Shell();
        public Shell Top2000 { get; set; } = new Shell();
        public Shell Top3000 { get; set; } = new Shell();
        public Shell Top4000 { get; set; } = new Shell();
        public Shell Top6000 { get; set; } = new Shell();
        public Shell Top8000 { get; set; } = new Shell();

        public Dictionary<string, Shell> TopDpsShells { get; set; } = new Dictionary<string, Shell>();
        public List<Shell> TopShellsLocal { get; set; } = new List<Shell>();


        /// <summary>
        /// The iterable generator for shells.  Generates all shell possible permutations of shell within given parameters.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ModuleCount> GenerateModuleCounts()
        {
            float var0Max = 20f - FixedModuleTotal;
            float var1Max;
            float var2Max;
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
                            if (VariableModuleIndices[2] == VariableModuleIndices[0] || VariableModuleIndices[2] == VariableModuleIndices[1])
                            {
                                var2Max = 0; // No need to add duplicates
                            }
                            else
                            {
                                var2Max = 20f - (FixedModuleTotal + var0Count + var1Count);
                            }
                            for (float var2Count = 0; var2Count <= var2Max; var2Count++)
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
                                            Var2Count = var2Count,
                                            GPCount = gpCount,
                                            RGCount = rgCount
                                        };
                                    }
                                }
<<<<<<< Updated upstream
                            }                            
=======
                            }
>>>>>>> Stashed changes
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Divides a range of integers into even groups.  Used by railgun testing method.
        /// </summary>
        /// <param name="start">Beginning of range to be divided</param>
        /// <param name="end">End of range to be divided</param>
        /// <param name="numGroups">Number of groups into which range is divided</param>
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
        /// Calculates damage output for pendepth shells
        /// </summary>
        public void PendepthTest()
        {
            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                Shell shellUnderTesting = new();
                shellUnderTesting.BarrelCount = BarrelCount;
                shellUnderTesting.BoreEvacuator = BoreEvacuator;
                shellUnderTesting.HeadModule = Module.AllModules[counts.HeadIndex];
                shellUnderTesting.BaseModule = BaseModule;
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                shellUnderTesting.Gauge = counts.Gauge;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                shellUnderTesting.GPCasingCount = counts.GPCount;
                shellUnderTesting.RGCasingCount = counts.RGCount;

                shellUnderTesting.CalculateLengths();

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateLoaderVolumeAndCost();
                    shellUnderTesting.CalculateVelocityModifier();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);

                    if (maxDraw >= minDraw)
                    {
                        // Test for pen at max draw
                        shellUnderTesting.RailDraw = maxDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateAPModifier();
                        shellUnderTesting.CalculateAP();
                        shellUnderTesting.CalculateKDModifier();
                        shellUnderTesting.CalculateKineticDamage();

                        if (shellUnderTesting.KineticDamage >= TargetArmorScheme.GetRequiredKD(shellUnderTesting.ArmorPierce))
                        {
                            shellUnderTesting.CalculateReloadTime();
                            shellUnderTesting.CalculateBeltfedReload();
                            shellUnderTesting.CalculateChemModifier();
                            shellUnderTesting.CalculateChemDamage();

                            float optimalDraw = 0;
                            if (maxDraw > 0)
                            {
                                float bottomScore = 0;
                                float topScore = 0;
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;


                                shellUnderTesting.RailDraw = minDraw;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                shellUnderTesting.RailDraw = maxDraw;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTesting.RailDraw = maxDraw - 1f;
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTesting.ChemDpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTesting.ChemDpsPerCost;
                                    }

                                    if (topScore > bottomScore)
                                    {
                                        optimalDraw = maxDraw;
                                    }
                                }
                                else
                                {
                                    // Check if min draw is optimal
                                    shellUnderTesting.RailDraw = minDraw + 1f;
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        topScore = shellUnderTesting.ChemDpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        topScore = shellUnderTesting.ChemDpsPerCost;
                                    }

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

                                if (optimalDraw == 0)
                                {
<<<<<<< Updated upstream
                                    float topOfRange = maxDraw;
                                    // Binary search to find optimal draw without testing every value
=======
                                    // Binary search to find optimal draw without testing every value
                                    float topOfRange = maxDraw;
>>>>>>> Stashed changes
                                    float bottomOfRange = 0;
                                    while (topOfRange - bottomOfRange > 1)
                                    {

                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTesting.RailDraw = midRangeLower;
                                        shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            midRangeLowerScore = shellUnderTesting.ChemDpsPerVolume;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeLowerScore = shellUnderTesting.ChemDpsPerCost;
                                        }

                                        shellUnderTesting.RailDraw = midRangeUpper;
                                        shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            midRangeUpperScore = shellUnderTesting.ChemDpsPerVolume;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeUpperScore = shellUnderTesting.ChemDpsPerCost;
                                        }

                                        // Determine which half of range to continue testing
<<<<<<< Updated upstream
                                        // Midrange upper will equal a lot of time for pendepth
=======
>>>>>>> Stashed changes
                                        if (midRangeUpperScore == 0)
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                        else if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            topOfRange = midRangeLower;
                                        }
                                        else
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                    }
                                    // Take better of two remaining values
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        optimalDraw = midRangeLower;
                                    }
                                    else
                                    {
                                        optimalDraw = midRangeUpper;
                                    }
                                }
                            }

                            // Check performance against top shells
                            shellUnderTesting.RailDraw = optimalDraw;
                            shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                            shellUnderTesting.CalculateEffectiveRange();

                            if (TestType == 0)
                            {
                                if (shellUnderTesting.TotalLength <= 1000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerVolume > Top1000.ChemDpsPerVolume)
                                    {
                                        Top1000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 2000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerVolume > Top2000.ChemDpsPerVolume)
                                    {
                                        Top2000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 3000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerVolume > Top3000.ChemDpsPerVolume)
                                    {
                                        Top3000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 4000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerVolume > Top4000.ChemDpsPerVolume)
                                    {
                                        Top4000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 6000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerVolume > Top6000.ChemDpsPerVolume)
                                    {
                                        Top6000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 8000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerVolume > Top8000.ChemDpsPerVolume)
                                    {
                                        Top8000 = shellUnderTesting;
                                    }
                                }
                            }
                            else if (TestType == 1)
                            {
                                if (shellUnderTesting.TotalLength <= 1000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerCost > Top1000.ChemDpsPerCost)
                                    {
                                        Top1000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 2000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerCost > Top2000.ChemDpsPerCost)
                                    {
                                        Top2000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 3000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerCost > Top3000.ChemDpsPerCost)
                                    {
                                        Top3000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 4000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerCost > Top4000.ChemDpsPerCost)
                                    {
                                        Top4000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 6000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerCost > Top6000.ChemDpsPerCost)
                                    {
                                        Top6000 = shellUnderTesting;
                                    }
                                }
                                else if (shellUnderTesting.TotalLength <= 8000f)
                                {
                                    if (shellUnderTesting.ChemDpsPerCost > Top8000.ChemDpsPerCost)
                                    {
                                        Top8000 = shellUnderTesting;
                                    }
                                }
                            }


                            // Beltfed testing
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                Shell shellUnderTestingBelt = new()
                                {
                                    BarrelCount = BarrelCount,
                                    BoreEvacuator = BoreEvacuator,
                                    HeadModule = Module.AllModules[counts.HeadIndex],
                                    BaseModule = BaseModule
                                };
                                FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                                shellUnderTestingBelt.Gauge = counts.Gauge;
                                shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                                shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                                shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                                shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                                shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                                shellUnderTestingBelt.CalculateLengths();
                                shellUnderTestingBelt.CalculateLoaderVolumeAndCost();
                                shellUnderTestingBelt.CalculateVelocityModifier();
                                shellUnderTestingBelt.CalculateReloadTime();
                                shellUnderTestingBelt.CalculateBeltfedReload();
                                shellUnderTestingBelt.CalculateCooldownTime();
                                shellUnderTestingBelt.CalculateCoolerVolume();
                                shellUnderTestingBelt.CalculateChemModifier();
                                shellUnderTestingBelt.CalculateChemDamage();
                                shellUnderTestingBelt.CalculateAPModifier();
                                shellUnderTestingBelt.CalculateKDModifier();
                                if (maxDraw > 0)
                                {
                                    // Binary search to find optimal draw without testing every value
                                    float bottomScore = 0;
                                    float topScore = 0;
<<<<<<< Updated upstream
                                    float bottomOfRange = minDraw;
                                    float topOfRange = maxDraw;
=======
>>>>>>> Stashed changes
                                    float midRangeLower = 0;
                                    float midRangeLowerScore = 0;
                                    float midRangeUpper = 0;
                                    float midRangeUpperScore = 0;

                                    shellUnderTestingBelt.RailDraw = minDraw;
                                    shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                    }

                                    shellUnderTestingBelt.RailDraw = maxDraw;
                                    shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        topScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                    }

                                    if (topScore > bottomScore)
                                    {
                                        // Check if max draw is optimal
                                        shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            bottomScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                        }

                                        if (topScore > bottomScore)
                                        {
                                            optimalDraw = maxDraw;
                                        }
                                    }
                                    else
                                    {
                                        // Check if min draw is optimal
                                        shellUnderTestingBelt.RailDraw = minDraw + 1f;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            topScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                        }

                                        if (bottomScore > topScore)
                                        {
                                            optimalDraw = minDraw;
                                        }
                                    }

<<<<<<< Updated upstream
                                    while (topOfRange - bottomOfRange > 1)
                                    {
                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTestingBelt.RailDraw = midRangeLower;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                        }

                                        shellUnderTestingBelt.RailDraw = midRangeUpper;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                        }

                                        // Determine which half of range to continue testing
                                        if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            topOfRange = midRangeLower;
                                        }
                                        else
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                    }
                                    // Take better of two remaining values
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        optimalDraw = midRangeLower;
                                    }
                                    else
                                    {
                                        optimalDraw = midRangeUpper;
=======
                                    if (optimalDraw == 0)
                                    {
                                        // Binary search to find optimal draw without testing every value
                                        float topOfRange = maxDraw;
                                        float bottomOfRange = 0;

                                        while (topOfRange - bottomOfRange > 1)
                                        {
                                            midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                            midRangeUpper = midRangeLower + 1f;

                                            shellUnderTestingBelt.RailDraw = midRangeLower;
                                            shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                            if (TestType == 0)
                                            {
                                                midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                            }
                                            else if (TestType == 1)
                                            {
                                                midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                            }

                                            shellUnderTestingBelt.RailDraw = midRangeUpper;
                                            shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                            if (TestType == 0)
                                            {
                                                midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                            }
                                            else if (TestType == 1)
                                            {
                                                midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                            }

                                            // Determine which half of range to continue testing
                                            if (midRangeLowerScore >= midRangeUpperScore)
                                            {
                                                topOfRange = midRangeLower;
                                            }
                                            else
                                            {
                                                bottomOfRange = midRangeUpper;
                                            }
                                        }
                                        // Take better of two remaining values
                                        if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            optimalDraw = midRangeLower;
                                        }
                                        else
                                        {
                                            optimalDraw = midRangeUpper;
                                        }

>>>>>>> Stashed changes
                                    }
                                }

                                // Check performance against top shells
                                shellUnderTestingBelt.RailDraw = optimalDraw;
                                shellUnderTestingBelt.CalculateEffectiveRange();
                                shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);

                                if (TestType == 0)
                                {
                                    if (shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained > TopBelt.ChemDpsPerVolumeBeltSustained)
                                    {
                                        shellUnderTestingBelt.IsBelt = true;
                                        TopBelt = shellUnderTestingBelt;
                                    }
                                }
                                else if (TestType == 1)
                                {
                                    if (shellUnderTestingBelt.ChemDpsPerCostBeltSustained > TopBelt.ChemDpsPerCostBeltSustained)
                                    {
                                        shellUnderTestingBelt.IsBelt = true;
                                        TopBelt = shellUnderTestingBelt;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Optimizes kinetic damage output
        /// </summary>
        public void KineticTest()
        {
            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                Shell shellUnderTesting = new()
                {
                    BarrelCount = BarrelCount,
                    BoreEvacuator = BoreEvacuator,
                    HeadModule = Module.AllModules[counts.HeadIndex],
                    BaseModule = BaseModule
                };
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                shellUnderTesting.Gauge = counts.Gauge;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                shellUnderTesting.GPCasingCount = counts.GPCount;
                shellUnderTesting.RGCasingCount = counts.RGCount;

                shellUnderTesting.CalculateLengths();

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateLoaderVolumeAndCost();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);
                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateBeltfedReload();
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolume();
                        shellUnderTesting.CalculateKDModifier();
                        shellUnderTesting.CalculateAPModifier();

                        float optimalDraw = 0;
                        if (maxDraw > 0)
                        {
                            // Binary search to find optimal draw without testing every value
                            float bottomScore = 0;
                            float topScore = 0;
<<<<<<< Updated upstream
                            float bottomOfRange = minDraw;
                            float topOfRange = maxDraw;
=======
>>>>>>> Stashed changes
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            if (TestType == 0)
                            {
                                bottomScore = shellUnderTesting.KineticDpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                bottomScore = shellUnderTesting.KineticDpsPerCost;
                            }

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            if (TestType == 0)
                            {
                                topScore = shellUnderTesting.KineticDpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                topScore = shellUnderTesting.KineticDpsPerCost;
                            }

                            if (topScore > bottomScore)
<<<<<<< Updated upstream
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTesting.KineticDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.KineticDpsPerCost;
                                }

                                if (topScore > bottomScore)
                                {
                                    optimalDraw = maxDraw;
                                }
                            }
                            else
                            {
                                // Check if min draw is optimal
                                shellUnderTesting.RailDraw = minDraw + 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.KineticDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.KineticDpsPerCost;
                                }

                                if (bottomScore > topScore)
                                {
                                    optimalDraw = minDraw;
                                }
                            }

                            while (topOfRange - bottomOfRange > 1)
                            {
                                midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                midRangeUpper = midRangeLower + 1f;

                                shellUnderTesting.RailDraw = midRangeLower;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
                                    midRangeLowerScore = shellUnderTesting.KineticDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    midRangeLowerScore = shellUnderTesting.KineticDpsPerCost;
                                }

                                shellUnderTesting.RailDraw = midRangeUpper;
=======
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
>>>>>>> Stashed changes
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
<<<<<<< Updated upstream
                                    midRangeUpperScore = shellUnderTesting.KineticDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    midRangeUpperScore = shellUnderTesting.KineticDpsPerCost;
                                }

                                // Determine which half of range to continue testing
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    topOfRange = midRangeLower;
                                }
                                else
                                {
                                    bottomOfRange = midRangeUpper;
                                }
                            }
                            // Take better of two remaining values
                            if (midRangeLowerScore >= midRangeUpperScore)
                            {
                                optimalDraw = midRangeLower;
                            }
                            else
                            {
                                optimalDraw = midRangeUpper;
                            }
=======
                                    bottomScore = shellUnderTesting.KineticDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.KineticDpsPerCost;
                                }

                                if (topScore > bottomScore)
                                {
                                    optimalDraw = maxDraw;
                                }
                            }
                            else
                            {
                                // Check if min draw is optimal
                                shellUnderTesting.RailDraw = minDraw + 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.KineticDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.KineticDpsPerCost;
                                }

                                if (bottomScore > topScore)
                                {
                                    optimalDraw = minDraw;
                                }
                            }

                            if (optimalDraw == 0)
                            {
                                float topOfRange = maxDraw;
                                float bottomOfRange = minDraw;
                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTesting.RailDraw = midRangeLower;
                                    shellUnderTesting.CalculateRecoil();
                                    shellUnderTesting.CalculateRecoilVolumeAndCost();
                                    shellUnderTesting.CalculateChargerVolumeAndCost();
                                    shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                    shellUnderTesting.CalculateVelocity();
                                    shellUnderTesting.CalculateAP();
                                    shellUnderTesting.CalculateKineticDamage();
                                    shellUnderTesting.CalculateKineticDps(TargetAC);
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTesting.KineticDpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTesting.KineticDpsPerCost;
                                    }

                                    shellUnderTesting.RailDraw = midRangeUpper;
                                    shellUnderTesting.CalculateRecoil();
                                    shellUnderTesting.CalculateRecoilVolumeAndCost();
                                    shellUnderTesting.CalculateChargerVolumeAndCost();
                                    shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                    shellUnderTesting.CalculateVelocity();
                                    shellUnderTesting.CalculateAP();
                                    shellUnderTesting.CalculateKineticDamage();
                                    shellUnderTesting.CalculateKineticDps(TargetAC);
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTesting.KineticDpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTesting.KineticDpsPerCost;
                                    }

                                    // Determine which half of range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take better of two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
                            }
>>>>>>> Stashed changes
                        }

                        // Check performance against top shells
                        shellUnderTesting.RailDraw = optimalDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolumeAndCost();
                        shellUnderTesting.CalculateChargerVolumeAndCost();
                        shellUnderTesting.CalculateVolumeAndCostPerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateAP();
                        shellUnderTesting.CalculateKineticDamage();
                        shellUnderTesting.CalculateKineticDps(TargetAC);

                        if (TestType == 0)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.KineticDpsPerVolume > Top1000.KineticDpsPerVolume)
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.KineticDpsPerVolume > Top2000.KineticDpsPerVolume)
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.KineticDpsPerVolume > Top3000.KineticDpsPerVolume)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.KineticDpsPerVolume > Top4000.KineticDpsPerVolume)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.KineticDpsPerVolume > Top6000.KineticDpsPerVolume)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.KineticDpsPerVolume > Top8000.KineticDpsPerVolume)
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }
                        else if (TestType == 1)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top1000.KineticDpsPerCost)
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top2000.KineticDpsPerCost)
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
<<<<<<< Updated upstream
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top3000.KineticDpsPerCost)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top4000.KineticDpsPerCost)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top6000.KineticDpsPerCost)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
=======
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top3000.KineticDpsPerCost)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top4000.KineticDpsPerCost)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.KineticDpsPerCost > Top6000.KineticDpsPerCost)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
>>>>>>> Stashed changes
                                if (shellUnderTesting.KineticDpsPerCost > Top8000.KineticDpsPerCost)
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }


                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new()
                            {
                                BarrelCount = BarrelCount,
                                BoreEvacuator = BoreEvacuator,
                                HeadModule = Module.AllModules[counts.HeadIndex],
                                BaseModule = BaseModule
                            };
                            FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                            shellUnderTestingBelt.Gauge = counts.Gauge;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                            shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                            shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                            shellUnderTestingBelt.CalculateLengths();
                            shellUnderTestingBelt.CalculateLoaderVolumeAndCost();
                            shellUnderTestingBelt.CalculateVelocityModifier();
                            shellUnderTestingBelt.CalculateReloadTime();
                            shellUnderTestingBelt.CalculateBeltfedReload();
                            shellUnderTestingBelt.CalculateCooldownTime();
                            shellUnderTestingBelt.CalculateCoolerVolume();
                            shellUnderTestingBelt.CalculateAPModifier();
                            shellUnderTestingBelt.CalculateKDModifier();
                            if (maxDraw > 0)
                            {
                                // Binary search to find optimal draw without testing every value
                                float bottomScore = 0;
                                float topScore = 0;
<<<<<<< Updated upstream
                                float bottomOfRange = minDraw;
                                float topOfRange = maxDraw;
=======
>>>>>>> Stashed changes
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                shellUnderTestingBelt.CalculateVelocity();
                                shellUnderTestingBelt.CalculateAP();
                                shellUnderTestingBelt.CalculateKineticDamage();
                                shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                }

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                shellUnderTestingBelt.CalculateVelocity();
                                shellUnderTestingBelt.CalculateAP();
                                shellUnderTestingBelt.CalculateKineticDamage();
                                shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                }

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                    }

                                    if (topScore > bottomScore)
                                    {
                                        optimalDraw = maxDraw;
                                    }
                                }
                                else
                                {
                                    // Check if min draw is optimal
                                    shellUnderTestingBelt.RailDraw = minDraw + 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    if (TestType == 0)
                                    {
                                        topScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        topScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                    }

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

<<<<<<< Updated upstream
                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTestingBelt.RailDraw = midRangeLower;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                    }

                                    shellUnderTestingBelt.RailDraw = midRangeUpper;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                    }

                                    // Determine which half of range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take better of two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
=======
                                if (optimalDraw == 0)
                                {
                                    float topOfRange = maxDraw;
                                    float bottomOfRange = minDraw;
                                    while (topOfRange - bottomOfRange > 1)
                                    {
                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTestingBelt.RailDraw = midRangeLower;
                                        shellUnderTestingBelt.CalculateRecoil();
                                        shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                        shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                        shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                        shellUnderTestingBelt.CalculateVelocity();
                                        shellUnderTestingBelt.CalculateAP();
                                        shellUnderTestingBelt.CalculateKineticDamage();
                                        shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                        if (TestType == 0)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                        }

                                        shellUnderTestingBelt.RailDraw = midRangeUpper;
                                        shellUnderTestingBelt.CalculateRecoil();
                                        shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                        shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                        shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                        shellUnderTestingBelt.CalculateVelocity();
                                        shellUnderTestingBelt.CalculateAP();
                                        shellUnderTestingBelt.CalculateKineticDamage();
                                        shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                        if (TestType == 0)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.KineticDpsPerCostBeltSustained;
                                        }

                                        // Determine which half of range to continue testing
                                        if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            topOfRange = midRangeLower;
                                        }
                                        else
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                    }
                                    // Take better of two remaining values
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        optimalDraw = midRangeLower;
                                    }
                                    else
                                    {
                                        optimalDraw = midRangeUpper;
                                    }

>>>>>>> Stashed changes
                                }
                            }

                            // Check performance against top shells
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateRecoil();
                            shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                            shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                            shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                            shellUnderTestingBelt.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();
                            shellUnderTestingBelt.CalculateAP();
                            shellUnderTestingBelt.CalculateKineticDamage();
                            shellUnderTestingBelt.CalculateKineticDps(TargetAC);

                            if (TestType == 0)
                            {
                                if (shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained > TopBelt.KineticDpsPerVolumeBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
                            else if (TestType == 1)
                            {
                                if (shellUnderTestingBelt.KineticDpsPerCostBeltSustained > TopBelt.KineticDpsPerCostBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
<<<<<<< Updated upstream
                        }                        
=======
                        }
>>>>>>> Stashed changes
                    }
                }
            }
        }


        /// <summary>
        /// Calculates damage output for shell configurations with nonzero rail draw
        /// </summary>
        public void ChemTest()
        {
            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                Shell shellUnderTesting = new()
                {
                    BarrelCount = BarrelCount,
                    BoreEvacuator = BoreEvacuator,
                    HeadModule = Module.AllModules[counts.HeadIndex],

                    BaseModule = BaseModule
                };
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                shellUnderTesting.Gauge = counts.Gauge;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                shellUnderTesting.GPCasingCount = counts.GPCount;
                shellUnderTesting.RGCasingCount = counts.RGCount;

                shellUnderTesting.CalculateLengths();
<<<<<<< Updated upstream

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateLoaderVolumeAndCost();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);
                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateBeltfedReload();
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolume();
                        shellUnderTesting.CalculateChemModifier();
                        shellUnderTesting.CalculateChemDamage();

                        float optimalDraw = 0;
                        if (maxDraw > 0)
                        {
                            // Binary search to find optimal draw without testing every value
                            float bottomScore = 0;
                            float topScore = 0;
                            float bottomOfRange = minDraw;
                            float topOfRange = maxDraw;
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
=======

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateLoaderVolumeAndCost();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);
                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateBeltfedReload();
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolume();
                        shellUnderTesting.CalculateChemModifier();
                        shellUnderTesting.CalculateChemDamage();

                        float optimalDraw = 0;
                        if (maxDraw > 0)
                        {
                            // Binary search to find optimal draw without testing every value
                            float bottomScore = 0;
                            float topScore = 0;
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateChemDps();
                            if (TestType == 0)
                            {
                                bottomScore = shellUnderTesting.ChemDpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                bottomScore = shellUnderTesting.ChemDpsPerCost;
                            }

                            shellUnderTesting.RailDraw = maxDraw;
>>>>>>> Stashed changes
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateChemDps();
                            if (TestType == 0)
                            {
<<<<<<< Updated upstream
                                bottomScore = shellUnderTesting.ChemDpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                bottomScore = shellUnderTesting.ChemDpsPerCost;
                            }

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateChemDps();
                            if (TestType == 0)
                            {
                                topScore = shellUnderTesting.ChemDpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                topScore = shellUnderTesting.ChemDpsPerCost;
                            }

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                if (topScore > bottomScore)
                                {
                                    optimalDraw = maxDraw;
                                }
                            }
                            else
                            {
                                // Check if min draw is optimal
                                shellUnderTesting.RailDraw = minDraw + 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                if (bottomScore > topScore)
                                {
                                    optimalDraw = minDraw;
                                }
                            }

                            while (topOfRange - bottomOfRange > 1)
                            {
                                midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                midRangeUpper = midRangeLower + 1f;

                                shellUnderTesting.RailDraw = midRangeLower;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    midRangeLowerScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    midRangeLowerScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    midRangeUpperScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    midRangeUpperScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                // Determine which half of range to continue testing
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    topOfRange = midRangeLower;
                                }
                                else
                                {
                                    bottomOfRange = midRangeUpper;
                                }
                            }
                            // Take better of two remaining values
                            if (midRangeLowerScore >= midRangeUpperScore)
                            {
                                optimalDraw = midRangeLower;
                            }
                            else
                            {
                                optimalDraw = midRangeUpper;
=======
                                topScore = shellUnderTesting.ChemDpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                topScore = shellUnderTesting.ChemDpsPerCost;
                            }

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                if (topScore > bottomScore)
                                {
                                    optimalDraw = maxDraw;
                                }
                            }
                            else
                            {
                                // Check if min draw is optimal
                                shellUnderTesting.RailDraw = minDraw + 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.ChemDpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.ChemDpsPerCost;
                                }

                                if (bottomScore > topScore)
                                {
                                    optimalDraw = minDraw;
                                }
                            }

                            if (optimalDraw == 0)
                            {
                                float bottomOfRange = minDraw;
                                float topOfRange = maxDraw;
                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTesting.RailDraw = midRangeLower;
                                    shellUnderTesting.CalculateRecoil();
                                    shellUnderTesting.CalculateRecoilVolumeAndCost();
                                    shellUnderTesting.CalculateChargerVolumeAndCost();
                                    shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                    shellUnderTesting.CalculateChemDps();
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTesting.ChemDpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTesting.ChemDpsPerCost;
                                    }

                                    shellUnderTesting.RailDraw = midRangeUpper;
                                    shellUnderTesting.CalculateRecoil();
                                    shellUnderTesting.CalculateRecoilVolumeAndCost();
                                    shellUnderTesting.CalculateChargerVolumeAndCost();
                                    shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                    shellUnderTesting.CalculateChemDps();
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTesting.ChemDpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTesting.ChemDpsPerCost;
                                    }

                                    // Determine which half of range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take better of two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }

>>>>>>> Stashed changes
                            }
                        }

                        // Check performance against top shells
                        shellUnderTesting.RailDraw = optimalDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolumeAndCost();
                        shellUnderTesting.CalculateChargerVolumeAndCost();
                        shellUnderTesting.CalculateVolumeAndCostPerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateChemDps();

                        if (TestType == 0)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > Top1000.ChemDpsPerVolume)
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > Top2000.ChemDpsPerVolume)
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > Top3000.ChemDpsPerVolume)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > Top4000.ChemDpsPerVolume)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > Top6000.ChemDpsPerVolume)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > Top8000.ChemDpsPerVolume)
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }
                        else if (TestType == 1)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.ChemDpsPerCost > Top1000.ChemDpsPerCost)
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.ChemDpsPerCost > Top2000.ChemDpsPerCost)
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.ChemDpsPerCost > Top3000.ChemDpsPerCost)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.ChemDpsPerCost > Top4000.ChemDpsPerCost)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.ChemDpsPerCost > Top6000.ChemDpsPerCost)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.ChemDpsPerCost > Top8000.ChemDpsPerCost)
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }

                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new()
                            {
                                BarrelCount = BarrelCount,
                                BoreEvacuator = BoreEvacuator,
                                HeadModule = Module.AllModules[counts.HeadIndex],
                                BaseModule = BaseModule
                            };
                            FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                            shellUnderTestingBelt.Gauge = counts.Gauge;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                            shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                            shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                            shellUnderTestingBelt.CalculateLengths();
                            shellUnderTestingBelt.CalculateLoaderVolumeAndCost();
                            shellUnderTestingBelt.CalculateVelocityModifier();
                            shellUnderTestingBelt.CalculateReloadTime();
                            shellUnderTestingBelt.CalculateBeltfedReload();
                            shellUnderTestingBelt.CalculateCooldownTime();
                            shellUnderTestingBelt.CalculateCoolerVolume();
                            shellUnderTestingBelt.CalculateChemModifier();
                            shellUnderTestingBelt.CalculateChemDamage();

                            if (maxDraw > 0)
                            {
                                // Binary search to find optimal draw without testing every value
                                float bottomScore = 0;
                                float topScore = 0;
<<<<<<< Updated upstream
                                float bottomOfRange = minDraw;
                                float topOfRange = maxDraw;
=======
>>>>>>> Stashed changes
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                shellUnderTestingBelt.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                }

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                shellUnderTestingBelt.CalculateChemDps();
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                }

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                    }

                                    if (topScore > bottomScore)
                                    {
                                        optimalDraw = maxDraw;
                                    }
                                }
                                else
                                {
                                    // Check if min draw is optimal
                                    shellUnderTestingBelt.RailDraw = minDraw + 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                    }

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

<<<<<<< Updated upstream
                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTestingBelt.RailDraw = midRangeLower;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                    }

                                    shellUnderTestingBelt.RailDraw = midRangeUpper;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                    }

                                    // Determine which half of range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take better of two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
=======
                                if (optimalDraw == 0)
                                {
                                    float bottomOfRange = minDraw;
                                    float topOfRange = maxDraw;
                                    while (topOfRange - bottomOfRange > 1)
                                    {
                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTestingBelt.RailDraw = midRangeLower;
                                        shellUnderTestingBelt.CalculateRecoil();
                                        shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                        shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                        shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                        shellUnderTestingBelt.CalculateChemDps();
                                        if (TestType == 0)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                        }

                                        shellUnderTestingBelt.RailDraw = midRangeUpper;
                                        shellUnderTestingBelt.CalculateRecoil();
                                        shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                        shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                        shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                        shellUnderTestingBelt.CalculateChemDps();
                                        if (TestType == 0)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerCostBeltSustained;
                                        }

                                        // Determine which half of range to continue testing
                                        if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            topOfRange = midRangeLower;
                                        }
                                        else
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                    }
                                    // Take better of two remaining values
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        optimalDraw = midRangeLower;
                                    }
                                    else
                                    {
                                        optimalDraw = midRangeUpper;
                                    }
                                }
>>>>>>> Stashed changes
                            }

                            // Check performance against top shells
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateRecoil();
                            shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                            shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                            shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                            shellUnderTestingBelt.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();
                            shellUnderTestingBelt.CalculateChemDps();

                            if (TestType == 0)
                            {
                                if (shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained > TopBelt.ChemDpsPerVolumeBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
                            else if (TestType == 1)
                            {
                                if (shellUnderTestingBelt.ChemDpsPerCostBeltSustained > TopBelt.ChemDpsPerCostBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Calculates damage output for shell configurations with nonzero rail draw
        /// </summary>
        public void DisruptorTest()
        {
            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                Shell shellUnderTesting = new()
                {
                    BarrelCount = BarrelCount,
                    BoreEvacuator = BoreEvacuator,
                    HeadModule = Module.AllModules[counts.HeadIndex],

                    BaseModule = BaseModule
                };
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                shellUnderTesting.Gauge = counts.Gauge;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                shellUnderTesting.GPCasingCount = counts.GPCount;
                shellUnderTesting.RGCasingCount = counts.RGCount;

                shellUnderTesting.CalculateLengths();

                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateLoaderVolumeAndCost();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);
                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateBeltfedReload();
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolume();
                        shellUnderTesting.CalculateChemModifier();
                        shellUnderTesting.CalculateChemDamage();
                        shellUnderTesting.CalculateShieldReduction();

                        float optimalDraw = 0;
                        if (maxDraw > 0)
                        {
                            // Binary search to find optimal draw without testing every value
                            float bottomScore = 0;
                            float topScore = 0;
<<<<<<< Updated upstream
                            float bottomOfRange = minDraw;
                            float topOfRange = maxDraw;
=======
>>>>>>> Stashed changes
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateShieldRps();
                            if (TestType == 0)
                            {
                                bottomScore = shellUnderTesting.ShieldRpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                bottomScore = shellUnderTesting.ShieldRpsPerCost;
                            }

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolumeAndCost();
                            shellUnderTesting.CalculateChargerVolumeAndCost();
                            shellUnderTesting.CalculateVolumeAndCostPerIntake();
                            shellUnderTesting.CalculateShieldRps();
                            if (TestType == 0)
                            {
                                topScore = shellUnderTesting.ShieldRpsPerVolume;
                            }
                            else if (TestType == 1)
                            {
                                topScore = shellUnderTesting.ShieldRpsPerCost;
                            }

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTesting.ShieldRpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.ShieldRpsPerCost;
                                }

                                if (topScore > bottomScore)
                                {
                                    optimalDraw = maxDraw;
                                }
                            }
                            else
                            {
                                // Check if min draw is optimal
                                shellUnderTesting.RailDraw = minDraw + 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.ShieldRpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.ShieldRpsPerCost;
                                }

                                if (bottomScore > topScore)
                                {
                                    optimalDraw = minDraw;
                                }
                            }

<<<<<<< Updated upstream
                            while (topOfRange - bottomOfRange > 1)
                            {
                                midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                midRangeUpper = midRangeLower + 1f;

                                shellUnderTesting.RailDraw = midRangeLower;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                if (TestType == 0)
                                {
                                    midRangeLowerScore = shellUnderTesting.ShieldRpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    midRangeLowerScore = shellUnderTesting.ShieldRpsPerCost;
                                }

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolumeAndCost();
                                shellUnderTesting.CalculateChargerVolumeAndCost();
                                shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                if (TestType == 0)
                                {
                                    midRangeUpperScore = shellUnderTesting.ShieldRpsPerVolume;
                                }
                                else if (TestType == 1)
                                {
                                    midRangeUpperScore = shellUnderTesting.ShieldRpsPerCost;
                                }

                                // Determine which half of range to continue testing
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    topOfRange = midRangeLower;
                                }
                                else
                                {
                                    bottomOfRange = midRangeUpper;
                                }
                            }
                            // Take better of two remaining values
                            if (midRangeLowerScore >= midRangeUpperScore)
                            {
                                optimalDraw = midRangeLower;
                            }
                            else
                            {
                                optimalDraw = midRangeUpper;
                            }
=======
                            if (optimalDraw == 0)
                            {
                                float bottomOfRange = minDraw;
                                float topOfRange = maxDraw;

                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTesting.RailDraw = midRangeLower;
                                    shellUnderTesting.CalculateRecoil();
                                    shellUnderTesting.CalculateRecoilVolumeAndCost();
                                    shellUnderTesting.CalculateChargerVolumeAndCost();
                                    shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                    shellUnderTesting.CalculateShieldRps();
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTesting.ShieldRpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTesting.ShieldRpsPerCost;
                                    }

                                    shellUnderTesting.RailDraw = midRangeUpper;
                                    shellUnderTesting.CalculateRecoil();
                                    shellUnderTesting.CalculateRecoilVolumeAndCost();
                                    shellUnderTesting.CalculateChargerVolumeAndCost();
                                    shellUnderTesting.CalculateVolumeAndCostPerIntake();
                                    shellUnderTesting.CalculateShieldRps();
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTesting.ShieldRpsPerVolume;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTesting.ShieldRpsPerCost;
                                    }

                                    // Determine which half of range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take better of two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
                            }
>>>>>>> Stashed changes
                        }

                        // Check performance against top shells
                        shellUnderTesting.RailDraw = optimalDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolumeAndCost();
                        shellUnderTesting.CalculateChargerVolumeAndCost();
                        shellUnderTesting.CalculateVolumeAndCostPerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateChemDps();
                        shellUnderTesting.CalculateShieldRps();

                        if (TestType == 0)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerVolume > Top1000.ShieldRpsPerVolume)
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerVolume > Top2000.ShieldRpsPerVolume)
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerVolume > Top3000.ShieldRpsPerVolume)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerVolume > Top4000.ShieldRpsPerVolume)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerVolume > Top6000.ShieldRpsPerVolume)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerVolume > Top8000.ShieldRpsPerVolume)
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }
                        else if (TestType == 1)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerCost > Top1000.ShieldRpsPerCost)
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerCost > Top2000.ShieldRpsPerCost)
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerCost > Top3000.ShieldRpsPerCost)
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerCost > Top4000.ShieldRpsPerCost)
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerCost > Top6000.ShieldRpsPerCost)
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.ShieldRpsPerCost > Top8000.ShieldRpsPerCost)
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }
                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new()
                            {
                                BarrelCount = BarrelCount,
                                BoreEvacuator = BoreEvacuator,
                                HeadModule = Module.AllModules[counts.HeadIndex],
                                BaseModule = BaseModule
                            };
                            FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                            shellUnderTestingBelt.Gauge = counts.Gauge;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                            shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                            shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                            shellUnderTestingBelt.CalculateLengths();
                            shellUnderTestingBelt.CalculateLoaderVolumeAndCost();
                            shellUnderTestingBelt.CalculateVelocityModifier();
                            shellUnderTestingBelt.CalculateReloadTime();
                            shellUnderTestingBelt.CalculateBeltfedReload();
                            shellUnderTestingBelt.CalculateCooldownTime();
                            shellUnderTestingBelt.CalculateCoolerVolume();
                            shellUnderTestingBelt.CalculateChemModifier();
                            shellUnderTestingBelt.CalculateChemDamage();
                            shellUnderTestingBelt.CalculateShieldReduction();

                            if (maxDraw > 0)
                            {
                                // Binary search to find optimal draw without testing every value
                                float bottomScore = 0;
                                float topScore = 0;
<<<<<<< Updated upstream
                                float bottomOfRange = minDraw;
                                float topOfRange = maxDraw;
=======
>>>>>>> Stashed changes
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                shellUnderTestingBelt.CalculateShieldRps();
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                }

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                shellUnderTestingBelt.CalculateShieldRps();
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                }

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                    }

                                    if (topScore > bottomScore)
                                    {
                                        optimalDraw = maxDraw;
                                    }
                                }
                                else
                                {
                                    // Check if min draw is optimal
                                    shellUnderTestingBelt.RailDraw = minDraw + 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    if (TestType == 0)
                                    {
                                        topScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        topScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                    }

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

<<<<<<< Updated upstream
                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTestingBelt.RailDraw = midRangeLower;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                    }

                                    shellUnderTestingBelt.RailDraw = midRangeUpper;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                    shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                    shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                    }

                                    // Determine which half of range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take better of two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
=======
                                if (optimalDraw == 0)
                                {
                                    float bottomOfRange = minDraw;
                                    float topOfRange = maxDraw;
                                    while (topOfRange - bottomOfRange > 1)
                                    {
                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTestingBelt.RailDraw = midRangeLower;
                                        shellUnderTestingBelt.CalculateRecoil();
                                        shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                        shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                        shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                        shellUnderTestingBelt.CalculateShieldRps();
                                        if (TestType == 0)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                        }

                                        shellUnderTestingBelt.RailDraw = midRangeUpper;
                                        shellUnderTestingBelt.CalculateRecoil();
                                        shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                                        shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                                        shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                                        shellUnderTestingBelt.CalculateShieldRps();
                                        if (TestType == 0)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.ShieldRpsPerCostBeltSustained;
                                        }

                                        // Determine which half of range to continue testing
                                        if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            topOfRange = midRangeLower;
                                        }
                                        else
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                    }
                                    // Take better of two remaining values
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        optimalDraw = midRangeLower;
                                    }
                                    else
                                    {
                                        optimalDraw = midRangeUpper;
                                    }
                                }
>>>>>>> Stashed changes
                            }

                            // Check performance against top shells
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateRecoil();
                            shellUnderTestingBelt.CalculateRecoilVolumeAndCost();
                            shellUnderTestingBelt.CalculateChargerVolumeAndCost();
                            shellUnderTestingBelt.CalculateVolumeAndCostPerIntake();
                            shellUnderTestingBelt.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();
                            shellUnderTestingBelt.CalculateChemDps();
                            shellUnderTestingBelt.CalculateShieldRps();

                            if (TestType == 0)
                            {
                                if (shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained > TopBelt.ShieldRpsPerVolumeBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
                            else if (TestType == 1)
                            {
                                if (shellUnderTestingBelt.ShieldRpsPerCostBeltSustained > TopBelt.ShieldRpsPerCostBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Adds current top-performing shells to TopShells list for comparison with other lists
        /// Note that DPS is used only to determine whether a shell has been assigned to a particular length slot
        /// </summary>
        public void AddTopShellsToLocalList()
        {
            if (TopBelt.KineticDpsBeltSustained > 0 || TopBelt.ChemDpsBeltSustained > 0 || TopBelt.ShieldRpsBeltSustained > 0)
            {
                TopShellsLocal.Add(TopBelt);
            }

            if (Top1000.KineticDps > 0 || Top1000.ChemDps > 0 || Top1000.ShieldRps > 0)
            {
                TopShellsLocal.Add(Top1000);
            }

            if (Top2000.KineticDps > 0 || Top2000.ChemDps > 0 || Top2000.ShieldRps > 0)
            {
                TopShellsLocal.Add(Top2000);
            }

            if (Top3000.KineticDps > 0 || Top3000.ChemDps > 0 || Top3000.ShieldRps > 0)
            {
                TopShellsLocal.Add(Top3000);
            }

            if (Top4000.KineticDps > 0 || Top4000.ChemDps > 0 || Top4000.ShieldRps > 0)
            {
                TopShellsLocal.Add(Top4000);
            }

            if (Top6000.KineticDps > 0 || Top6000.ChemDps > 0 || Top6000.ShieldRps > 0)
            {
                TopShellsLocal.Add(Top6000);
            }

            if (Top8000.KineticDps > 0 || Top8000.ChemDps > 0 || Top8000.ShieldRps > 0)
            {
                TopShellsLocal.Add(Top8000);
            }
        }


        /// <summary>
        /// Adds current top-performing shells to TopShells dictionary for writing to console
        /// Note that DPS is used only to determine whether a shell has been assigned to a length slot
        /// </summary>
        public void AddTopShellsToDictionary()
        {
            if (TopBelt.KineticDpsBeltSustained > 0 || TopBelt.ChemDpsBeltSustained > 0 || TopBelt.ShieldRpsBeltSustained > 0)
            {
                TopDpsShells.Add("1 m (belt)", TopBelt);
            }

            if (Top1000.KineticDps > 0 || Top1000.ChemDps > 0 || Top1000.ShieldRps > 0)
            {
                TopDpsShells.Add("1 m", Top1000);
            }

            if (Top2000.KineticDps > 0 || Top2000.ChemDps > 0 || Top2000.ShieldRps > 0)
            {
                TopDpsShells.Add("2 m", Top2000);
            }

            if (Top3000.KineticDps > 0 || Top3000.ChemDps > 0 || Top3000.ShieldRps > 0)
            {
                TopDpsShells.Add("3 m", Top3000);
            }

            if (Top4000.KineticDps > 0 || Top4000.ChemDps > 0 || Top4000.ShieldRps > 0)
            {
                TopDpsShells.Add("4 m", Top4000);
            }

            if (Top6000.KineticDps > 0 || Top6000.ChemDps > 0 || Top6000.ShieldRps > 0)
            {
                TopDpsShells.Add("6 m", Top6000);
            }

            if (Top8000.KineticDps > 0 || Top8000.ChemDps > 0 || Top8000.ShieldRps > 0)
            {
                TopDpsShells.Add("8 m", Top8000);
            }
        }


        /// <summary>
        /// Finds top shells in given list.  Used in multithreading.
        /// </summary>
        /// <param name="shellBag"></param>
        public void FindTopShellsInList(ConcurrentBag<Shell> shellBag)
        {
            if (DamageType == 0)
            {
                foreach (Shell rawShell in shellBag)
                {
                    if (rawShell.IsBelt)
                    {
                        if (rawShell.KineticDpsPerVolumeBeltSustained > TopBelt.KineticDpsPerVolumeBeltSustained)
                        {
                            TopBelt = rawShell;
                        }
                    }
                    if (rawShell.TotalLength <= 1000f)
                    {
                        if (rawShell.KineticDpsPerVolume > Top1000.KineticDpsPerVolume)
                        {
                            Top1000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 2000f)
                    {
                        if (rawShell.KineticDpsPerVolume > Top2000.KineticDpsPerVolume)
                        {
                            Top2000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 3000f)
                    {
                        if (rawShell.KineticDpsPerVolume > Top3000.KineticDpsPerVolume)
                        {
                            Top3000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 4000f)
                    {
                        if (rawShell.KineticDpsPerVolume > Top4000.KineticDpsPerVolume)
                        {
                            Top4000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 6000f)
                    {
                        if (rawShell.KineticDpsPerVolume > Top6000.KineticDpsPerVolume)
                        {
                            Top6000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 8000f)
                    {
                        if (rawShell.KineticDpsPerVolume > Top8000.KineticDpsPerVolume)
                        {
                            Top8000 = rawShell;
                        }
                    }
                }
            }
            else if (DamageType == 1 || DamageType == 2)
            {
                foreach (Shell rawShell in shellBag)
                {
                    if (rawShell.IsBelt)
                    {
                        if (rawShell.ChemDpsPerVolumeBeltSustained > TopBelt.ChemDpsPerVolumeBeltSustained)
                        {
                            TopBelt = rawShell;
                        }
                    }
                    if (rawShell.TotalLength <= 1000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top1000.ChemDpsPerVolume)
                        {
                            Top1000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 2000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top2000.ChemDpsPerVolume)
                        {
                            Top2000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 3000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top3000.ChemDpsPerVolume)
                        {
                            Top3000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 4000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top4000.ChemDpsPerVolume)
                        {
                            Top4000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 6000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top6000.ChemDpsPerVolume)
                        {
                            Top6000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 8000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top8000.ChemDpsPerVolume)
                        {
                            Top8000 = rawShell;
                        }
                    }
                }
            }
            else if (DamageType == 3)
            {
                foreach (Shell rawShell in shellBag)
                {
                    if (rawShell.IsBelt)
                    {
                        if (rawShell.ShieldRpsPerVolumeBeltSustained > TopBelt.ShieldRpsPerVolumeBeltSustained)
                        {
                            TopBelt = rawShell;
                        }
                    }
                    if (rawShell.TotalLength <= 1000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > Top1000.ShieldRpsPerVolume)
                        {
                            Top1000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 2000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > Top2000.ShieldRpsPerVolume)
                        {
                            Top2000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 3000f)
                    {
                        if (rawShell.ChemDpsPerVolume > Top3000.ChemDpsPerVolume)
                        {
                            Top3000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 4000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > Top4000.ShieldRpsPerVolume)
                        {
                            Top4000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 6000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > Top6000.ShieldRpsPerVolume)
                        {
                            Top6000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 8000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > Top8000.ShieldRpsPerVolume)
                        {
                            Top8000 = rawShell;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Write to console statistics of top shells
        /// </summary>
        public void WriteTopShells()
        {
            // Determine presence of disruptor conduit
            bool disruptor = false;
            foreach (int headIndex in HeadList)
            {
                if (Module.AllModules[headIndex] == Module.Disruptor)
                {
                    disruptor = true;
                    break;
                }
            }

            Console.WriteLine("Test Parameters");
            Console.WriteLine(BarrelCount + " Barrels");
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

            if (BaseModule != null)
            {
                Console.WriteLine("Base: " + BaseModule.Name);
            }

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
            if (MaxGPInput > 0 && BoreEvacuator)
            {
                Console.WriteLine("Bore evacuator equipped");
            }
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
            else if (DamageType == 2)
            {
                Console.WriteLine("Damage type: pendepth (chemical)");
                Console.WriteLine("Target armor scheme:");
                foreach (Layer armorLayer in TargetArmorScheme.LayerList)
                {
                    Console.WriteLine(armorLayer.Name);
                }
            }
            else if (DamageType == 3)
            {
                Console.WriteLine("Damage type: shield disruption");
            }

            if (TestType == 0)
            {
                Console.WriteLine("Testing for DPS / volume");
            }
            else if (TestType == 1)
            {
                Console.WriteLine("Testing for DPS / cost");
            }
            Console.WriteLine("\n");


            if (!Labels)
            {
                Console.WriteLine("\n");
                Console.WriteLine("Row Headers:");
                Console.WriteLine("Gauge (mm)");
                Console.WriteLine("Total length (mm)");
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
                else if (DamageType == 1 || DamageType == 2 || DamageType == 3)
                {
                    Console.WriteLine("Chemical payload strength");
                    if (disruptor)
                    {
                        Console.WriteLine("Shield reduction");
                    }
                }

                Console.WriteLine("Reload (s)");
                Console.WriteLine("DPS");
                Console.WriteLine("DPS per volume");
                Console.WriteLine("DPS per cost");
                if (disruptor)
                {
                    Console.WriteLine("Shield reduction / s");
                    Console.WriteLine("Shield RPS / volume");
                    Console.WriteLine("Shield RPS / cost");
                }
                Console.WriteLine("Uptime (belt)");
                Console.WriteLine("DPS (belt, sustained)");
                Console.WriteLine("DPS per volume (sustained)");
                Console.WriteLine("DPS per cost (sustained)");
                if (disruptor)
                {
                    Console.WriteLine("Shield RPS (belt, sustained)");
                    Console.WriteLine("Shield RPS / volume (sustained)");
                    Console.WriteLine("Shield RPS / cost (sustained)");
                }
            }


            if (DamageType == 0)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    Console.WriteLine("\n");
                    Console.WriteLine(topShell.Key);
                    topShell.Value.GetModuleCounts();
                    topShell.Value.GetShellInfoKinetic(Labels);
                }
            }
            else if (DamageType == 1 || DamageType == 2 || DamageType == 3)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    Console.WriteLine("\n");
                    Console.WriteLine(topShell.Key);
                    topShell.Value.GetModuleCounts();
                    topShell.Value.GetShellInfoChem(Labels);
                }
            }
        }


        /// <summary>
        /// The main test body.  Iterates over IEnumerables to compare every permutation within given parameters, then stores results
        /// </summary>
        public void ShellTest()
        {
            TestComparisons = 0;
            TestRejectLength = 0;
            TestRejectPen = 0;
            TestRejectVelocityOrRange = 0;
            TestTotal = 0;

            if (DamageType == 0)
            {
                KineticTest();
            }
            else if (DamageType == 1)
            {
                ChemTest();
            }
            else if (DamageType == 2)
            {
                PendepthTest();
            }
            else if (DamageType == 3)
            {
                DisruptorTest();
            }
        }
    }
}