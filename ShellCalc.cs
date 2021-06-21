using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using PenCalc;

namespace ApsCalc
{
    public struct ModuleCount
    {
        public int HeadIndex;
        public float Var0Count;
        public float Var1Count;
        public float Var2Count;
    }
    

    public class ShellCalc
    {
        /// <summary>
        /// Takes shell parameters and calculates performance of shell permutations.
        /// </summary>
        /// <param name="barrelCount">Number of barrels</param>
        /// <param name="minGauge">Gauge in mm</param>
        /// 
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

        readonly Dictionary<float, float> LengthBrackets = new()
        {
            { 0f, 1000f },
            { 1000f, 2000f },
            { 2000f, 3000f },
            { 3000f, 4000f },
            { 4000f, 6000f },
            { 6000f, 8000f }
        };

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


        // Store top-performing shells by loader length
        public Shell TopBelt { get; set; } = new Shell();
        public Shell Top1000 { get; set; } = new Shell();
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

            foreach (int index in HeadList)
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
                            yield return new ModuleCount
                            {
                                HeadIndex = index,
                                Var0Count = var0Count,
                                Var1Count = var1Count,
                                Var2Count = var2Count,
                            };
                        }
                    }
                }

            }
        }


        /// <summary>
        /// Compares shell under testing to existing top shells
        /// </summary>
        public void CompareByDamageType(Shell shellUnderTesting)
        {
            Shell shellCopyToCompare = new();
            shellCopyToCompare.CopyStatsFrom(shellUnderTesting);
            if (TestType == 0)
            {
                if (shellCopyToCompare.TotalLength <= 1000f)
                {
                    if (shellCopyToCompare.DpsPerVolumeDict[DamageType] > Top1000.DpsPerVolumeDict[DamageType])
                    {
                        Top1000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 2000f)
                {
                    if (shellCopyToCompare.DpsPerVolumeDict[DamageType] > Top2000.DpsPerVolumeDict[DamageType])
                    {
                        Top2000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 3000f)
                {
                    if (shellCopyToCompare.DpsPerVolumeDict[DamageType] > Top3000.DpsPerVolumeDict[DamageType])
                    {
                        Top3000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 4000f)
                {
                    if (shellCopyToCompare.DpsPerVolumeDict[DamageType] > Top4000.DpsPerVolumeDict[DamageType])
                    {
                        Top4000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 6000f)
                {
                    if (shellCopyToCompare.DpsPerVolumeDict[DamageType] > Top6000.DpsPerVolumeDict[DamageType])
                    {
                        Top6000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 8000f)
                {
                    if (shellCopyToCompare.DpsPerVolumeDict[DamageType] > Top8000.DpsPerVolumeDict[DamageType])
                    {
                        Top8000 = shellCopyToCompare;
                    }
                }
            }
            else if (TestType == 1)
            {
                if (shellCopyToCompare.TotalLength <= 1000f)
                {
                    if (shellCopyToCompare.DpsPerCostDict[DamageType] > Top1000.DpsPerCostDict[DamageType])
                    {
                        Top1000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 2000f)
                {
                    if (shellCopyToCompare.DpsPerCostDict[DamageType] > Top2000.DpsPerCostDict[DamageType])
                    {
                        Top2000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 3000f)
                {
                    if (shellCopyToCompare.DpsPerCostDict[DamageType] > Top3000.DpsPerCostDict[DamageType])
                    {
                        Top3000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 4000f)
                {
                    if (shellCopyToCompare.DpsPerCostDict[DamageType] > Top4000.DpsPerCostDict[DamageType])
                    {
                        Top4000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 6000f)
                {
                    if (shellCopyToCompare.DpsPerCostDict[DamageType] > Top6000.DpsPerCostDict[DamageType])
                    {
                        Top6000 = shellCopyToCompare;
                    }
                }
                else if (shellCopyToCompare.TotalLength <= 8000f)
                {
                    if (shellCopyToCompare.DpsPerCostDict[DamageType] > Top8000.DpsPerCostDict[DamageType])
                    {
                        Top8000 = shellCopyToCompare;
                    }
                }
            }
        }


        /// <summary>
        /// Compares shell under testing to existing top shells
        /// </summary>
        public void CompareByDamageTypeBelt(Shell shellUnderTestingBelt)
        {
            Shell shellCopyToCompareBelt = new();
            shellCopyToCompareBelt.CopyStatsFrom(shellUnderTestingBelt);
            shellCopyToCompareBelt.IsBelt = true;

            if (shellCopyToCompareBelt.TotalLength <= 1000f)
            {
                if (TestType == 0)
                {
                    if (shellCopyToCompareBelt.DpsPerVolumeDict[DamageType] > TopBelt.DpsPerVolumeDict[DamageType])
                    {
                        TopBelt = shellCopyToCompareBelt;
                    }
                }
                else if (TestType == 1)
                {
                    if (shellCopyToCompareBelt.DpsPerCostDict[DamageType] > TopBelt.DpsPerCostDict[DamageType])
                    {
                        TopBelt = shellCopyToCompareBelt;
                    }
                }
            }
        }

        /// <summary>
        /// Optimize DPS per cost or volume by adjusting rail draw
        /// </summary>
        public void OptimizeDrawByDamageType(Shell shellUnderTesting)
        {
            float optimalDraw = 0;
            shellUnderTesting.CalculateMaxDraw();
            float maxDraw = MathF.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
            float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);

            // Length restrictions compartmentalize results by loader size and apply max length restrictions
            if (maxDraw < minDraw)
            {
                foreach (float damageType in shellUnderTesting.DpsPerVolumeDict.Keys)
                {
                    shellUnderTesting.DpsPerVolumeDict[damageType] = 0;
                }
                foreach (float damageType in shellUnderTesting.DpsPerCostDict.Keys)
                {
                    shellUnderTesting.DpsPerCostDict[damageType] = 0;
                }
            }
            else
            {
                if (maxDraw == 0)
                {
                    shellUnderTesting.RailDraw = 0;
                }
                else
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
                        // Check if max is optimal
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
                        // Check if min is optimal
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
                        // Binary search to find optimal without testing every value
                        float topOfRange = maxDraw;
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
                    shellUnderTesting.RailDraw = optimalDraw;
                }
                shellUnderTesting.CalculateDpsByType(DamageType, TargetAC, TargetArmorScheme);
            }
        }

        /// <summary>
        /// DPS per cost or volume by adjusting rail draw
        /// </summary>
        public void OptimizeDrawByDamageTypeBelt(Shell shellUnderTesting)
        {
            shellUnderTesting.CalculateMaxDraw();
            float maxDraw = MathF.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
            float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);

            if (maxDraw < minDraw)
            {
                foreach (float damageType in shellUnderTesting.DpsPerVolumeDict.Keys)
                {
                    shellUnderTesting.DpsPerVolumeDict[damageType] = 0;
                }
                foreach (float damageType in shellUnderTesting.DpsPerCostDict.Keys)
                {
                    shellUnderTesting.DpsPerCostDict[damageType] = 0;
                }
            }
            else
            {
                if (maxDraw == 0)
                {
                    shellUnderTesting.RailDraw = 0;
                }
                else
                {
                    float optimalDraw = 0;
                    float bottomScore = 0;
                    float topScore = 0;
                    float midRangeLower = 0;
                    float midRangeLowerScore = 0;
                    float midRangeUpper = 0;
                    float midRangeUpperScore = 0;

                    shellUnderTesting.RailDraw = minDraw;
                    shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                    if (TestType == 0)
                    {
                        bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                    }
                    else if (TestType == 1)
                    {
                        bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                    }

                    shellUnderTesting.RailDraw = maxDraw;
                    shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
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
                        // Check if max is optimal
                        shellUnderTesting.RailDraw = maxDraw - 1f;
                        shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
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
                        // Check if min is optimal
                        shellUnderTesting.RailDraw = minDraw;
                        shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                        if (TestType == 0)
                        {
                            bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                        }

                        shellUnderTesting.RailDraw = minDraw + 1f;
                        shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
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
                        // Binary search to find optimal without testing every value
                        float topOfRange = maxDraw;
                        float bottomOfRange = 0;

                        while (topOfRange - bottomOfRange > 1)
                        {
                            midRangeLower = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                            midRangeUpper = midRangeLower + 1f;

                            shellUnderTesting.RailDraw = midRangeLower;
                            shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                            if (TestType == 0)
                            {
                                midRangeLowerScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                midRangeLowerScore = shellUnderTesting.DpsPerCostDict[DamageType];
                            }

                            shellUnderTesting.RailDraw = midRangeUpper;
                            shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
                            if (TestType == 0)
                            {
                                midRangeUpperScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                midRangeUpperScore = shellUnderTesting.DpsPerCostDict[DamageType];
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
                    shellUnderTesting.RailDraw = optimalDraw;
                }
                shellUnderTesting.CalculateDpsByTypeBelt(DamageType, TargetAC, TargetArmorScheme);
            }
        }

        /// <summary>
        /// Optimize DPS per cost or volume by adjusting number of RG casings
        /// </summary>
        public void OptimizeRGCasingsByDamageType(Shell shellUnderTesting, float minLength, float maxLength)
        {
            maxLength = MathF.Min(MaxShellLength, maxLength);
            // Calculate max rg casings by length, module count, and input
            shellUnderTesting.GetModuleCounts();
            float maxRG = MathF.Floor((maxLength - shellUnderTesting.TotalLength) / shellUnderTesting.Gauge);
            maxRG = MathF.Min(maxRG, 20f - shellUnderTesting.ModuleCountTotal);
            maxRG = MathF.Min(maxRG, MaxRGInput);

            // Calculate min rg casings from draw for min velocity and effective range
            shellUnderTesting.CalculateVelocityModifier();
            shellUnderTesting.CalculateRecoil();
            float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);
            float minRG = MathF.Ceiling((minDraw - shellUnderTesting.MaxDraw) / shellUnderTesting.GaugeCoefficient / 6250f);

            // Calculate min rg casings to fill loader length for rg-only shells
            if (MaxGPInput == 0)
            {
                minRG = MathF.Max(minRG, MathF.Ceiling((minLength - shellUnderTesting.ProjectileLength) / shellUnderTesting.Gauge));
            }
            minRG = MathF.Max(0, minRG);


            if (maxRG < minRG)
            {
                foreach (float damageType in shellUnderTesting.DpsPerVolumeDict.Keys)
                {
                    shellUnderTesting.DpsPerVolumeDict[damageType] = 0;
                }
                foreach (float damageType in shellUnderTesting.DpsPerCostDict.Keys)
                {
                    shellUnderTesting.DpsPerCostDict[damageType] = 0;
                }
            }
            else
            {
                if (maxRG == 0)
                {
                    shellUnderTesting.RGCasingCount = 0;
                }
                else
                {
                    float optimalRGCount = 0;
                    float bottomScore = 0;
                    float topScore = 0;
                    float midRangeLower = 0;
                    float midRangeLowerScore = 0;
                    float midRangeUpper = 0;
                    float midRangeUpperScore = 0;

                    shellUnderTesting.RGCasingCount = minRG;
                    OptimizeDrawByDamageType(shellUnderTesting);
                    if (TestType == 0)
                    {
                        bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                    }
                    else if (TestType == 1)
                    {
                        bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                    }

                    shellUnderTesting.RGCasingCount = maxRG;
                    OptimizeDrawByDamageType(shellUnderTesting);
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
                        // Check if max is optimal
                        shellUnderTesting.RGCasingCount = maxRG - 1f;
                        OptimizeDrawByDamageType(shellUnderTesting);
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
                            optimalRGCount = maxRG;
                        }
                    }
                    else
                    {
                        // Check if min is optimal
                        shellUnderTesting.RGCasingCount = minRG;
                        OptimizeDrawByDamageType(shellUnderTesting);
                        if (TestType == 0)
                        {
                            bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                        }

                        shellUnderTesting.RGCasingCount = minRG + 1f;
                        OptimizeDrawByDamageType(shellUnderTesting);
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
                            optimalRGCount = minRG;
                        }
                    }

                    if (optimalRGCount == 0)
                    {
                        // Binary search to find optimal without testing every value
                        float topOfRange = maxRG;
                        float bottomOfRange = minRG;

                        while (topOfRange - bottomOfRange > 1)
                        {
                            midRangeLower = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                            midRangeUpper = midRangeLower + 1f;

                            shellUnderTesting.RGCasingCount = midRangeLower;
                            OptimizeDrawByDamageType(shellUnderTesting);
                            if (TestType == 0)
                            {
                                midRangeLowerScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                midRangeLowerScore = shellUnderTesting.DpsPerCostDict[DamageType];
                            }

                            shellUnderTesting.RGCasingCount = midRangeUpper;
                            OptimizeDrawByDamageType(shellUnderTesting);
                            if (TestType == 0)
                            {
                                midRangeUpperScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                midRangeUpperScore = shellUnderTesting.DpsPerCostDict[DamageType];
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
                            optimalRGCount = midRangeLower;
                        }
                        else
                        {
                            optimalRGCount = midRangeUpper;
                        }
                    }
                    shellUnderTesting.RGCasingCount = optimalRGCount;
                }
                shellUnderTesting.CalculateLengths();
                OptimizeDrawByDamageType(shellUnderTesting);
            }
        }


        /// <summary>
        /// Optimize DPS per cost or volume by adjusting number of RG casings
        /// </summary>
        public void OptimizeRGCasingsByDamageTypeBelt(Shell shellUnderTesting)
        {
            // Calculate max rg casings by length, module count, and input
            shellUnderTesting.GetModuleCounts();
            float maxRG = MathF.Floor((1000f - shellUnderTesting.TotalLength) / shellUnderTesting.Gauge);
            maxRG = MathF.Min(maxRG, 20f - shellUnderTesting.ModuleCountTotal);
            maxRG = MathF.Min(maxRG, MaxRGInput);

            // Calculate min rg casings from draw for min velocity and effective range
            shellUnderTesting.CalculateVelocityModifier();
            shellUnderTesting.CalculateRecoil();
            float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput);
            float minRG = MathF.Ceiling((minDraw - shellUnderTesting.MaxDraw) / shellUnderTesting.GaugeCoefficient / 6250f);
            minRG = MathF.Max(0, minRG);

            // Calculate min rg casings to fill loader

            if (maxRG < minRG)
            {
                foreach (float damageType in shellUnderTesting.DpsPerVolumeDict.Keys)
                {
                    shellUnderTesting.DpsPerVolumeDict[damageType] = 0;
                }
                foreach (float damageType in shellUnderTesting.DpsPerCostDict.Keys)
                {
                    shellUnderTesting.DpsPerCostDict[damageType] = 0;
                }
            }
            else
            {
                if (maxRG == 0)
                {
                    shellUnderTesting.RGCasingCount = 0;
                }
                else
                {
                    float optimalRGCount = 0;
                    float bottomScore = 0;
                    float topScore = 0;
                    float midRangeLower = 0;
                    float midRangeLowerScore = 0;
                    float midRangeUpper = 0;
                    float midRangeUpperScore = 0;

                    shellUnderTesting.RGCasingCount = minRG;
                    OptimizeDrawByDamageTypeBelt(shellUnderTesting);
                    if (TestType == 0)
                    {
                        bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                    }
                    else if (TestType == 1)
                    {
                        bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                    }

                    shellUnderTesting.RGCasingCount = maxRG;
                    OptimizeDrawByDamageTypeBelt(shellUnderTesting);
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
                        // Check if max is optimal
                        shellUnderTesting.RGCasingCount = maxRG - 1f;
                        OptimizeDrawByDamageTypeBelt(shellUnderTesting);
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
                            optimalRGCount = maxRG;
                        }
                    }
                    else
                    {
                        // Check if min is optimal
                        shellUnderTesting.RGCasingCount = minRG;
                        OptimizeDrawByDamageTypeBelt(shellUnderTesting);
                        if (TestType == 0)
                        {
                            bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                        }

                        shellUnderTesting.RGCasingCount = minRG + 1f;
                        OptimizeDrawByDamageTypeBelt(shellUnderTesting);
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
                            optimalRGCount = minRG;
                        }
                    }

                    if (optimalRGCount == 0)
                    {
                        // Binary search to find optimal without testing every value
                        float topOfRange = maxRG;
                        float bottomOfRange = minRG;

                        while (topOfRange - bottomOfRange > 1)
                        {
                            midRangeLower = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                            midRangeUpper = midRangeLower + 1f;

                            shellUnderTesting.RGCasingCount = midRangeLower;
                            OptimizeDrawByDamageTypeBelt(shellUnderTesting);
                            if (TestType == 0)
                            {
                                midRangeLowerScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                midRangeLowerScore = shellUnderTesting.DpsPerCostDict[DamageType];
                            }

                            shellUnderTesting.RGCasingCount = midRangeUpper;
                            OptimizeDrawByDamageTypeBelt(shellUnderTesting);
                            if (TestType == 0)
                            {
                                midRangeUpperScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                            }
                            else if (TestType == 1)
                            {
                                midRangeUpperScore = shellUnderTesting.DpsPerCostDict[DamageType];
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
                            optimalRGCount = midRangeLower;
                        }
                        else
                        {
                            optimalRGCount = midRangeUpper;
                        }
                    }
                    shellUnderTesting.RGCasingCount = optimalRGCount;
                }
                shellUnderTesting.CalculateLengths();
                OptimizeDrawByDamageTypeBelt(shellUnderTesting);
            }
        }

        /// <summary>
        /// Optimize DPS per cost or volume by adjusting number of RG casings
        /// </summary>
        public void OptimizeGPCasingsByDamageType(Shell shellUnderTesting, float minLength, float maxLength)
        {
            // Calculate max GP casings
            maxLength = MathF.Min(MaxShellLength, maxLength);
            shellUnderTesting.CalculateLengths();
            // Multiply and divide by 100 to get floor to two decimal places
            float maxGP = MathF.Min(MaxGPInput, MathF.Floor(100f * maxLength / shellUnderTesting.Gauge) / 100f);
            shellUnderTesting.GetModuleCounts();
            maxGP = MathF.Min(maxGP, 20f - shellUnderTesting.ModuleCountTotal);

            if (maxGP == 0)
            {
                shellUnderTesting.GPCasingCount = 0;
            }
            else
            {
                float minGP;
                float optimalGPCount = 0;
                float lengthError;
                float bottomScore = 0;
                float topScore = 0;
                float midRangeLower = 0;
                float midRangeLowerScore = 0;
                float midRangeUpper = 0;
                float midRangeUpperScore = 0;

                // Determine minimum GP count by length
                shellUnderTesting.GPCasingCount = 0;
                OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
                lengthError = MathF.Max(minLength - shellUnderTesting.TotalLength, 0);
                minGP = MathF.Min(lengthError / shellUnderTesting.Gauge, maxGP);

                shellUnderTesting.GPCasingCount = minGP;
                OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
                if (TestType == 0)
                {
                    bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                }
                else if (TestType == 1)
                {
                    bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                }

                shellUnderTesting.GPCasingCount = maxGP;
                OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
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
                    // Check if max is optimal
                    shellUnderTesting.GPCasingCount = MathF.Max(maxGP - 1f, minGP);
                    OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);

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
                        optimalGPCount = maxGP;
                    }
                }
                else
                {
                    // Check if min is optimal
                    shellUnderTesting.GPCasingCount = minGP;
                    OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
                    if (TestType == 0)
                    {
                        bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                    }
                    else if (TestType == 1)
                    {
                        bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                    }

                    shellUnderTesting.GPCasingCount = MathF.Min(minGP + 1f, maxGP);
                    OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
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
                        optimalGPCount = minGP;
                    }
                }

                if (optimalGPCount == 0)
                {
                    // Binary search to find optimal without testing every value
                    float topOfRange = maxGP;
                    float bottomOfRange = minGP;

                    while (topOfRange - bottomOfRange > 0.01)
                    {
                        midRangeLower = MathF.Floor((topOfRange + bottomOfRange) * 50f) / 100f;
                        midRangeUpper = midRangeLower + 0.01f;

                        shellUnderTesting.GPCasingCount = midRangeLower;
                        OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
                        if (TestType == 0)
                        {
                            midRangeLowerScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            midRangeLowerScore = shellUnderTesting.DpsPerCostDict[DamageType];
                        }

                        shellUnderTesting.GPCasingCount = midRangeUpper;
                        OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
                        if (TestType == 0)
                        {
                            midRangeUpperScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            midRangeUpperScore = shellUnderTesting.DpsPerCostDict[DamageType];
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
                        optimalGPCount = midRangeLower;
                    }
                    else
                    {
                        optimalGPCount = midRangeUpper;
                    }
                }
                shellUnderTesting.GPCasingCount = optimalGPCount;
            }
            shellUnderTesting.CalculateLengths();
            OptimizeRGCasingsByDamageType(shellUnderTesting, minLength, maxLength);
        }


        /// <summary>
        /// Optimize DPS per cost or volume by adjusting number of RG casings
        /// </summary>
        public void OptimizeGPCasingsByDamageTypeBelt(Shell shellUnderTesting)
        {
            // Calculate max GP casings
            shellUnderTesting.CalculateLengths();
            // Multiply and divide by 100 to get floor to two decimal places
            float maxGP = MathF.Min(MaxGPInput, MathF.Floor(100000f / shellUnderTesting.Gauge) / 100f);
            shellUnderTesting.GetModuleCounts();
            maxGP = MathF.Min(maxGP, 20f - shellUnderTesting.ModuleCountTotal);

            if (maxGP == 0)
            {
                shellUnderTesting.GPCasingCount = 0;
            }
            else
            {
                float minGP = 0;
                float optimalGPCount = 0;
                float bottomScore = 0;
                float topScore = 0;
                float midRangeLower = 0;
                float midRangeLowerScore = 0;
                float midRangeUpper = 0;
                float midRangeUpperScore = 0;

                shellUnderTesting.GPCasingCount = minGP;
                OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
                if (TestType == 0)
                {
                    bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                }
                else if (TestType == 1)
                {
                    bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                }

                shellUnderTesting.GPCasingCount = maxGP;
                OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
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
                    shellUnderTesting.GPCasingCount = maxGP - 0.01f;
                    OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
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
                        optimalGPCount = maxGP;
                    }
                }
                else
                {
                    // Check if min draw is optimal
                    shellUnderTesting.GPCasingCount = minGP;
                    OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
                    if (TestType == 0)
                    {
                        bottomScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                    }
                    else if (TestType == 1)
                    {
                        bottomScore = shellUnderTesting.DpsPerCostDict[DamageType];
                    }

                    shellUnderTesting.GPCasingCount = minGP + 0.01f;
                    OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
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
                        optimalGPCount = minGP;
                    }
                }

                if (optimalGPCount == 0)
                {
                    // Binary search to find optimal draw without testing every value
                    float topOfRange = maxGP;
                    float bottomOfRange = minGP;

                    while (topOfRange - bottomOfRange > 1)
                    {
                        midRangeLower = MathF.Floor((topOfRange + bottomOfRange) * 50f) / 100f;
                        midRangeUpper = midRangeLower + 0.01f;

                        shellUnderTesting.GPCasingCount = midRangeLower;
                        OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
                        if (TestType == 0)
                        {
                            midRangeLowerScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            midRangeLowerScore = shellUnderTesting.DpsPerCostDict[DamageType];
                        }

                        shellUnderTesting.GPCasingCount = midRangeUpper;
                        OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
                        if (TestType == 0)
                        {
                            midRangeUpperScore = shellUnderTesting.DpsPerVolumeDict[DamageType];
                        }
                        else if (TestType == 1)
                        {
                            midRangeUpperScore = shellUnderTesting.DpsPerCostDict[DamageType];
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
                        optimalGPCount = midRangeLower;
                    }
                    else
                    {
                        optimalGPCount = midRangeUpper;
                    }
                }
                shellUnderTesting.GPCasingCount = optimalGPCount;
            }
            shellUnderTesting.CalculateLengths();
            OptimizeRGCasingsByDamageTypeBelt(shellUnderTesting);
        }

        /// <summary>
        /// Finds optimal shell configuration by loader length within given parameters
        /// </summary>
        public void ShellTest()
        {
            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                // Set up shell
                Shell shellUnderTesting = new();
                shellUnderTesting.BarrelCount = BarrelCount;
                shellUnderTesting.BoreEvacuator = BoreEvacuator;
                shellUnderTesting.HeadModule = Module.AllModules[counts.HeadIndex];
                shellUnderTesting.BaseModule = BaseModule;
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                shellUnderTesting.Gauge = MaxGauge; // Min and max gauge should be equal to "gauge" from Parallel.For in Program
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                shellUnderTesting.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;

                // Check length <= max allowable length
                shellUnderTesting.CalculateLengths();
                if (shellUnderTesting.TotalLength <= MaxShellLength)
                {
                    shellUnderTesting.CalculateVelocityModifier();
                    shellUnderTesting.CalculateDamageModifierByType(DamageType);
                    shellUnderTesting.CalculateDamageByType(DamageType);
                    shellUnderTesting.CalculateReloadTime();
                    shellUnderTesting.CalculateBeltfedReload();

                    // Compare shells in each length bracket
                    foreach (float minLength in LengthBrackets.Keys)
                    {
                        OptimizeGPCasingsByDamageType(shellUnderTesting, minLength, LengthBrackets[minLength]);
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateVelocity();
                        CompareByDamageType(shellUnderTesting);
                    }

                    if (shellUnderTesting.ProjectileLength <= 1000f)
                    {
                        OptimizeGPCasingsByDamageTypeBelt(shellUnderTesting);
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateVelocity();
                        CompareByDamageTypeBelt(shellUnderTesting);
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
            if (TopBelt.DpsPerVolumeDict[DamageType] > 0 || TopBelt.DpsPerCostDict[DamageType] > 0)
            {
                TopShellsLocal.Add(TopBelt);
            }

            if (Top1000.DpsPerVolumeDict[DamageType] > 0 || Top1000.DpsPerCostDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Top1000);
            }

            if (Top2000.DpsPerVolumeDict[DamageType] > 0 || Top2000.DpsPerCostDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Top2000);
            }

            if (Top3000.DpsPerVolumeDict[DamageType] > 0 || Top3000.DpsPerCostDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Top3000);
            }

            if (Top4000.DpsPerVolumeDict[DamageType] > 0 || Top4000.DpsPerCostDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Top4000);
            }

            if (Top6000.DpsPerVolumeDict[DamageType] > 0 || Top6000.DpsPerCostDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Top6000);
            }

            if (Top8000.DpsPerVolumeDict[DamageType] > 0 || Top8000.DpsPerCostDict[DamageType] > 0)
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
        public void WriteTopShellsToConsole()
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
    }
}