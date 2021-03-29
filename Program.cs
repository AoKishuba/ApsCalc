using System;
using System.Collections.Generic;

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
            // Initialize shell
            Shell TestShell = new Shell();

            // Get minimum gauge
            float MinGauge;
            string input;

            Console.WriteLine("Enter minimum gauge in mm from 18 thru 500: ");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MinGauge))
                {
                    if (MinGauge < 18f || MinGauge > 500f)
                    {
                        Console.WriteLine("\nMIN GAUGE RANGE ERROR: Enter an integer from 18 thru 500.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMIN GAUGE PARSE ERROR: Enter an integer from 18 thru 500.");
                }
            }

            // Get maximum gauge
            float MaxGauge;

            Console.WriteLine("\nEnter maximum gauge in mm from " + MinGauge + " thru 500.");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MaxGauge))
                {
                    if (MaxGauge < MinGauge || MaxGauge > 500f)
                    {
                        Console.WriteLine("\nMAX GAUGE RANGE ERROR: Enter an integer from " + MinGauge + " thru 500.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX GAUGE PARSE ERROR: Enter an integer from " + MinGauge + " thru 500.");
                }
            }
            Console.WriteLine("\nWill test gauges from " + MinGauge + " mm thru " + MaxGauge + " mm.\n");

            // Get head


            int modIndex;
            int headCount = 0;
            List<int> HeadIndices = new List<int>();
            while (true)
            {
                for (int i = 0; i < 12; i++) // Indices of all modules which can be used as heads
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
                    if (modIndex < 0 || modIndex > 12) // Indices of all modules which can be used as heads
                    {
                        Console.WriteLine("\nHEAD INDEX RANGE ERROR: Enter an integer from 0 thru 11, or type 'done'.");
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
                    Console.WriteLine("\nHEAD INDEX PARSE ERROR: Enter an integer from 0 thru 11, or type 'done'.");
                }

            }


            // Get base
            Module Base = default(Module);
            for (int i = 12; i < Module.AllModules.Length; i++) // Indices of all modules which can be used as bases
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
                    if (baseIndex < 12 || baseIndex > Module.AllModules.Length) // Indices of all modules which can be used as bases
                    {
                        Console.WriteLine("\nBASE INDEX RANGE ERROR: Enter an integer from 12 thru " + Module.AllModules.Length + ".");
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
                    Console.WriteLine("\nBASE INDEX PARSE ERROR: Enter an integer from 12 thru " + Module.AllModules.Length + ".");
                }
            }
            TestShell.BaseModule = Base;

            
            // Get fixed modules
            float[] FixedModuleCounts = { 0, 0, 0, 0, 0 };
            while (true)
            {
                for (int i = 0; i < 5; i++) // Indices of all modules except bases and heads
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
                    if (modIndex < 0 || modIndex > 5) // Indices of all modules except bases and heads
                    {
                        Console.WriteLine("\nFIXEDMOD INDEX RANGE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                    }
                    else
                    {
                        FixedModuleCounts[modIndex] += 1f;
                        Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to fixed module list.\n");
                    }
                }
                else
                {
                    Console.WriteLine("\nFIXEDMOD INDEX PARSE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                }
            }

            // Calculate minimum module count
            float MinModuleCount = 1; // The head

            modIndex = 0;
            foreach (float modCount in FixedModuleCounts)
            {
                MinModuleCount += modCount;
                modIndex++;
            }

            if (Base != null)
            {
                MinModuleCount++;
            }

            // Calculate maximum casings and variable modules
            float MaxOtherCount = 20 - MinModuleCount;

            Console.WriteLine("Fixed module count: " + MinModuleCount);
            Console.WriteLine("Maximum casing and variable module count: " + MaxOtherCount);

            // Get variable modules
            Console.WriteLine("\n\n");
            int[] VariableModuleIndices = { 0, 0 };
            int varModCount = 0;
            while (varModCount < 2)
            {
                for (int i = 0; i < 5; i++) // Indices of all modules except bases and heads
                {
                    Console.WriteLine(i + " : " + Module.AllModules[i].Name);
                }
                Console.WriteLine("\nEnter a number to add a variable module.  If only one variable module is desired, enter the same number both times."
                    + "\nVariable modules will be tested at every combination from 0 thru " + MaxOtherCount + " modules.");
                input = Console.ReadLine();
                if (int.TryParse(input, out modIndex))
                {
                    if (modIndex < 0 || modIndex > 5) // Indices of all modules except bases and heads
                    {
                        Console.WriteLine("\nVARIABLEMOD INDEX RANGE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                    }
                    else
                    {
                        VariableModuleIndices[varModCount] = modIndex;
                        Console.WriteLine("\n" + Module.AllModules[modIndex].Name + " added to variable module list.\n");
                        varModCount++;
                    }
                }
                else
                {
                    Console.WriteLine("\nVARIABLEMOD INDEX PARSE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                }
            }

            // Get maximum GP casing count
            float MaxGunpowderCasingInput;
            Console.WriteLine("\nEnter maximum number of GP casings from 0 thru " + MaxOtherCount + ": ");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MaxGunpowderCasingInput))
                {
                    if (MaxGunpowderCasingInput < 0f || MaxGunpowderCasingInput > MaxOtherCount)
                    {
                        Console.WriteLine("\nMAX GP CASING COUNT RANGE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX GP COUNT PARSE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                }
            }

            // Get maximum RG casing count
            float MaxRailgunCasingInput;
            Console.WriteLine("\nEnter maximum number of RG casings from 0 thru " + MaxOtherCount + ": ");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MaxRailgunCasingInput))
                {
                    if (MaxRailgunCasingInput < 0f || MaxRailgunCasingInput > MaxOtherCount)
                    {
                        Console.WriteLine("\nMAX RG CASING COUNT RANGE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX RG COUNT PARSE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                }
            }


            // Get maximum rail draw
            float MaxRailDrawInput;
            Console.WriteLine("\nEnter maximum rail draw from 0 thru 200 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MaxRailDrawInput))
                {
                    if (MaxRailDrawInput < 0 || MaxRailDrawInput > 200000f)
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


            // Calculate minimum shell length
            float MinShellLengthInput = MinGauge;

            modIndex = 0;
            foreach (float modCount in FixedModuleCounts)
            {
                MinShellLengthInput += Math.Min(MinGauge, Module.AllModules[modIndex].MaxLength) * modCount;
                modIndex++;
            }
            

            if (Base != null)
            {
                MinShellLengthInput += Math.Min(MinGauge, Base.MaxLength);
            }

            
            // Get maximum shell length
            float MaxShellLengthInput;
            Console.WriteLine("\nEnter maximum shell length in mm from " + MinShellLengthInput + " thru 8 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MaxShellLengthInput))
                {
                    if (MaxShellLengthInput < MinShellLengthInput || MaxShellLengthInput > 8000f)
                    {
                        Console.WriteLine("\nMAX SHELL LENGTH RANGE ERROR: Enter an integer from " + MinShellLengthInput + " thru 8 000.");
                    }
                    else
                    {
                        Console.WriteLine("\nWill test shells up to " + MaxShellLengthInput + " mm.\n");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nMAX SHELL LENGTH PARSE ERROR: Enter an integer from " + MinShellLengthInput + " thru 8 000.");
                }
            }

            // Get minimum velocity
            float MinShellVelocityInput;
            Console.WriteLine("\nEnter minimum shell velocity in m/s from 0 thru 5 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MinShellVelocityInput))
                {
                    if (MinShellVelocityInput < 0f || MinShellVelocityInput > 5000f)
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

            int DamageTypeInput = 0;
            float TargetACInput = default(float);
            // Get damage type to measure
            Console.WriteLine("\nEnter 0 to measure kinetic damage\nEnter 1 to measure chemical damage (HE, Frag, FlaK, EMP).");
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out DamageTypeInput))
                {
                    if (DamageTypeInput < 0 || DamageTypeInput > 1)
                    {
                        Console.WriteLine("\nDAMAGE TYPE RANGE ERROR: Enter 0 for kinetic or 1 for chemical.");
                    }
                    else
                    {
                        DamageTypeInput = Convert.ToInt32(input);
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\nDAMAGE TYPE PARSE ERROR: Enter 0 for kinetic or 1 for chemical.");
                }
            }
            if (DamageTypeInput == 0)
            {

                // Get target armor class
                Console.WriteLine("\nEnter target AC from 0.1 thru 100.0.");
                while (true)
                {
                    input = Console.ReadLine();
                    if (float.TryParse(input, out TargetACInput))
                    {
                        if (TargetACInput < 0.1f || TargetACInput > 100.0f)
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
                Console.WriteLine("\nWill test kinetic damage against AC " + TargetACInput + ".\n");
            }
            else
            {
                Console.WriteLine("\nWill test chemical damage.\n");
            }



            ShellCalc Calc1 = new ShellCalc(
                MinGauge,
                MaxGauge,
                HeadIndices,
                Base,
                FixedModuleCounts,
                MinModuleCount,
                VariableModuleIndices,
                MaxGunpowderCasingInput,
                MaxRailgunCasingInput,
                MaxShellLengthInput,
                MaxRailDrawInput,
                MinShellVelocityInput,
                TargetACInput,
                DamageTypeInput
                );

            Calc1.ShellTest();
        }
    }
}
