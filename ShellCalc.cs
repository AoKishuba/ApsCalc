using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The iterable generator for shells.  Generates all shell possible permutations of shell within the given parameters.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ModuleCount> GenerateModuleCounts()
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
            float lastGauge = MinGauge;
            Console.WriteLine("Testing " + Module.AllModules[HeadList[0]].Name + " " + MinGauge + " mm.  Max " + MaxGauge + " mm.");

            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                if (counts.Gauge != lastGauge)
                {
                    Console.WriteLine("\nTesting " + Module.AllModules[counts.HeadIndex].Name + " " + counts.Gauge + " mm.  Max " + MaxGauge + " mm.");
                    lastGauge = counts.Gauge;
                }
                Shell shellUnderTesting = new Shell();
                shellUnderTesting.BarrelCount = BarrelCount;
                shellUnderTesting.BoreEvacuator = BoreEvacuator;
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
                            TestComparisons++;
                            shellUnderTesting.CalculateReloadTime();
                            shellUnderTesting.CalculateChemModifier();
                            shellUnderTesting.CalculateChemDamage();

                            // Binary search to find optimal draw without testing every value
                            float bottomOfRange = minDraw;
                            float bottomScore = 0;
                            float topOfRange = maxDraw;
                            float topScore = 0;
                            float midRangeLower = 0;
                            float midRangeLowerScore = 0;
                            float midRangeUpper = 0;
                            float midRangeUpperScore = 0;
                            float optimalDraw = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                            bottomScore = shellUnderTesting.ChemDPSPerVolume;

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                            topScore = shellUnderTesting.ChemDPSPerVolume;

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                bottomScore = shellUnderTesting.ChemDPSPerVolume;

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
                                topScore = shellUnderTesting.ChemDPSPerVolume;

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
                                    midRangeLowerScore = shellUnderTesting.ChemDPSPerVolume;

                                    shellUnderTesting.RailDraw = midRangeUpper;
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    midRangeUpperScore = shellUnderTesting.ChemDPSPerVolume;

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
                            


                            // Check performance against top shells
                            shellUnderTesting.RailDraw = optimalDraw;
                            shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                            shellUnderTesting.CalculateEffectiveRange();


                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                if (shellUnderTesting.ChemDPSPerVolume > TopDps1000.ChemDPSPerVolume)
                                {
                                    TopDps1000 = shellUnderTesting;
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

                            // Beltfed testing
                            if (shellUnderTesting.TotalLength <= 1000f)
                            {
                                // Binary search to find optimal draw without testing every value
                                bottomOfRange = minDraw;
                                bottomScore = 0;
                                topOfRange = maxDraw;
                                topScore = 0;
                                midRangeLower = 0;
                                midRangeLowerScore = 0;
                                midRangeUpper = 0;
                                midRangeUpperScore = 0;
                                optimalDraw = 0;

                                shellUnderTesting.RailDraw = minDraw;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                bottomScore = shellUnderTesting.ChemDPSPerVolumeBelt;

                                shellUnderTesting.RailDraw = maxDraw;
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                topScore = shellUnderTesting.ChemDPSPerVolumeBelt;

                                if (topScore > bottomScore)
                                {
                                    // Check if max draw is optimal
                                    shellUnderTesting.RailDraw = maxDraw - 1f;
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    bottomScore = shellUnderTesting.ChemDPSPerVolumeBelt;

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
                                    topScore = shellUnderTesting.ChemDPSPerVolumeBelt;

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
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    midRangeLowerScore = shellUnderTesting.ChemDPSPerVolumeBelt;

                                    shellUnderTesting.RailDraw = midRangeUpper;
                                    shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);
                                    midRangeUpperScore = shellUnderTesting.ChemDPSPerVolumeBelt;

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


                                // Check performance against top shells
                                shellUnderTesting.RailDraw = optimalDraw;
                                shellUnderTesting.CalculateEffectiveRange();
                                shellUnderTesting.CalculatePendepthDps(TargetArmorScheme);

                                if (shellUnderTesting.ChemDPSPerVolumeBelt > TopDpsBelt.ChemDPSPerVolumeBelt)
                                {
                                    shellUnderTesting.IsBelt = true;
                                    TopDpsBelt = shellUnderTesting;
                                }
                            }
                        }
                        else
                        {
                            TestRejectPen++;
                        }
                    }
                    else
                    {
                        TestRejectVelocityOrRange++;
                    }
                }
                else
                {
                    TestRejectLength++;
                }
            }
        }



        /// <summary>
        /// Calculates damage output for shell configurations with nonzero rail draw
        /// </summary>
        public void KineticTest()
        {
            float lastGauge = MinGauge;
            Console.WriteLine("Testing " + Module.AllModules[HeadList[0]].Name + " " + MinGauge + " mm.  Max " + MaxGauge + " mm.");

            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                if (counts.Gauge != lastGauge)
                {
                    Console.WriteLine("\nTesting " + Module.AllModules[counts.HeadIndex].Name + " " + counts.Gauge + " mm.  Max " + MaxGauge + " mm.");
                    lastGauge = counts.Gauge;
                }
                Shell shellUnderTesting = new Shell();
                shellUnderTesting.BarrelCount = BarrelCount;
                shellUnderTesting.BoreEvacuator = BoreEvacuator;
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
                    shellUnderTesting.CalculateLoaderVolume();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput, maxDraw);
                    if (maxDraw >= minDraw)
                    {
                        TestComparisons++;
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolume();
                        shellUnderTesting.CalculateKDModifier();
                        shellUnderTesting.CalculateAPModifier();

                        // Binary search to find optimal draw without testing every value
                        float bottomOfRange = minDraw;
                        float bottomScore = 0;
                        float topOfRange = maxDraw;
                        float topScore = 0;
                        float midRangeLower = 0;
                        float midRangeLowerScore = 0;
                        float midRangeUpper = 0;
                        float midRangeUpperScore = 0;
                        float optimalDraw = 0;

                        shellUnderTesting.RailDraw = minDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateAP();
                        shellUnderTesting.CalculateKineticDamage();
                        shellUnderTesting.CalculateKineticDps(TargetAC);
                        bottomScore = shellUnderTesting.KineticDPSPerVolume;

                        shellUnderTesting.RailDraw = maxDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateVelocity();
                        shellUnderTesting.CalculateAP();
                        shellUnderTesting.CalculateKineticDamage();
                        shellUnderTesting.CalculateKineticDps(TargetAC);
                        topScore = shellUnderTesting.KineticDPSPerVolume;

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
                            bottomScore = shellUnderTesting.KineticDPSPerVolume;

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
                            topScore = shellUnderTesting.KineticDPSPerVolume;

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
                            midRangeLowerScore = shellUnderTesting.KineticDPSPerVolume;

                            shellUnderTesting.RailDraw = midRangeUpper;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            midRangeUpperScore = shellUnderTesting.KineticDPSPerVolume;

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
                            if (shellUnderTesting.KineticDPSPerVolume > TopDps1000.KineticDPSPerVolume)
                            {
                                TopDps1000 = shellUnderTesting;
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

                        // Beltfed testing
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            // Binary search to find optimal draw without testing every value
                            bottomOfRange = minDraw;
                            bottomScore = 0;
                            topOfRange = maxDraw;
                            topScore = 0;
                            midRangeLower = 0;
                            midRangeLowerScore = 0;
                            midRangeUpper = 0;
                            midRangeUpperScore = 0;
                            optimalDraw = 0;

                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            bottomScore = shellUnderTesting.KineticDPSPerVolumeBelt;

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateAP();
                            shellUnderTesting.CalculateKineticDamage();
                            shellUnderTesting.CalculateKineticDps(TargetAC);
                            topScore = shellUnderTesting.KineticDPSPerVolumeBelt;

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
                                bottomScore = shellUnderTesting.KineticDPSPerVolumeBelt;

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
                                topScore = shellUnderTesting.KineticDPSPerVolumeBelt;

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
                                midRangeLowerScore = shellUnderTesting.KineticDPSPerVolumeBelt;

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateVelocity();
                                shellUnderTesting.CalculateAP();
                                shellUnderTesting.CalculateKineticDamage();
                                shellUnderTesting.CalculateKineticDps(TargetAC);
                                midRangeUpperScore = shellUnderTesting.KineticDPSPerVolumeBelt;

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

                            if (shellUnderTesting.KineticDPSPerVolumeBelt > TopDpsBelt.KineticDPSPerVolumeBelt)
                            {
                                shellUnderTesting.IsBelt = true;
                                TopDpsBelt = shellUnderTesting;
                            }
                        }                        
                    }
                    else
                    {
                        TestRejectVelocityOrRange++;
                    }
                }
                else
                {
                    TestRejectLength++;
                }
            }
        }


        /// <summary>
        /// Calculates damage output for shell configurations with nonzero rail draw
        /// </summary>
        public void ChemTest()
        {
            float lastGauge = MinGauge;
            Console.WriteLine("Testing " + Module.AllModules[HeadList[0]].Name + " " + MinGauge + " mm.  Max " + MaxGauge + " mm.");

            foreach (ModuleCount counts in GenerateModuleCounts())
            {
                if (counts.Gauge != lastGauge)
                {
                    Console.WriteLine("\nTesting " + Module.AllModules[counts.HeadIndex].Name + " " + counts.Gauge + " mm.  Max " + MaxGauge + " mm.");
                    lastGauge = counts.Gauge;
                }
                Shell shellUnderTesting = new Shell();
                shellUnderTesting.BarrelCount = BarrelCount;
                shellUnderTesting.BoreEvacuator = BoreEvacuator;
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
                    shellUnderTesting.CalculateLoaderVolume();
                    shellUnderTesting.CalculateRecoil();
                    shellUnderTesting.CalculateMaxDraw();
                    shellUnderTesting.CalculateVelocityModifier();

                    float maxDraw = Math.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange(MinVelocityInput, MinEffectiveRangeInput, maxDraw);
                    if (maxDraw >= minDraw)
                    {
                        TestComparisons++;
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateCooldownTime();
                        shellUnderTesting.CalculateCoolerVolume();
                        shellUnderTesting.CalculateChemModifier();
                        shellUnderTesting.CalculateChemDamage();

                        // Binary search to find optimal draw without testing every value
                        float bottomOfRange = minDraw;
                        float bottomScore = 0;
                        float topOfRange = maxDraw;
                        float topScore = 0;
                        float midRangeLower = 0;
                        float midRangeLowerScore = 0;
                        float midRangeUpper = 0;
                        float midRangeUpperScore = 0;
                        float optimalDraw = 0;

                        shellUnderTesting.RailDraw = minDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateChemDps();
                        bottomScore = shellUnderTesting.ChemDPSPerVolume;

                        shellUnderTesting.RailDraw = maxDraw;
                        shellUnderTesting.CalculateRecoil();
                        shellUnderTesting.CalculateRecoilVolume();
                        shellUnderTesting.CalculateChargerVolume();
                        shellUnderTesting.CalculateVolumePerIntake();
                        shellUnderTesting.CalculateChemDps();
                        topScore = shellUnderTesting.ChemDPSPerVolume;

                        if (topScore > bottomScore)
                        {
                            // Check if max draw is optimal
                            shellUnderTesting.RailDraw = maxDraw - 1f;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateChemDps();
                            bottomScore = shellUnderTesting.ChemDPSPerVolume;

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
                            topScore = shellUnderTesting.ChemDPSPerVolume;

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
                            midRangeLowerScore = shellUnderTesting.ChemDPSPerVolume;

                            shellUnderTesting.RailDraw = midRangeUpper;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateChemDps();
                            midRangeUpperScore = shellUnderTesting.ChemDPSPerVolume;

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
                            if (shellUnderTesting.ChemDPSPerVolume > TopDps1000.ChemDPSPerVolume)
                            {
                                TopDps1000 = shellUnderTesting;
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


                        // Beltfed testing
                        // Binary search to find optimal draw without testing every value
                        bottomOfRange = minDraw;
                        bottomScore = 0;
                        topOfRange = maxDraw;
                        topScore = 0;
                        midRangeLower = 0;
                        midRangeLowerScore = 0;
                        midRangeUpper = 0;
                        midRangeUpperScore = 0;
                        optimalDraw = 0;
                        if (shellUnderTesting.TotalLength <= 1000f)
                        {
                            shellUnderTesting.RailDraw = minDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateChemDps();
                            bottomScore = shellUnderTesting.ChemDPSPerVolumeBelt;

                            shellUnderTesting.RailDraw = maxDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateChemDps();
                            topScore = shellUnderTesting.ChemDPSPerVolumeBelt;

                            if (topScore > bottomScore)
                            {
                                // Check if max draw is optimal
                                shellUnderTesting.RailDraw = maxDraw - 1f;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateChemDps();
                                bottomScore = shellUnderTesting.ChemDPSPerVolumeBelt;

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
                                topScore = shellUnderTesting.ChemDPSPerVolumeBelt;

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
                                midRangeLowerScore = shellUnderTesting.ChemDPSPerVolumeBelt;

                                shellUnderTesting.RailDraw = midRangeUpper;
                                shellUnderTesting.CalculateRecoil();
                                shellUnderTesting.CalculateRecoilVolume();
                                shellUnderTesting.CalculateChargerVolume();
                                shellUnderTesting.CalculateVolumePerIntake();
                                shellUnderTesting.CalculateChemDps();
                                midRangeUpperScore = shellUnderTesting.ChemDPSPerVolumeBelt;

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


                            // Check performance against top shells
                            shellUnderTesting.RailDraw = optimalDraw;
                            shellUnderTesting.CalculateRecoil();
                            shellUnderTesting.CalculateRecoilVolume();
                            shellUnderTesting.CalculateChargerVolume();
                            shellUnderTesting.CalculateVolumePerIntake();
                            shellUnderTesting.CalculateVelocity();
                            shellUnderTesting.CalculateEffectiveRange();
                            shellUnderTesting.CalculateChemDps();

                            if (shellUnderTesting.ChemDPSPerVolumeBelt > TopDpsBelt.ChemDPSPerVolumeBelt)
                            {
                                shellUnderTesting.IsBelt = true;
                                TopDpsBelt = shellUnderTesting;
                            }
                        }
                    }
                    else
                    {
                        TestRejectVelocityOrRange++;
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
                else if (DamageType == 1 || DamageType == 2)
                {
                    Console.WriteLine("Chemical payload strength");
                    bool disruptor = false;
                    foreach(int headIndex in HeadList)
                    {
                        if (Module.AllModules[headIndex] == Module.Disruptor)
                        {
                            disruptor = true;
                            break;
                        }
                    }
                    if (disruptor)
                    {
                        Console.WriteLine("Shield reduction (decimal)");
                    }
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
                    topShell.Value.GetModuleCounts();
                    topShell.Value.GetShellInfoKinetic(Labels);
                }
            }
            else if (DamageType == 1 || DamageType == 2)
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

            TestTotal = TestComparisons + TestRejectLength + TestRejectVelocityOrRange;
            Console.WriteLine(TestComparisons + " shells compared.");
            Console.WriteLine(TestRejectLength + " shells rejected due to length.");
            Console.WriteLine(TestRejectVelocityOrRange + " shells rejected due to velocity or effective range.");
            if (DamageType == 2)
            {
                Console.WriteLine(TestRejectPen + " shells rejected due to insufficient armor pen.");
            }
            Console.WriteLine(TestTotal + " total.");
            Console.WriteLine("\n");
        }
    }
}