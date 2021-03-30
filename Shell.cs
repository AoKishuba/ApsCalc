using System;

namespace ApsCalc
{
    public class Shell
    {
        public Shell() { BaseModule = default(Module); }

        /// <summary>
        /// Sets the gauge and simultaneously calculates the Gauge Coefficient, which is used in several formulae as a way to scale with gauge.
        /// </summary>
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
        public float CooldownTime { get; set; } // Applies to both regular and beltfed

        // Damage
        public float KineticDamage { get; set; }
        public float ArmorPierce { get; set; }
        public float EffectiveKineticDamage { get; set; }
        public float KineticDPS { get; set; } = 0;
        public float KineticDPSPerVolume { get; set; } = 0;
        public float KineticDPSBelt { get; set; } = 0;
        public float KineticDPSPerVolumeBelt { get; set; } = 0;
        public bool IsBelt { get; set; } = false; // Whether the shell should use its beltfed for comparison
        public float ChemDamage { get; set; } // Frag, FlaK, HE, and EMP all scale the same
        public float ChemDPS { get; set; } = 0; // Effective Warheads per Second
        public float ChemDPSPerVolume { get; set; } = 0;
        public float ChemDPSBelt { get; set; } = 0;
        public float ChemDPSPerVolumeBelt { get; set; } = 0;

        public float ModuleCountTotal { get; set; } = 1; // There must always be a head


        // Volume
        public float VolumePerIntake { get; set; }
        public float VolumePerIntakeBelt { get; set; }


        /// <summary>
        /// Calculates the body, projectile, casing, and total lengths, as well as the length differential, which is used to penalize short shells
        /// </summary>
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

        /// <summary>
        /// Calculates recoil from gunpowder casings
        /// </summary>
        public void CalculateGPRecoil()
        {
            GPRecoil = GaugeCoefficient * GPCasingCount * 2500;
        }

        /// <summary>
        /// Calculates max rail draw of the shell
        /// </summary>
        public void CalculateMaxDraw()
        {
            MaxDraw = 12500f * GaugeCoefficient * (EffectiveProjectileModuleCount + (0.5f * RGCasingCount));
        }


        /// <summary>
        /// Calculates velocity, kinetic damage, armor pierce, and chemical payload modifiers
        /// </summary>
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


        /// <summary>
        /// Calculates shell velocity
        /// </summary>
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


        /// <summary>
        /// Calculates the volume used by the shell, including intake, loader, cooling, recoil absorbers, and rail chargers
        /// </summary>
        public void CalculateVolume()
        {
            float intakeVolume = 1f; // Always have an intake

            float loaderVolume;
            if (TotalLength <= 1000f)
            {
                loaderVolume = 1f;
            }
            else if (TotalLength <= 2000f)
            {
                loaderVolume = 2f;
            }
            else if (TotalLength <= 4000f)
            {
                loaderVolume = 4f;
            }
            else if (TotalLength <= 6000f)
            {
                loaderVolume = 6f;
            }
            else
            {
                loaderVolume = 8f;
            }

            float recoilVolume = TotalRecoil / (ReloadTime * 120f); // Absorbers absorb 120 per second per metre

            float rpmPerCooler = 200f * (float)Math.Pow(ReloadTime, -1.4f);
            float coolerVolume = 60f / (ReloadTime * rpmPerCooler);

            float chargerVolume = RailDraw / (ReloadTime * 200f); // Chargers are 200 Energy per second

            VolumePerIntake = loaderVolume + intakeVolume + recoilVolume + coolerVolume + chargerVolume;

            if (TotalLength <= 1000f)
            {
                float recoilVolumeBelt = TotalRecoil / (ReloadTimeBelt * 120f);

                float coolerVolumeBelt = 60f / (ReloadTimeBelt * rpmPerCooler);

                float chargerVolumeBelt = RailDraw / (ReloadTimeBelt * 200f);
                // The extra 1 volume is for the clip, which is required for beltfed
                VolumePerIntakeBelt = 1f + loaderVolume + intakeVolume + recoilVolumeBelt + coolerVolumeBelt + chargerVolumeBelt;
            }
        }


        /// <summary>
        /// Calculates raw kinetic damage
        /// </summary>
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


        /// <summary>
        /// Calculates the armor pierce rating
        /// </summary>
        public void CalculateAP()
        {
            // Head must be set before this can be called
            if (HeadModule == null)
            {
                Console.WriteLine("\nERROR: Cannot calculate armor pierce.  Set Head Module first.\n");
            }

            ArmorPierce = Velocity * OverallArmorPierceModifier * 0.0175f;
        }


        /// <summary>
        /// Calculates reload time; also calculates beltfed reload time for shells 1000 mm or shorter
        /// </summary>
        public void CalculateReloadTime()
        {
            ReloadTime = (float)(Math.Pow(Gauge * Gauge * Gauge / 125000000f, 0.45)
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


        /// <summary>
        /// Calculates barrel cooldown time
        /// </summary>
        public void CalculateCooldownTime()
        {
            CooldownTime = (float)(Math.Pow(GPCasingCount, 0.35f)
                * 3.75
                * ReloadTime
                / (2 * (2 + EffectiveProjectileModuleCount + 0.25f * (GPCasingCount + RGCasingCount))));
        }


        /// <summary>
        /// Calculates chemical payload damage in "Equivalent warheads", which serves as a multiplier for the various kinds of chemical damage
        /// </summary>
        public void CalculateChemDamage()
        {
            // Count chemical bodies.
            float chemBodies = BodyModuleCounts[2];

            if (BaseModule?.IsChem == true)
            {
                chemBodies++;
            }
            if (HeadModule.IsChem == true)
            {
                chemBodies++;
            }

            ChemDamage = GaugeCoefficient * chemBodies * OverallPayloadModifier;
        }


        /// <summary>
        /// Calculates applied kinetic damage for a given target armor class
        /// </summary>
        /// <param name="targetAC"></param>
        public void CalculateKineticDPS(float targetAC)
        {
            EffectiveKineticDamage = KineticDamage * Math.Min(1, ArmorPierce / targetAC);
            KineticDPS = EffectiveKineticDamage / ReloadTime;
            KineticDPSPerVolume = KineticDPS / VolumePerIntake;

            if (TotalLength <= 1000f)
            {
                KineticDPSBelt = EffectiveKineticDamage / ReloadTimeBelt;
                KineticDPSPerVolumeBelt = KineticDPSBelt / VolumePerIntakeBelt;
            }
        }


        /// <summary>
        /// Calculates relative chemical payload damage per second, in Equivalent Warheads per second
        /// </summary>
        public void CalculateChemDPS()
        {
            ChemDPS = ChemDamage / ReloadTime;
            ChemDPSPerVolume = ChemDPS / VolumePerIntake;

            if (TotalLength <= 1000f)
            {
                ChemDPSBelt = ChemDamage / ReloadTimeBelt;
                ChemDPSPerVolumeBelt = ChemDPSBelt / VolumePerIntakeBelt;
            }
        }


        /// <summary>
        /// Calculates the total number of modules in the shell
        /// </summary>
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


        /// <summary>
        /// Gather info for top-performing shells and write to console
        /// </summary>
        /// <param name="labels">False if labels should be omitted from result printout.  Labels are hard to copy to spreadsheets</param>
        public void GetShellInfoKinetic(bool labels)
        {
            if (labels)
            {
                Console.WriteLine("Gauge (mm): " + Gauge);
                Console.WriteLine("Total length (mm): " + TotalLength);
                Console.WriteLine("Length without casings: " + ProjectileLength);


                if (RGCasingCount > 0)
                {
                    Console.WriteLine("RG Casing: " + RGCasingCount);
                }

                if (GPCasingCount > 0)
                {
                    Console.WriteLine("GP Casing: " + GPCasingCount);
                }

                int modIndex = 0;
                foreach (float modCount in BodyModuleCounts)
                {
                    if (modCount > 0)
                    {
                        Console.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                    }
                    modIndex++;
                }

                Console.WriteLine("Head: " + HeadModule.Name);
                Console.WriteLine("Rail draw: " + RailDraw);
                Console.WriteLine("Recoil: " + TotalRecoil);
                Console.WriteLine("Velocity (m/s): " + Velocity);
                Console.WriteLine("Raw kinetic damage: " + KineticDamage);
                Console.WriteLine("AP: " + ArmorPierce);
                Console.WriteLine("Effective kinetic damage: " + EffectiveKineticDamage);

                if (IsBelt)
                {
                    Console.WriteLine("Reload time (belt): " + ReloadTimeBelt);
                    Console.WriteLine("Effective kinetic DPS (belt): " + KineticDPSBelt);
                    Console.WriteLine("Effective kinetic DPS per volume (belt): " + KineticDPSPerVolumeBelt);
                }
                else
                {
                    Console.WriteLine("Reload time: " + ReloadTime);
                    Console.WriteLine("Effective kinetic DPS: " + KineticDPS);
                    Console.WriteLine("Effective kinetic DPS per volume: " + KineticDPSPerVolume);
                }
            }


            else if (!labels)
            {
                Console.WriteLine(Gauge);
                Console.WriteLine(TotalLength);
                Console.WriteLine(ProjectileLength);
                Console.WriteLine(RGCasingCount);
                Console.WriteLine(GPCasingCount);

                foreach (float modCount in BodyModuleCounts)
                {
                    Console.WriteLine(modCount);
                }

                Console.WriteLine(HeadModule.Name);

                Console.WriteLine(RailDraw);
                Console.WriteLine(TotalRecoil);
                Console.WriteLine(Velocity);
                Console.WriteLine(KineticDamage);
                Console.WriteLine(ArmorPierce);
                Console.WriteLine(EffectiveKineticDamage);

                if (IsBelt)
                {
                    Console.WriteLine(ReloadTimeBelt);
                    Console.WriteLine(KineticDPSBelt);
                    Console.WriteLine(KineticDPSPerVolumeBelt);
                }
                else
                {
                    Console.WriteLine(ReloadTime);
                    Console.WriteLine(KineticDPS);
                    Console.WriteLine(KineticDPSPerVolume);
                }
            }
        }

        public void GetShellInfoChem(bool labels)
        {
            if (labels)
            {
                Console.WriteLine("Gauge (mm): " + Gauge);
                Console.WriteLine("Total length (mm): " + TotalLength);
                Console.WriteLine("Length without casings: " + ProjectileLength);


                if (RGCasingCount > 0)
                {
                    Console.WriteLine("RG Casing: " + RGCasingCount);
                }

                if (GPCasingCount > 0)
                {
                    Console.WriteLine("GP Casing: " + GPCasingCount);
                }

                int modIndex = 0;
                foreach (float modCount in BodyModuleCounts)
                {
                    if (modCount > 0)
                    {
                        Console.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                    }
                    modIndex++;
                }

                Console.WriteLine("Head: " + HeadModule.Name);
                Console.WriteLine("Rail draw: " + RailDraw);
                Console.WriteLine("Recoil: " + TotalRecoil);
                Console.WriteLine("Velocity (m/s): " + Velocity);
                Console.WriteLine("Chemical payload strength: " + ChemDamage);


                if (IsBelt)
                {
                    Console.WriteLine("Reload time (belt): " + ReloadTimeBelt);
                    Console.WriteLine("Chemical DPS (belt): " + ChemDPSBelt);
                    Console.WriteLine("Chemical DPS per volume (belt): " + ChemDPSPerVolumeBelt);
                }
                else
                {
                    Console.WriteLine("Reload time: " + ReloadTime);
                    Console.WriteLine("Chemical DPS: " + ChemDPS);
                    Console.WriteLine("Chemical DPS per volume: " + ChemDPSPerVolume);
                }
            }


            else if (!labels)
            {
                Console.WriteLine(Gauge);
                Console.WriteLine(TotalLength);
                Console.WriteLine(ProjectileLength);
                Console.WriteLine(RGCasingCount);
                Console.WriteLine(GPCasingCount);

                foreach (float modCount in BodyModuleCounts)
                {
                    Console.WriteLine(modCount);
                }

                Console.WriteLine(HeadModule.Name);
                Console.WriteLine(RailDraw);
                Console.WriteLine(TotalRecoil);
                Console.WriteLine(Velocity);
                Console.WriteLine(ChemDamage);

                if (IsBelt)
                {
                    Console.WriteLine(ReloadTimeBelt);
                    Console.WriteLine(ChemDPSBelt);
                    Console.WriteLine(ChemDPSPerVolumeBelt);
                }
                else
                {
                    Console.WriteLine(ReloadTime);
                    Console.WriteLine(ChemDPS);
                    Console.WriteLine(ChemDPSPerVolume);
                }
            }
        }
    }
}
