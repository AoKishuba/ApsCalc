using System;
using System.Collections.Generic;
using System.Text;
using ApsCalc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApsCalcTests
{
    [TestClass]
    public class ShellUnitTests
    {
        /// <summary>
        /// Verifies the gauge coefficient updates whenever the gauge is changed
        /// </summary>
        [TestMethod]
        public void Gauge_Coefficient_Updates()
        {
            float expectedGaugeCoefficient500 = 1;
            float expectedGaugeCoefficient18 = 0.0025196748f;

            Shell TestShell = new Shell();
            TestShell.Gauge = 500f;
            float actual500 = TestShell.GaugeCoefficient;

            TestShell.Gauge = 18f;
            float actual18 = TestShell.GaugeCoefficient;

            Assert.AreEqual(expectedGaugeCoefficient500, actual500);
            Assert.AreEqual(expectedGaugeCoefficient18, actual18);
        }


        /// <summary>
        /// Verifies new shells start with a null value for the base module
        /// </summary>
        [TestMethod]
        public void Default_Base_Is_Null()
        {
            Module expectedDefaultBase = null;

            Shell TestShell = new Shell();
            Module actualDefaultBase = TestShell.BaseModule;

            Assert.AreEqual(expectedDefaultBase, actualDefaultBase);
        }


        /// <summary>
        /// Verifies the base module can be set
        /// </summary>
        [TestMethod]
        public void Base_Can_Be_Set()
        {
            Module expectedNewBase = Module.AllModules[12];

            Shell TestShell = new Shell();
            TestShell.BaseModule = Module.AllModules[12];
            Module actualNewBase = TestShell.BaseModule;

            Assert.AreEqual(expectedNewBase, actualNewBase);
        }


        /// <summary>
        /// Verifies the lengths are correct for shells at max gauge
        /// </summary>
        [TestMethod]
        public void Long_Lengths_Are_Correct()
        {
            float testGauge = 500f;
            // One each solid body, sabot body, chem body, fuse, fin
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1 };

            Module testBase = Module.BaseBleeder;
            Module testHead = Module.HeavyHead;
            float testGPCount = 3.47f;
            float testRGCount = 3f;

            // Base bleeder and fuse are limited to 100; fins limited to 300
            float expectedBodyLength = (100f * 2) + 300f + (testGauge * 3);
            // Heavy Head has no limit
            float expectedProjectileLength = expectedBodyLength + testGauge;

            float expectedCasingLength = (testGPCount + testRGCount) * testGauge;
            // Add casings
            float expectedTotalLength = expectedProjectileLength + expectedCasingLength;

            float expectedShortLength = testGauge * 2;
            // No differential for long shell
            float expectedLengthDifferential = 0f;

            // Effective length should equal physical length for long shell
            float expectedEffectiveBodyLength = expectedBodyLength;

            float expectedEffectiveBodyModuleCount = expectedBodyLength / testGauge;
            float expectedEffectiveProjectileModuleCount = expectedProjectileLength / testGauge;


            Shell TestShell = new Shell();
            TestShell.Gauge = testGauge;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.BaseModule = testBase;
            TestShell.HeadModule = testHead;
            TestShell.GPCasingCount = testGPCount;
            TestShell.RGCasingCount = testRGCount;
            TestShell.CalculateLengths();
            float actualBodyLength = TestShell.BodyLength;
            float actualProjectileLength = TestShell.ProjectileLength;
            float actualCasingLength = TestShell.CasingLength;
            float actualTotalLength = TestShell.TotalLength;
            float actualShortLength = TestShell.ShortLength;
            float actualLengthDifferential = TestShell.LengthDifferential;
            float actualEffectiveBodyLength = TestShell.EffectiveBodyLength;
            float actualEffectiveBodyModuleCount = TestShell.EffectiveBodyModuleCount;
            float actualEffectiveProjectileModuleCount = TestShell.EffectiveProjectileModuleCount;


            Assert.AreEqual(expectedBodyLength, actualBodyLength);
            Assert.AreEqual(expectedProjectileLength, actualProjectileLength);
            Assert.AreEqual(expectedCasingLength, actualCasingLength);
            Assert.AreEqual(expectedTotalLength, actualTotalLength);
            Assert.AreEqual(expectedShortLength, actualShortLength);
            Assert.AreEqual(expectedLengthDifferential, actualLengthDifferential);
            Assert.AreEqual(expectedEffectiveBodyLength, actualEffectiveBodyLength);
            Assert.AreEqual(expectedEffectiveBodyModuleCount, actualEffectiveBodyModuleCount);
            Assert.AreEqual(expectedEffectiveProjectileModuleCount, actualEffectiveProjectileModuleCount);
        }


        /// <summary>
        /// Verifies lengths are correct for shells with gauges between 100 and 300 mm (max length of fuzes/bases and fins, respectively)
        /// </summary>
        [TestMethod]
        public void Mid_Lengths_Are_Correct()
        {
            // Gauge must be > 100 and < 300
            float testGauge = 150f;
            // One each solid body, sabot body, chem body, fuse, fin
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1 };

            Module testBase = Module.BaseBleeder;
            Module testHead = Module.HeavyHead;
            float testGPCount = 3.47f;
            float testRGCount = 3f;

            // Base bleeder and fuse are limited to 100
            float expectedBodyLength = (100f * 2) + (testGauge * 4);
            // Heavy Head has no limit
            float expectedProjectileLength = expectedBodyLength + testGauge;

            float expectedCasingLength = (testGPCount + testRGCount) * testGauge;
            // Add casings
            float expectedTotalLength = expectedProjectileLength + expectedCasingLength;

            float expectedShortLength = testGauge * 2;
            // No differential for long shell
            float expectedLengthDifferential = 0f;

            // Effective length should equal physical length for long shell
            float expectedEffectiveBodyLength = expectedBodyLength;

            float expectedEffectiveBodyModuleCount = expectedBodyLength / testGauge;
            float expectedEffectiveProjectileModuleCount = expectedProjectileLength / testGauge;


            Shell TestShell = new Shell();
            TestShell.Gauge = testGauge;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.BaseModule = testBase;
            TestShell.HeadModule = testHead;
            TestShell.GPCasingCount = testGPCount;
            TestShell.RGCasingCount = testRGCount;
            TestShell.CalculateLengths();
            float actualBodyLength = TestShell.BodyLength;
            float actualProjectileLength = TestShell.ProjectileLength;
            float actualCasingLength = TestShell.CasingLength;
            float actualTotalLength = TestShell.TotalLength;
            float actualShortLength = TestShell.ShortLength;
            float actualLengthDifferential = TestShell.LengthDifferential;
            float actualEffectiveBodyLength = TestShell.EffectiveBodyLength;
            float actualEffectiveBodyModuleCount = TestShell.EffectiveBodyModuleCount;
            float actualEffectiveProjectileModuleCount = TestShell.EffectiveProjectileModuleCount;


            Assert.AreEqual(expectedBodyLength, actualBodyLength);
            Assert.AreEqual(expectedProjectileLength, actualProjectileLength);
            Assert.AreEqual(expectedCasingLength, actualCasingLength);
            Assert.AreEqual(expectedTotalLength, actualTotalLength);
            Assert.AreEqual(expectedShortLength, actualShortLength);
            Assert.AreEqual(expectedLengthDifferential, actualLengthDifferential);
            Assert.AreEqual(expectedEffectiveBodyLength, actualEffectiveBodyLength);
            Assert.AreEqual(expectedEffectiveBodyModuleCount, actualEffectiveBodyModuleCount);
            Assert.AreEqual(expectedEffectiveProjectileModuleCount, actualEffectiveProjectileModuleCount);
        }


        /// <summary>
        /// Verifies lengths are correct for shells whose projectile length is < 2*gauge, which incurs a penalty proprotional to the Length Differential
        /// </summary>
        [TestMethod]
        public void Short_Lengths_Are_Correct()
        {
            // Gauge must be > 300
            float testGauge = 450;
            // One fuse, one fin
            float[] testBodyModuleCounts = { 0, 0, 0, 1, 1, 0 };

            Module testBase = default(Module);
            Module testHead = Module.HeavyHead;
            float testGPCount = 3.47f;
            float testRGCount = 3f;

            // Fuse limited to 100, fin limited to 300
            float expectedBodyLength = 400f;
            // Heavy Head has no limit
            float expectedProjectileLength = expectedBodyLength + testGauge;

            float expectedCasingLength = (testGPCount + testRGCount) * testGauge;
            // Add casings
            float expectedTotalLength = expectedProjectileLength + expectedCasingLength;

            float expectedShortLength = testGauge * 2;
            // Differential should exist for short shell
            float expectedLengthDifferential = expectedShortLength - 400f;

            // Effective length be longer than physical length for short shell
            float expectedEffectiveBodyLength = 2 * testGauge;

            float expectedEffectiveBodyModuleCount = expectedBodyLength / testGauge;
            float expectedEffectiveProjectileModuleCount = expectedProjectileLength / testGauge;


            Shell TestShell = new Shell();
            TestShell.Gauge = testGauge;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.BaseModule = testBase;
            TestShell.HeadModule = testHead;
            TestShell.GPCasingCount = testGPCount;
            TestShell.RGCasingCount = testRGCount;
            TestShell.CalculateLengths();
            float actualBodyLength = TestShell.BodyLength;
            float actualProjectileLength = TestShell.ProjectileLength;
            float actualCasingLength = TestShell.CasingLength;
            float actualTotalLength = TestShell.TotalLength;
            float actualShortLength = TestShell.ShortLength;
            float actualLengthDifferential = TestShell.LengthDifferential;
            float actualEffectiveBodyLength = TestShell.EffectiveBodyLength;
            float actualEffectiveBodyModuleCount = TestShell.EffectiveBodyModuleCount;
            float actualEffectiveProjectileModuleCount = TestShell.EffectiveProjectileModuleCount;


            Assert.AreEqual(expectedBodyLength, actualBodyLength);
            Assert.AreEqual(expectedProjectileLength, actualProjectileLength);
            Assert.AreEqual(expectedCasingLength, actualCasingLength);
            Assert.AreEqual(expectedTotalLength, actualTotalLength);
            Assert.AreEqual(expectedShortLength, actualShortLength);
            Assert.AreEqual(expectedLengthDifferential, actualLengthDifferential);
            Assert.AreEqual(expectedEffectiveBodyLength, actualEffectiveBodyLength);
            Assert.AreEqual(expectedEffectiveBodyModuleCount, actualEffectiveBodyModuleCount);
            Assert.AreEqual(expectedEffectiveProjectileModuleCount, actualEffectiveProjectileModuleCount);
        }


        /// <summary>
        /// Checks gunpowder recoil calculations
        /// </summary>
        [TestMethod]
        public void GP_Recoil_Math()
        {
            float expectedGPRecoil500 = 1250f;
            float expectedGPRecoil18 = 3.1495936f;


            Shell TestShell = new Shell();
            TestShell.Gauge = 500;
            TestShell.GPCasingCount = 0.5f;
            TestShell.CalculateGPRecoil();
            float actualGPRecoil500 = TestShell.GPRecoil;

            TestShell.Gauge = 18;
            TestShell.CalculateGPRecoil();
            float actualGPRecoil18 = TestShell.GPRecoil;


            Assert.AreEqual(expectedGPRecoil500, actualGPRecoil500);
            Assert.AreEqual(expectedGPRecoil18, actualGPRecoil18);
        }


        /// <summary>
        /// Checks max rail draw calculations
        /// </summary>
        [TestMethod]
        public void Max_Draw_Math()
        {
            float[] testBodyModuleCounts = { 5, 0, 0, 0, 0, 0 };
            float expectedDraw500 = (6.2f + 0.5f * 5f) * 12500f;
            float expectedDraw18 = 0.0025196748f * (7f + 0.5f * 5f) * 12500f;

            Shell TestShell = new Shell();
            TestShell.Gauge = 500;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.HeavyHead;
            TestShell.BaseModule = Module.Supercav;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.CalculateLengths(); // MUST be updated before CalculateMaxDraw
            TestShell.CalculateMaxDraw();
            float actualDraw500 = TestShell.MaxDraw;

            TestShell.Gauge = 18;
            TestShell.CalculateLengths();
            TestShell.CalculateMaxDraw();
            float actualDraw18 = TestShell.MaxDraw;


            Assert.AreEqual(expectedDraw500, actualDraw500);
            Assert.AreEqual(expectedDraw18, actualDraw18);
        }


        /// <summary>
        /// Checks modifier calculations
        /// </summary>
        [TestMethod]
        public void Modifier_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1, 0 };
            float expectedVMod = 1.7950001f;
            float expectedKDMod = 0.9625f;
            float expectedAPMod = 1.4953125f;


            Shell TestShell = new Shell();
            TestShell.Gauge = 150;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 5;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 2000f;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            float actualVMod = TestShell.OverallVelocityModifier;
            float actualKDMod = TestShell.OverallKineticDamageModifier;
            float actualAPMod = TestShell.OverallArmorPierceModifier;

            Assert.AreEqual(expectedVMod, actualVMod);
            Assert.AreEqual(expectedKDMod, actualKDMod);
            Assert.AreEqual(expectedAPMod, actualAPMod);
        }


        /// <summary>
        /// Checks shell velocity calculations
        /// </summary>
        [TestMethod]
        public void Velocity_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1, 0 };
            float expectedVelocityWithRecoil = 1138.3544f;
            float expectedVelocityWithoutRecoil = 0;


            Shell TestShell = new Shell();
            TestShell.Gauge = 150;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 5;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 2000f;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            float actualVelocityWithRecoil = TestShell.Velocity;

            TestShell.Gauge = 150;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 0;
            TestShell.RGCasingCount = 0;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 0f;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            float actualVelocityWithoutRecoil = TestShell.Velocity;

            Assert.AreEqual(expectedVelocityWithRecoil, actualVelocityWithRecoil);
            Assert.AreEqual(expectedVelocityWithoutRecoil, actualVelocityWithoutRecoil);
        }


        /// <summary>
        /// Checks shell effective range calculations
        /// </summary>
        [TestMethod]
        public void Effective_Range_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1, 0 };
            float expectedRangeWithRecoil = 19411.787f;
            float expectedRangeWithoutRecoil = 0;


            Shell TestShell = new Shell();
            TestShell.Gauge = 150;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 5;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 2000f;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            TestShell.CalculateEffectiveRange();
            float actualRangeWithRecoil = TestShell.EffectiveRange;

            TestShell.Gauge = 250;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 0;
            TestShell.RGCasingCount = 0;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 0f;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            TestShell.CalculateEffectiveRange();
            float actualRangeWithoutRecoil = TestShell.EffectiveRange;

            Assert.AreEqual(expectedRangeWithRecoil, actualRangeWithRecoil);
            Assert.AreEqual(expectedRangeWithoutRecoil, actualRangeWithoutRecoil);
        }


        /// <summary>
        /// Checks armor pierce and kinetic damage calculations
        /// </summary>
        [TestMethod]
        public void AP_KD_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1, 0 };
            float expectedKineticDamageWithRecoil = 3331.4146f;
            float expectedArmorPierceWithRecoil = 29.78842f;
            float expectedKineticDamageWithoutRecoil = 0;
            float expectedArmorPierceWithoutRecoil = 0;

            Shell TestShell = new Shell();
            TestShell.Gauge = 150;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 5;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 2000f;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            TestShell.CalculateKineticDamage();
            TestShell.CalculateAP();
            float actualKineticDamageWithRecoil = TestShell.KineticDamage;
            float actualArmorPierceWithRecoil = TestShell.ArmorPierce;

            TestShell.Gauge = 150;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 0;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.RailDraw = 0;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateModifiers();
            TestShell.CalculateVelocity();
            TestShell.CalculateKineticDamage();
            TestShell.CalculateAP();
            float actualKineticDamageWithoutRecoil = TestShell.KineticDamage;
            float actualArmorPierceWithoutRecoil = TestShell.ArmorPierce;

            Assert.AreEqual(expectedKineticDamageWithRecoil, actualKineticDamageWithRecoil);
            Assert.AreEqual(expectedArmorPierceWithRecoil, actualArmorPierceWithRecoil);
            Assert.AreEqual(expectedKineticDamageWithoutRecoil, actualKineticDamageWithoutRecoil);
            Assert.AreEqual(expectedArmorPierceWithoutRecoil, actualArmorPierceWithoutRecoil);
        }


        /// <summary>
        /// Verifies chemical shells start with 0 damage
        /// </summary>
        [TestMethod]
        public void Chem_Starts_At_0()
        {
            float[] testBodyModuleCounts0 = { 0, 0, 0, 0, 0, 0 };
            float[] testBodyModuleCounts5 = { 0, 0, 5, 0, 0, 0 };
            float expectedChemDamage0 = 0;
            float expectedChemDamage5_500 = 5f;


            Shell TestShell = new Shell();
            TestShell.Gauge = 500;
            TestShell.HeadModule = Module.APHead;
            TestShell.BodyModuleCounts = testBodyModuleCounts0;
            TestShell.CalculateModifiers();
            TestShell.CalculateChemDamage();
            float actualChemDamage0 = TestShell.ChemDamage;

            TestShell.Gauge = 500;
            TestShell.HeadModule = Module.APHead;
            TestShell.BodyModuleCounts = testBodyModuleCounts5;
            TestShell.CalculateModifiers();
            TestShell.CalculateChemDamage();
            float actualChemDamage5_500 = TestShell.ChemDamage;


            Assert.AreEqual(expectedChemDamage0, actualChemDamage0);
            Assert.AreEqual(expectedChemDamage5_500, actualChemDamage5_500);
        }


        /// <summary>
        /// Checks chemical payload math with varying gauge
        /// </summary>
        [TestMethod]
        public void Chem_Math_Gauge()
        {
            Shell TestShell = new Shell();
            float[] testBodyModuleCounts5 = { 0, 0, 5, 0, 0, 0 };

            float expectedChemDamage5_18 = 0.012598374f;
            float expectedChemDamage5_500 = 5f;

            TestShell.Gauge = 18;
            TestShell.HeadModule = Module.APHead;
            TestShell.BodyModuleCounts = testBodyModuleCounts5;
            TestShell.CalculateModifiers();
            TestShell.CalculateChemDamage();
            float actualChemDamage5_18 = TestShell.ChemDamage;

            TestShell.Gauge = 500;
            TestShell.HeadModule = Module.APHead;
            TestShell.BodyModuleCounts = testBodyModuleCounts5;
            TestShell.CalculateModifiers();
            TestShell.CalculateChemDamage();
            float actualChemDamage5_500 = TestShell.ChemDamage;


            Assert.AreEqual(expectedChemDamage5_18, actualChemDamage5_18);
            Assert.AreEqual(expectedChemDamage5_500, actualChemDamage5_500);
        }


        /// <summary>
        /// Checks payload modifier of supercavitation base
        /// </summary>
        [TestMethod]
        public void Payload_Modifier_Supercav()
        {
            Shell TestShell = new Shell();
            float expectedPayloadModifierSupercav = 0.75f;

            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.Supercav;
            TestShell.CalculateModifiers();
            float actualPayloadModifierSupercav = TestShell.OverallPayloadModifier;


            Assert.AreEqual(expectedPayloadModifierSupercav, actualPayloadModifierSupercav);
        }


        /// <summary>
        /// Checks payload modifier of sabot
        /// </summary>
        [TestMethod]
        public void Payload_Modifier_Sabot()
        {
            Shell TestShell = new Shell();
            float expectedPayloadModifierSabot = 0.25f;


            TestShell.HeadModule = Module.SabotHead;
            TestShell.CalculateModifiers();
            float actualPayloadModifierSabot = TestShell.OverallPayloadModifier;


            Assert.AreEqual(expectedPayloadModifierSabot, actualPayloadModifierSabot);
        }


        /// <summary>
        /// Checks payload modifier of disruptor
        /// </summary>
        [TestMethod]
        public void Payload_Modifier_Disruptor()
        {
            Shell TestShell = new Shell();
            float expectedPayloadModifierDisruptor = 0.5f;


            TestShell.HeadModule = Module.Disruptor;
            TestShell.CalculateModifiers();
            float actualPayloadModifierDisruptor = TestShell.OverallPayloadModifier;


            Assert.AreEqual(expectedPayloadModifierDisruptor, actualPayloadModifierDisruptor);
        }


        /// <summary>
        /// Verifies the 50% payload modifier penalty from the disruptor head stacks
        /// </summary>
        [TestMethod]
        public void Payload_Modifier_Disruptor_Stacks()
        {
            Shell TestShell = new Shell();
            float expectedPayloadModDisruptorAndSupercav = 0.75f * 0.5f;


            TestShell.BaseModule = Module.Supercav;
            TestShell.HeadModule = Module.Disruptor;
            TestShell.CalculateModifiers();
            float actualPayloadModifierDisruptorAndSupercav = TestShell.OverallPayloadModifier;


            Assert.AreEqual(expectedPayloadModDisruptorAndSupercav, actualPayloadModifierDisruptorAndSupercav);
        }


        /// <summary>
        /// Checks reload and cooldown time math
        /// </summary>
        [TestMethod]
        public void Reload_And_Cooldown_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1, 0 };

            float expectedReloadTime500 = 166.25f;
            float expectedCooldownTime500 = 57.634007f;
            float expectedReloadTime500Belt = default(float); // Total length > 1000 mm

            float expectedReloadTime18 = 2.2633111f;
            float expectedCooldownTime18 = 0.64816743f;
            float expectedReloadTime18Belt = 2.2633111f * 0.75f * (float)Math.Pow(0.018f, 0.45f);

            Shell TestShell = new Shell();
            TestShell.Gauge = 500;
            testBodyModuleCounts.CopyTo(TestShell.BodyModuleCounts, 0);
            TestShell.GPCasingCount = 5;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.CalculateLengths();
            TestShell.CalculateReloadTime();
            TestShell.CalculateCooldownTime();
            float actualReloadTime500 = TestShell.ReloadTime;
            float actualCooldownTime500 = TestShell.CooldownTime;
            float actualReloadTime500Belt = TestShell.ReloadTimeBelt;

            TestShell.Gauge = 18;
            TestShell.CalculateLengths();
            TestShell.CalculateReloadTime();
            TestShell.CalculateCooldownTime();
            float actualReloadTime18 = TestShell.ReloadTime;
            float actualCooldownTime18 = TestShell.CooldownTime;
            float actualReloadTime18Belt = TestShell.ReloadTimeBelt;


            Assert.AreEqual(expectedReloadTime500, actualReloadTime500);
            Assert.AreEqual(expectedCooldownTime500, actualCooldownTime500);
            Assert.AreEqual(expectedReloadTime500Belt, actualReloadTime500Belt);

            Assert.AreEqual(expectedReloadTime18, actualReloadTime18);
            Assert.AreEqual(expectedCooldownTime18, actualCooldownTime18);
            Assert.AreEqual(expectedReloadTime18Belt, actualReloadTime18Belt);
        }

        
        /// <summary>
        /// Checks volume per intake math
        /// </summary>
        [TestMethod]
        public void Volume_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1, 0 };

            float expectedVolume500 = 7.0529375f;
            float expectedVolume18 = 4.0529375f;


            Shell TestShell = new Shell();
            TestShell.Gauge = 500;
            testBodyModuleCounts.CopyTo(TestShell.BodyModuleCounts, 0);
            TestShell.GPCasingCount = 0;
            TestShell.RGCasingCount = 0;
            TestShell.RailDraw = 0;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.CalculateLengths();
            TestShell.CalculateGPRecoil();
            TestShell.CalculateCooldownTime();
            TestShell.CalculateReloadTime();
            TestShell.CalculateVolume();
            float actualVolume500 = TestShell.VolumePerIntake;
            Console.WriteLine(actualVolume500);

            TestShell.Gauge = 18;
            TestShell.CalculateLengths();
            TestShell.CalculateVolume();
            float actualVolume18 = TestShell.VolumePerIntake;
            Console.WriteLine(actualVolume18);



            Assert.AreEqual(expectedVolume500, actualVolume500);
            Assert.AreEqual(expectedVolume18, actualVolume18);
        }
    }
}
