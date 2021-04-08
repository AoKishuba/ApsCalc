using System;
using PenCalc;

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

        // Keep counts of body modules.
        public float[] BodyModuleCounts { get; set; } = { 0, 0, 0, 0, 0, 0 };

        public Module BaseModule { get; set; } // Optional; is 'null' if no base is chosen by user
        public Module HeadModule { get; set; } // There must always be a Head

        // Gunpowder and Railgun casing counts
        public float GPCasingCount { get; set; }
        public bool BoreEvacuator { get; set; }
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
        public float OverallChemModifier { get; set; }

        // Power
        public float GPRecoil { get; set; }
        public float MaxDraw { get; set; }
        public float RailDraw { get; set; }
        public float TotalRecoil { get; set; }
        public float Velocity { get; set; }

        // Reload
        public float ReloadTime { get; set; }
        public float ReloadTimeBelt { get; set; } // Beltfed Loader

        // Effective range
        public float EffectiveRange { get; set; }

        // Damage
        public float KineticDamage { get; set; }
        public float ArmorPierce { get; set; }
        public float EffectiveKineticDamage { get; set; }
        public float KineticDPS { get; set; }
        public float KineticDPSPerVolume { get; set; }
        public float KineticDPSBelt { get; set; }
        public float KineticDPSPerVolumeBelt { get; set; }
        public bool IsBelt { get; set; } = false; // Whether the shell should use its beltfed for comparison
        public float ChemDamage { get; set; } // Frag, FlaK, HE, and EMP all scale the same
        public float ChemDPS { get; set; } // Effective Warheads per Second
        public float ChemDPSPerVolume { get; set; }
        public float ChemDPSBelt { get; set; }
        public float ChemDPSPerVolumeBelt { get; set; }
        public float ShieldReduction { get; set; }

        public float ModuleCountTotal { get; set; }


        // Volume
        public int BarrelCount { get; set; }
        public float CooldownTime { get; set; }
        public float LoaderVolume { get; set; }
        public float RecoilVolume { get; set; }
        public float RecoilVolumeBelt { get; set; }
        public float ChargerVolume { get; set; }
        public float ChargerVolumeBelt { get; set; }
        public float CoolerVolume { get; set; }
        public float CoolerVolumeBelt { get; set; }
        public float VolumePerIntake { get; set; }
        public float VolumePerIntakeBelt { get; set; }


        /// <summary>
        /// Calculates the body, projectile, casing, and total lengths, as well as the length differential, which is used to penalize short shells
        /// </summary>
        public void CalculateLengths()
        {
            BodyLength = 0;
            if (BaseModule != null)
            {
                BodyLength += Math.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float ModuleLength = Math.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                BodyLength += ModuleLength * modCount;
                modIndex++;
            }

            CasingLength = (GPCasingCount + RGCasingCount) * Gauge;

            float HeadLength = Math.Min(Gauge, HeadModule.MaxLength);
            ProjectileLength = BodyLength + HeadLength;

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
        public void CalculateRecoil()
        {
            GPRecoil = GaugeCoefficient * GPCasingCount * 2500f;
            TotalRecoil = GPRecoil + RailDraw;
        }

        /// <summary>
        /// Calculates max rail draw of the shell
        /// </summary>
        public void CalculateMaxDraw()
        {
            MaxDraw = 12500f * GaugeCoefficient * (EffectiveProjectileModuleCount + (0.5f * RGCasingCount));
        }


        /// <summary>
        /// Calculates velocity modifier
        /// </summary>
        public void CalculateVelocityModifier()
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
            if (BaseModule?.Name == "Base bleeder")
            {
                OverallVelocityModifier += 0.15f;
            }
        }


        /// <summary>
        /// Calculates kinetic damage modifier
        /// </summary>
        public void CalculateKDModifier()
        {
            // Calculate weighted KineticDamage modifier of body
            float weightedKineticDamageMod = 0f;
            if (BaseModule != null)
            {
                weightedKineticDamageMod += BaseModule.KineticDamageMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
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
        }


        /// <summary>
        /// Calculates AP modifier
        /// </summary>
        public void CalculateAPModifier()
        {
            // Calculate weighted AP modifier of body
            float weightedArmorPierceMod = 0f;
            if (BaseModule != null)
            {
                weightedArmorPierceMod += BaseModule.ArmorPierceMod * Math.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
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
        }


        /// <summary>
        /// Calculates chemical payload modifier
        /// </summary>
        public void CalculateChemModifier()
        {
            OverallChemModifier = 1f;
            if (BaseModule != null)
            {
                OverallChemModifier = Math.Min(OverallChemModifier, BaseModule.ChemMod);
            }


            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (modCount > 0)
                {
                    OverallChemModifier = Math.Min(OverallChemModifier, Module.AllModules[modIndex].ChemMod);
                }
                modIndex++;
            }

            if (HeadModule == Module.Disruptor) // Disruptor 50% penalty stacks
            {
                OverallChemModifier *= 0.5f;
            }
            else
            {
                OverallChemModifier = Math.Min(OverallChemModifier, HeadModule.ChemMod);
            }
        }


        /// <summary>
        /// Calculates shell velocity
        /// </summary>
        public void CalculateVelocity()
        {
            Velocity = (float)Math.Sqrt((TotalRecoil * 85f * Gauge) / (GaugeCoefficient * ProjectileLength)) * OverallVelocityModifier;
        }


        /// <summary>
        /// Calculates minimum rail draw needed to achieve the given velocity and effective range
        /// </summary>
        public float CalculateMinimumDrawForVelocityandRange(float minVelocityInput, float minRangeInput, float maxDraw)
        {
            // Calculate effective time
            float gravityCompensatorCount = 0;
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (Module.AllModules[modIndex].Name == "Grav. Compensator")
                {
                    gravityCompensatorCount = BodyModuleCounts[modIndex];
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            float effectiveTime = 10f * OverallVelocityModifier * (ProjectileLength / 1000f) * (1f + gravityCompensatorCount);

            // Determine whether range or velocity is the limiting factor
            float minVelocity = Math.Max(minVelocityInput, minRangeInput / effectiveTime);

            // Calculate draw required for either range or velocity
            float minDrawVelocity = (float)(Math.Pow(minVelocity / OverallVelocityModifier, 2)
                * (GaugeCoefficient * ProjectileLength)
                / (Gauge * 85f)
                - GPRecoil);

            float minDraw = Math.Max(0, minDrawVelocity);

            return minDraw;
        }


        /// <summary>
        /// Calculates the effective range of the shell
        /// </summary>
        public void CalculateEffectiveRange()
        {
            float gravityCompensatorCount = 0;
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (Module.AllModules[modIndex].Name == "Grav. Compensator")
                {
                    gravityCompensatorCount = BodyModuleCounts[modIndex];
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            float effectiveTime = 10f * OverallVelocityModifier * (ProjectileLength / 1000f) * (1f + gravityCompensatorCount);
            EffectiveRange = Velocity * effectiveTime;
        }


        /// <summary>
        /// Calculate the volume of the intake and loader
        /// </summary>
        public void CalculateLoaderVolume()
        {
            LoaderVolume = 0;

            if (TotalLength <= 1000f)
            {
                LoaderVolume = 1f;
            }
            else if (TotalLength <= 2000f)
            {
                LoaderVolume = 2f;
            }
            else if (TotalLength <= 4000f)
            {
                LoaderVolume = 4f;
            }
            else if (TotalLength <= 6000f)
            {
                LoaderVolume = 6f;
            }
            else if (TotalLength <= 8000f)
            {
                LoaderVolume = 8f;
            }

            LoaderVolume += 1f; // Always have an intake
        }


        /// <summary>
        /// Calculates volume per intake of recoil absorbers
        /// </summary>
        public void CalculateRecoilVolume()
        { 
            RecoilVolume = TotalRecoil / (ReloadTime * 120f); // Absorbers absorb 120 per second per metre

            if (TotalLength <= 1000f)
            {
                RecoilVolumeBelt = TotalRecoil / (ReloadTimeBelt * 120f);
            }
            else
            {
                RecoilVolumeBelt = 0;
            }
        }


        /// <summary>
        /// Calculates barrel cooldown time
        /// </summary>
        public void CalculateCooldownTime()
        {
            CooldownTime =
                (float)(3.75f
                * GaugeCoefficient
                / Math.Pow(Gauge * Gauge * Gauge / 125000000, 0.15)
                * 17.5f
                * Math.Pow(GPCasingCount, 0.35)
                / 2);
            CooldownTime = Math.Max(CooldownTime, 0);
        }



        /// <summary>
        /// Calculates marginal volume of coolers to sustain fire from one additional intake.  Ignores cooling from firing piece
        /// </summary>
        public void CalculateCoolerVolume()
        {
            float multiBarrelPenalty;
            if (BarrelCount > 1f)
            {
                multiBarrelPenalty = 1f + (BarrelCount - 1f) * 0.2f;
            }
            else
            {
                multiBarrelPenalty = 1f;
            }

            float boreEvacuatorBonus;
            if (BoreEvacuator)
            {
                boreEvacuatorBonus =
                    (float)(0.15f
                    / (0.35355f / Math.Sqrt(Gauge / 1000f))
                    * multiBarrelPenalty
                    / BarrelCount);
            }
            else
            {
                boreEvacuatorBonus = 0;
            }

            float coolerVolume;
            float coolerVolumeBelt;
            if (GPCasingCount > 0)
            {
                coolerVolume =
                    (float)((CooldownTime * multiBarrelPenalty / ReloadTime - boreEvacuatorBonus)
                    / (1f + BarrelCount * 0.05f)
                    / multiBarrelPenalty
                    * Math.Sqrt(Gauge/1000f)
                    / 0.176775f);

                if (TotalLength <= 1000f)
                {
                    coolerVolumeBelt =
                    (float)((CooldownTime * multiBarrelPenalty / ReloadTimeBelt - boreEvacuatorBonus)
                    / (1f + BarrelCount * 0.05f)
                    / multiBarrelPenalty
                    * Math.Sqrt(Gauge / 1000f)
                    / 0.176775f);
                }
                else
                {
                    coolerVolumeBelt = 0;                
                }
            }
            else
            {
                coolerVolume = 0;
                coolerVolumeBelt = 0;
            }

            CoolerVolume = Math.Max(coolerVolume, 0);
            CoolerVolumeBelt = Math.Max(coolerVolumeBelt, 0);
        }


        /// <summary>
        /// Calculates the volume of railgun chargers needed to sustain fire from one intake
        /// </summary>
        public void CalculateChargerVolume()
        {
            if (RailDraw > 0)
            {
                ChargerVolume = RailDraw / (ReloadTime * 200f); // Chargers are 200 Energy per second

                if (TotalLength <= 1000f)
                {
                    ChargerVolumeBelt = RailDraw / (ReloadTimeBelt * 200f);
                }
                else
                {
                    ChargerVolumeBelt = 0;
                }
            }
            else
            {
                ChargerVolume = 0;
            }
        }


        /// <summary>
        /// Calculates the volume used by the shell, including intake, loader, cooling, recoil absorbers, and rail chargers
        /// </summary>
        public void CalculateVolumePerIntake()
        {
            VolumePerIntake = LoaderVolume + RecoilVolume + CoolerVolume + ChargerVolume;

            if (TotalLength <= 1000f)
            {
                VolumePerIntakeBelt = LoaderVolume + RecoilVolumeBelt + CoolerVolumeBelt + ChargerVolumeBelt;
            }
        }


        /// <summary>
        /// Calculates raw kinetic damage
        /// </summary>
        public void CalculateKineticDamage()
        {
            KineticDamage = (float)Math.Pow(500 / Math.Max(Gauge, 100f), 0.15)
                * GaugeCoefficient
                * EffectiveProjectileModuleCount
                * Velocity
                * OverallKineticDamageModifier
                * 3.5f;
        }


        /// <summary>
        /// Calculates the armor pierce rating
        /// </summary>
        public void CalculateAP()
        {
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
                ReloadTimeBelt = 0;
            }
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

            ChemDamage = GaugeCoefficient * chemBodies * OverallChemModifier;

            if (HeadModule == Module.Disruptor)
            {
                CalculateShieldReduction();
            }
            else
            {
                ShieldReduction = 0;
            }
        }


        /// <summary>
        /// Calculates planar shield reduction for shells with disruptor head
        /// </summary>
        public void CalculateShieldReduction()
        {
            float reductionDecimal = ChemDamage / 1.3333334f;
            ShieldReduction = Math.Min(reductionDecimal, 1f);
        }


        /// <summary>
        /// Calculate the chemical damage of a shell if it is capable of penetrating the given armor scheme
        /// </summary>
        public void CalculatePendepthDps(Scheme targetArmorScheme)
        {
            CalculateRecoil();
            CalculateVelocity();
            CalculateKineticDamage();
            CalculateAP();

            if (KineticDamage >= targetArmorScheme.GetRequiredKD(ArmorPierce))
            {
                CalculateRecoilVolume();
                CalculateChargerVolume();
                CalculateVolumePerIntake();
                CalculateChemDps();
            }
            else
            {
                ChemDPS = 0;
                ChemDPSBelt = 0;
                ChemDPSPerVolume = 0;
                ChemDPSPerVolumeBelt = 0;
            }
        }


        /// <summary>
        /// Calculates applied kinetic damage for a given target armor class
        /// </summary>
        /// <param name="targetAC"></param>
        public void CalculateKineticDps(float targetAC)
        {
            KineticDPS = 0;
            KineticDPSPerVolume = 0;
            KineticDPSBelt = 0;
            KineticDPSPerVolumeBelt = 0;

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
        public void CalculateChemDps()
        {
            ChemDPS = 0;
            ChemDPSPerVolume = 0;
            ChemDPSBelt = 0;
            ChemDPSPerVolumeBelt = 0;

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
            ModuleCountTotal = 1;
            if (BaseModule != null)
            {
                ModuleCountTotal += 1;
            }

            foreach (float modCount in BodyModuleCounts)
            {
                ModuleCountTotal += modCount;
            }

            ModuleCountTotal += (float)(Math.Ceiling(GPCasingCount) + RGCasingCount);
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
                Console.WriteLine("Total modules: " + ModuleCountTotal);


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
                Console.WriteLine("Effective range: " + EffectiveRange);
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
                Console.WriteLine(ModuleCountTotal);
                Console.WriteLine(GPCasingCount);
                Console.WriteLine(RGCasingCount);

                foreach (float modCount in BodyModuleCounts)
                {
                    Console.WriteLine(modCount);
                }

                Console.WriteLine(HeadModule.Name);

                Console.WriteLine(RailDraw);
                Console.WriteLine(TotalRecoil);
                Console.WriteLine(Velocity);
                Console.WriteLine(EffectiveRange);
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
                Console.WriteLine("Total modules: " + ModuleCountTotal);


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
                Console.WriteLine("Effective range (m): " + EffectiveRange);
                Console.WriteLine("Chemical payload strength: " + ChemDamage);
                if (HeadModule == Module.Disruptor)
                {
                    Console.WriteLine("Shield reduction (decimal): " + ShieldReduction);
                }


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
                Console.WriteLine(ModuleCountTotal);
                Console.WriteLine(GPCasingCount);
                Console.WriteLine(RGCasingCount);
                foreach (float modCount in BodyModuleCounts)
                {
                    Console.WriteLine(modCount);
                }

                Console.WriteLine(HeadModule.Name);
                Console.WriteLine(RailDraw);
                Console.WriteLine(TotalRecoil);
                Console.WriteLine(Velocity);
                Console.WriteLine(EffectiveRange);
                Console.WriteLine(ChemDamage);
                if (HeadModule == Module.Disruptor)
                {
                    Console.WriteLine(ShieldReduction);
                }

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