using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
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
        /// <param name="writeToFile">True if results should be written to text file instead of console</param>
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
            bool labels,
            bool writeToFile
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
            WriteToFile = writeToFile;
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
        public bool WriteToFile { get; }


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
                                gpMax = MathF.Min(20f - (FixedModuleTotal + var0Count + var1Count), MaxGPInput);

                                for (float gpCount = 0; gpCount <= gpMax; gpCount += 0.01f)
                                {
                                    rgMax = MathF.Min(20f - (FixedModuleTotal + var0Count + var1Count + gpCount), MaxRGInput);

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
                            }                            
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
                float total = MathF.Abs(end - start);
                float value;
                float groupSize = MathF.Floor(total / numGroups);

                for (float i = 0; i < numGroups; i++)
                {
                    value = i * groupSize;
                    yield return value;
                }

                yield return end;
            }
        }



        /// <summary>
        /// Iterates over possible configurations and stores the best according to test parameters
        /// </summary>
        public void ShellTest()
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
                    shellUnderTesting.CalculateVelocityModifier();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();

                    float maxDraw = MathF.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);

                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateDamageModifierByType(DamageType);
                        shellUnderTesting.CalculateDamageByType(DamageType);
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolumeAndCost();
                        shellUnderTesting.CalculateLoaderVolumeAndCost();


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
                            shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                            if (TestType == 0)
                            {
                                bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                            }

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                            if (TestType == 0)
                            {
                                topScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                topScore = shellUnderTesting.DpsPerCostDict[DamageType];
                            }

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
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
                                shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTesting.DpsPerCostDict[DamageType];
                                }

                                if (bottomScore > topScore)
                                {
                                    optimalDraw = minDraw;
                                }
                            }

                            if (optimalDraw == 0)
                            {
                                float topOfRange = maxDraw;
                                // Binary search to find optimal draw without testing every value
                                float bottomOfRange = 0;
                                while (topOfRange - bottomOfRange > 1)
                                {

                                    midRangeLower = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTesting.RailDraw = midRangeLower;
                                    shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        midRangeLowerScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeLowerScore = shellUnderTesting.DpsPerCostDict[DamageType];
                                    }

                                    shellUnderTesting.RailDraw = midRangeUpper;
                                    shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        midRangeUpperScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                                    }
                                    else if (TestType == 1)
                                    {
                                        midRangeUpperScore = shellUnderTesting.DpsPerCostDict[DamageType];
                                    }

                                    // Determine which half of range to continue testing
                                    // Midrange upper will equal a lot of time for pendepth
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
                        shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();

                        if (TestType == 0)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.DpsPerVolumeDict[DamageType] > Top1000.DpsPerVolumeDict[DamageType])
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.DpsPerVolumeDict[DamageType] > Top2000.DpsPerVolumeDict[DamageType])
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.DpsPerVolumeDict[DamageType] > Top3000.DpsPerVolumeDict[DamageType])
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.DpsPerVolumeDict[DamageType] > Top4000.DpsPerVolumeDict[DamageType])
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.DpsPerVolumeDict[DamageType] > Top6000.DpsPerVolumeDict[DamageType])
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.DpsPerVolumeDict[DamageType] > Top8000.DpsPerVolumeDict[DamageType])
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }
                        else if (TestType == 1)
                        {
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.DpsPerCostDict[DamageType] > Top1000.DpsPerCostDict[DamageType])
                                {
                                    Top1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.DpsPerCostDict[DamageType] > Top2000.DpsPerCostDict[DamageType])
                                {
                                    Top2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 3000f)
                            {
                                if (shellUnderTesting.DpsPerCostDict[DamageType] > Top3000.DpsPerCostDict[DamageType])
                                {
                                    Top3000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.DpsPerCostDict[DamageType] > Top4000.DpsPerCostDict[DamageType])
                                {
                                    Top4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.DpsPerCostDict[DamageType] > Top6000.DpsPerCostDict[DamageType])
                                {
                                    Top6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.DpsPerCostDict[DamageType] > Top8000.DpsPerCostDict[DamageType])
                                {
                                    Top8000 = shellUnderTesting;
                                }
                            }
                        }


                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new();
                            shellUnderTestingBelt.CopyStatsFrom(shellUnderTesting);
                            shellUnderTestingBelt.IsBelt = true;
                            shellUnderTestingBelt.CalculateReloadTimeBelt();
                            shellUnderTestingBelt.CalculateCoolerVolumeAndCost();

                            if (maxDraw > 0)
                            {
                                float bottomScore = 0;
                                float topScore = 0;
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                                if (TestType == 0)
                                {
                                    bottomScore = shellUnderTestingBelt.DpsPerVolumeDict[DamageType];
                                }
                                else if (TestType == 1)
                                {
                                    bottomScore = shellUnderTestingBelt.DpsPerCostDict[DamageType];
                                }

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                                if (TestType == 0)
                                {
                                    topScore = shellUnderTestingBelt.DpsPerVolumeDict[DamageType];
                                }
                                else if (TestType == 1)
                                {
                                    topScore = shellUnderTestingBelt.DpsPerCostDict[DamageType];
                                }

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        bottomScore = shellUnderTestingBelt.DpsPerVolumeDict[DamageType];
                                    }
                                    else if (TestType == 1)
                                    {
                                        bottomScore = shellUnderTestingBelt.DpsPerCostDict[DamageType];
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
                                    shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                                    if (TestType == 0)
                                    {
                                        topScore = shellUnderTestingBelt.DpsPerVolumeDict[DamageType];
                                    }
                                    else if (TestType == 1)
                                    {
                                        topScore = shellUnderTestingBelt.DpsPerCostDict[DamageType];
                                    }

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

                                if (optimalDraw == 0)
                                {
                                    float topOfRange = maxDraw;
                                    // Binary search to find optimal draw without testing every value
                                    float bottomOfRange = 0;
                                    while (topOfRange - bottomOfRange > 1)
                                    {
                                        midRangeLower = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTestingBelt.RailDraw = midRangeLower;
                                        shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.DpsPerVolumeDict[DamageType];
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeLowerScore = shellUnderTestingBelt.DpsPerCostDict[DamageType];
                                        }

                                        shellUnderTestingBelt.RailDraw = midRangeUpper;
                                        shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                                        if (TestType == 0)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.DpsPerVolumeDict[DamageType];
                                        }
                                        else if (TestType == 1)
                                        {
                                            midRangeUpperScore = shellUnderTestingBelt.DpsPerCostDict[DamageType];
                                        }

                                        // Determine which half of range to continue testing
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
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();

                            if (TestType == 0)
                            {
                                if (shellUnderTestingBelt.DpsPerVolumeDict[DamageType] > TopBelt.DpsPerVolumeDict[DamageType])
                                {
                                    TopBelt = shellUnderTestingBelt;
                                }
                            }
                            else if (TestType == 1)
                            {
                                if (shellUnderTestingBelt.DpsPerCostDict[DamageType] > TopBelt.DpsPerCostDict[DamageType])
                                {
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
        /// Write top shell information
        /// </summary>
        public void WriteTopShells()
        {
            if (WriteToFile)
            {
                WriteTopShellsToFile();
            }
            else
            {
                WriteTopShellsToConsole();
            }
        }

        /// <summary>
        /// Write to console statistics of top shells
        /// </summary>
        void WriteTopShellsToConsole()
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
                    topShell.Value.WriteShellInfoToConsoleKinetic(Labels);
                }
            }
            else if (DamageType == 1 || DamageType == 2 || DamageType == 3)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    Console.WriteLine("\n");
                    Console.WriteLine(topShell.Key);
                    topShell.Value.GetModuleCounts();
                    topShell.Value.WriteShellInfoToConsoleChem(Labels);
                }
            }
        }


        /// <summary>
        /// Write to file statistics of top shells
        /// </summary>
        void WriteTopShellsToFile()
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

            // Create filename from current time
            string fileName = DateTime.Now.ToString("HHmmss");

            using var writer = new StreamWriter(fileName, append: true);
            FileStream fs = (FileStream)writer.BaseStream;
            Console.WriteLine("Writing results to filename: " + fs.Name);


            writer.WriteLine("Test Parameters");
            writer.WriteLine(BarrelCount + " Barrels");
            if (MinGauge == MaxGauge)
            {
                writer.WriteLine("Gauge: " + MinGauge);
            }
            else
            {
                writer.WriteLine("Gauge: " + MinGauge + " mm thru " + MaxGauge + " mm");
            }


            if (HeadList.Count == 1)
            {
                writer.WriteLine("Head: " + Module.AllModules[HeadList[0]].Name);
            }
            else
            {
                writer.WriteLine("Heads: ");
                foreach (int headIndex in HeadList)
                {
                    writer.WriteLine(Module.AllModules[headIndex].Name);
                }
            }

            if (BaseModule != null)
            {
                writer.WriteLine("Base: " + BaseModule.Name);
            }

            writer.WriteLine("Fixed modules: ");

            int modIndex = 0;
            foreach (float modCount in FixedModuleCounts)
            {
                writer.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                modIndex++;
            }

            if (VariableModuleIndices[0] == VariableModuleIndices[1])
            {
                writer.WriteLine("Variable module: " + Module.AllModules[VariableModuleIndices[0]].Name);
            }
            else
            {
                writer.WriteLine("Variable modules: ");
                foreach (int varModIndex in VariableModuleIndices)
                {
                    writer.WriteLine(Module.AllModules[varModIndex].Name);
                }
            }

            writer.WriteLine("Max GP casings: " + MaxGPInput);
            if (MaxGPInput > 0 && BoreEvacuator)
            {
                writer.WriteLine("Bore evacuator equipped");
            }
            writer.WriteLine("Max RG casings: " + MaxRGInput);
            writer.WriteLine("Max draw: " + MaxDrawInput);
            writer.WriteLine("Max length: " + MaxShellLength);
            writer.WriteLine("Min velocity: " + MinVelocityInput);
            writer.WriteLine("Min effective range: " + MinEffectiveRangeInput);

            if (DamageType == 0)
            {
                writer.WriteLine("Damage type: kinetic");
                writer.WriteLine("Target AC: " + TargetAC);
            }
            else if (DamageType == 1)
            {
                writer.WriteLine("Damage type: chemical");
            }
            else if (DamageType == 2)
            {
                writer.WriteLine("Damage type: pendepth (chemical)");
                writer.WriteLine("Target armor scheme:");
                foreach (Layer armorLayer in TargetArmorScheme.LayerList)
                {
                    writer.WriteLine(armorLayer.Name);
                }
            }
            else if (DamageType == 3)
            {
                writer.WriteLine("Damage type: shield disruption");
            }

            if (TestType == 0)
            {
                writer.WriteLine("Testing for DPS / volume");
            }
            else if (TestType == 1)
            {
                writer.WriteLine("Testing for DPS / cost");
            }
            writer.WriteLine("\n");


            if (!Labels)
            {
                writer.WriteLine("\n");
                writer.WriteLine("Row Headers:");
                writer.WriteLine("Gauge (mm)");
                writer.WriteLine("Total length (mm)");
                writer.WriteLine("Length without casings (mm)");
                writer.WriteLine("Total modules");
                writer.WriteLine("GP casings");
                writer.WriteLine("RG casings");

                for (int i = 0; i < FixedModuleCounts.Length; i++)
                {
                    writer.WriteLine(Module.AllModules[i].Name);
                }

                writer.WriteLine("Head");
                writer.WriteLine("Draw");
                writer.WriteLine("Recoil");
                writer.WriteLine("Velocity (m/s)");
                writer.WriteLine("Effective range (m)");

                if (DamageType == 0)
                {
                    writer.WriteLine("Raw KD");
                    writer.WriteLine("AP");
                    writer.WriteLine("Eff. KD");
                }
                else if (DamageType == 1 || DamageType == 2 || DamageType == 3)
                {
                    writer.WriteLine("Chemical payload strength");
                    if (disruptor)
                    {
                        writer.WriteLine("Shield reduction");
                    }
                }

                writer.WriteLine("Reload (s)");
                writer.WriteLine("DPS");
                writer.WriteLine("DPS per volume");
                writer.WriteLine("DPS per cost");
                if (disruptor)
                {
                    writer.WriteLine("Shield reduction / s");
                    writer.WriteLine("Shield RPS / volume");
                    writer.WriteLine("Shield RPS / cost");
                }
                writer.WriteLine("Uptime (belt)");
                writer.WriteLine("DPS (belt, sustained)");
                writer.WriteLine("DPS per volume (sustained)");
                writer.WriteLine("DPS per cost (sustained)");
                if (disruptor)
                {
                    writer.WriteLine("Shield RPS (belt, sustained)");
                    writer.WriteLine("Shield RPS / volume (sustained)");
                    writer.WriteLine("Shield RPS / cost (sustained)");
                }
            }


            if (DamageType == 0)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    writer.WriteLine("\n");
                    writer.WriteLine(topShell.Key);
                    topShell.Value.GetModuleCounts();
                    topShell.Value.WriteShellInfoToFileKinetic(Labels, writer);
                }
            }
            else if (DamageType == 1 || DamageType == 2 || DamageType == 3)
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    writer.WriteLine("\n");
                    writer.WriteLine(topShell.Key);
                    topShell.Value.GetModuleCounts();
                    topShell.Value.WriteShellInfoToFileChem(Labels, writer);
                }
            }
        }
    }
}