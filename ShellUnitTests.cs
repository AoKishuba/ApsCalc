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

        [TestMethod]
        public void Default_Base_Is_Null()
        {
            Module expectedDefaultBase = null;

            Shell TestShell = new Shell();
            Module actualDefaultBase = TestShell.BaseModule;

            Assert.AreEqual(expectedDefaultBase, actualDefaultBase);
        }

        [TestMethod]
        public void Base_Can_Be_Set()
        {
            Module expectedNewBase = Module.AllModules[12];

            Shell TestShell = new Shell();
            TestShell.BaseModule = Module.AllModules[12];
            Module actualNewBase = TestShell.BaseModule;

            Assert.AreEqual(expectedNewBase, actualNewBase);
        }

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

        [TestMethod]
        public void Short_Lengths_Are_Correct()
        {
            // Gauge must be > 300
            float testGauge = 450;
            // One fuse, one fin
            float[] testBodyModuleCounts = { 0, 0, 0, 1, 1 };

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

        [TestMethod]
        public void Max_Draw_Math()
        {
            float[] testBodyModuleCounts = { 5, 0, 0, 0, 0 };
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

        [TestMethod]
        public void Velocity_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1 };
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
            float actualVelocityWithoutRecoil = TestShell.Velocity;

            Assert.AreEqual(expectedVelocityWithRecoil, actualVelocityWithRecoil);
            Assert.AreEqual(expectedVelocityWithoutRecoil, actualVelocityWithoutRecoil);
        }

        [TestMethod]
        public void AP_KD_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1 };
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

        [TestMethod]
        public void Reload_Math()
        {
            // A shell with a bit of everything
            float[] testBodyModuleCounts = { 1, 1, 1, 1, 1 };

            float expectedReloadTime500 = 166.25f;
            float expectedReloadTime500Belt = default(float); // Total length > 1000 mm

            float expectedReloadTime18 = 2.2633111f;
            float expectedReloadTime18Belt = 2.2633111f * 0.75f * (float)Math.Pow(0.018f, 0.45f);

            Shell TestShell = new Shell();
            TestShell.Gauge = 500;
            TestShell.BodyModuleCounts = testBodyModuleCounts;
            TestShell.GPCasingCount = 5;
            TestShell.RGCasingCount = 5;
            TestShell.HeadModule = Module.APHead;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.CalculateLengths();
            TestShell.CalculateReloadTime();
            float actualReloadTime500 = TestShell.ReloadTime;
            float actualReloadTime500Belt = TestShell.ReloadTimeBelt;

            TestShell.Gauge = 18;
            TestShell.CalculateLengths();
            TestShell.CalculateReloadTime();
            float actualReloadTime18 = TestShell.ReloadTime;
            float actualReloadTime18Belt = TestShell.ReloadTimeBelt;


            Assert.AreEqual(expectedReloadTime500, actualReloadTime500);
            Assert.AreEqual(expectedReloadTime500Belt, actualReloadTime500Belt);
            Assert.AreEqual(expectedReloadTime18, actualReloadTime18);
            Assert.AreEqual(expectedReloadTime18Belt, actualReloadTime18Belt);
        }
    }
}
