using System;
using System.Collections.Generic;
using System.Text;

namespace ApsCalc
{
    public struct ModuleCount
    {
        public float Gauge;
        public float Var0Count;
        public float Var1Count;
        public float GPCount;
        public float RGCount;
    }

    public class ShellCalc
    {
        /// <summary>
        /// Perform calculations on every possible permutation of the given shell configuration within the given limits
        /// </summary>
        /// <param name="minGauge"></param>
        /// <param name="maxGauge"></param>
        /// <param name="headModule"></param>
        /// <param name="baseModule"></param>
        /// <param name="fixedModuleCounts"></param>
        /// <param name="fixedModuleTotal"></param>
        /// <param name="variableModuleIndices"></param>
        /// <param name="maxGPInput"></param>
        /// <param name="maxRGInput"></param>
        /// <param name="maxShellLengthInput"></param>
        /// <param name="maxDrawInput"></param>
        /// <param name="minVelocityInput"></param>
        /// <param name="targetAC"></param>
        /// <param name="damageType"></param>
        public ShellCalc(
            float minGauge,
            float maxGauge,
            Module headModule,
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
            HeadModule = headModule;
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
        public Module HeadModule { get; }
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
        public Shell TopDpsDif { get; set; } = new Shell();

        public Dictionary<string, Shell> TopDpsShells { get; set; } = new Dictionary<string, Shell>();

        public IEnumerable<ModuleCount> GetModuleCounts()
        {
            float var0Max = 20f - FixedModuleTotal;
            float var1Max;
            float gpMax;
            float rgMax;

            for (float gauge = MinGauge; gauge <= MaxGauge; gauge++)
            {
                for (float var0Count = 0; var0Count <= var0Max; var0Count++)
                {
                    var1Max = 20f - (FixedModuleTotal + var0Count);

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


        public void GetTopShells()
        {
            if (TopDps1000.KineticDPS > 0 || TopDps1000.ChemDPS > 0)
            {
                TopDpsShells.Add("1 m", TopDps1000);
            }

            if (TopDpsBelt.KineticDPS > 0 || TopDpsBelt.ChemDPS > 0)
            {
                TopDpsShells.Add("1 m (belt)", TopDpsBelt);
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

            if (TopDpsDif.KineticDPS > 0 || TopDpsDif.ChemDPS > 0)
            {
                TopDpsShells.Add("DIF", TopDpsDif);
            }
        }



        public void ShellTest()
        {

            foreach (ModuleCount counts in GetModuleCounts())
            {
                Shell ShellUnderTesting = new Shell();
                ShellUnderTesting.HeadModule = HeadModule;
                ShellUnderTesting.BaseModule = BaseModule;
                FixedModuleCounts.CopyTo(ShellUnderTesting.BodyModuleCounts, 0);

                ShellUnderTesting.Gauge = counts.Gauge;
                ShellUnderTesting.BodyModuleCounts[VariableModuleIndices[0]] += counts.Var0Count;
                ShellUnderTesting.BodyModuleCounts[VariableModuleIndices[1]] += counts.Var1Count;
                ShellUnderTesting.GPCasingCount = counts.GPCount;
                ShellUnderTesting.RGCasingCount = counts.RGCount;

                ShellUnderTesting.CalculateLengths();

                if (ShellUnderTesting.TotalLength <= MaxShellLength)
                {
                    ShellUnderTesting.CalculateGPRecoil();
                    ShellUnderTesting.CalculateModifiers();

                    ShellUnderTesting.CalculateMaxDraw();
                    float maxDraw = Math.Min(MaxDrawInput, ShellUnderTesting.MaxDraw);
                    for (float draw = 0f; draw <= maxDraw; draw++)
                    {
                        ShellUnderTesting.RailDraw = draw;
                        ShellUnderTesting.CalculateVelocity();

                        if (ShellUnderTesting.Velocity >= MinVelocityInput)
                        {
                            ShellUnderTesting.CalculateReloadTime();

                            if (DamageType == 0) // Kinetic
                            {
                                ShellUnderTesting.CalculateKineticDamage();
                                ShellUnderTesting.CalculateAP();
                                ShellUnderTesting.CalculateKineticDPS(TargetAC);

                                if (ShellUnderTesting.TotalLength <= 10000f)
                                {
                                    if (ShellUnderTesting.KineticDPS > TopDpsDif.KineticDPS)
                                    {
                                        TopDpsDif = ShellUnderTesting;
                                    }
                                    if (ShellUnderTesting.TotalLength <= 8000f)
                                    {
                                        if (ShellUnderTesting.KineticDPS > TopDps8000.KineticDPS)
                                        {
                                            TopDps8000 = ShellUnderTesting;
                                        }
                                        if (ShellUnderTesting.TotalLength <= 6000f)
                                        {
                                            if (ShellUnderTesting.KineticDPS > TopDps6000.KineticDPS)
                                            {
                                                TopDps6000 = ShellUnderTesting;
                                            }
                                            if (ShellUnderTesting.TotalLength <= 4000f)
                                            {
                                                if (ShellUnderTesting.KineticDPS > TopDps4000.KineticDPS)
                                                {
                                                    TopDps4000 = ShellUnderTesting;
                                                }
                                                if (ShellUnderTesting.TotalLength <= 2000f)
                                                {
                                                    if (ShellUnderTesting.KineticDPS > TopDps2000.KineticDPS)
                                                    {
                                                        TopDps2000 = ShellUnderTesting;
                                                    }
                                                    if (ShellUnderTesting.TotalLength <= 1000f)
                                                    {
                                                        if (ShellUnderTesting.KineticDPS > TopDps1000.KineticDPS)
                                                        {
                                                            TopDps1000 = ShellUnderTesting;
                                                        }
                                                        if (ShellUnderTesting.KineticDPSBelt > TopDpsBelt.KineticDPSBelt)
                                                        {
                                                            TopDpsBelt = ShellUnderTesting;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (DamageType == 1) // Chem
                            {
                                ShellUnderTesting.CalculateChemDamage();
                                ShellUnderTesting.CalculateAP();
                                ShellUnderTesting.CalculateChemDPS();

                                if (ShellUnderTesting.TotalLength <= 10000f)
                                {
                                    if (ShellUnderTesting.ChemDPS > TopDpsDif.ChemDPS)
                                    {
                                        TopDpsDif = ShellUnderTesting;
                                    }
                                    if (ShellUnderTesting.TotalLength <= 8000f)
                                    {
                                        if (ShellUnderTesting.ChemDPS > TopDps8000.ChemDPS)
                                        {
                                            TopDps8000 = ShellUnderTesting;
                                        }
                                        if (ShellUnderTesting.TotalLength <= 6000f)
                                        {
                                            if (ShellUnderTesting.ChemDPS > TopDps6000.ChemDPS)
                                            {
                                                TopDps6000 = ShellUnderTesting;
                                            }
                                            if (ShellUnderTesting.TotalLength <= 4000f)
                                            {
                                                if (ShellUnderTesting.ChemDPS > TopDps4000.ChemDPS)
                                                {
                                                    TopDps4000 = ShellUnderTesting;
                                                }
                                                if (ShellUnderTesting.TotalLength <= 2000f)
                                                {
                                                    if (ShellUnderTesting.ChemDPS > TopDps2000.ChemDPS)
                                                    {
                                                        TopDps2000 = ShellUnderTesting;
                                                    }
                                                    if (ShellUnderTesting.TotalLength <= 1000f)
                                                    {
                                                        if (ShellUnderTesting.ChemDPS > TopDps1000.ChemDPS)
                                                        {
                                                            TopDps1000 = ShellUnderTesting;
                                                        }
                                                        if (ShellUnderTesting.ChemDPSBelt > TopDpsBelt.ChemDPSBelt)
                                                        {
                                                            TopDpsBelt = ShellUnderTesting;
                                                        }
                                                    }
                                                }
                                            }
                                        }
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

            }
            TestTotal = TestComparisons + TestRejectLength + TestRejectVelocity;
            Console.WriteLine(TestComparisons + " shells compared.");
            Console.WriteLine(TestRejectLength + " shells rejected due to length.");
            Console.WriteLine(TestRejectVelocity + " shells rejected due to velocity.");
            Console.WriteLine(TestTotal + " total.");
            Console.WriteLine("\n");

            GetTopShells();

            if (DamageType == 0) // Kinetic
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    if (topShell.Value.KineticDPS > 0)
                    {
                        Console.WriteLine(topShell.Key);
                        topShell.Value.GetShellInfoKinetic();
                        Console.WriteLine("\n");
                    }
                }
            }
            else if (DamageType == 1) // Chemical
            {
                foreach (KeyValuePair<string, Shell> topShell in TopDpsShells)
                {
                    if (topShell.Value.ChemDPS > 0)
                    {
                        Console.WriteLine(topShell.Key);
                        topShell.Value.GetShellInfoChem();
                        Console.WriteLine("\n");
                    }
                }
            }
        }
    }
}
