using System;
using System.Collections.Generic;
using PenCalc;
using System.IO;

namespace ApsCalc
{
    public class Shell
    {
        // Conversion factor from chemical multiplier to HE damage
        readonly float chemToHE = MathF.Pow(24f / 30f, 0.9f) * 3000;
        readonly float chemToEmp = 1500;

        public Shell() { BaseModule = default; }

        /// <summary>
        /// Sets gauge and simultaneously calculates Gauge Coefficient, which is used in several formulae as a way to scale with gauge.
        /// </summary>
        private float _gauge;
        public float Gauge
        {
            get { return _gauge; }
            set
            {
                _gauge = value;
                GaugeCoefficient = MathF.Pow(Gauge * Gauge * Gauge / 125000000f, 0.6f);
            }
        }
        public float GaugeCoefficient { get; set; } // Expensive to calculate and used in several formulae

        // Keep counts of body modules.
        public float[] BodyModuleCounts { get; set; } = { 0, 0, 0, 0, 0, 0 };
        public float ModuleCountTotal { get; set; }


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
        public float UptimeBelt { get; set; }
        public int BarrelCount { get; set; }
        public float CooldownTime { get; set; }

        // Effective range
        public float EffectiveRange { get; set; }


        // Damage
        public float KineticDamage { get; set; }
        public float ArmorPierce { get; set; }
        public float EffectiveKineticDamage { get; set; }
        public float KineticDps { get; set; }
        public float KineticDpsBelt { get; set; }
        public float KineticDpsBeltSustained { get; set; }
        public bool IsBelt { get; set; } = false; // Whether shell should use its beltfed stats for comparison
        public float ChemDamage { get; set; } // Frag, FlaK, HE, and EMP all scale same
        public float ChemDps { get; set; } // Effective Warheads per Second
        public float ChemDpsBelt { get; set; }
        public float ChemDpsBeltSustained { get; set; }
        public float ShieldReduction { get; set; }
        public float ShieldRps { get; set; }
        public float ShieldRpsBelt { get; set; }
        public float ShieldRpsBeltSustained { get; set; }
        public Dictionary<float, float> DpsPerVolumeDict = new()
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 }
        }; // 0 for kinetic, 1 for chem, 2 for pendepth (chem), 3 for disruptor

        public Dictionary<float, float> DpsPerCostDict = new()
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 }
        };


        // Volume
        public float LoaderVolume { get; set; }
        public float LoaderVolumeBelt { get; } = 4f; // loader, clip, 2 intakes
        public float RecoilVolume { get; set; }
        public float RecoilVolumeBelt { get; set; }
        public float ChargerVolume { get; set; }
        public float ChargerVolumeBelt { get; set; }
        public float CoolerVolume { get; set; }
        public float CoolerVolumeBelt { get; set; }
        public float VolumePerIntake { get; set; }
        public float VolumePerIntakeBelt { get; set; }


        // Cost
        public float LoaderCost { get; set; }
        public float LoaderCostBelt { get; } = 860f; // loader, clip, 2 intakes
        public float RecoilCost { get; set; }
        public float RecoilCostBelt { get; set; }
        public float ChargerCost { get; set; }
        public float ChargerCostBelt { get; set; }
        public float CoolerCost { get; set; }
        public float CoolerCostBelt { get; set; }
        public float CostPerIntake { get; set; }
        public float CostPerIntakeBelt { get; set; }


        // Damage per volume
        public float KineticDpsPerVolume { get; set; }
        public float KineticDpsPerVolumeBelt { get; set; }
        public float KineticDpsPerVolumeBeltSustained { get; set; }
        public float ChemDpsPerVolume { get; set; }
        public float ChemDpsPerVolumeBelt { get; set; }
        public float ChemDpsPerVolumeBeltSustained { get; set; }
        public float ShieldRpsPerVolume { get; set; }
        public float ShieldRpsPerVolumeBelt { get; set; }
        public float ShieldRpsPerVolumeBeltSustained { get; set; }


        // Damage per cost
        public float KineticDpsPerCost { get; set; }
        public float KineticDpsPerCostBelt { get; set; }
        public float KineticDpsPerCostBeltSustained { get; set; }
        public float ChemDpsPerCost { get; set; }
        public float ChemDpsPerCostBelt { get; set; }
        public float ChemDpsPerCostBeltSustained { get; set; }
        public float ShieldRpsPerCost { get; set; }
        public float ShieldRpsPerCostBelt { get; set; }
        public float ShieldRpsPerCostBeltSustained { get; set; }


        /// <summary>
        /// Calculates body, projectile, casing, and total lengths, as well as length differential, which is used to penalize short shells
        /// </summary>
        public void CalculateLengths()
        {
            BodyLength = 0;
            if (BaseModule != null)
            {
                BodyLength += MathF.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float ModuleLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                BodyLength += ModuleLength * modCount;
                modIndex++;
            }

            CasingLength = (GPCasingCount + RGCasingCount) * Gauge;

            float HeadLength = MathF.Min(Gauge, HeadModule.MaxLength);
            ProjectileLength = BodyLength + HeadLength;

            TotalLength = CasingLength + ProjectileLength;

            ShortLength = 2 * Gauge;
            LengthDifferential = MathF.Max(ShortLength - BodyLength, 0);
            EffectiveBodyLength = MathF.Max(2 * Gauge, BodyLength);

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
        /// Calculates max rail draw of shell
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
                weightedVelocityMod += BaseModule.VelocityMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            // Add body module weighted modifiers
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
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
                weightedKineticDamageMod += BaseModule.KineticDamageMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
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
                weightedArmorPierceMod += BaseModule.ArmorPierceMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
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
                OverallChemModifier = MathF.Min(OverallChemModifier, BaseModule.ChemMod);
            }


            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (modCount > 0)
                {
                    OverallChemModifier = MathF.Min(OverallChemModifier, Module.AllModules[modIndex].ChemMod);
                }
                modIndex++;
            }

            if (HeadModule == Module.Disruptor) // Disruptor 50% penalty stacks
            {
                OverallChemModifier *= 0.5f;
            }
            else
            {
                OverallChemModifier = MathF.Min(OverallChemModifier, HeadModule.ChemMod);
            }
        }


        /// <summary>
        /// Calculates shell velocity
        /// </summary>
        public void CalculateVelocity()
        {
            Velocity = MathF.Sqrt((TotalRecoil * 85f * Gauge) / (GaugeCoefficient * ProjectileLength)) * OverallVelocityModifier;
        }


        /// <summary>
        /// Calculates minimum rail draw needed to achieve given velocity and effective range
        /// </summary>
        public float CalculateMinimumDrawForVelocityandRange(float minVelocityInput, float minRangeInput)
        {
            CalculateRecoil();
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

            // Determine whether range or velocity is limiting factor
            float minVelocity = MathF.Max(minVelocityInput, minRangeInput / effectiveTime);

            // Calculate draw required for either range or velocity
            float minDrawVelocity = (MathF.Pow(minVelocity / OverallVelocityModifier, 2)
                * (GaugeCoefficient * ProjectileLength)
                / (Gauge * 85f)
                - GPRecoil);

            float minDraw = MathF.Max(0, minDrawVelocity);

            return minDraw;
        }


        /// <summary>
        /// Calculates effective range of shell
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
        /// Calculate volume of intake and loader
        /// </summary>
        public void CalculateLoaderVolumeAndCost()
        {
            LoaderVolume = 0;
            LoaderCost = 0;

            if (TotalLength <= 1000f)
            {
                LoaderVolume = 1f;
                LoaderCost = 240f;
            }
            else if (TotalLength <= 2000f)
            {
                LoaderVolume = 2f;
                LoaderCost = 300f;
            }
            else if (TotalLength <= 3000f)
            {
                LoaderVolume = 3f;
                LoaderCost = 330f;
            }
            else if (TotalLength <= 4000f)
            {
                LoaderVolume = 4f;
                LoaderCost = 360f;
            }
            else if (TotalLength <= 6000f)
            {
                LoaderVolume = 6f;
                LoaderCost = 420f;
            }
            else if (TotalLength <= 8000f)
            {
                LoaderVolume = 8f;
                LoaderCost = 480f;
            }
            LoaderVolume += 1f; // Always have an intake
            LoaderCost += 50f;
        }


        /// <summary>
        /// Calculates volume per intake of recoil absorbers
        /// </summary>
        public void CalculateRecoilVolumeAndCost()
        {
            RecoilVolume = TotalRecoil / (ReloadTime * 120f); // Absorbers absorb 120 per second per metre
            RecoilCost = RecoilVolume * 80f; // Absorbers cost 80 per metre

            if (TotalLength <= 1000f)
            {
                RecoilVolumeBelt = TotalRecoil / (ReloadTimeBelt * 120f);
                RecoilCostBelt = RecoilVolumeBelt * 80f;
            }
            else
            {
                RecoilVolumeBelt = 0f;
                RecoilCostBelt = 0f;
            }
        }


        /// <summary>
        /// Calculates barrel cooldown time
        /// </summary>
        public void CalculateCooldownTime()
        {
            CooldownTime =
                (3.75f
                * GaugeCoefficient
                / MathF.Pow(Gauge * Gauge * Gauge / 125000000f, 0.15f)
                * 17.5f
                * MathF.Pow(GPCasingCount, 0.35f)
                / 2);
            CooldownTime = MathF.Max(CooldownTime, 0);
        }



        /// <summary>
        /// Calculates marginal volume of coolers to sustain fire from one additional intake.  Ignores cooling from firing piece
        /// </summary>
        public void CalculateCoolerVolumeAndCost()
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
                    (0.15f
                    / (0.35355f / MathF.Sqrt(Gauge / 1000f))
                    * multiBarrelPenalty
                    / BarrelCount);
            }
            else
            {
                boreEvacuatorBonus = 0;
            }

            float coolerVolume;
            float coolerCost;

            float coolerVolumeBelt;
            float coolerCostBelt;
            if (GPCasingCount > 0)
            {
                coolerVolume =
                    ((CooldownTime * multiBarrelPenalty / ReloadTime - boreEvacuatorBonus)
                    / (1f + BarrelCount * 0.05f)
                    / multiBarrelPenalty
                    * MathF.Sqrt(Gauge / 1000f)
                    / 0.176775f);

                coolerCost = coolerVolume * 50f;

                if (TotalLength <= 1000f)
                {
                    coolerVolumeBelt =
                    ((CooldownTime * multiBarrelPenalty / ReloadTimeBelt - boreEvacuatorBonus)
                    / (1f + BarrelCount * 0.05f)
                    / multiBarrelPenalty
                    * MathF.Sqrt(Gauge / 1000f)
                    / 0.176775f);

                    coolerCostBelt = coolerVolumeBelt * 50f;
                }
                else
                {
                    coolerVolumeBelt = 0;
                    coolerCostBelt = 0;
                }
            }
            else
            {
                coolerVolume = 0;
                coolerCost = 0;
                coolerVolumeBelt = 0;
                coolerCostBelt = 0;
            }

            CoolerVolume = MathF.Max(coolerVolume, 0);
            CoolerCost = MathF.Max(coolerCost, 0);

            CoolerVolumeBelt = MathF.Max(coolerVolumeBelt, 0);
            CoolerCostBelt = MathF.Max(coolerCostBelt, 0);
        }


        /// <summary>
        /// Calculates marginal volume per intake of rail chargers
        /// </summary>
        public void CalculateChargerVolumeAndCost()
        {
            if (RailDraw > 0)
            {
                ChargerVolume = RailDraw / (ReloadTime * 200f); // Chargers are 200 Energy per second
                ChargerCost = ChargerVolume * 400f; // Chargers cost 400 per metre

                if (TotalLength <= 1000f)
                {
                    ChargerVolumeBelt = RailDraw / (ReloadTimeBelt * 200f);
                    ChargerCostBelt = ChargerVolumeBelt * 400f;
                }
                else
                {
                    ChargerVolumeBelt = 0;
                    ChargerCostBelt = 0;
                }
            }
            else
            {
                ChargerVolume = 0;
                ChargerCost = 0;
                ChargerVolumeBelt = 0;
                ChargerCostBelt = 0;
            }
        }


        /// <summary>
        /// Calculates volume used by shell, including intake, loader, cooling, recoil absorbers, and rail chargers
        /// </summary>
        public void CalculateVolumeAndCostPerIntake()
        {

            VolumePerIntake = LoaderVolume + RecoilVolume + CoolerVolume + ChargerVolume;
            CostPerIntake = LoaderCost + RecoilCost + CoolerCost + ChargerCost;

            if (TotalLength <= 1000f)
            {
                VolumePerIntakeBelt = LoaderVolumeBelt + RecoilVolumeBelt + CoolerVolumeBelt + ChargerVolumeBelt;
                CostPerIntakeBelt = LoaderCostBelt + RecoilCostBelt + CoolerCostBelt + ChargerCostBelt;
            }
        }


        /// <summary>
        /// Calculates raw kinetic damage
        /// </summary>
        public void CalculateKineticDamage()
        {
            CalculateVelocity();

            if (HeadModule == Module.HollowPoint)
            {
                KineticDamage = GaugeCoefficient
                    * EffectiveProjectileModuleCount
                    * Velocity
                    * OverallKineticDamageModifier
                    * 3.5f;
            }
            else
            {
                KineticDamage = MathF.Pow(500 / MathF.Max(Gauge, 100f), 0.15f)
                    * GaugeCoefficient
                    * EffectiveProjectileModuleCount
                    * Velocity
                    * OverallKineticDamageModifier
                    * 3.5f;
            }
        }


        /// <summary>
        /// Calculates armor pierce rating
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
            ReloadTime = (MathF.Pow(Gauge * Gauge * Gauge / 125000000f, 0.45f)
                * (2f + EffectiveProjectileModuleCount + 0.25f * (RGCasingCount + GPCasingCount))
                * 17.5f);
        }


        /// <summary>
        /// Calculates beltfed loader reload time and uptime
        /// </summary>
        public void CalculateReloadTimeBelt()
        {
            if (TotalLength <= 1000f)
            {
                ReloadTimeBelt = (ReloadTime * 0.75f * MathF.Pow(Gauge / 1000f, 0.45f));
                float gaugeModifier;
                if (Gauge <= 250f)
                {
                    gaugeModifier = 2f;
                }
                else
                {
                    gaugeModifier = 1f;
                }
                float shellCapacity = (1f * MathF.Min(64f, MathF.Floor(1000f / Gauge) * gaugeModifier) + 1f);
                float firingCycleLength = (shellCapacity - 1f) * ReloadTimeBelt;
                float loadingCycleLength = (shellCapacity - 2f) * ReloadTimeBelt / 2f; // 2 intakes
                UptimeBelt = firingCycleLength / (firingCycleLength + loadingCycleLength);
            }
            else
            {
                ReloadTimeBelt = 0;
            }
        }


        /// <summary>
        /// Calculates chemical payload damage in "Equivalent warheads", which serves as a multiplier for various kinds of chemical damage
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
                if (HeadModule.Name == "HE, Frag, FlaK, or EMP head")
                {
                    chemBodies++;
                }
                else if (HeadModule.Name == "Squash or Shaped charge head")
                {
                    chemBodies += 0.2f;
                }
            }

            ChemDamage = GaugeCoefficient * chemBodies * OverallChemModifier;
        }


        /// <summary>
        /// Calculates planar shield reduction for shells with disruptor head
        /// </summary>
        public void CalculateShieldReduction()
        {
            CalculateChemDamage();
            if (HeadModule == Module.Disruptor)
            {
                float reductionDecimal = ChemDamage * 0.75f;
                ShieldReduction = MathF.Min(reductionDecimal, 1f);
            }
            else
            {
                ShieldReduction = 0;
            }
        }


        /// <summary>
        /// Calculates shield disruption, in % reduction per second per volume
        /// </summary>
        public void CalculateShieldRps()
        {
            ShieldRps = ShieldReduction / ReloadTime;
            ShieldRpsPerVolume = ShieldRps / VolumePerIntake;
            DpsPerVolumeDict[3] = ShieldRpsPerVolume;

            ShieldRpsPerCost = ShieldRps / CostPerIntake;
            DpsPerCostDict[3] = ShieldRpsPerCost;
        }


        public void CalculateShieldRpsBelt()
        {
            if (TotalLength <= 1000f)
            {
                ShieldRpsBelt = ShieldReduction / ReloadTimeBelt;
                ShieldRpsBeltSustained = ShieldRpsBelt * UptimeBelt;

                ShieldRpsPerVolumeBelt = ShieldRpsBelt / VolumePerIntakeBelt;
                ShieldRpsPerVolumeBeltSustained = ShieldRpsBeltSustained / VolumePerIntakeBelt;
                DpsPerVolumeDict[3] = ShieldRpsPerVolumeBeltSustained;

                ShieldRpsPerCostBelt = ShieldRpsBelt / CostPerIntakeBelt;
                ShieldRpsPerCostBeltSustained = ShieldRpsBeltSustained / CostPerIntakeBelt;
                DpsPerCostDict[3] = ShieldRpsPerCostBeltSustained;
            }
            else
            {
                ShieldRpsBelt = 0;
                ShieldRpsBeltSustained = 0;

                ShieldRpsPerVolumeBelt = 0;
                ShieldRpsPerVolumeBeltSustained = 0;
                DpsPerVolumeDict[3] = 0;

                ShieldRpsPerCostBelt = 0;
                ShieldRpsPerCostBeltSustained = 0;
                DpsPerCostDict[3] = 0;
            }
        }


        /// <summary>
        /// Calculate chemical damage of a shell if it is capable of penetrating given armor scheme
        /// </summary>
        public void CalculatePendepthDps(Scheme targetArmorScheme)
        {
            CalculateKineticDamage();
            CalculateAP();

            if (KineticDamage >= targetArmorScheme.GetRequiredKD(ArmorPierce))
            {
                CalculateChemDps();
                DpsPerVolumeDict[2] = ChemDpsPerVolume;
                DpsPerCostDict[2] = ChemDpsPerCost;
            }
            else
            {
                ChemDps = 0;

                ChemDpsPerVolume = 0;
                DpsPerVolumeDict[2] = 0;

                ChemDpsPerCost = 0;
                DpsPerCostDict[2] = 0;
            }
        }


        public void CalculatePendepthDpsBelt(Scheme targetArmorScheme)
        {
            CalculateKineticDamage();
            CalculateAP();

            if (KineticDamage >= targetArmorScheme.GetRequiredKD(ArmorPierce))
            {
                CalculateChemDpsBelt();
                DpsPerVolumeDict[2] = ChemDpsPerVolumeBeltSustained;
                DpsPerCostDict[2] = ChemDpsPerCostBeltSustained;
            }
            else
            {
                ChemDpsBelt = 0;
                ChemDpsBeltSustained = 0;

                ChemDpsPerVolumeBelt = 0;
                ChemDpsPerVolumeBeltSustained = 0;
                DpsPerVolumeDict[2] = 0;

                ChemDpsPerCostBelt = 0;
                ChemDpsPerCostBeltSustained = 0;
                DpsPerCostDict[2] = 0;
            }
        }


        /// <summary>
        /// Calculates applied kinetic damage for a given target armor class
        /// </summary>
        public void CalculateKineticDps(float targetAC)
        {
            CalculateKineticDamage();
            CalculateAP();

            EffectiveKineticDamage = KineticDamage * MathF.Min(1, ArmorPierce / targetAC);
            KineticDps = EffectiveKineticDamage / ReloadTime;

            KineticDpsPerVolume = KineticDps / VolumePerIntake;
            DpsPerVolumeDict[0] = KineticDpsPerVolume;

            KineticDpsPerCost = KineticDps / CostPerIntake;
            DpsPerCostDict[0] = KineticDpsPerCost;
        }


        public void CalculateKineticDpsBelt(float targetAC)
        {
            if (TotalLength <= 1000f)
            {
                CalculateKineticDamage();
                CalculateAP();

                EffectiveKineticDamage = KineticDamage * MathF.Min(1, ArmorPierce / targetAC);

                KineticDpsBelt = EffectiveKineticDamage / ReloadTimeBelt;
                KineticDpsBeltSustained = KineticDpsBelt * UptimeBelt;

                KineticDpsPerVolumeBelt = KineticDpsBelt / VolumePerIntakeBelt;
                KineticDpsPerVolumeBeltSustained = KineticDpsBeltSustained / VolumePerIntakeBelt;
                DpsPerVolumeDict[0] = KineticDpsPerVolumeBeltSustained;

                KineticDpsPerCostBelt = KineticDpsBelt / CostPerIntakeBelt;
                KineticDpsPerCostBeltSustained = KineticDpsBeltSustained / CostPerIntakeBelt;
                DpsPerCostDict[0] = KineticDpsPerCostBeltSustained;
            }
            else
            {
                KineticDpsBelt = 0;
                KineticDpsBeltSustained = 0;

                KineticDpsPerVolumeBelt = 0;
                KineticDpsPerVolumeBeltSustained = 0;
                DpsPerVolumeDict[0] = 0;

                KineticDpsPerCostBelt = 0;
                KineticDpsPerCostBeltSustained = 0;
                DpsPerCostDict[0] = 0;
            }
        }


        /// <summary>
        /// Calculates relative chemical payload damage per second, in Equivalent Warheads per second
        /// </summary>
        public void CalculateChemDps()
        {
            ChemDps = ChemDamage / ReloadTime;
            ChemDpsPerVolume = ChemDps / VolumePerIntake;
            DpsPerVolumeDict[1] = ChemDpsPerVolume;

            ChemDpsPerCost = ChemDps / CostPerIntake;
            DpsPerCostDict[1] = ChemDpsPerCost;
        }


        /// <summary>
        /// Calculates relative chemical payload damage per second, in Equivalent Warheads per second
        /// </summary>
        public void CalculateChemDpsBelt()
        {
            if (TotalLength <= 1000f)
            {
                ChemDpsBelt = ChemDamage / ReloadTimeBelt;
                ChemDpsBeltSustained = ChemDpsBelt * UptimeBelt;

                ChemDpsPerVolumeBelt = ChemDpsBelt / VolumePerIntakeBelt;
                ChemDpsPerVolumeBeltSustained = ChemDpsBeltSustained / VolumePerIntakeBelt;
                DpsPerVolumeDict[1] = ChemDpsPerVolumeBeltSustained;

                ChemDpsPerCostBelt = ChemDpsBelt / CostPerIntakeBelt;
                ChemDpsPerCostBeltSustained = ChemDpsBeltSustained / CostPerIntakeBelt;
                DpsPerCostDict[1] = ChemDpsPerCostBeltSustained;
            }
            else
            {
                ChemDpsBelt = 0;
                ChemDpsBeltSustained = 0;

                ChemDpsPerVolumeBelt = 0;
                ChemDpsPerVolumeBeltSustained = 0;
                DpsPerVolumeDict[1] = 0;

                ChemDpsPerCostBelt = 0;
                ChemDpsPerCostBeltSustained = 0;
                DpsPerCostDict[1] = 0;
            }
        }


        /// <summary>
        /// Calculates total number of modules in shell
        /// </summary>
        public void GetModuleCounts()
        {
            // ModuleCountTotal starts at 1 for head
            ModuleCountTotal = 1;
            if (BaseModule != null)
            {
                ModuleCountTotal += 1;
            }

            foreach (float modCount in BodyModuleCounts)
            {
                ModuleCountTotal += modCount;
            }

            ModuleCountTotal += (MathF.Ceiling(GPCasingCount) + RGCasingCount);
        }


        /// <summary>
        /// Gather info for top-performing shells and write to console
        /// </summary>
        /// <param name="labels">False if labels should be omitted from result printout.  Labels are hard to copy to spreadsheets</param>
        public void WriteShellInfoToConsoleKinetic(bool labels)
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
                    Console.WriteLine("Effective kinetic DPS (belt): " + KineticDpsBelt);

                    Console.WriteLine("Loader volume: " + LoaderVolumeBelt);
                    Console.WriteLine("Cooler volume: " + CoolerVolumeBelt);
                    Console.WriteLine("Charger volume: " + ChargerVolumeBelt);
                    Console.WriteLine("Recoil volume: " + RecoilVolumeBelt);
                    Console.WriteLine("Total volume: " + VolumePerIntakeBelt);
                    Console.WriteLine("Effective kinetic DPS per volume (belt): " + KineticDpsPerVolumeBelt);

                    Console.WriteLine("Loader cost: " + LoaderCostBelt);
                    Console.WriteLine("Cooler cost: " + CoolerCostBelt);
                    Console.WriteLine("Charger cost: " + ChargerCostBelt);
                    Console.WriteLine("Recoil cost: " + RecoilCostBelt);
                    Console.WriteLine("Total cost: " + CostPerIntakeBelt);
                    Console.WriteLine("Effective kinetic DPS per cost (belt): " + KineticDpsPerCostBelt);

                    Console.WriteLine("Uptime: " + UptimeBelt);
                    Console.WriteLine("Effective kinetic DPS (belt, sustained): " + KineticDpsBeltSustained);
                    Console.WriteLine("Effective kinetic DPS per volume (sustained): " + KineticDpsPerVolumeBeltSustained);
                    Console.WriteLine("Effective kinetic DPS per cost (sustained): " + KineticDpsPerCostBeltSustained);
                }
                else
                {
                    Console.WriteLine("Reload time: " + ReloadTime);
                    Console.WriteLine("Effective kinetic DPS: " + KineticDps);

                    Console.WriteLine("Loader volume: " + LoaderVolume);
                    Console.WriteLine("Cooler volume: " + CoolerVolume);
                    Console.WriteLine("Charger volume: " + ChargerVolume);
                    Console.WriteLine("Recoil volume: " + RecoilVolume);
                    Console.WriteLine("Total volume: " + VolumePerIntake);
                    Console.WriteLine("Effective kinetic DPS per volume: " + KineticDpsPerVolume);

                    Console.WriteLine("Loader cost: " + LoaderCost);
                    Console.WriteLine("Cooler cost: " + CoolerCost);
                    Console.WriteLine("Charger cost: " + ChargerCost);
                    Console.WriteLine("Recoil cost: " + RecoilCost);
                    Console.WriteLine("Total cost: " + CostPerIntake);
                    Console.WriteLine("Effective kinetic DPS per cost: " + KineticDpsPerCost);
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
                    Console.WriteLine(KineticDpsBelt);

                    Console.WriteLine(LoaderVolumeBelt);
                    Console.WriteLine(CoolerVolumeBelt);
                    Console.WriteLine(ChargerVolumeBelt);
                    Console.WriteLine(RecoilVolumeBelt);
                    Console.WriteLine(VolumePerIntakeBelt);
                    Console.WriteLine(KineticDpsPerVolumeBelt);

                    Console.WriteLine(LoaderCostBelt);
                    Console.WriteLine(CoolerCostBelt);
                    Console.WriteLine(ChargerCostBelt);
                    Console.WriteLine(RecoilCostBelt);
                    Console.WriteLine(CostPerIntakeBelt);
                    Console.WriteLine(KineticDpsPerCostBelt);

                    Console.WriteLine(UptimeBelt);
                    Console.WriteLine(KineticDpsBeltSustained);
                    Console.WriteLine(KineticDpsPerVolumeBeltSustained);
                    Console.WriteLine(KineticDpsPerCostBeltSustained);
                }
                else
                {
                    Console.WriteLine(ReloadTime);
                    Console.WriteLine(KineticDps);

                    Console.WriteLine(LoaderVolume);
                    Console.WriteLine(CoolerVolume);
                    Console.WriteLine(ChargerVolume);
                    Console.WriteLine(RecoilVolume);
                    Console.WriteLine(VolumePerIntake);
                    Console.WriteLine(KineticDpsPerVolume);

                    Console.WriteLine(LoaderCost);
                    Console.WriteLine(CoolerCost);
                    Console.WriteLine(ChargerCost);
                    Console.WriteLine(RecoilCost);
                    Console.WriteLine(CostPerIntake);
                    Console.WriteLine(KineticDpsPerCost);
                }
            }
        }


        /// <summary>
        /// Gather info for top-performing shells and write to console
        /// </summary>
        /// <param name="labels">False if labels should be omitted from result printout.  Labels are hard to copy to spreadsheets</param>
        public void WriteShellInfoToFileKinetic(bool labels, StreamWriter writer)
        {
            if (labels)
            {
                writer.WriteLine("Gauge (mm): " + Gauge);
                writer.WriteLine("Total length (mm): " + TotalLength);
                writer.WriteLine("Length without casings: " + ProjectileLength);
                writer.WriteLine("Total modules: " + ModuleCountTotal);


                if (RGCasingCount > 0)
                {
                    writer.WriteLine("RG Casing: " + RGCasingCount);
                }

                if (GPCasingCount > 0)
                {
                    writer.WriteLine("GP Casing: " + GPCasingCount);
                }

                int modIndex = 0;
                foreach (float modCount in BodyModuleCounts)
                {
                    if (modCount > 0)
                    {
                        writer.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                    }
                    modIndex++;
                }

                writer.WriteLine("Head: " + HeadModule.Name);
                writer.WriteLine("Rail draw: " + RailDraw);
                writer.WriteLine("Recoil: " + TotalRecoil);
                writer.WriteLine("Velocity (m/s): " + Velocity);
                writer.WriteLine("Effective range: " + EffectiveRange);
                writer.WriteLine("Raw kinetic damage: " + KineticDamage);
                writer.WriteLine("AP: " + ArmorPierce);
                writer.WriteLine("Effective kinetic damage: " + EffectiveKineticDamage);

                if (IsBelt)
                {
                    writer.WriteLine("Reload time (belt): " + ReloadTimeBelt);
                    writer.WriteLine("Effective kinetic DPS (belt): " + KineticDpsBelt);

                    writer.WriteLine("Loader volume: " + LoaderVolumeBelt);
                    writer.WriteLine("Cooler volume: " + CoolerVolumeBelt);
                    writer.WriteLine("Charger volume: " + ChargerVolumeBelt);
                    writer.WriteLine("Recoil volume: " + RecoilVolumeBelt);
                    writer.WriteLine("Total volume: " + VolumePerIntakeBelt);
                    writer.WriteLine("Effective kinetic DPS per volume (belt): " + KineticDpsPerVolumeBelt);

                    writer.WriteLine("Loader cost: " + LoaderCostBelt);
                    writer.WriteLine("Cooler cost: " + CoolerCostBelt);
                    writer.WriteLine("Charger cost: " + ChargerCostBelt);
                    writer.WriteLine("Recoil cost: " + RecoilCostBelt);
                    writer.WriteLine("Total cost: " + CostPerIntakeBelt);
                    writer.WriteLine("Effective kinetic DPS per cost (belt): " + KineticDpsPerCostBelt);

                    writer.WriteLine("Uptime: " + UptimeBelt);
                    writer.WriteLine("Effective kinetic DPS (belt, sustained): " + KineticDpsBeltSustained);
                    writer.WriteLine("Effective kinetic DPS per volume (sustained): " + KineticDpsPerVolumeBeltSustained);
                    writer.WriteLine("Effective kinetic DPS per cost (sustained): " + KineticDpsPerCostBeltSustained);
                }
                else
                {
                    writer.WriteLine("Reload time: " + ReloadTime);
                    writer.WriteLine("Effective kinetic DPS: " + KineticDps);

                    writer.WriteLine("Loader volume: " + LoaderVolume);
                    writer.WriteLine("Cooler volume: " + CoolerVolume);
                    writer.WriteLine("Charger volume: " + ChargerVolume);
                    writer.WriteLine("Recoil volume: " + RecoilVolume);
                    writer.WriteLine("Total volume: " + VolumePerIntake);
                    writer.WriteLine("Effective kinetic DPS per volume: " + KineticDpsPerVolume);

                    writer.WriteLine("Loader cost: " + LoaderCost);
                    writer.WriteLine("Cooler cost: " + CoolerCost);
                    writer.WriteLine("Charger cost: " + ChargerCost);
                    writer.WriteLine("Recoil cost: " + RecoilCost);
                    writer.WriteLine("Total cost: " + CostPerIntake);
                    writer.WriteLine("Effective kinetic DPS per cost: " + KineticDpsPerCost);
                }
            }


            else if (!labels)
            {
                writer.WriteLine(Gauge);
                writer.WriteLine(TotalLength);
                writer.WriteLine(ProjectileLength);
                writer.WriteLine(ModuleCountTotal);
                writer.WriteLine(GPCasingCount);
                writer.WriteLine(RGCasingCount);

                foreach (float modCount in BodyModuleCounts)
                {
                    writer.WriteLine(modCount);
                }

                writer.WriteLine(HeadModule.Name);

                writer.WriteLine(RailDraw);
                writer.WriteLine(TotalRecoil);
                writer.WriteLine(Velocity);
                writer.WriteLine(EffectiveRange);
                writer.WriteLine(KineticDamage);
                writer.WriteLine(ArmorPierce);
                writer.WriteLine(EffectiveKineticDamage);

                if (IsBelt)
                {
                    writer.WriteLine(ReloadTimeBelt);
                    writer.WriteLine(KineticDpsBelt);

                    writer.WriteLine(LoaderVolumeBelt);
                    writer.WriteLine(CoolerVolumeBelt);
                    writer.WriteLine(ChargerVolumeBelt);
                    writer.WriteLine(RecoilVolumeBelt);
                    writer.WriteLine(VolumePerIntakeBelt);
                    writer.WriteLine(KineticDpsPerVolumeBelt);

                    writer.WriteLine(LoaderCostBelt);
                    writer.WriteLine(CoolerCostBelt);
                    writer.WriteLine(ChargerCostBelt);
                    writer.WriteLine(RecoilCostBelt);
                    writer.WriteLine(CostPerIntakeBelt);
                    writer.WriteLine(KineticDpsPerCostBelt);

                    writer.WriteLine(UptimeBelt);
                    writer.WriteLine(KineticDpsBeltSustained);
                    writer.WriteLine(KineticDpsPerVolumeBeltSustained);
                    writer.WriteLine(KineticDpsPerCostBeltSustained);
                }
                else
                {
                    writer.WriteLine(ReloadTime);
                    writer.WriteLine(KineticDps);

                    writer.WriteLine(LoaderVolume);
                    writer.WriteLine(CoolerVolume);
                    writer.WriteLine(ChargerVolume);
                    writer.WriteLine(RecoilVolume);
                    writer.WriteLine(VolumePerIntake);
                    writer.WriteLine(KineticDpsPerVolume);

                    writer.WriteLine(LoaderCost);
                    writer.WriteLine(CoolerCost);
                    writer.WriteLine(ChargerCost);
                    writer.WriteLine(RecoilCost);
                    writer.WriteLine(CostPerIntake);
                    writer.WriteLine(KineticDpsPerCost);
                }
            }
        }

        public void WriteShellInfoToConsoleChem(bool labels)
        {
            // Determine if disruptor
            bool disruptor;
            if (HeadModule == Module.Disruptor)
            {
                disruptor = true;
            }
            else
            {
                disruptor = false;
            }
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
                if (disruptor)
                {
                    Console.WriteLine("EMP damage: " + ChemDamage * chemToEmp);
                    Console.WriteLine("Shield reduction (decimal): " + ShieldReduction);
                }
                else
                {
                    Console.WriteLine("HE damage: " + ChemDamage * chemToHE);
                }


                if (IsBelt)
                {
                    Console.WriteLine("Reload time (belt): " + ReloadTimeBelt);

                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS (belt): " + ChemDpsBelt * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS (belt): " + ChemDpsBelt * chemToHE);
                    }

                    Console.WriteLine("Loader volume: " + LoaderVolumeBelt);
                    Console.WriteLine("Cooler volume: " + CoolerVolumeBelt);
                    Console.WriteLine("Charger volume: " + ChargerVolumeBelt);
                    Console.WriteLine("Recoil volume: " + RecoilVolumeBelt);
                    Console.WriteLine("Total volume: " + VolumePerIntakeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS per volume (belt): " + ChemDpsPerVolumeBelt * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS per volume (belt): " + ChemDpsPerVolumeBelt * chemToHE);
                    }


                    Console.WriteLine("Loader cost: " + LoaderCostBelt);
                    Console.WriteLine("Cooler cost: " + CoolerCostBelt);
                    Console.WriteLine("Charger cost: " + ChargerCostBelt);
                    Console.WriteLine("Recoil cost: " + RecoilCostBelt);
                    Console.WriteLine("Total cost: " + CostPerIntakeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS per cost (belt): " + ChemDpsPerCostBelt * chemToEmp);
                        Console.WriteLine("Shield RPS (belt): " + ShieldRpsBelt);
                        Console.WriteLine("Shield RPS per volume (belt): " + ShieldRpsPerVolumeBelt);
                        Console.WriteLine("Shield RPS per cost (belt): " + ShieldRpsPerCostBelt);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS per cost (belt): " + ChemDpsPerCostBelt * chemToHE);
                    }


                    Console.WriteLine("Uptime: " + UptimeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS (belt, sustained): " + ChemDpsBeltSustained * chemToEmp);
                        Console.WriteLine("EMP DPS per volume (sustained): " + ChemDpsPerVolumeBeltSustained * chemToEmp);
                        Console.WriteLine("EMP DPS per cost (sustained): " + ChemDpsPerCostBeltSustained * chemToEmp);
                        Console.WriteLine("Shield RPS (belt, sustained): " + ShieldRpsBeltSustained);
                        Console.WriteLine("RPS per volume (sustained): " + ShieldRpsPerVolumeBeltSustained);
                        Console.WriteLine("RPS per cost (sustained): " + ShieldRpsPerCostBeltSustained);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS (belt, sustained): " + ChemDpsBeltSustained * chemToHE);
                        Console.WriteLine("HE DPS per volume (sustained): " + ChemDpsPerVolumeBeltSustained * chemToHE);
                        Console.WriteLine("HE DPS per cost (sustained): " + ChemDpsPerCostBeltSustained * chemToHE);
                    }
                }
                else
                {
                    Console.WriteLine("Reload time: " + ReloadTime);
                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS: " + ChemDps * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS: " + ChemDps * chemToHE);
                    }

                    Console.WriteLine("Loader volume: " + LoaderVolume);
                    Console.WriteLine("Cooler volume: " + CoolerVolume);
                    Console.WriteLine("Charger volume: " + ChargerVolume);
                    Console.WriteLine("Recoil volume: " + RecoilVolume);
                    Console.WriteLine("Total volume: " + VolumePerIntake);
                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS per volume: " + ChemDpsPerVolume * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS per volume: " + ChemDpsPerVolume * chemToHE);
                    }

                    Console.WriteLine("Loader cost: " + LoaderCost);
                    Console.WriteLine("Cooler cost: " + CoolerCost);
                    Console.WriteLine("Charger cost: " + ChargerCost);
                    Console.WriteLine("Recoil cost: " + RecoilCost);
                    Console.WriteLine("Total cost: " + CostPerIntake);
                    if (disruptor)
                    {
                        Console.WriteLine("EMP DPS per cost: " + ChemDpsPerCost * chemToEmp);
                        Console.WriteLine("Shield RPS: " + ShieldRps);
                        Console.WriteLine("Shield RPS per Volume: " + ShieldRpsPerVolume);
                        Console.WriteLine("Shield RPS per cost: " + ShieldRpsPerCost);
                    }
                    else
                    {
                        Console.WriteLine("HE DPS per cost: " + ChemDpsPerCost * chemToHE);
                    }
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
                if (disruptor)
                {
                    Console.WriteLine(ChemDamage * chemToEmp);
                    Console.WriteLine(ShieldReduction);
                }
                else
                {
                    Console.WriteLine(ChemDamage * chemToHE);
                }

                if (IsBelt)
                {
                    Console.WriteLine(ReloadTimeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDpsBelt * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine(ChemDpsBelt * chemToHE);
                    }

                    Console.WriteLine(LoaderVolumeBelt);
                    Console.WriteLine(CoolerVolumeBelt);
                    Console.WriteLine(ChargerVolumeBelt);
                    Console.WriteLine(RecoilVolumeBelt);
                    Console.WriteLine(VolumePerIntakeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDpsPerVolumeBelt * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine(ChemDpsPerVolumeBelt * chemToHE);
                    }


                    Console.WriteLine(LoaderCostBelt);
                    Console.WriteLine(CoolerCostBelt);
                    Console.WriteLine(ChargerCostBelt);
                    Console.WriteLine(RecoilCostBelt);
                    Console.WriteLine(CostPerIntakeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDpsPerCostBelt * chemToEmp);
                        Console.WriteLine(ShieldRpsBelt);
                        Console.WriteLine(ShieldRpsPerVolumeBelt);
                        Console.WriteLine(ShieldRpsPerCostBelt);
                    }
                    else
                    {
                        Console.WriteLine(ChemDpsPerCostBelt * chemToHE);
                    }

                    Console.WriteLine(UptimeBelt);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDpsBeltSustained * chemToEmp);
                        Console.WriteLine(ChemDpsPerVolumeBeltSustained * chemToEmp);
                        Console.WriteLine(ChemDpsPerCostBeltSustained * chemToEmp);
                        Console.WriteLine(ShieldRpsBeltSustained);
                        Console.WriteLine(ShieldRpsPerVolumeBeltSustained);
                        Console.WriteLine(ShieldRpsPerCostBeltSustained);
                    }
                    else
                    {
                        Console.WriteLine(ChemDpsBeltSustained * chemToHE);
                        Console.WriteLine(ChemDpsPerVolumeBeltSustained * chemToHE);
                        Console.WriteLine(ChemDpsPerCostBeltSustained * chemToHE);
                    }
                }
                else
                {
                    Console.WriteLine(ReloadTime);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDps * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine(ChemDps * chemToHE);
                    }

                    Console.WriteLine(LoaderVolume);
                    Console.WriteLine(CoolerVolume);
                    Console.WriteLine(ChargerVolume);
                    Console.WriteLine(RecoilVolume);
                    Console.WriteLine(VolumePerIntake);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDpsPerVolume * chemToEmp);
                    }
                    else
                    {
                        Console.WriteLine(ChemDpsPerVolume * chemToHE);
                    }

                    Console.WriteLine(LoaderCost);
                    Console.WriteLine(CoolerCost);
                    Console.WriteLine(ChargerCost);
                    Console.WriteLine(RecoilCost);
                    Console.WriteLine(CostPerIntake);
                    if (disruptor)
                    {
                        Console.WriteLine(ChemDpsPerCost * chemToEmp);
                        Console.WriteLine(ShieldRps);
                        Console.WriteLine(ShieldRpsPerVolume);
                        Console.WriteLine(ShieldRpsPerCost);
                    }
                    else
                    {
                        Console.WriteLine(ChemDpsPerCost * chemToHE);
                    }
                }
            }
        }

        public void WriteShellInfoToFileChem(bool labels, StreamWriter writer)
        {
            // Determine if disruptor
            bool disruptor;
            if (HeadModule == Module.Disruptor)
            {
                disruptor = true;
            }
            else
            {
                disruptor = false;
            }

            if (labels)
            {
                writer.WriteLine("Gauge (mm): " + Gauge);
                writer.WriteLine("Total length (mm): " + TotalLength);
                writer.WriteLine("Length without casings: " + ProjectileLength);
                writer.WriteLine("Total modules: " + ModuleCountTotal);


                if (RGCasingCount > 0)
                {
                    writer.WriteLine("RG Casing: " + RGCasingCount);
                }

                if (GPCasingCount > 0)
                {
                    writer.WriteLine("GP Casing: " + GPCasingCount);
                }

                int modIndex = 0;
                foreach (float modCount in BodyModuleCounts)
                {
                    if (modCount > 0)
                    {
                        writer.WriteLine(Module.AllModules[modIndex].Name + ": " + modCount);
                    }
                    modIndex++;
                }

                writer.WriteLine("Head: " + HeadModule.Name);
                writer.WriteLine("Rail draw: " + RailDraw);
                writer.WriteLine("Recoil: " + TotalRecoil);
                writer.WriteLine("Velocity (m/s): " + Velocity);
                writer.WriteLine("Effective range (m): " + EffectiveRange);
                if (disruptor)
                {
                    writer.WriteLine("EMP damage: " + ChemDamage * chemToEmp);
                    writer.WriteLine("Shield reduction (decimal): " + ShieldReduction);
                }
                else
                {
                    writer.WriteLine("HE damage: " + ChemDamage * chemToHE);
                }


                if (IsBelt)
                {
                    writer.WriteLine("Reload time (belt): " + ReloadTimeBelt);

                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS (belt): " + ChemDpsBelt * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS (belt): " + ChemDpsBelt * chemToHE);
                    }

                    writer.WriteLine("Loader volume: " + LoaderVolumeBelt);
                    writer.WriteLine("Cooler volume: " + CoolerVolumeBelt);
                    writer.WriteLine("Charger volume: " + ChargerVolumeBelt);
                    writer.WriteLine("Recoil volume: " + RecoilVolumeBelt);
                    writer.WriteLine("Total volume: " + VolumePerIntakeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS per volume (belt): " + ChemDpsPerVolumeBelt * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS per volume (belt): " + ChemDpsPerVolumeBelt * chemToHE);
                    }


                    writer.WriteLine("Loader cost: " + LoaderCostBelt);
                    writer.WriteLine("Cooler cost: " + CoolerCostBelt);
                    writer.WriteLine("Charger cost: " + ChargerCostBelt);
                    writer.WriteLine("Recoil cost: " + RecoilCostBelt);
                    writer.WriteLine("Total cost: " + CostPerIntakeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS per cost (belt): " + ChemDpsPerCostBelt * chemToEmp);
                        writer.WriteLine("Shield RPS (belt): " + ShieldRpsBelt);
                        writer.WriteLine("Shield RPS per volume (belt): " + ShieldRpsPerVolumeBelt);
                        writer.WriteLine("Shield RPS per cost (belt): " + ShieldRpsPerCostBelt);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS per cost (belt): " + ChemDpsPerCostBelt * chemToHE);
                    }


                    writer.WriteLine("Uptime: " + UptimeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS (belt, sustained): " + ChemDpsBeltSustained * chemToEmp);
                        writer.WriteLine("EMP DPS per volume (sustained): " + ChemDpsPerVolumeBeltSustained * chemToEmp);
                        writer.WriteLine("EMP DPS per cost (sustained): " + ChemDpsPerCostBeltSustained * chemToEmp);
                        writer.WriteLine("Shield RPS (belt, sustained): " + ShieldRpsBeltSustained);
                        writer.WriteLine("RPS per volume (sustained): " + ShieldRpsPerVolumeBeltSustained);
                        writer.WriteLine("RPS per cost (sustained): " + ShieldRpsPerCostBeltSustained);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS (belt, sustained): " + ChemDpsBeltSustained * chemToHE);
                        writer.WriteLine("HE DPS per volume (sustained): " + ChemDpsPerVolumeBeltSustained * chemToHE);
                        writer.WriteLine("HE DPS per cost (sustained): " + ChemDpsPerCostBeltSustained * chemToHE);
                    }
                }
                else
                {
                    writer.WriteLine("Reload time: " + ReloadTime);
                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS: " + ChemDps * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS: " + ChemDps * chemToHE);
                    }

                    writer.WriteLine("Loader volume: " + LoaderVolume);
                    writer.WriteLine("Cooler volume: " + CoolerVolume);
                    writer.WriteLine("Charger volume: " + ChargerVolume);
                    writer.WriteLine("Recoil volume: " + RecoilVolume);
                    writer.WriteLine("Total volume: " + VolumePerIntake);
                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS per volume: " + ChemDpsPerVolume * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS per volume: " + ChemDpsPerVolume * chemToHE);
                    }

                    writer.WriteLine("Loader cost: " + LoaderCost);
                    writer.WriteLine("Cooler cost: " + CoolerCost);
                    writer.WriteLine("Charger cost: " + ChargerCost);
                    writer.WriteLine("Recoil cost: " + RecoilCost);
                    writer.WriteLine("Total cost: " + CostPerIntake);
                    if (disruptor)
                    {
                        writer.WriteLine("EMP DPS per cost: " + ChemDpsPerCost * chemToEmp);
                        writer.WriteLine("Shield RPS: " + ShieldRps);
                        writer.WriteLine("Shield RPS per Volume: " + ShieldRpsPerVolume);
                        writer.WriteLine("Shield RPS per cost: " + ShieldRpsPerCost);
                    }
                    else
                    {
                        writer.WriteLine("HE DPS per cost: " + ChemDpsPerCost * chemToHE);
                    }
                }
            }


            else if (!labels)
            {
                writer.WriteLine(Gauge);
                writer.WriteLine(TotalLength);
                writer.WriteLine(ProjectileLength);
                writer.WriteLine(ModuleCountTotal);
                writer.WriteLine(GPCasingCount);
                writer.WriteLine(RGCasingCount);
                foreach (float modCount in BodyModuleCounts)
                {
                    writer.WriteLine(modCount);
                }

                writer.WriteLine(HeadModule.Name);
                writer.WriteLine(RailDraw);
                writer.WriteLine(TotalRecoil);
                writer.WriteLine(Velocity);
                writer.WriteLine(EffectiveRange);
                if (disruptor)
                {
                    writer.WriteLine(ChemDamage * chemToEmp);
                    writer.WriteLine(ShieldReduction);
                }
                else
                {
                    writer.WriteLine(ChemDamage * chemToHE);
                }

                if (IsBelt)
                {
                    writer.WriteLine(ReloadTimeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDpsBelt * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine(ChemDpsBelt * chemToHE);
                    }

                    writer.WriteLine(LoaderVolumeBelt);
                    writer.WriteLine(CoolerVolumeBelt);
                    writer.WriteLine(ChargerVolumeBelt);
                    writer.WriteLine(RecoilVolumeBelt);
                    writer.WriteLine(VolumePerIntakeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDpsPerVolumeBelt * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine(ChemDpsPerVolumeBelt * chemToHE);
                    }


                    writer.WriteLine(LoaderCostBelt);
                    writer.WriteLine(CoolerCostBelt);
                    writer.WriteLine(ChargerCostBelt);
                    writer.WriteLine(RecoilCostBelt);
                    writer.WriteLine(CostPerIntakeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDpsPerCostBelt * chemToEmp);
                        writer.WriteLine(ShieldRpsBelt);
                        writer.WriteLine(ShieldRpsPerVolumeBelt);
                        writer.WriteLine(ShieldRpsPerCostBelt);
                    }
                    else
                    {
                        writer.WriteLine(ChemDpsPerCostBelt * chemToHE);
                    }

                    writer.WriteLine(UptimeBelt);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDpsBeltSustained * chemToEmp);
                        writer.WriteLine(ChemDpsPerVolumeBeltSustained * chemToEmp);
                        writer.WriteLine(ChemDpsPerCostBeltSustained * chemToEmp);
                        writer.WriteLine(ShieldRpsBeltSustained);
                        writer.WriteLine(ShieldRpsPerVolumeBeltSustained);
                        writer.WriteLine(ShieldRpsPerCostBeltSustained);
                    }
                    else
                    {
                        writer.WriteLine(ChemDpsBeltSustained * chemToHE);
                        writer.WriteLine(ChemDpsPerVolumeBeltSustained * chemToHE);
                        writer.WriteLine(ChemDpsPerCostBeltSustained * chemToHE);
                    }
                }
                else
                {
                    writer.WriteLine(ReloadTime);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDps * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine(ChemDps * chemToHE);
                    }

                    writer.WriteLine(LoaderVolume);
                    writer.WriteLine(CoolerVolume);
                    writer.WriteLine(ChargerVolume);
                    writer.WriteLine(RecoilVolume);
                    writer.WriteLine(VolumePerIntake);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDpsPerVolume * chemToEmp);
                    }
                    else
                    {
                        writer.WriteLine(ChemDpsPerVolume * chemToHE);
                    }

                    writer.WriteLine(LoaderCost);
                    writer.WriteLine(CoolerCost);
                    writer.WriteLine(ChargerCost);
                    writer.WriteLine(RecoilCost);
                    writer.WriteLine(CostPerIntake);
                    if (disruptor)
                    {
                        writer.WriteLine(ChemDpsPerCost * chemToEmp);
                        writer.WriteLine(ShieldRps);
                        writer.WriteLine(ShieldRpsPerVolume);
                        writer.WriteLine(ShieldRpsPerCost);
                    }
                    else
                    {
                        writer.WriteLine(ChemDpsPerCost * chemToHE);
                    }
                }
            }
        }



        /// <summary>
        /// Calculate damage modifier according to current damageType
        /// </summary>
        public void CalculateDamageModifierByType(float damageType)
        {
            if (damageType == 0)
            {
                CalculateKDModifier();
                CalculateAPModifier();
            }
            else if (damageType == 1 || damageType == 3)
            {
                CalculateChemModifier();
            }
            else if (damageType == 2)
            {
                CalculateKDModifier();
                CalculateAPModifier();
                CalculateChemModifier();
            }
        }


        /// <summary>
        /// Calculates damage according to current damageType
        /// </summary>
        public void CalculateDamageByType(float damageType)
        {
            if (damageType == 0)
            {
                CalculateKineticDamage();
                CalculateAP();
            }
            else if (damageType == 1)
            {
                CalculateChemDamage();
            }
            else if (damageType == 2)
            {
                CalculateKineticDamage();
                CalculateAP();
                CalculateChemDamage();
            }
            else if (damageType == 3)
            {
                CalculateShieldReduction();
            }
        }


        public void CalculateDpsByType(float damageType, float targetAC, Scheme targetArmorScheme)
        {
            CalculateRecoil();
            CalculateChargerVolumeAndCost();
            CalculateRecoilVolumeAndCost();
            CalculateVolumeAndCostPerIntake();

            if (damageType == 0)
            {
                CalculateKineticDps(targetAC);
            }
            else if (damageType == 1)
            {
                CalculateChemDps();
            }
            else if (damageType == 2)
            {
                CalculatePendepthDps(targetArmorScheme);
            }
            else if (damageType == 3)
            {
                CalculateChemDps();
                CalculateShieldRps();
            }
        }

        public void CalculateDpsByTypeBelt(float damageType, float targetAC, Scheme targetArmorScheme)
        {
            CalculateRecoil();
            CalculateChargerVolumeAndCost();
            CalculateRecoilVolumeAndCost();
            CalculateVolumeAndCostPerIntake();

            if (damageType == 0)
            {
                CalculateKineticDpsBelt(targetAC);
            }
            else if (damageType == 1)
            {
                CalculateChemDpsBelt();
            }
            else if (damageType == 2)
            {
                CalculatePendepthDpsBelt(targetArmorScheme);
            }
            else if (damageType == 3)
            {
                CalculateChemDpsBelt();
                CalculateShieldRpsBelt();
            }
        }
    }
}