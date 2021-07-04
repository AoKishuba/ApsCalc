using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using PenCalc;
using System.Linq;

namespace ApsCalc
{
    public struct TestParameters
    {
        public int BarrelCount;
        public int MinGauge;
        public int MaxGauge;
        public List<int> HeadIndices;
        public Module BaseModule;
        public float[] FixedModulecounts;
        public float MinModulecount;
        public int[] VariableModuleIndices;
        public float MaxGPCasingCount;
        public bool UseEvacuator;
        public float MaxRGCasingCount;
        public float MaxLength;
        public float MaxDraw;
        public float MaxRecoil;
        public float MinVelocity;
        public float MinEffectiverange;
        public List<float> TargetACList;
        public DamageType DamageType;
        public Scheme ArmorScheme;
        public int TestType;
        public bool Labels;
        public bool WriteToFile;
    }
    class Program
    {
        static TestParameters GetTestParameters()
        {
            string input;

            // Get number of barrels
            int barrelCount;
            int maxGaugeHardCap;
            Dictionary<int, int> gaugeHardCaps = new()
            {
                { 1, 500 },
                { 2, 250 },
                { 3, 225 },
                { 4, 200 },
                { 5, 175 },
                { 6, 150 }
            };
            // Get number of barrels
            Console.WriteLine("\nNumber of barrels : Max gauge in mm");
            foreach (KeyValuePair<int, int> entry in gaugeHardCaps)
            {
                Console.WriteLine(entry.Key + " : " + entry.Value);
            }
            Console.WriteLine("\nEnter number of barrels from 1 thru 6.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out barrelCount))
                {
                    if (barrelCount < 1 || barrelCount > 6)
                    {
                        Console.WriteLine("\nBARRELCOUNT RANGE ERROR: Enter an integer from 1 thru 6.");
                    }
                    else
                    {
                        if (barrelCount == 1)
                        {
                            Console.WriteLine("Will use 1 barrel.\n");
                        }
                        else
                        {
                            Console.WriteLine("Will use " + barrelCount + " barrels.\n");
                        }
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nBARRELCOUNT PARSE ERROR: Enter an integer from 1 thru 6.");
                }
            }
            maxGaugeHardCap = gaugeHardCaps[barrelCount];


            // Get minimum gauge
            int minGaugeInput;
            Console.WriteLine("Enter minimum gauge in mm from 18 thru " + maxGaugeHardCap + ":");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out minGaugeInput))
                {
                    if (minGaugeInput < 18 || minGaugeInput > maxGaugeHardCap)
                    {
                        Console.WriteLine("\nMIN GAUGE RANGE ERROR: Enter an integer from 18 thru " + maxGaugeHardCap + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMIN GAUGE PARSE ERROR: Enter an integer from 18 thru " + maxGaugeHardCap + ".");
                }
            }
            int minGauge = minGaugeInput;

            // Get maximum gauge
            int maxGaugeInput;
            Console.WriteLine("\nEnter maximum gauge in mm from " + minGaugeInput + " thru " + maxGaugeHardCap + ".");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out maxGaugeInput))
                {
                    if (maxGaugeInput < minGaugeInput || maxGaugeInput > maxGaugeHardCap)
                    {
                        Console.WriteLine("\nMAX GAUGE RANGE ERROR: Enter an integer from " + minGaugeInput + " thru " + maxGaugeHardCap + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX GAUGE PARSE ERROR: Enter an integer from " + minGaugeInput + " thru " + maxGaugeHardCap + ".");
                }
            }
            Console.WriteLine("\nWill test gauges from " + minGaugeInput + " mm thru " + maxGaugeInput + " mm.\n");
            int maxGauge = maxGaugeInput;

            // Get head
            // Find indices of modules which can be used as heads
            int modIndex = 0;
            int minHeadIndex = 0;
            int maxHeadIndex = 0;
            foreach (Module mod in Module.AllModules)
            {
                if (mod.ModulePosition == Module.Position.Middle || mod.ModulePosition == Module.Position.Head)
                {
                    minHeadIndex = modIndex;
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            // Counting backwards from end of module list
            for (int i = Module.AllModules.Length - 1; i >= 0; i--)
            {
                if (Module.AllModules[i].ModulePosition == Module.Position.Middle || Module.AllModules[i].ModulePosition == Module.Position.Head)
                {
                    maxHeadIndex = i;
                    break;
                }
            }
            int headCount = 0;
            List<int> headIndices = new();
            while (true)
            {
                for (int i = minHeadIndex; i <= maxHeadIndex; i++)
                {
                    if (i < 10)
                    {
                        Console.WriteLine(" " + i + " : " + Module.AllModules[i].Name); // Fix indentation
                    }
                    else
                    {
                        Console.WriteLine(i + " : " + Module.AllModules[i].Name);
                    }
                }
                if (headCount > 0)
                {
                    Console.WriteLine("\nEnter a number to select an additional head, or type 'done' if finished.");
                }
                else
                {
                    Console.WriteLine("\nEnter a number to select a head.");
                }
                input = Console.ReadLine();
                if (input.ToLower() == "done")
                {
                    if (headCount > 0)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("\n ERROR: At least one head must be selected.");
                    }
                }
                if (int.TryParse(input, out modIndex))
                {
                    if (modIndex < minHeadIndex || modIndex > maxHeadIndex) // Indices of all modules which can be used as heads
                    {
                        if (headCount > 0)
                        {
                            Console.WriteLine("\nHEAD INDEX RANGE ERROR: Enter an integer from "
                                + minHeadIndex
                                + " thru "
                                + maxHeadIndex
                                + ", or type 'done'.");
                        }
                        else
                        {
                            Console.WriteLine("\nHEAD INDEX RANGE ERROR: Enter an integer from "
                                + minHeadIndex
                                + " thru "
                                + maxHeadIndex);
                        }
                    }
                    else
                    {
                        if (headIndices.Contains(modIndex))
                        {
                            Console.WriteLine("\nERROR: Duplicate head index.");
                        }
                        else
                        {
                            headIndices.Add(modIndex);
                            Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to head list.\n");
                            headCount++;
                        }
                    }
                }
                else
                {
                    if (headCount > 0)
                    {
                        Console.WriteLine("\nHEAD INDEX PARSE ERROR: Enter an integer from "
                            + minHeadIndex
                            + " thru "
                            + maxHeadIndex
                            + ", or type 'done'.");
                    }
                    else
                    {
                        Console.WriteLine("\nHEAD INDEX PARSE ERROR: Enter an integer from "
                            + minHeadIndex
                            + " thru "
                            + maxHeadIndex);
                    }
                }
            }


            // Get base
            // Find indices of modules which can be used as bases
            modIndex = 0;
            int minBaseIndex = 0;
            int maxBaseIndex = 0;
            foreach (Module mod in Module.AllModules)
            {
                if (mod.ModulePosition == Module.Position.Base)
                {
                    minBaseIndex = modIndex;
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            // Counting backwards from end of module list
            for (int i = Module.AllModules.Length - 1; i >= 0; i--)
            {
                if (Module.AllModules[i].ModulePosition == Module.Position.Base)
                {
                    maxBaseIndex = i;
                    break;
                }
            }
            Module baseModule = default;
            for (int i = minBaseIndex; i <= maxBaseIndex; i++)
            {
                Console.WriteLine(i + " : " + Module.AllModules[i].Name);
            }
            Console.WriteLine("\nEnter a number to select a base for shell, or type 'done' if no special base is desired.");
            while (true)
            {
                input = Console.ReadLine();
                if (input.ToLower() == "done")
                {
                    break;
                }
                if (int.TryParse(input, out int baseIndex))
                {
                    if (baseIndex < minBaseIndex || baseIndex > maxBaseIndex)
                    {
                        Console.WriteLine("\nBASE INDEX RANGE ERROR: Enter an integer from "
                            + minBaseIndex
                            + " thru "
                            + maxBaseIndex
                            + ", or type 'done'.");
                    }
                    else
                    {
                        baseModule = Module.AllModules[baseIndex];
                        Console.WriteLine("\n" + baseModule.Name + " selected.\n");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nBASE INDEX PARSE ERROR: Enter an integer from "
                        + minBaseIndex
                        + " thru "
                        + maxBaseIndex
                        + ", or type 'done'.");
                }
            }


            // Get fixed body modules
            // Find indices of modules which can be used as body modules
            modIndex = 0;
            int minBodyIndex = 0;
            int maxBodyIndex = 0;
            foreach (Module mod in Module.AllModules)
            {
                if (mod.ModulePosition == Module.Position.Middle)
                {
                    minBodyIndex = modIndex;
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            // Counting backwards from end of module list
            for (int i = Module.AllModules.Length - 1; i >= 0; i--)
            {
                if (Module.AllModules[i].ModulePosition == Module.Position.Middle)
                {
                    maxBodyIndex = i;
                    break;
                }
            }
            // Get fixed body module counts
            // Create array with a number of elements equal to number of body module types
            List<float> fixedModuleCounts = new();
            for (int i = minBodyIndex; i <= maxBodyIndex; i++)
            {
                fixedModuleCounts.Add(0);
            }
            float[] fixedModulecounts = fixedModuleCounts.ToArray();

            while (true)
            {
                for (int i = minBodyIndex; i <= maxBodyIndex; i++)
                {
                    Console.WriteLine(i + " : " + Module.AllModules[i].Name);
                }
                Console.WriteLine("\nEnter a number to add a fixed module, or type 'done'.  Fixed modules will be included in every shell.");
                input = Console.ReadLine();
                if (input.ToLower() == "done")
                {
                    break;
                }
                if (int.TryParse(input, out modIndex))
                {
                    if (modIndex < minBodyIndex || modIndex > maxBodyIndex)
                    {
                        Console.WriteLine("\nFIXEDMOD INDEX RANGE ERROR: Enter an integer from "
                            + minBodyIndex
                            + " thru "
                            + maxBodyIndex
                            + ", or type 'done'.");
                    }
                    else
                    {
                        fixedModulecounts[modIndex] += 1f;
                        Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to fixed module list.\n");
                    }
                }
                else
                {
                    Console.WriteLine("\nFIXEDMOD INDEX PARSE ERROR: Enter an integer from "
                        + minBodyIndex
                        + " thru "
                        + maxBodyIndex
                        + ", or type 'done'.");
                }
            }


            // Calculate minimum module count
            float minModulecount = 1; // The head
            modIndex = 0;
            foreach (float modCount in fixedModulecounts)
            {
                minModulecount += modCount;
                modIndex++;
            }

            if (baseModule != null)
            {
                minModulecount++;
            }

            // Calculate maximum casings and variable modules
            float maxOtherCount = 20 - minModulecount;

            Console.WriteLine("Fixed module count: " + minModulecount);
            Console.WriteLine("Maximum casing and variable module count: " + maxOtherCount);


            // Get variable modules
            Console.WriteLine("\n\n");
            int[] variableModuleIndices = { 100, 100, 100, 100, 100, 100, 100, 100, 100 };
            int varModCount = 0;
            int arrayLength = variableModuleIndices.Length;
            while (varModCount < arrayLength)
            {
                for (int i = minBodyIndex; i <= maxBodyIndex; i++)
                {
                    Console.WriteLine(i + " : " + Module.AllModules[i].Name);
                }
                if (varModCount > 0)
                {
                    Console.WriteLine("\nEnter a number to select an additional variable module "
                        + (varModCount + 1) // Compensate for 0 indexing for display
                        + " of "
                        + arrayLength
                        + ", or type 'done' if finished.");
                }
                else
                {
                    Console.WriteLine("\nEnter a number to select a variable module.\nVariable modules will be tested at every combination from 0 thru "
                        + maxOtherCount
                        + " each.");
                }
                input = Console.ReadLine();
                if (input.ToLower() == "done")
                {
                    if (varModCount > 0)
                    {
                        // Set remaining indices to first index entered by user to overwrite default values
                        for (int i = varModCount; i < arrayLength; i++)
                        {
                            variableModuleIndices[i] = variableModuleIndices[0];
                        }
                        break;
                    }
                    else
                    {
                        Console.WriteLine("\n ERROR: At least one variable module must be selected.");
                    }
                }

                if (int.TryParse(input, out modIndex))
                {
                    if (modIndex < minBodyIndex || modIndex > maxBodyIndex)
                    {
                        if (varModCount > 0)
                        {
                            Console.WriteLine("\nVARIABLEMOD INDEX RANGE ERROR: Enter an integer from "
                                + minBodyIndex
                                + " thru "
                                + maxBodyIndex
                                + ", or type 'done'.");
                        }
                        else
                        {
                            Console.WriteLine("\nVARIABLEMOD INDEX RANGE ERROR: Enter an integer from "
                                + minBodyIndex
                                + " thru "
                                + maxBodyIndex);
                        }
                    }
                    else
                    {
                        if (variableModuleIndices.Contains(modIndex))
                        {
                            Console.WriteLine("\nERROR: Duplicate variable module index.");
                        }
                        else
                        {
                            variableModuleIndices[varModCount] = modIndex;
                            Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to variable module list.\n");
                            varModCount++;
                        }
                    }
                }
                else
                {
                    if (varModCount > 0)
                    {
                        Console.WriteLine("\nVARIABLEMOD INDEX PARSE ERROR: Enter an integer from "
                            + minBodyIndex
                            + " thru "
                            + maxBodyIndex
                            + ", or type 'done'.");
                    }
                    else
                    {
                        Console.WriteLine("\nVARIABLEMOD INDEX PARSE ERROR: Enter an integer from "
                            + minBodyIndex
                            + " thru "
                            + maxBodyIndex);
                    }
                }
            }


            // Get maximum GP casing count
            int maxGunPowderCasingInput;
            Console.WriteLine("\nEnter maximum number of GP casings from 0 thru " + maxOtherCount + ": ");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out maxGunPowderCasingInput))
                {
                    if (maxGunPowderCasingInput < 0 || maxGunPowderCasingInput > maxOtherCount)
                    {
                        Console.WriteLine("\nMAX GP CASING COUNT RANGE ERROR: Enter an integer from 0 thru " + maxOtherCount + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX GP COUNT PARSE ERROR: Enter an integer from 0 thru " + maxOtherCount + ".");
                }
            }
            float maxGPCasingCount = maxGunPowderCasingInput;


            // Get bore evacuator
            bool useEvacuator = true;
            if (maxGunPowderCasingInput > 0)
            {
                Console.WriteLine("\nBore evacuator?\nEnter 'y' or 'n'.");
                while (true)
                {
                    input = Console.ReadLine();
                    if (input.ToLower() == "y")
                    {
                        useEvacuator = true;
                        Console.WriteLine("\nUsing bore evacuator.\n");
                        break;
                    }
                    else if (input.ToLower() == "n")
                    {
                        useEvacuator = false;
                        Console.WriteLine("\nNo evacuator.\n");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("\nERROR: Enter 'y' to include bore evacuator, or 'n' to omit evacuator.\n");
                    }
                }
            }


            // Get maximum RG casing count
            int maxRailgunCasingInput;
            Console.WriteLine("\nEnter maximum number of RG casings from 0 thru " + maxOtherCount + ": ");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out maxRailgunCasingInput))
                {
                    if (maxRailgunCasingInput < 0 || maxRailgunCasingInput > maxOtherCount)
                    {
                        Console.WriteLine("\nMAX RG CASING COUNT RANGE ERROR: Enter an integer from 0 thru " + maxOtherCount + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX RG COUNT PARSE ERROR: Enter an integer from 0 thru " + maxOtherCount + ".");
                }
            }
            float maxRGCasingCount = maxRailgunCasingInput;


            // Get maximum rail draw
            int maxRailDrawInput;
            Console.WriteLine("\nEnter maximum rail draw from 0 thru 200 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out maxRailDrawInput))
                {
                    if (maxRailDrawInput < 0 || maxRailDrawInput > 200000)
                    {
                        Console.WriteLine("\nMAX RAIL DRAW RANGE ERROR: Enter an integer from 0 thru 200 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX RAIL DRAW PARSE ERROR: Enter an integer from 0 thru 200 000.");
                }
            }
            float maxDraw = maxRailDrawInput;


            // Get maximum recoil
            int maxRecoilInput;
            Console.WriteLine("\nEnter maximum recoil from 0 thru 250 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out maxRecoilInput))
                {
                    if (maxRecoilInput < 0 || maxRecoilInput > 250000)
                    {
                        Console.WriteLine("\nMAX RECOIL RANGE ERROR: Enter an integer from 0 thru 250 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX RECOIL PARSE ERROR: Enter an integer from 0 thru 250 000.");
                }
            }
            float maxRecoil = maxRecoilInput;


            // Calculate minimum shell length
            float minShellLength = minGaugeInput;
            modIndex = 0;
            foreach (float modCount in fixedModulecounts)
            {
                minShellLength += MathF.Min(minGaugeInput, Module.AllModules[modIndex].MaxLength) * modCount;
                modIndex++;
            }


            if (baseModule != null)
            {
                minShellLength += MathF.Min(minGaugeInput, baseModule.MaxLength);
            }


            // Get maximum shell length
            int maxShellLengthInput;
            Console.WriteLine("\nEnter maximum shell length in mm from " + minShellLength + " thru 8 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out maxShellLengthInput))
                {
                    if (maxShellLengthInput < minShellLength || maxShellLengthInput > 8000)
                    {
                        Console.WriteLine("\nMAX SHELL LENGTH RANGE ERROR: Enter an integer from " + minShellLength + " thru 8 000.");
                    }
                    else
                    {
                        Console.WriteLine("\nWill test shells up to " + maxShellLengthInput + " mm.\n");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX SHELL LENGTH PARSE ERROR: Enter an integer from " + minShellLength + " thru 8 000.");
                }
            }
            float maxLength = maxShellLengthInput;


            // Get minimum velocity
            int minShellVelocityInput;
            Console.WriteLine("\nEnter minimum shell velocity in m/s from 0 thru 5 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out minShellVelocityInput))
                {
                    if (minShellVelocityInput < 0f || minShellVelocityInput > 5000f)
                    {
                        Console.WriteLine("\nMIN SHELL VELOCITY RANGE ERROR: Enter an integer from 0 thru 5 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMIN SHELL VELOCITY PARSE ERROR: Enter an integer from 0 thru 5 000.");
                }
            }
            float minVelocity = minShellVelocityInput;


            // Get minimum effective range
            int minEffectiveRangeInput;
            Console.WriteLine("\nEnter minimum effective range in m from 0 thru 2 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out minEffectiveRangeInput))
                {
                    if (minEffectiveRangeInput < 0 || minEffectiveRangeInput > 2000)
                    {
                        Console.WriteLine("\nMIN RANGE RANGE ERROR: Enter a value from 0 thru 2 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMIN RANGE PARSE ERROR: Enter a value from 0 thru 2 000.");
                }
            }
            float minEffectiveRange = minEffectiveRangeInput;



            // Get damage type to optimize
            int damageTypeInput;
            DamageType damageType;
            Scheme armorScheme = new();
            List<float> targetACList = new();
            Console.WriteLine("\nSelect damage type to optimize:\n0: Kinetic\n1: EMP\n2: FlaK\n3: Frag\n4: HE\n5: Pendepth\n6: Disruptor");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out damageTypeInput))
                {
                    if (damageTypeInput < 0 || damageTypeInput > 6)
                    {
                        Console.WriteLine("\nDAMAGE TYPE RANGE ERROR: 0: Kinetic | 1: EMP | 2: FlaK | 3: Frag | 4: HE | 5: Pendepth | 6: Disruptor.");
                    }
                    else
                    {
                        damageTypeInput = Convert.ToInt32(input);
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nDAMAGE TYPE PARSE ERROR: 0: Kinetic | 1: EMP | 2: FlaK | 3: Frag | 4: HE | 5: Pendepth | 6: Disruptor.");
                }
            }
            if (damageTypeInput == 0)
            {
                damageType = DamageType.Kinetic;
                float minAC = 0.1f;
                float maxAC = 100f;
                float ACCount = 0;
                // Get target armor class
                while (true)
                {
                    if (ACCount > 0)
                    {
                        Console.WriteLine("\nEnter an additional AC, or type 'done' if finished.");
                    }
                    else
                    {
                        Console.WriteLine("\nEnter target AC from 0.1 thru 100.0.");
                    }
                    input = Console.ReadLine();
                    if (input.ToLower() == "done")
                    {
                        if (ACCount > 0)
                        {
                            break;
                        }
                        else
                        {
                            Console.WriteLine("\nERROR: Enter at least one AC.");
                        }
                    }
                    if (float.TryParse(input, out float targetAC))
                    {
                        if (targetAC < minAC || targetAC > maxAC)
                        {
                            if (ACCount > 0)
                            {
                                Console.WriteLine("\nAC RANGE ERROR: Enter a value from "
                                    + minAC
                                    + " thru "
                                    + maxAC
                                    + ", or type 'done'.");
                            }
                            else
                            {
                                Console.WriteLine("\nAC RANGE ERROR: Enter a value from"
                                    + minAC
                                    + " thru "
                                    + maxAC);
                            }
                        }
                        else
                        {
                            if (targetACList.Contains(targetAC))
                            {
                                Console.WriteLine("\nERROR: Duplicate AC.");
                            }
                            else
                            {
                                targetACList.Add(targetAC);
                                Console.WriteLine(targetAC + " added to AC list.\n");
                                ACCount++;
                            }
                        }
                    }
                    else
                    {
                        if (ACCount > 0)
                        {
                            Console.WriteLine("\nAC PARSE ERROR: Enter a value from "
                                + minAC
                                + " thru "
                                + maxAC
                                + ", or type 'done'.");
                        }
                        else
                        {
                            Console.WriteLine("\nAC PARSE ERROR: Enter a value from "
                                + minAC
                                + " thru "
                                + maxAC);
                        }
                    }
                }
                Console.WriteLine("\nTarget AC List:");
                foreach (float ac in targetACList)
                {
                    Console.WriteLine(ac);
                }
            }
            else if (damageTypeInput == 1)
            {
                damageType = DamageType.Emp;
                Console.WriteLine("\nWill optimize EMP damage.\n");
            }
            else if (damageTypeInput == 2)
            {
                damageType = DamageType.FlaK;
                Console.WriteLine("\nWill optimize flaK damage.\n");
            }
            else if (damageTypeInput == 3)
            {
                damageType = DamageType.Frag;
                Console.WriteLine("\nWill optimize frag damage.\n");
            }
            else if (damageTypeInput == 4)
            {
                damageType = DamageType.HE;
                Console.WriteLine("\nWill optimize HE damage.\n");
            }
            else if (damageTypeInput == 5)
            {
                damageType = DamageType.Pendepth;
                Console.WriteLine("\n");
                armorScheme.GetLayerList();
                armorScheme.CalculateLayerAC();
            }
            else
            {
                damageType = DamageType.Disruptor;
                // Overwrite head list with disruptor conduit
                headIndices.Clear();
                modIndex = 0;
                foreach (Module head in Module.AllModules)
                {
                    if (head == Module.Disruptor)
                    {
                        headIndices.Add(modIndex);
                        break;
                    }
                    modIndex++;
                }
                Console.WriteLine("Head set to Disruptor conduit.  Will optimize shield reduction strength.");
            }


            // Get volume vs cost
            int testType;
            Console.WriteLine("\nEnter 0 to measure damage per volume\nEnter 1 to damage per cost.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out testType))
                {
                    if (testType < 0 || testType > 1)
                    {
                        Console.WriteLine("\nTEST TYPE RANGE ERROR: Enter 0 for damage per volume, 1 for damage per cost.");
                    }
                    else
                    {
                        testType = Convert.ToInt32(input);
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nTEST TYPE PARSE ERROR: Enter 0 for damage per volume, 1 for damage per cost.");
                }
            }
            if (testType == 0)
            {
                Console.WriteLine("\nWill test damage per volume.\n");
            }
            else if (testType == 1)
            {
                Console.WriteLine("\nWill test damage per cost.\n");
            }


            // Get user preference on whether labels should be included in results
            bool labels;
            Console.WriteLine("\nInclude labels on results?  Labels are human-readable but inconvenient for copying to a spreadsheet.\nEnter 'y' or 'n'.");
            while (true)
            {
                input = Console.ReadLine();
                if (input.ToLower() == "y")
                {
                    labels = true;
                    Console.WriteLine("\nData readout will have labels.\n");
                    break;
                }
                else if (input.ToLower() == "n")
                {
                    labels = false;
                    Console.WriteLine("\nData readout will NOT have labels.\n");
                    break;
                }
                else
                {
                    Console.WriteLine("\nERROR: Enter 'y' to include labels on results, or 'n' to omit labels.\n");
                }
            }

            // Get user preference on writing to file or to console
            bool writeToFile;
            Console.WriteLine("\nWrite results to console or to text file?\nEnter 'c' for console or 'f' for file.");
            while (true)
            {
                input = Console.ReadLine();
                if (input.ToLower() == "f")
                {
                    writeToFile = true;
                    Console.WriteLine("\nResults will be written to file in script directory.\n");
                    break;
                }
                else if (input.ToLower() == "c")
                {
                    writeToFile = false;
                    Console.WriteLine("\nResults will be written to this console.\n");
                    break;
                }
                else
                {
                    Console.WriteLine("\nERROR: Enter 'c' to write to console, or 'f' to write to text file.\n");
                }
            }

            TestParameters tP = new();
            tP.BarrelCount = barrelCount;
            tP.MinGauge = minGauge;
            tP.MaxGauge = maxGauge;
            tP.HeadIndices = headIndices;
            tP.BaseModule = baseModule;
            tP.FixedModulecounts = fixedModulecounts;
            tP.MinModulecount = minModulecount;
            tP.VariableModuleIndices = variableModuleIndices;
            tP.MaxGPCasingCount = maxGPCasingCount;
            tP.UseEvacuator = useEvacuator;
            tP.MaxRGCasingCount = maxRGCasingCount;
            tP.MaxDraw = maxDraw;
            tP.MaxRecoil = maxRecoil;
            tP.MaxLength = maxLength;
            tP.MinVelocity = minVelocity;
            tP.MinEffectiverange = minEffectiveRange;
            tP.TargetACList = targetACList;
            tP.DamageType = damageType;
            tP.ArmorScheme = armorScheme;
            tP.TestType = testType;
            tP.Labels = labels;
            tP.WriteToFile = writeToFile;

            return tP;
        }


        /// <summary>
        /// Gathers shell parameters from user
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("From the Depths APS Shell Optimizer\nWritten by Ao Kishuba\nhttps://github.com/AoKishuba/ApsCalc");

            List<TestParameters> paramList = new();

            paramList.Add(GetTestParameters());

            // Get user preference on whether labels should be included in results
            while (true)
            {
                string input;

                Console.WriteLine("\nWould you like to enter parameters for an additional test, to run after the one you just entered?\nEnter 'y' or 'n'.");
                input = Console.ReadLine();
                if (input.ToLower() == "y")
                {
                    paramList.Add(GetTestParameters());
                }
                else if (input.ToLower() == "n")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("\nERROR: Enter 'y' to enter parameters for next test, or 'n' to begin testing.\n");
                }
            }

            Console.WriteLine("Calculating...");

            // For tracking progress
            Stopwatch stopWatchParallel = Stopwatch.StartNew();

            foreach (TestParameters tP in paramList)
            {
                Stopwatch stopWatchIndiv = Stopwatch.StartNew();

                if (tP.DamageType == DamageType.Kinetic)
                {
                    foreach (float ac in tP.TargetACList)
                    {
                        ConcurrentBag<Shell> shellBag = new();
                        Parallel.For(tP.MinGauge, tP.MaxGauge + 1, gauge =>
                        {
                            float gaugeFloat = gauge;
                            ShellCalc calcLocal = new(
                                tP.BarrelCount,
                                gauge,
                                tP.HeadIndices,
                                tP.BaseModule,
                                tP.FixedModulecounts,
                                tP.MinModulecount,
                                tP.VariableModuleIndices,
                                tP.MaxGPCasingCount,
                                tP.UseEvacuator,
                                tP.MaxRGCasingCount,
                                tP.MaxLength,
                                tP.MaxDraw,
                                tP.MaxRecoil,
                                tP.MinVelocity,
                                tP.MinEffectiverange,
                                ac,
                                tP.DamageType,
                                tP.ArmorScheme,
                                tP.TestType,
                                tP.Labels,
                                tP.WriteToFile
                                );


                            calcLocal.ShellTest();
                            calcLocal.AddTopShellsToLocalList();

                            foreach (Shell topShellLocal in calcLocal.TopShellsLocal)
                            {
                                shellBag.Add(topShellLocal);
                            }
                        });

                        ShellCalc calcFinal = new(
                                tP.BarrelCount,
                                0f, // Gauge does not matter for calcFinal because it is only running tests on pre-calculated shells
                                tP.HeadIndices,
                                tP.BaseModule,
                                tP.FixedModulecounts,
                                tP.MinModulecount,
                                tP.VariableModuleIndices,
                                tP.MaxGPCasingCount,
                                tP.UseEvacuator,
                                tP.MaxRGCasingCount,
                                tP.MaxLength,
                                tP.MaxDraw,
                                tP.MaxRecoil,
                                tP.MinVelocity,
                                tP.MinEffectiverange,
                                ac,
                                tP.DamageType,
                                tP.ArmorScheme,
                                tP.TestType,
                                tP.Labels,
                                tP.WriteToFile
                            );

                        calcFinal.FindTopShellsInList(shellBag);
                        calcFinal.AddTopShellsToDictionary();
                        calcFinal.WriteTopShells(tP.MinGauge, tP.MaxGauge);
                    }
                }
                else
                {
                    ConcurrentBag<Shell> shellBag = new();
                    Parallel.For(tP.MinGauge, tP.MaxGauge + 1, gauge =>
                    {
                        float gaugeFloat = gauge;
                        ShellCalc calcLocal = new(
                            tP.BarrelCount,
                            gauge,
                            tP.HeadIndices,
                            tP.BaseModule,
                            tP.FixedModulecounts,
                            tP.MinModulecount,
                            tP.VariableModuleIndices,
                            tP.MaxGPCasingCount,
                            tP.UseEvacuator,
                            tP.MaxRGCasingCount,
                            tP.MaxLength,
                            tP.MaxDraw,
                            tP.MaxRecoil,
                            tP.MinVelocity,
                            tP.MinEffectiverange,
                            0, // Target AC does not matter for non-kinetic tests
                            tP.DamageType,
                            tP.ArmorScheme,
                            tP.TestType,
                            tP.Labels,
                            tP.WriteToFile
                            );

                        calcLocal.ShellTest();
                        calcLocal.AddTopShellsToLocalList();

                        foreach (Shell topShellLocal in calcLocal.TopShellsLocal)
                        {
                            shellBag.Add(topShellLocal);
                        }
                    });

                    ShellCalc calcFinal = new(
                            tP.BarrelCount,
                            0f, // Gauge does not matter for calcFinal because it is only running tests on pre-calculated shells
                            tP.HeadIndices,
                            tP.BaseModule,
                            tP.FixedModulecounts,
                            tP.MinModulecount,
                            tP.VariableModuleIndices,
                            tP.MaxGPCasingCount,
                            tP.UseEvacuator,
                            tP.MaxRGCasingCount,
                            tP.MaxLength,
                            tP.MaxDraw,
                            tP.MaxRecoil,
                            tP.MinVelocity,
                            tP.MinEffectiverange,
                            0, // Target AC does not matter for non-kinetic tests
                            tP.DamageType,
                            tP.ArmorScheme,
                            tP.TestType,
                            tP.Labels,
                            tP.WriteToFile
                        );

                    calcFinal.FindTopShellsInList(shellBag);
                    calcFinal.AddTopShellsToDictionary();
                    calcFinal.WriteTopShells(tP.MinGauge, tP.MaxGauge);
                }

                Console.WriteLine("Time elapsed for this test: " + stopWatchIndiv.Elapsed);
                stopWatchIndiv.Stop();
            }
            TimeSpan parallelDuration = stopWatchParallel.Elapsed;
            stopWatchParallel.Stop();

            Console.WriteLine("\nTime elapsed (all tests): " + parallelDuration);

            // Keep window open until user presses Esc
            ConsoleKeyInfo cki;
            // Prevent window from ending if CTL+C is pressed.
            Console.TreatControlCAsInput = true;

            Console.WriteLine("Press Escape (Esc) key to quit: \n");
            do
            {
                cki = Console.ReadKey();
            } while (cki.Key != ConsoleKey.Escape);
        }
    }
}