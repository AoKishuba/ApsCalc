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
        /// <param name="headList">List of module indices for every module to be used as the head</param>
        /// <param name="baseModule">The special base module, if any</param>
        /// <param name="fixedModuleCounts">An array of integers representing the number of shells at that index in the module list</param>
        /// <param name="fixedModuleTotal">Minimum number of modules on every shell</param>
        /// <param name="variableModuleIndices">Module indices of the modules to be used in varying numbers in testing</param>
        /// <param name="maxGPInput">Max desired number of gunpowder casings</param>
        /// <param name="boreEvacuator">True if bore evacuator is used</param>
        /// <param name="maxRGInput">Max desired number of railgun casings</param>
        /// <param name="maxShellLengthInput">Max desired shell length in mm</param>
        /// <param name="maxDrawInput">Max desired rail draw</param>
        /// <param name="minVelocityInput">Min desired velocity</param>
        /// <param name="minEffectiveRangeInput">Min desired effective range</param>
        /// <param name="targetAC">Armor class of the target for kinetic damage calculations</param>
        /// <param name="damageType">0 for kinetic, 1 for chemical</param>
        /// <param name="targetArmorScheme">Target armor scheme, from the Pencalc namespace</param>
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
            Labels = labels;
        }

        /// <summary>
        /// Creates an instance of ShellCalc for multithreading
        /// </summary>
        /// <param name="damageType">0 for kinetic, 1 for chem, 2 for pendepth, 3 for shield disruption</param>
        public ShellCalc(int damageType)
        {
            DamageType = damageType;
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
        public bool Labels { get; }

        // Testing data
        public int TestComparisons { get; set; }
        public int TestRejectLength { get; set; }
        public int TestRejectVelocityOrRange { get; set; }
        public int TestRejectPen { get; set; }
        public int TestTotal { get; set; }
        public List<int> TestCounts { get; set; }


        // Store top-DPS shells by loader length
        public Shell TopDps1000 { get; set; } = new Shell();
        public Shell TopDpsBelt { get; set; } = new Shell();
        public Shell TopDps2000 { get; set; } = new Shell();
        public Shell TopDps4000 { get; set; } = new Shell();
        public Shell TopDps6000 { get; set; } = new Shell();
        public Shell TopDps8000 { get; set; } = new Shell();

        public Dictionary<string, Shell> TopDpsShells { get; set; } = new Dictionary<string, Shell>();
        public List<Shell> TopDpsShellsLocal { get; set; } = new List<Shell>();


        /// <summary>
        /// The iterable generator for shells.  Generates all shell possible permutations of shell within the given parameters.
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
                            }                            
                        }
                    }
                }
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
        /// Calculates damage output for pendepth shells
        /// </summary>
        public void PendepthTest()
        {
            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                Shell shellUnderTesting = new Shell();
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
                    shellUnderTesting.CalculateLoaderVolume();
                    shellUnderTesting.CalculateVelocityModifier();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput, maxDraw);

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
                                // Binary search to find optimal draw without testing every value
                                float bottomOfRange = minDraw;
                                float bottomScore = 0;
                                float topOfRange = maxDraw;
                                float topScore = 0;
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;


                                shellUnderTesting.RailDraw = minDraw;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                bottomScore = shellUnderTesting.ChemDpsPerVolume;

                                shellUnderTesting.RailDraw = maxDraw;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                topScore = shellUnderTesting.ChemDpsPerVolume;

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTesting.RailDraw = maxDraw - 1f;
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    bottomScore = shellUnderTesting.ChemDpsPerVolume;

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
                                    topScore = shellUnderTesting.ChemDpsPerVolume;

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

                                if (optimalDraw == 0)
                                {
                                    topOfRange = maxDraw;
                                    bottomOfRange = 0;
                                    while (topOfRange - bottomOfRange > 1)
                                    {

                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTesting.RailDraw = midRangeLower;
                                        shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                        midRangeLowerScore = shellUnderTesting.ChemDpsPerVolume;

                                        shellUnderTesting.RailDraw = midRangeUpper;
                                        shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                        midRangeUpperScore = shellUnderTesting.ChemDpsPerVolume;

                                        // Determine which half of the range to continue testing
                                        // Midrange upper will equal a lot of the time for pendepth
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
                                    // Take the better of the two remaining values
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


                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > TopDps1000.ChemDpsPerVolume)
                                {
                                    TopDps1000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 2000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > TopDps2000.ChemDpsPerVolume)
                                {
                                    TopDps2000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 4000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > TopDps4000.ChemDpsPerVolume)
                                {
                                    TopDps4000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 6000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > TopDps6000.ChemDpsPerVolume)
                                {
                                    TopDps6000 = shellUnderTesting;
                                }
                            }
                            else if (shellUnderTesting.TotalLength <= 8000f)
                            {
                                if (shellUnderTesting.ChemDpsPerVolume > TopDps8000.ChemDpsPerVolume)
                                {
                                    TopDps8000 = shellUnderTesting;
                                }
                            }

                            // Beltfed testing
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                Shell shellUnderTestingBelt = new Shell();
                                shellUnderTestingBelt.BarrelCount = BarrelCount;
                                shellUnderTestingBelt.BoreEvacuator = BoreEvacuator;
                                shellUnderTestingBelt.HeadModule = Module.AllModules[counts.HeadIndex];
                                shellUnderTestingBelt.BaseModule = BaseModule;
                                FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                                shellUnderTestingBelt.Gauge = counts.Gauge;
                                shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                                shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                                shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                                shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                                shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                                shellUnderTestingBelt.CalculateLengths();
                                shellUnderTestingBelt.CalculateLoaderVolume();
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
                                    float bottomOfRange = minDraw;
                                    float bottomScore = 0;
                                    float topOfRange = maxDraw;
                                    float topScore = 0;
                                    float midRangeLower = 0;
                                    float midRangeLowerScore = 0;
                                    float midRangeUpper = 0;
                                    float midRangeUpperScore = 0;

                                    shellUnderTestingBelt.RailDraw = minDraw;
                                    shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                    bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                    shellUnderTestingBelt.RailDraw = maxDraw;
                                    shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                    topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                    if (topScore > bottomScore)
                                    {
                                        // Check if max draw is optimal
                                        shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

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
                                        topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                        if (bottomScore > topScore)
                                        {
                                            optimalDraw = minDraw;
                                        }
                                    }

                                    while (topOfRange - bottomOfRange > 1)
                                    {
                                        midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                        midRangeUpper = midRangeLower + 1f;

                                        shellUnderTestingBelt.RailDraw = midRangeLower;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                        shellUnderTestingBelt.RailDraw = midRangeUpper;
                                        shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);
                                        midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                        // Determine which half of the range to continue testing
                                        if (midRangeLowerScore >= midRangeUpperScore)
                                        {
                                            topOfRange = midRangeLower;
                                        }
                                        else
                                        {
                                            bottomOfRange = midRangeUpper;
                                        }
                                    }
                                    // Take the better of the two remaining values
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        optimalDraw = midRangeLower;
                                    }
                                    else
                                    {
                                        optimalDraw = midRangeUpper;
                                    }
                                }

                                // Check performance against top shells
                                shellUnderTestingBelt.RailDraw = optimalDraw;
                                shellUnderTestingBelt.CalculateEffectiveRange();
                                shellUnderTestingBelt.CalculatePendepthDps(TargetArmorScheme);

                                if (shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained > TopDpsBelt.ChemDpsPerVolumeBeltSustained)
                                {
                                    shellUnderTestingBelt.IsBelt = true;
                                    TopDpsBelt = shellUnderTestingBelt;
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
                Shell shellUnderTesting = new Shell();
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
                    shellUnderTesting.CalculateLoaderVolume();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput, maxDraw);
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
                            float bottomOfRange = minDraw;
                            float bottomScore = 0;
                            float topOfRange = maxDraw;
                            float topScore = 0;
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            bottomScore = shellUnderTesting.KineticDpsPerVolume;

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            topScore = shellUnderTesting.KineticDpsPerVolume;

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                bottomScore = shellUnderTesting.KineticDpsPerVolume;

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
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                topScore = shellUnderTesting.KineticDpsPerVolume;

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
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                midRangeLowerScore = shellUnderTesting.KineticDpsPerVolume;

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                midRangeUpperScore = shellUnderTesting.KineticDpsPerVolume;

                                // Determine which half of the range to continue testing
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    topOfRange = midRangeLower;
                                }
                                else
                                {
                                    bottomOfRange = midRangeUpper;
                                }
                            }
                            // Take the better of the two remaining values
                            if (midRangeLowerScore >= midRangeUpperScore)
                            {
                                optimalDraw = midRangeLower;
                            }
                            else
                            {
                                optimalDraw = midRangeUpper;
                            }
                        }

                        // Check performance against top shells
                        shellUnderTesting.RailDraw = optimalDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateAP();
                        shellUnderTesting.CalculateKineticDamage();
                        shellUnderTesting.CalculateKineticDps(TargetAC);

                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            if (shellUnderTesting.KineticDpsPerVolume > TopDps1000.KineticDpsPerVolume)
                            {
                                TopDps1000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 2000f)
                        {
                            if (shellUnderTesting.KineticDpsPerVolume > TopDps2000.KineticDpsPerVolume)
                            {
                                TopDps2000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 4000f)
                        {
                            if (shellUnderTesting.KineticDpsPerVolume > TopDps4000.KineticDpsPerVolume)
                            {
                                TopDps4000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 6000f)
                        {
                            if (shellUnderTesting.KineticDpsPerVolume > TopDps6000.KineticDpsPerVolume)
                            {
                                TopDps6000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 8000f)
                        {
                            if (shellUnderTesting.KineticDpsPerVolume > TopDps8000.KineticDpsPerVolume)
                            {
                                TopDps8000 = shellUnderTesting;
                            }
                        }

                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new Shell();
                            shellUnderTestingBelt.BarrelCount = BarrelCount;
                            shellUnderTestingBelt.BoreEvacuator = BoreEvacuator;
                            shellUnderTestingBelt.HeadModule = Module.AllModules[counts.HeadIndex];
                            shellUnderTestingBelt.BaseModule = BaseModule;
                            FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                            shellUnderTestingBelt.Gauge = counts.Gauge;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                            shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                            shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                            shellUnderTestingBelt.CalculateLengths();
                            shellUnderTestingBelt.CalculateLoaderVolume();
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
                                float bottomOfRange = minDraw;
                                float bottomScore = 0;
                                float topOfRange = maxDraw;
                                float topScore = 0;
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;
                                optimalDraw = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolume();
                                shellUnderTestingBelt.CalculateChargerVolume();
                                shellUnderTestingBelt.CalculateVolumePerIntake();
                                shellUnderTestingBelt.CalculateVelocity();
                                shellUnderTestingBelt.CalculateAP();
                                shellUnderTestingBelt.CalculateKineticDamage();
                                shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                bottomScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolume();
                                shellUnderTestingBelt.CalculateChargerVolume();
                                shellUnderTestingBelt.CalculateVolumePerIntake();
                                shellUnderTestingBelt.CalculateVelocity();
                                shellUnderTestingBelt.CalculateAP();
                                shellUnderTestingBelt.CalculateKineticDamage();
                                shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                topScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    bottomScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;

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
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    topScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTestingBelt.RailDraw = midRangeLower;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    midRangeLowerScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;

                                    shellUnderTestingBelt.RailDraw = midRangeUpper;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateVelocity();
                                    shellUnderTestingBelt.CalculateAP();
                                    shellUnderTestingBelt.CalculateKineticDamage();
                                    shellUnderTestingBelt.CalculateKineticDps(TargetAC);
                                    midRangeUpperScore = shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained;

                                    // Determine which half of the range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take the better of the two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
                            }

                            // Check performance against top shells
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateRecoil();
                            shellUnderTestingBelt.CalculateRecoilVolume();
                            shellUnderTestingBelt.CalculateChargerVolume();
                            shellUnderTestingBelt.CalculateVolumePerIntake();
                            shellUnderTestingBelt.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();
                            shellUnderTestingBelt.CalculateAP();
                            shellUnderTestingBelt.CalculateKineticDamage();
                            shellUnderTestingBelt.CalculateKineticDps(TargetAC);

                            if (shellUnderTestingBelt.KineticDpsPerVolumeBeltSustained > TopDpsBelt.KineticDpsPerVolumeBeltSustained)
                            {
                                shellUnderTestingBelt.IsBelt = true;
                                TopDpsBelt = shellUnderTestingBelt;
                            }
                        }                        
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
                Shell shellUnderTesting = new Shell();
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
                    shellUnderTesting.CalculateLoaderVolume();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput, maxDraw);
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
                            float bottomOfRange = minDraw;
                            float bottomScore = 0;
                            float topOfRange = maxDraw;
                            float topScore = 0;
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateChemDps();
                            bottomScore = shellUnderTesting.ChemDpsPerVolume;

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateChemDps();
                            topScore = shellUnderTesting.ChemDpsPerVolume;

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateChemDps();
                                bottomScore = shellUnderTesting.ChemDpsPerVolume;

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
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateChemDps();
                                topScore = shellUnderTesting.ChemDpsPerVolume;

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
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateChemDps();
                                midRangeLowerScore = shellUnderTesting.ChemDpsPerVolume;

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateChemDps();
                                midRangeUpperScore = shellUnderTesting.ChemDpsPerVolume;

                                // Determine which half of the range to continue testing
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    topOfRange = midRangeLower;
                                }
                                else
                                {
                                    bottomOfRange = midRangeUpper;
                                }
                            }
                            // Take the better of the two remaining values
                            if (midRangeLowerScore >= midRangeUpperScore)
                            {
                                optimalDraw = midRangeLower;
                            }
                            else
                            {
                                optimalDraw = midRangeUpper;
                            }
                        }

                        // Check performance against top shells
                        shellUnderTesting.RailDraw = optimalDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateChemDps();
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            if (shellUnderTesting.ChemDpsPerVolume > TopDps1000.ChemDpsPerVolume)
                            {
                                TopDps1000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 2000f)
                        {
                            if (shellUnderTesting.ChemDpsPerVolume > TopDps2000.ChemDpsPerVolume)
                            {
                                TopDps2000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 4000f)
                        {
                            if (shellUnderTesting.ChemDpsPerVolume > TopDps4000.ChemDpsPerVolume)
                            {
                                TopDps4000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 6000f)
                        {
                            if (shellUnderTesting.ChemDpsPerVolume > TopDps6000.ChemDpsPerVolume)
                            {
                                TopDps6000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 8000f)
                        {
                            if (shellUnderTesting.ChemDpsPerVolume > TopDps8000.ChemDpsPerVolume)
                            {
                                TopDps8000 = shellUnderTesting;
                            }
                        }

                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new Shell();
                            shellUnderTestingBelt.BarrelCount = BarrelCount;
                            shellUnderTestingBelt.BoreEvacuator = BoreEvacuator;
                            shellUnderTestingBelt.HeadModule = Module.AllModules[counts.HeadIndex];
                            shellUnderTestingBelt.BaseModule = BaseModule;
                            FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                            shellUnderTestingBelt.Gauge = counts.Gauge;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                            shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                            shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                            shellUnderTestingBelt.CalculateLengths();
                            shellUnderTestingBelt.CalculateLoaderVolume();
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
                                float bottomOfRange = minDraw;
                                float bottomScore = 0;
                                float topOfRange = maxDraw;
                                float topScore = 0;
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolume();
                                shellUnderTestingBelt.CalculateChargerVolume();
                                shellUnderTestingBelt.CalculateVolumePerIntake();
                                shellUnderTestingBelt.CalculateChemDps();
                                bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolume();
                                shellUnderTestingBelt.CalculateChargerVolume();
                                shellUnderTestingBelt.CalculateVolumePerIntake();
                                shellUnderTestingBelt.CalculateChemDps();
                                topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    bottomScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

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
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    topScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTestingBelt.RailDraw = midRangeLower;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    midRangeLowerScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                    shellUnderTestingBelt.RailDraw = midRangeUpper;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateChemDps();
                                    midRangeUpperScore = shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained;

                                    // Determine which half of the range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take the better of the two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
                            }

                            // Check performance against top shells
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateRecoil();
                            shellUnderTestingBelt.CalculateRecoilVolume();
                            shellUnderTestingBelt.CalculateChargerVolume();
                            shellUnderTestingBelt.CalculateVolumePerIntake();
                            shellUnderTestingBelt.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();
                            shellUnderTestingBelt.CalculateChemDps();

                            if (shellUnderTestingBelt.ChemDpsPerVolumeBeltSustained > TopDpsBelt.ChemDpsPerVolumeBeltSustained)
                            {
                                shellUnderTestingBelt.IsBelt = true;
                                TopDpsBelt = shellUnderTestingBelt;
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
                Shell shellUnderTesting = new Shell();
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
                    shellUnderTesting.CalculateLoaderVolume();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput, maxDraw);
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
                            float bottomOfRange = minDraw;
                            float bottomScore = 0;
                            float topOfRange = maxDraw;
                            float topScore = 0;
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateShieldRps();
                            bottomScore = shellUnderTesting.ShieldRpsPerVolume;

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateShieldRps();
                            topScore = shellUnderTesting.ShieldRpsPerVolume;

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                bottomScore = shellUnderTesting.ShieldRpsPerVolume;

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
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                topScore = shellUnderTesting.ShieldRpsPerVolume;

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
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                midRangeLowerScore = shellUnderTesting.ShieldRpsPerVolume;

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateShieldRps();
                                midRangeUpperScore = shellUnderTesting.ShieldRpsPerVolume;

                                // Determine which half of the range to continue testing
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    topOfRange = midRangeLower;
                                }
                                else
                                {
                                    bottomOfRange = midRangeUpper;
                                }
                            }
                            // Take the better of the two remaining values
                            if (midRangeLowerScore >= midRangeUpperScore)
                            {
                                optimalDraw = midRangeLower;
                            }
                            else
                            {
                                optimalDraw = midRangeUpper;
                            }
                        }

                        // Check performance against top shells
                        shellUnderTesting.RailDraw = optimalDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateEffectiveRange();
                        shellUnderTesting.CalculateChemDps();
                        shellUnderTesting.CalculateShieldRps();
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            if (shellUnderTesting.ShieldRpsPerVolume > TopDps1000.ShieldRpsPerVolume)
                            {
                                TopDps1000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 2000f)
                        {
                            if (shellUnderTesting.ShieldRpsPerVolume > TopDps2000.ShieldRpsPerVolume)
                            {
                                TopDps2000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 4000f)
                        {
                            if (shellUnderTesting.ShieldRpsPerVolume > TopDps4000.ShieldRpsPerVolume)
                            {
                                TopDps4000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 6000f)
                        {
                            if (shellUnderTesting.ShieldRpsPerVolume > TopDps6000.ShieldRpsPerVolume)
                            {
                                TopDps6000 = shellUnderTesting;
                            }
                        }
                        else if (shellUnderTesting.TotalLength <= 8000f)
                        {
                            if (shellUnderTesting.ShieldRpsPerVolume > TopDps8000.ShieldRpsPerVolume)
                            {
                                TopDps8000 = shellUnderTesting;
                            }
                        }

                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            Shell shellUnderTestingBelt = new Shell();
                            shellUnderTestingBelt.BarrelCount = BarrelCount;
                            shellUnderTestingBelt.BoreEvacuator = BoreEvacuator;
                            shellUnderTestingBelt.HeadModule = Module.AllModules[counts.HeadIndex];
                            shellUnderTestingBelt.BaseModule = BaseModule;
                            FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);
                            shellUnderTestingBelt.Gauge = counts.Gauge;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                            shellUnderTestingBelt.BodyModuleCounts[VariableModuleIndices[2]] += counts.Var2Count;
                            shellUnderTestingBelt.GPCasingCount = counts.GPCount;
                            shellUnderTestingBelt.RGCasingCount = counts.RGCount;
                            shellUnderTestingBelt.CalculateLengths();
                            shellUnderTestingBelt.CalculateLoaderVolume();
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
                                float bottomOfRange = minDraw;
                                float bottomScore = 0;
                                float topOfRange = maxDraw;
                                float topScore = 0;
                                float midRangeLower = 0;
                                float midRangeLowerScore = 0;
                                float midRangeUpper = 0;
                                float midRangeUpperScore = 0;

                                shellUnderTestingBelt.RailDraw = minDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolume();
                                shellUnderTestingBelt.CalculateChargerVolume();
                                shellUnderTestingBelt.CalculateVolumePerIntake();
                                shellUnderTestingBelt.CalculateShieldRps();
                                bottomScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;

                                shellUnderTestingBelt.RailDraw = maxDraw;
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateRecoilVolume();
                                shellUnderTestingBelt.CalculateChargerVolume();
                                shellUnderTestingBelt.CalculateVolumePerIntake();
                                shellUnderTestingBelt.CalculateShieldRps();
                                topScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTestingBelt.RailDraw = maxDraw - 1f;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    bottomScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;

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
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    topScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;

                                    if (bottomScore > topScore)
                                    {
                                        optimalDraw = minDraw;
                                    }
                                }

                                while (topOfRange - bottomOfRange > 1)
                                {
                                    midRangeLower = (float)Math.Floor((topOfRange + bottomOfRange) / 2f);
                                    midRangeUpper = midRangeLower + 1f;

                                    shellUnderTestingBelt.RailDraw = midRangeLower;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    midRangeLowerScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;

                                    shellUnderTestingBelt.RailDraw = midRangeUpper;
                                    shellUnderTestingBelt.CalculateRecoil();
                                    shellUnderTestingBelt.CalculateRecoilVolume();
                                    shellUnderTestingBelt.CalculateChargerVolume();
                                    shellUnderTestingBelt.CalculateVolumePerIntake();
                                    shellUnderTestingBelt.CalculateShieldRps();
                                    midRangeUpperScore = shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained;

                                    // Determine which half of the range to continue testing
                                    if (midRangeLowerScore >= midRangeUpperScore)
                                    {
                                        topOfRange = midRangeLower;
                                    }
                                    else
                                    {
                                        bottomOfRange = midRangeUpper;
                                    }
                                }
                                // Take the better of the two remaining values
                                if (midRangeLowerScore >= midRangeUpperScore)
                                {
                                    optimalDraw = midRangeLower;
                                }
                                else
                                {
                                    optimalDraw = midRangeUpper;
                                }
                            }

                            // Check performance against top shells
                            shellUnderTestingBelt.RailDraw = optimalDraw;
                            shellUnderTestingBelt.CalculateRecoil();
                            shellUnderTestingBelt.CalculateRecoilVolume();
                            shellUnderTestingBelt.CalculateChargerVolume();
                            shellUnderTestingBelt.CalculateVolumePerIntake();
                            shellUnderTestingBelt.CalculateVelocity();
                            shellUnderTestingBelt.CalculateEffectiveRange();
                            shellUnderTestingBelt.CalculateChemDps();
                            shellUnderTestingBelt.CalculateShieldRps();

                            if (shellUnderTestingBelt.ShieldRpsPerVolumeBeltSustained > TopDpsBelt.ShieldRpsPerVolumeBeltSustained)
                            {
                                shellUnderTestingBelt.IsBelt = true;
                                TopDpsBelt = shellUnderTestingBelt;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Adds the current top-performing shells to the TopDpsShells list for comparison with other lists
        /// </summary>
        public void AddTopShellsToLocalList()
        {
            if (TopDpsBelt.KineticDpsBeltSustained > 0 || TopDpsBelt.ChemDpsBeltSustained > 0 || TopDpsBelt.ShieldRpsBeltSustained > 0)
            {
                TopDpsShellsLocal.Add(TopDpsBelt);
            }

            if (TopDps1000.KineticDps > 0 || TopDps1000.ChemDps > 0 || TopDps1000.ShieldRps > 0)
            {
                TopDpsShellsLocal.Add(TopDps1000);
            }

            if (TopDps2000.KineticDps > 0 || TopDps2000.ChemDps > 0 || TopDps2000.ShieldRps > 0)
            {
                TopDpsShellsLocal.Add(TopDps2000);
            }

            if (TopDps4000.KineticDps > 0 || TopDps4000.ChemDps > 0 || TopDps4000.ShieldRps > 0)
            {
                TopDpsShellsLocal.Add(TopDps4000);
            }

            if (TopDps6000.KineticDps > 0 || TopDps6000.ChemDps > 0 || TopDps6000.ShieldRps > 0)
            {
                TopDpsShellsLocal.Add(TopDps6000);
            }

            if (TopDps8000.KineticDps > 0 || TopDps8000.ChemDps > 0 || TopDps8000.ShieldRps > 0)
            {
                TopDpsShellsLocal.Add(TopDps8000);
            }
        }


        /// <summary>
        /// Adds the current top-performing shells to the TopDpsShells dictionary for writing to console
        /// </summary>
        public void AddTopShellsToDictionary()
        {
            if (TopDpsBelt.KineticDpsBeltSustained > 0 || TopDpsBelt.ChemDpsBeltSustained > 0 || TopDpsBelt.ShieldRpsBeltSustained > 0)
            {
                TopDpsShells.Add("1 m (belt)", TopDpsBelt);
            }

            if (TopDps1000.KineticDps > 0 || TopDps1000.ChemDps > 0 || TopDps1000.ShieldRps > 0)
            {
                TopDpsShells.Add("1 m", TopDps1000);
            }

            if (TopDps2000.KineticDps > 0 || TopDps2000.ChemDps > 0 || TopDps2000.ShieldRps > 0)
            {
                TopDpsShells.Add("2 m", TopDps2000);
            }

            if (TopDps4000.KineticDps > 0 || TopDps4000.ChemDps > 0 || TopDps4000.ShieldRps > 0)
            {
                TopDpsShells.Add("4 m", TopDps4000);
            }

            if (TopDps6000.KineticDps > 0 || TopDps6000.ChemDps > 0 || TopDps6000.ShieldRps > 0)
            {
                TopDpsShells.Add("6 m", TopDps6000);
            }

            if (TopDps8000.KineticDps > 0 || TopDps8000.ChemDps > 0 || TopDps8000.ShieldRps > 0)
            {
                TopDpsShells.Add("8 m", TopDps8000);
            }
        }


        /// <summary>
        /// Finds the top shells in the given list.  Used in multithreading.
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
                        if (rawShell.KineticDpsPerVolumeBeltSustained > TopDpsBelt.KineticDpsPerVolumeBeltSustained)
                        {
                            TopDpsBelt = rawShell;
                        }
                    }
                    if (rawShell.TotalLength <= 1000f)
                    {
                        if (rawShell.KineticDpsPerVolume > TopDps1000.KineticDpsPerVolume)
                        {
                            TopDps1000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 2000f)
                    {
                        if (rawShell.KineticDpsPerVolume > TopDps2000.KineticDpsPerVolume)
                        {
                            TopDps2000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 4000f)
                    {
                        if (rawShell.KineticDpsPerVolume > TopDps4000.KineticDpsPerVolume)
                        {
                            TopDps4000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 6000f)
                    {
                        if (rawShell.KineticDpsPerVolume > TopDps6000.KineticDpsPerVolume)
                        {
                            TopDps6000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 8000f)
                    {
                        if (rawShell.KineticDpsPerVolume > TopDps8000.KineticDpsPerVolume)
                        {
                            TopDps8000 = rawShell;
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
                        if (rawShell.ChemDpsPerVolumeBeltSustained > TopDpsBelt.ChemDpsPerVolumeBeltSustained)
                        {
                            TopDpsBelt = rawShell;
                        }
                    }
                    if (rawShell.TotalLength <= 1000f)
                    {
                        if (rawShell.ChemDpsPerVolume > TopDps1000.ChemDpsPerVolume)
                        {
                            TopDps1000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 2000f)
                    {
                        if (rawShell.ChemDpsPerVolume > TopDps2000.ChemDpsPerVolume)
                        {
                            TopDps2000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 4000f)
                    {
                        if (rawShell.ChemDpsPerVolume > TopDps4000.ChemDpsPerVolume)
                        {
                            TopDps4000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 6000f)
                    {
                        if (rawShell.ChemDpsPerVolume > TopDps6000.ChemDpsPerVolume)
                        {
                            TopDps6000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 8000f)
                    {
                        if (rawShell.ChemDpsPerVolume > TopDps8000.ChemDpsPerVolume)
                        {
                            TopDps8000 = rawShell;
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
                        if (rawShell.ShieldRpsPerVolumeBeltSustained > TopDpsBelt.ShieldRpsPerVolumeBeltSustained)
                        {
                            TopDpsBelt = rawShell;
                        }
                    }
                    if (rawShell.TotalLength <= 1000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > TopDps1000.ShieldRpsPerVolume)
                        {
                            TopDps1000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 2000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > TopDps2000.ShieldRpsPerVolume)
                        {
                            TopDps2000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 4000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > TopDps4000.ShieldRpsPerVolume)
                        {
                            TopDps4000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 6000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > TopDps6000.ShieldRpsPerVolume)
                        {
                            TopDps6000 = rawShell;
                        }
                    }
                    else if (rawShell.TotalLength <= 8000f)
                    {
                        if (rawShell.ShieldRpsPerVolume > TopDps8000.ShieldRpsPerVolume)
                        {
                            TopDps8000 = rawShell;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Write to the console the statistics of the top shells
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
                if (disruptor)
                {
                    Console.WriteLine("Shield reduction / s");
                    Console.WriteLine("Shield RPS / volume");
                }
                Console.WriteLine("Uptime (belt)");
                Console.WriteLine("DPS (belt, sustained)");
                Console.WriteLine("DPS per volume (sustained)");
                if (disruptor)
                {
                    Console.WriteLine("Shield RPS (belt, sustained)");
                    Console.WriteLine("Shield RPS / volume (sustained)");
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
        /// The main test body.  Iterates over the IEnumerables to compare every permutation within the given parameters, then stores the results
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