using System;
using System.Collections.Generic;
using System.Text;

namespace ApsCalc
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

        // Keep counts of body modules.  0 thru 4 are indices for body module types: solid, sabot, chem, fuse, fin.
        public float[] BodyModuleCounts { get; set; } = { 0, 0, 0, 0, 0 };

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
        public float OverallKineticDamageModifier { get; set; }
        public float OverallArmorPierceModifier { get; set; }
        public float OverallPayloadModifier { get; set; } = 1f;

        // Power
        public float GPRecoil { get; set; }
        public float MaxDraw { get; set; }
        public float RailDraw { get; set; } = 0;
        public float TotalRecoil { get; set; }
        public float Velocity { get; set; }

        // Reload and Cooldown
        public float ReloadTime { get; set; }
        public float ReloadTimeBelt { get; set; } = 0; // Beltfed Loader
        public float CooldownTime { get; set; }

        // Damage
        public float KineticDamage { get; set; }
        public float ArmorPierce { get; set; }
        public float EffectiveKineticDamage { get; set; }
        public float KineticDPS { get; set; } = 0;
        public float KineticDPSBelt { get; set; } = 0;
        public float ChemDamage { get; set; } // Frag, FlaK, HE, and EMP all scale the same
        public float ChemDPS { get; set; } = 0;
        public float ChemDPSBelt { get; set; } = 0;

        public float ModuleCountTotal { get; set; } = 1; // There must always be a head


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

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float ModuleLength = Math.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                BodyLength += ModuleLength * modCount;
                modIndex++;
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
            MaxDraw = 12500f * GaugeCoefficient * (EffectiveProjectileModuleCount + (0.5f * RGCasingCount));
        }

        public void CalculateModifiers()
        {
            // Calculate weighted velocity modifier of body
            float weightedVelocityMod = 0f;
            if (BaseModule != null)
            {
                weightedVelocityMod += BaseModule.VelocityMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            // Add body module weighted modifiers
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = Math.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedVelocityMod += (modLength * Module.AllModules[modIndex].VelocityMod * modCount);
                modIndex++;
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


            // Calculate weighted KineticDamage modifier of body
            float weightedKineticDamageMod = 0f;
            if (BaseModule != null)
            {
                weightedKineticDamageMod += BaseModule.KineticDamageMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = Math.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedKineticDamageMod += (modLength * Module.AllModules[modIndex].KineticDamageMod * modCount);
                modIndex++;
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedKineticDamageMod += LengthDifferential;
            }

            weightedKineticDamageMod /= EffectiveBodyLength;

            OverallKineticDamageModifier = weightedKineticDamageMod * HeadModule.KineticDamageMod;

            // Calculate weighted AP modifier of body
            float weightedArmorPierceMod = 0f;
            if (BaseModule != null)
            {
                weightedArmorPierceMod += BaseModule.ArmorPierceMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = Math.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedArmorPierceMod += (modLength * Module.AllModules[modIndex].ArmorPierceMod * modCount);
                modIndex++;
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedArmorPierceMod += LengthDifferential;
            }

            weightedArmorPierceMod /= EffectiveBodyLength;

            OverallArmorPierceModifier = weightedArmorPierceMod * HeadModule.ArmorPierceMod;


            // Get payload modifier for chemical shells
            if (BaseModule != null)
            {
                OverallPayloadModifier = Math.Min(OverallPayloadModifier, BaseModule.PayloadMod);
            }

            modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (modCount > 0)
                {
                    OverallPayloadModifier = Math.Min(OverallPayloadModifier, Module.AllModules[modIndex].PayloadMod);
                }
                modIndex++;
            }

            if (HeadModule == Module.Disruptor) // Disruptor 50% penalty stacks
            {
                OverallPayloadModifier *= 0.5f;
            }
            else
            {
                OverallPayloadModifier = Math.Min(OverallPayloadModifier, HeadModule.PayloadMod);
            }
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
            else
            {
                KineticDamage = (float)Math.Pow(500 / Math.Max(Gauge, 100f), 0.15)
                    * GaugeCoefficient
                    * EffectiveProjectileModuleCount
                    * Velocity
                    * OverallKineticDamageModifier
                    * 3.5f;
            }
        }

        public void CalculateAP()
        {
            // Head must be set before this can be called
            if (HeadModule == null)
            {
                Console.WriteLine("\nERROR: Cannot calculate armor pierce.  Set Head Module first.\n");
            }

            ArmorPierce = Velocity * OverallArmorPierceModifier * 0.0175f;
        }

        public void CalculateReloadTime()
        {
            ReloadTime = (float)(Math.Pow((Gauge * Gauge * Gauge / 125000000f), 0.45)
                * (2f + EffectiveProjectileModuleCount + 0.25f * (RGCasingCount + GPCasingCount))
                * 17.5f);

            if (TotalLength <= 1000f)
            {
                ReloadTimeBelt = (ReloadTime * 0.75f * (float)Math.Pow(Gauge / 1000f, 0.45));
            }
            else
            {
                ReloadTimeBelt = default(float);
            }
        }

        public void CalculateChemDamage()
        {
            float ChemBodies = 0;
            // Count chemical bodies.  This could be simplified to just adding the value at index 2, but indices might shift
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (Module.AllModules[modIndex].IsChem)
                {
                    ChemBodies += modCount;
                }
            }
            if (BaseModule?.IsChem == true)
            {
                ChemBodies += 1;
            }
            if (HeadModule.IsChem)
            {
                ChemBodies += 1;
            }

            ChemDamage = GaugeCoefficient * ChemBodies;
        }

        public void CalculateKineticDPS(float targetAC)
        {
            EffectiveKineticDamage = KineticDamage * Math.Min(1, ArmorPierce / targetAC);
            KineticDPS = EffectiveKineticDamage / ReloadTime;

            if (TotalLength <= 1000f)
            {
                KineticDPSBelt = EffectiveKineticDamage / ReloadTimeBelt;
            }
            else
            {
                KineticDPSBelt = default(float); // Reset value
            }
        }

        public void CalculateChemDPS()
        {
            ChemDPS = ChemDamage / ReloadTime;
            ChemDPSBelt = default(float); // Reset value
            if (TotalLength <= 1000f)
            {
                ChemDPSBelt = ChemDamage / ReloadTimeBelt;
            }
        }

        public void GetModuleCounts()
        {
            // ModuleCountTotal starts at 1 for the head

            if (BaseModule != null)
            {
                ModuleCountTotal += 1;
            }

            foreach (float modCount in BodyModuleCounts)
            {
                ModuleCountTotal += modCount;
            }

            ModuleCountTotal = (float)(Math.Ceiling(GPCasingCount) + RGCasingCount);
        }

        public void GetShellInfoKinetic()
        {
            Console.WriteLine("Gauge (mm): " + Gauge);
            Console.WriteLine("Total length (mm): " + TotalLength);
            Console.WriteLine("Length without casings: " + ProjectileLength);
            Console.WriteLine("Head: " + HeadModule.Name);

            // Add module counts
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (modCount > 0)
                {
                    Console.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                }
                modIndex++;
            }

            if (BaseModule != null)
            {
                Console.WriteLine("Base: " + BaseModule.Name);
            }

            if (GPCasingCount > 0)
            {
                Console.WriteLine("GP Casing: " + GPCasingCount);
            }

            if (RGCasingCount > 0)
            {
                Console.WriteLine("RG Casing: " + RGCasingCount);
            }

            Console.WriteLine("Rail draw: " + RailDraw);
            Console.WriteLine("Reload time (s): " + ReloadTime);

            if (ReloadTimeBelt > 0)
            {
                Console.WriteLine("Reload time (belt): " + ReloadTimeBelt);
            }

            Console.WriteLine("Velocity (m/s): " + Velocity);
            Console.WriteLine("Base KD: " + KineticDamage);
            Console.WriteLine("AP: " + ArmorPierce);
            Console.WriteLine("Eff. KD: " + EffectiveKineticDamage);
            Console.WriteLine("DPS: " + KineticDPS);

            if (KineticDPSBelt > 0)
            {
                Console.WriteLine("DPS (beltfed): " + KineticDPSBelt);
            }
        }

        public void GetShellInfoChem()
        {
            Console.WriteLine("Gauge (mm): " + Gauge);
            Console.WriteLine("Total length (mm): " + TotalLength);
            Console.WriteLine("Length without casings: " + ProjectileLength);
            Console.WriteLine("Head:" + HeadModule.Name);

            // Add module counts
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (modCount > 0)
                {
                    Console.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                }
                modIndex++;
            }

            if (BaseModule != null)
            {
                Console.WriteLine("Base: " + BaseModule.Name);
            }

            if (GPCasingCount > 0)
            {
                Console.WriteLine("GP Casing: " + GPCasingCount);
            }

            if (RGCasingCount > 0)
            {
                Console.WriteLine("RG Casing: " + RGCasingCount);
            }

            Console.WriteLine("Rail draw: " + RailDraw);
            Console.WriteLine("Reload time (s): " + ReloadTime);

            if (ReloadTimeBelt > 0)
            {
                Console.WriteLine("Reload time (belt): " + ReloadTimeBelt);
            }

            Console.WriteLine("Velocity (m/s): " + Velocity);
            Console.WriteLine("Chem damage multiplier: " + ChemDamage);
            Console.WriteLine("DPS: " + ChemDPS);

            if (KineticDPSBelt > 0)
            {
                Console.WriteLine("DPS (beltfed): " + ChemDPSBelt);
            }
        }
    }
}
