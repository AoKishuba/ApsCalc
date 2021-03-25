using System;
using System.Collections.Generic;
using System.Text;

namespace aps_calc
{
    public class Shell
    {
        public Shell() { BaseModule = default(Module); }

        private float _gauge;
        public float Gauge
        {
            get { return _gauge; }
            set
            {
                _gauge = value;
                GaugeCoefficient = (float)Math.Pow(Gauge * Gauge * Gauge / 125000000, 0.6);
            }
        }
        public float GaugeCoefficient { get; set; } // Expensive to calculate and used in several formulae

        // Keep count of middle modules.  Bases and heads (index 5 thru 14) excluded because they are unique.
        public float[] ShellModuleCounts { get; set; } = { 0, 0, 0, 0, 0 };

        public Module BaseModule { get; set; } // Optional; is 'null' if no base is chosen by user
        public Module HeadModule { get; set; } // There must always be a Head

        // Gunpowder and Railgun casing counts
        public float GPCasingCount { get; set; }
        public float RGCasingCount { get; set; }

        // Lengths
        public float CasingLength { get; set; }
        public float ProjectileLength { get; set; } // Everything but casings and Head
        public float BodyLength { get; set; } // Everything but casings
        public float TotalLength { get; set; }
        public float ShortLength { get; set; } // Used for penalizing short shells
        public float LengthDifferential { get; set; } // Used for penalizing short shells
        public float EffectiveBodyLength { get; set; } // Used for penalizing short shells
        public float EffectiveBodyModuleCount { get; set; } // Compensate for length-limited modules
        public float EffectiveProjectileModuleCount { get; set; } // Compensate for length-limited modules

        // Overall modifiers
        public float OverallVelocityModifier { get; set; }
        public float OverallKDModifier { get; set; }
        public float OverallAPModifier { get; set; }

        // Power
        public float GPRecoil { get; set; }
        public float MaxDraw { get; set; }
        public float RailDraw { get; set; } = 0;
        public float TotalRecoil { get; set; }
        public float Velocity { get; set; }

        // Reload and Cooldown
        public float ReloadTime { get; set; }
        public float BeltReloadTime { get; set; } = 0; // Beltfed Loader
        public float CooldownTime { get; set; }

        // Damage
        public float KineticDamage { get; set; }
        public float ArmorPierce { get; set; }
        public float ChemDamage { get; set; } // Frag, FlaK, HE, and EMP all scale the same


        public void CalculateLengths()
        {
            if (BaseModule == null)
            {
                BodyLength = 0;
            }
            else
            {
                BodyLength = Math.Min(Gauge, BaseModule.MaxLength);
            }

            for (int i = 0; i < ShellModuleCounts.Length; i++) // Module indices 0 thru 4 include all but bases and heads
            {
                float ModuleLength = Math.Min(Gauge, Module.AllModules[i].MaxLength);
                BodyLength += ModuleLength * ShellModuleCounts[i];
            }

            CasingLength = (GPCasingCount + RGCasingCount) * Gauge;

            // Head must be set before this can be called
            if (HeadModule == null)
            {
                Console.WriteLine("\nERROR: Cannot calculate length.  Set Head Module first.\n"); // Need to figure out proper error handling
            }
            else
            {
                float HeadLength = Math.Min(Gauge, HeadModule.MaxLength);
                ProjectileLength = BodyLength + HeadLength;
            }

            TotalLength = CasingLength + ProjectileLength;

            ShortLength = 2 * Gauge;
            LengthDifferential = Math.Max(ShortLength - BodyLength, 0);
            EffectiveBodyLength = Math.Max(2 * Gauge, BodyLength);

            EffectiveBodyModuleCount = BodyLength / Gauge;
            EffectiveProjectileModuleCount = ProjectileLength / Gauge;
        }

        public void CalculateGPRecoil()
        {
            GPRecoil = GaugeCoefficient * GPCasingCount * 2500;
        }

        public void CalculateMaxDraw()
        {
            MaxDraw = (GaugeCoefficient * ((ProjectileLength / Gauge) + (0.5f * RGCasingCount)) * 12500f);
        }

        public void CalculateVelocity()
        {
            // Head must be set before this can be called
            if (HeadModule == null)
            {
                Console.WriteLine("\nERROR: Cannot calculate velocity.  Set Head Module first.\n");
            }
            else
            {
                TotalRecoil = GPRecoil + RailDraw;

                // Calculate weighted velocity modifier of body
                float weightedVelocityMod = 0f;
                if (BaseModule != null)
                {
                    weightedVelocityMod += BaseModule.VelocityMod * Math.Min(Gauge, BaseModule.MaxLength);
                }

                int moduleIndex = 0;
                foreach (int moduleCount in ShellModuleCounts) // Add body module weighted modifiers
                {
                    weightedVelocityMod += Module.AllModules[moduleIndex].VelocityMod
                        * Math.Min(Gauge, Module.AllModules[moduleIndex].MaxLength)
                        * moduleCount;
                    moduleIndex++;
                }

                if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
                {
                    weightedVelocityMod += 0.7f * LengthDifferential;
                }

                weightedVelocityMod /= EffectiveBodyLength;

                OverallVelocityModifier = weightedVelocityMod * HeadModule.VelocityMod;
                if (BaseModule?.Name == "Base Bleeder")
                {
                    OverallVelocityModifier += 0.15f;
                }

                Velocity = (float)Math.Sqrt((TotalRecoil * 85f * Gauge) / (GaugeCoefficient * ProjectileLength)) * OverallVelocityModifier;
            }
        }

        public void CalculateKineticDamage()
        {
            // Head must be set before this can be called
            if (HeadModule == null)
            {
                Console.WriteLine("\nERROR: Cannot calculate kinetic damage.  Set Head Module first.\n");
            }

            // Calculate weighted KD modifier of body
            float weightedKDMod = 0f;
            if (BaseModule != null)
            {
                weightedKDMod += BaseModule.KineticDamageMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            foreach (int index in ShellModuleCounts) // Add body module weighted modifiers
            {
                weightedKDMod += Module.AllModules[index].KineticDamageMod * Math.Min(Gauge, Module.AllModules[index].MaxLength);
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedKDMod += LengthDifferential;
            }

            weightedKDMod /= EffectiveBodyLength;

            OverallKDModifier = weightedKDMod * HeadModule.KineticDamageMod;


        }

        public void CalculateAP()
        {
            // Head must be set before this can be called
            if (HeadModule == null)
            {
                Console.WriteLine("\nERROR: Cannot calculate armor pierce.  Set Head Module first.\n");
            }

            // Calculate weighted AP modifier of body
            float weightedAPMod = 0f;
            if (BaseModule != null)
            {
                weightedAPMod += BaseModule.ArmorPierceMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            foreach (int index in ShellModuleCounts) // Add body module weighted modifiers
            {
                weightedAPMod += Module.AllModules[index].ArmorPierceMod * Math.Min(Gauge, Module.AllModules[index].MaxLength);
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedAPMod += LengthDifferential;
            }

            weightedAPMod /= EffectiveBodyLength;

            OverallAPModifier = weightedAPMod * HeadModule.ArmorPierceMod;
        }

        public void CalculateReloadTime()
        {
            ReloadTime = (float)(Math.Pow((Gauge * Gauge * Gauge / 125000000f), 0.45)
                * (2f + EffectiveProjectileModuleCount + 0.25f * (RGCasingCount + GPCasingCount))
                * 17.5f);

            if (Gauge >= 100)
            {
                BeltReloadTime = (ReloadTime * 0.75f * (float)Math.Pow(Gauge / 1000f, 0.45));
            }
        }

        public void CalculateChemDamage()
        {
            // Count chemical bodies
            float ChemBodies = ShellModuleCounts[2];
            if (BaseModule != null)
            {
                if (BaseModule.IsChem)
                {
                    ChemBodies += 1;
                }
            }
            if (HeadModule.IsChem)
            {
                ChemBodies += 1;
            }

            ChemDamage = GaugeCoefficient * ChemBodies;
        }
    }
}
