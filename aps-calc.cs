using System;
using System.Collections.Generic;

namespace aps_calc
{
    class Program
    {
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
                        Console.WriteLine("MIN GAUGE RANGE ERROR: Enter an integer from 18 thru 500.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MIN GAUGE PARSE ERROR: Enter an integer from 18 thru 500.");
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
                        Console.WriteLine("MAX GAUGE RANGE ERROR: Enter an integer from " + MinGauge + " thru 500.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MAX GAUGE PARSE ERROR: Enter an integer from " + MinGauge + " thru 500.");
                }
            }
            Console.WriteLine("\nWill test gauges from " + MinGauge + " mm thru " + MaxGauge + " mm.\n");

            // Get head
            Module Head;
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
            Console.WriteLine("\nEnter a number to select a head.  The head will be at the front of every shell.");

            int head_index;
            while (true)
            {
                input = Console.ReadLine();
                if (int.TryParse(input, out head_index))
                {
                    if (head_index < 0 || head_index > 11) // Indices of all modules which can be used as heads
                    {
                        Console.WriteLine("HEAD INDEX RANGE ERROR: Enter an integer from 0 thru 11.");
                    }
                    else
                    {
                        Head = (Module.AllModules[head_index]);
                        Console.WriteLine("\n" + Head.Name + " selected.\n");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("HEAD INDEX PARSE ERROR: Enter an integer from 0 thru 11.");
                }
            }
            TestShell.HeadModule = Head;

            // Get base
            Module Base = default(Module);
            for (int i = 12; i < Module.AllModules.Length; i++) // Indices of all modules which can be used as bases
            {
                Console.WriteLine(i + " : " + Module.AllModules[i].Name);
            }
            Console.WriteLine("\nEnter a number to select a base for the shell, or type 'done' if no special base is desired.");

            int base_index;
            while (true)
            {
                input = Console.ReadLine();
                if (input == "done")
                {
                    break;
                }
                if (int.TryParse(input, out base_index))
                {
                    if (base_index < 12 || base_index > Module.AllModules.Length) // Indices of all modules which can be used as bases
                    {
                        Console.WriteLine("BASE INDEX RANGE ERROR: Enter an integer from 12 thru " + Module.AllModules.Length + ".");
                    }
                    else
                    {
                        Base = (Module.AllModules[base_index]);
                        Console.WriteLine("\n" + Base.Name + " selected.\n");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("Base INDEX PARSE ERROR: Enter an integer from 12 thru " + Module.AllModules.Length + ".");
                }
            }
            TestShell.BaseModule = Base;

            /*
            // Get required modules
            List<Module> RequiredMods = new List<Module>();
            int mod_index;

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
                if (int.TryParse(input, out mod_index))
                {
                    if (mod_index < 0 || mod_index > 5) // Indices of all modules except bases and heads
                    {
                        Console.WriteLine("REQUIREDMOD INDEX RANGE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                    }
                    else
                    {
                        RequiredMods.Add(Module.AllModules[mod_index]);
                        Console.WriteLine("\n" + Module.AllModules[mod_index].Name + " added to required module list.\n");
                    }
                }
                else
                {
                    Console.WriteLine("REQUIREDMOD INDEX PARSE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                }
            }

            // Calculate minimum module count
            Console.WriteLine("\n\n");
            float MinModuleCount = 1f; // The head
            Console.WriteLine("Current shell configuration:");
            Console.WriteLine(Head.Name);

            foreach (Module mod in RequiredMods)
            {
                Console.WriteLine(mod.Name);
                MinModuleCount += 1f;
            }

            if (Base != null)
            {
                Console.WriteLine(Base.Name);
                MinModuleCount += 1f;
            }

            // Calculate maximum casings and variable modules
            float MaxOtherCount = 20f - MinModuleCount;

            Console.WriteLine("Minimum module count: " + MinModuleCount);
            Console.WriteLine("Maximum casing and variable module count: " + MaxOtherCount);

            // Get variable modules
            Console.WriteLine("\n\n");
            int[] VariableModIndices = new int[] { 20, 20 }; // Default values are out of range for later testing
            int arrayIndex = 0;
            while (arrayIndex < 2)
            {
                for (int i = 0; i < 5; i++) // Indices of all modules except bases and heads
                {
                    Console.WriteLine(i + " : " + Module.AllModules[i].Name);
                }
                Console.WriteLine("\nEnter a number to add a variable module.  Two must be selected in total."
                    + "\nVariable modules will be tested at every combination from 0 thru " + MaxOtherCount + " modules.");
                input = Console.ReadLine();
                if (int.TryParse(input, out mod_index))
                {
                    if (mod_index < 0 || mod_index > 5) // Indices of all modules except bases and heads
                    {
                        Console.WriteLine("VARIABLEMOD INDEX RANGE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
                    }
                    else
                    {
                        VariableModIndices[arrayIndex] = mod_index;
                        Console.WriteLine("\n" + Module.AllModules[mod_index].Name + " added to variable module list.\n");
                        arrayIndex += 1;
                    }
                }
                else
                {
                    Console.WriteLine("VARIABLEMOD INDEX PARSE ERROR: Enter an integer from 0 thru 4, or type 'done'.");
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
                        Console.WriteLine("MAX GP CASING COUNT RANGE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MAX GP COUNT PARSE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
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
                        Console.WriteLine("MAX RG CASING COUNT RANGE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MAX RG COUNT PARSE ERROR: Enter an integer from 0 thru " + MaxOtherCount + ".");
                }
            }
            */

            // Calculate minimum shell length
            float MinShellLengthInput = Math.Min(MinGauge, Head.MaxLength);

            /*
            foreach (Module mod in RequiredMods)
            {
                MinShellLengthInput += Math.Min(MinGauge, mod.MaxLength);
            }
            */

            if (Base != null)
            {
                MinShellLengthInput += Math.Min(MinGauge, Base.MaxLength);
            }

            
            // Get maximum shell length
            float MaxShellLengthInput;
            Console.WriteLine("\nEnter maximum shell length in mm from " + MinShellLengthInput + " thru 10 000.");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out MaxShellLengthInput))
                {
                    if (MaxShellLengthInput < MinShellLengthInput || MaxShellLengthInput > 10000f)
                    {
                        Console.WriteLine("MAX SHELL LENGTH RANGE ERROR: Enter an integer from " + MinShellLengthInput + " thru 10 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MAX SHELL LENGTH PARSE ERROR: Enter an integer from " + MinShellLengthInput + " thru 10 000.");
                }
            }

            /*
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
                        Console.WriteLine("MAX RAIL DRAW RANGE ERROR: Enter an integer from 0 thru 200 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MAX RAIL DRAW PARSE ERROR: Enter an integer from 0 thru 200 000.");
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
                        Console.WriteLine("MIN SHELL VELOCITY RANGE ERROR: Enter an integer from 0 thru 5 000.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("MIN SHELL VELOCITY PARSE ERROR: Enter an integer from 0 thru 5 000.");
                }
            }

            // Get target armor class
            float TargetACInput;
            Console.WriteLine("\nEnter target AC from 0.1 thru 100.0.");
            while (true)
            {
                input = Console.ReadLine();
                if (float.TryParse(input, out TargetACInput))
                {
                    if (TargetACInput < 0.1f || TargetACInput > 100.0f)
                    {
                        Console.WriteLine("TARGET AC RANGE ERROR: Enter a decimal from 0.1 thru 100.0.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("TARGET AC PARSE ERROR: Enter a decimal from 0.1 thru 100.0.");
                }
            }
            */

            // Count number of tests for debugging and bragging rights
            int TestComparison = 0; // Completed tests
            int TestRejectLength = 0;
            int TestRejectVelocity = 0;
            int TestTotal = 0;

            for (float Gauge = MinGauge; Gauge <= MaxGauge; Gauge++)
            {
                TestShell.Gauge = Gauge;
                TestShell.GPCasingCount = 3;
                TestShell.RGCasingCount = 3;
                TestShell.ShellModuleCounts[2] = 4;
                TestShell.ShellModuleCounts[4] = 1;
                TestShell.RailDraw = 100;

                Console.WriteLine("Gauge: " + TestShell.Gauge);
                Console.WriteLine("GP Casings: " + TestShell.GPCasingCount);
                Console.WriteLine("RG Casings: " + TestShell.RGCasingCount);
                Console.WriteLine("Base: " + TestShell.BaseModule.Name);
                Console.WriteLine("Head: " + TestShell.HeadModule.Name);

                int totalModCount = (1
                    + (int)Math.Ceiling(TestShell.GPCasingCount)
                    + (int)(TestShell.RGCasingCount));

                int modIndex = 0;
                foreach (int modCount in TestShell.ShellModuleCounts)
                {
                    if (modCount > 0)
                    {
                        Console.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                    }
                    totalModCount += modCount;
                    modIndex += 1;
                }

                if (TestShell.BaseModule != null)
                {
                    totalModCount += 1;
                }

                Console.WriteLine("Total Modules: " + totalModCount);




                TestShell.CalculateLengths();
                Console.WriteLine("Casing Length: " + TestShell.CasingLength);
                Console.WriteLine("Body Length: " + TestShell.BodyLength);
                Console.WriteLine("Projectile Length: " + TestShell.ProjectileLength);
                Console.WriteLine("Total Length: " + TestShell.TotalLength);
                Console.WriteLine("Short Length: " + TestShell.ShortLength);
                Console.WriteLine("Effective Body Length: " + TestShell.EffectiveBodyLength);
                Console.WriteLine("Length Differential: " + TestShell.LengthDifferential);


                if (TestShell.TotalLength > MaxShellLengthInput)
                {
                    TestRejectLength += 1;
                }
                else
                {
                    TestShell.CalculateMaxDraw();
                    Console.WriteLine("Max Draw " + TestShell.MaxDraw);
                    TestShell.CalculateGPRecoil();
                    Console.WriteLine("GP Recoil " + TestShell.GPRecoil);
                    Console.WriteLine("Rail Draw: " + TestShell.RailDraw);
                    TestShell.CalculateReloadTime();
                    Console.WriteLine("Reload Time " + TestShell.ReloadTime);
                    TestShell.CalculateChemDamage();
                    Console.WriteLine("Chem Damage: " + TestShell.ChemDamage);
                    TestShell.CalculateVelocity();
                    Console.WriteLine("Velocity: " + TestShell.Velocity);
                    Console.WriteLine("Overall Velocity Modifier " + TestShell.OverallVelocityModifier);
                    Console.WriteLine("Total Recoil: " + TestShell.TotalRecoil);
                }
            }

            // Results
            TestTotal = TestComparison + TestRejectLength + TestRejectVelocity;
            Console.WriteLine("\n" + TestComparison + " shells compared.");
            Console.WriteLine("\n" + TestRejectLength + " tests aborted due to shell length.");
            Console.WriteLine("\n" + TestRejectVelocity + " tests aborted due to shell velocity.");
            Console.WriteLine("\n" + TestTotal + " shells total.");
        }
    }
}
