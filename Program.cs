using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using PenCalc;

namespace ApsCalc
{
    class Program
    {
        /// <summary>
        /// Gathers shell parameters from the user, then runs the ShellCalc tests to find the highest-performing shells within those parameters
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("From the Depths APS Shell Optimizer\nWritten by Ao Kishuba\nhttps://github.com/AoKishuba/ApsCalc");
            string input;

            // Get number of barrels
            int barrelCount;
            int maxGaugeHardCap; 
            Dictionary<int, int> gaugeHardCaps = new Dictionary<int, int>
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
            foreach(KeyValuePair<int, int> entry in gaugeHardCaps)
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
            float minGauge = minGaugeInput;

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
            float maxGauge = maxGaugeInput;

            // Get head
            // Find indices of modules which can be used as heads
            int modIndex = 0;
            int minHeadIndex = 0;
            int maxHeadIndex = 0;
            foreach (Module mod in Module.AllModules)
            {
                if (mod.ModuleType == Module.Position.Middle || mod.ModuleType == Module.Position.Head)
                {
                    minHeadIndex = modIndex;
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            // Counting backwards from the end of the module list
            for (int i = Module.AllModules.Length - 1; i >= 0; i--)
            {
                if (Module.AllModules[i].ModuleType == Module.Position.Middle || Module.AllModules[i].ModuleType == Module.Position.Head)
                {
                    maxHeadIndex = i;
                    break;
                }
            }
            int headCount = 0;
            List<int> HeadIndices = new List<int>();
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
                if (input == "done")
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
                        Console.WriteLine("\nHEAD INDEX RANGE ERROR: Enter an integer from "
                            + minHeadIndex
                            + " thru "
                            + maxHeadIndex
                            + ", or type 'done'.");
                    }
                    else
                    {
                        if (HeadIndices.Contains(modIndex))
                        {
                            Console.WriteLine("\nERROR: Duplicate head index.");
                        }
                        else
                        {
                            HeadIndices.Add(modIndex);
                            Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to head list.\n");
                            headCount++;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\nHEAD INDEX PARSE ERROR: Enter an integer from "
                        + minHeadIndex
                        + " thru "
                        + maxHeadIndex
                        + ", or type 'done'.");
                }
            }


            // Get base
            // Find indices of modules which can be used as bases
            modIndex = 0;
            int minBaseIndex = 0;
            int maxBaseIndex = 0;
            foreach (Module mod in Module.AllModules)
            {
                if (mod.ModuleType == Module.Position.Base)
                {
                    minBaseIndex = modIndex;
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            // Counting backwards from the end of the module list
            for (int i = Module.AllModules.Length - 1; i >= 0; i--)
            {
                if (Module.AllModules[i].ModuleType == Module.Position.Base)
                {
                    maxBaseIndex = i;
                    break;
                }
            }
            Module Base = default(Module);
            for (int i = minBaseIndex; i <= maxBaseIndex; i++)
            {
                Console.WriteLine(i + " : " + Module.AllModules[i].Name);
            }
            Console.WriteLine("\nEnter a number to select a base for the shell, or type 'done' if no special base is desired.");
            int baseIndex;
            while (true)
            {
                input = Console.ReadLine();
                if (input == "done")
                {
                    break;
                }
                if (int.TryParse(input, out baseIndex))
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
                        Base = (Module.AllModules[baseIndex]);
                        Console.WriteLine("\n" + Base.Name + " selected.\n");
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
                if (mod.ModuleType == Module.Position.Middle)
                {
                    minBodyIndex = modIndex;
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            // Counting backwards from the end of the module list
            for (int i = Module.AllModules.Length - 1; i >= 0; i--)
            {
                if (Module.AllModules[i].ModuleType == Module.Position.Middle)
                {
                    maxBodyIndex = i;
                    break;
                }
            }
            // Get fixed body module counts
            // Create array with a number of elements equal to the number of body module types
            List<float> fixedModuleCounts = new List<float>();
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
                if (input == "done")
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

            if (Base != null)
            {
                minModulecount++;
            }

            // Calculate maximum casings and variable modules
            float maxOtherCount = 20 - minModulecount;

            Console.WriteLine("Fixed module count: " + minModulecount);
            Console.WriteLine("Maximum casing and variable module count: " + maxOtherCount);


            // Get variable modules
            Console.WriteLine("\n\n");
            int[] variableModuleIndices = { 0, 0, 0 };
            int varModCount = 0;
            while (varModCount < 3)
            {
                for (int i = minBodyIndex; i <= maxBodyIndex; i++)
                {
                    Console.WriteLine(i + " : " + Module.AllModules[i].Name);
                }
                Console.WriteLine("\nEnter a number to add a variable module.  Variable modules will be tested at every combination from 0 thru "
                    + maxOtherCount
                    + " modules.\nThree modules must be added in total, but duplicates will be tested only once.");
                input = Console.ReadLine();
                if (int.TryParse(input, out modIndex))
                {
                    if (modIndex < minBodyIndex || modIndex > maxBodyIndex)
                    {
                        Console.WriteLine("\nVARIABLEMOD INDEX RANGE ERROR: Enter an integer from "
                            + minBodyIndex
                            + " thru "
                            + maxBodyIndex
                            + ", or type 'done'.");
                    }
                    else
                    {
                        variableModuleIndices[varModCount] = modIndex;
                        Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to variable module list.\n");
                        varModCount++;
                    }
                }
                else
                {
                    Console.WriteLine("\nVARIABLEMOD INDEX PARSE ERROR: Enter an integer from "
                        + minBodyIndex
                        + " thru "
                        + maxBodyIndex
                        + ", or type 'done'.");
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
            bool evacuator = true;
            if (maxGunPowderCasingInput > 0)
            {
                Console.WriteLine("\nBore evacuator?\nEnter 'y' or 'n'.");
                while (true)
                {
                    input = Console.ReadLine();
                    input.ToLower();
                    if (input == "y")
                    {
                        evacuator = true;
                        Console.WriteLine("\nUsing bore evacuator.\n");
                        break;
                    }
                    else if (input == "n")
                    {
                        evacuator = false;
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
                    if (maxRailgunCasingInput < 0f || maxRailgunCasingInput > maxOtherCount)
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


            // Calculate minimum shell length
            float minShellLength = minGaugeInput;
            modIndex = 0;
            foreach (float modCount in fixedModulecounts)
            {
                minShellLength += Math.Min(minGaugeInput, Module.AllModules[modIndex].MaxLength) * modCount;
                modIndex++;
            }


            if (Base != null)
            {
                minShellLength += Math.Min(minGaugeInput, Base.MaxLength);
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


            // Get minimum velocity
            int minEffectiverangeInput;
            Console.WriteLine("\nEnter minimum effective range in m from 0 thru 2 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out minEffectiverangeInput))
                {
                    if (minEffectiverangeInput < 0 || minEffectiverangeInput > 2000)
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
            float minEffectiveRange = minEffectiverangeInput;



            // Get damage type to measure
            int damageType = 0;
            Scheme armorScheme = new Scheme();
            float targetAC = default(float);
            Console.WriteLine("\nEnter 0 to measure kinetic damage\nEnter 1 to measure chemical damage (HE, Frag, FlaK, EMP).\nEnter 2 for pendepth." +
                "\nEnter 3 for shield disruptor.");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out damageType))
                {
                    if (damageType < 0 || damageType > 3)
                    {
                        Console.WriteLine("\nDAMAGE TYPE RANGE ERROR: Enter 0 for kinetic, 1 for chemical, 2 for pendepth, or 3 for disruptor.");
                    }
                    else
                    {
                        damageType = Convert.ToInt32(input);
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nDAMAGE TYPE PARSE ERROR: Enter 0 for kinetic, 1 for chemical, 2 for pendepth, or 3 for disruptor.");
                }
            }
            if (damageType == 0)
            {
                // Get target armor class
                Console.WriteLine("\nEnter target AC from 0.1 thru 100.0.");
                while (true)
                {
                    input = Console.ReadLine();
                    if (float.TryParse(input, out targetAC))
                    {
                        if (targetAC < 0.1f || targetAC > 100.0f)
                        {
                            Console.WriteLine("\nTARGET AC RANGE ERROR: Enter a decimal from 0.1 thru 100.0.");
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nTARGET AC PARSE ERROR: Enter a decimal from 0.1 thru 100.0.");
                    }
                }
                Console.WriteLine("\nWill test kinetic damage against AC " + targetAC + ".\n");
            }
            else if (damageType == 1)
            {
                Console.WriteLine("\nWill test chemical damage.\n");
            }
            else if (damageType == 2)
            {
                Console.WriteLine("\n");
                armorScheme.GetLayerList();
                armorScheme.CalculateLayerAC();
            }
            else if (damageType == 3)
            {
                // Overwrite head list with disruptor conduit
                HeadIndices.Clear();
                modIndex = 0;
                foreach (Module head in Module.AllModules)
                {
                    if (head == Module.Disruptor)
                    {
                        HeadIndices.Add(modIndex);
                        break;
                    }
                    modIndex++;
                }
                Console.WriteLine("Head set to Disruptor conduit.  Will test shield reduction strength.");
            }


            // Get user preference on whether labels should be included in the results
            bool labels;
            Console.WriteLine("\nInclude labels on results?  Labels are human-readable but inconvenient for copying to a spreadsheet.\nEnter 'y' or 'n'.");
            while (true)
            {
                input = Console.ReadLine();
                input.ToLower();
                if (input == "y")
                {
                    labels = true;
                    Console.WriteLine("\nData readout will have labels.\n");
                    break;
                }
                else if (input == "n")
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

            // For tracking progress
            float totalCombinations = HeadIndices.Count * Math.Min(maxGaugeInput - minGaugeInput, 1);
            Stopwatch stopWatchParallel = Stopwatch.StartNew();

            ConcurrentBag<Shell> shellBag = new ConcurrentBag<Shell>();
            Parallel.For(minGaugeInput, maxGaugeInput + 1, gauge =>
            {
                float gaugeFloat = (float)gauge;
                ShellCalc calcLocal = new ShellCalc(
                    barrelCount,
                    gauge,
                    gauge,
                    HeadIndices,
                    Base,
                    fixedModulecounts,
                    minModulecount,
                    variableModuleIndices,
                    maxGPCasingCount,
                    evacuator,
                    maxRGCasingCount,
                    maxLength,
                    maxDraw,
                    minVelocity,
                    minEffectiverangeInput,
                    targetAC,
                    damageType,
                    armorScheme,
                    labels
                    );
                
                calcLocal.ShellTest();
                calcLocal.AddTopShellsToLocalList();

                foreach(Shell topShellLocal in calcLocal.TopDpsShellsLocal)
                {
                    shellBag.Add(topShellLocal);
                }
            });

            ShellCalc calcFinal = new ShellCalc(
                barrelCount,
                minGauge,
                maxGauge,
                HeadIndices,
                Base,
                fixedModulecounts,
                minModulecount,
                variableModuleIndices,
                maxGPCasingCount,
                evacuator,
                maxRGCasingCount,
                maxLength,
                maxDraw,
                minVelocity,
                minEffectiverangeInput,
                targetAC,
                damageType,
                armorScheme,
                labels
                );

            calcFinal.FindTopShellsInList(shellBag);
            calcFinal.AddTopShellsToDictionary();
            calcFinal.WriteTopShells();
            TimeSpan parallelDuration = stopWatchParallel.Elapsed;
            stopWatchParallel.Stop();

            Console.WriteLine("Time elapsed: " + parallelDuration);

            // Keep window open until user presses Esc
            ConsoleKeyInfo cki;
            // Prevent window from ending if CTL+C is pressed.
            Console.TreatControlCAsInput = true;

            Console.WriteLine("Press the Escape (Esc) key to quit: \n");
            do
            {
                cki = Console.ReadKey();
            } while (cki.Key != ConsoleKey.Escape);
        }
    }
}