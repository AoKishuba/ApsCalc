using System;
// All dimensions in mm
// Module positions.  Enum is faster than strings.
public enum Position: int
{
    Base,
    Middle,
    Head
}

// A single module.  Building block of a Shell.
public class Module
{
    public Module(string name, float vMod, float kdMod, float apMod, float pMod, float mLength, Position mType, bool _isChem)
    {
        Name = name;
        VelocityMod = vMod;
        KineticDamageMod = kdMod;
        ArmorPierceMod = apMod;
        PayloadMod = pMod;
        MaxLength = mLength;
        ModuleType = mType;
        IsChem = _isChem; // Used for calculating relative chemical damage (HE, Frag, FlaK, EMP)
    }
    public string Name { get; }
    public float VelocityMod { get; }
    public float KineticDamageMod { get; }
    public float ArmorPierceMod { get; }
    public float PayloadMod { get; }
    public float MaxLength { get; }
    public Position ModuleType { get; }
    public bool IsChem { get; }

    // Initialize every unique module type
    public static Module SolidBody { get; } = new Module("Solid Body", 1.1f, 1.0f, 1.0f, 1.0f, 500, Position.Middle, false);
    public static Module SabotBody { get; } = new Module("Sabot Body", 1.1f, 0.8f, 1.4f, 0.25f, 500, Position.Middle, false);
    public static Module ChemBody { get; } = new Module("HE, Frag, FlaK, or EMP Body", 1.0f, 1.0f, 0.1f, 1.0f, 500, Position.Middle, true);
    public static Module FuseBody { get; } = new Module("Fuse", 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Middle, false);
    public static Module FinBody { get; } = new Module("Stabilizer Fin Body", 0.95f, 1.0f, 1.0f, 1.0f, 300, Position.Middle, false);
    public static Module ChemHead { get; } = new Module("HE, Frag, FlaK, or EMP Head", 1.3f, 1.0f, 0.1f, 1.0f, 500, Position.Head, true);
    public static Module SpecialHead { get; } = new Module("Squash or Shaped Charge Head", 1.45f, 0.1f, 0.1f, 1.0f, 500, Position.Head, true);
    public static Module APHead { get; } = new Module("Armor Piercing Head", 1.6f, 1.0f, 1.65f, 1.0f, 500, Position.Head, false);
    public static Module SabotHead { get; } = new Module("Sabot Head", 1.6f, 0.85f, 2.5f, 0.25f, 500, Position.Head, false);
    public static Module HeavyHead { get; } = new Module("Heavy Head", 1.45f, 1.65f, 1.0f, 1.0f, 500, Position.Head, false);
    public static Module SkimmerTip { get; } = new Module("Skimmer Tip", 1.6f, 1.0f, 1.4f, 1.0f, 500, Position.Head, false);
    public static Module Disruptor { get; } = new Module("Disruptor Conduit", 1.6f, 1.0f, 0.1f, 0.5f, 500, Position.Head, true);
    public static Module BaseBleeder { get; } = new Module("Base Bleeder", 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Base, false);
    public static Module Supercav { get; } = new Module("Supercavitation Base", 1.0f, 1.0f, 1.0f, 0.75f, 100, Position.Base, false);
    public static Module Tracer { get; } = new Module("Visible Tracer", 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Base, false);

    // List modules for reference
    public static Module[] AllModules { get; } =
    {
        SolidBody,
        SabotBody,
        ChemBody,
        FuseBody,
        FinBody,
        BaseBleeder,
        Supercav,
        Tracer,
        ChemHead,
        SpecialHead,
        APHead,
        SabotHead,
        HeavyHead,
        SkimmerTip,
        Disruptor
    };
}

// Shell.  A collection of modules, with calculations for its own performance.
public class Shell
{
    public Shell() { }

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

    public Module BaseModule { get; set; } // Should be left empty if only one non-casing module
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
    public float EffectiveBodyModuleCount { get; set; } // Compensate for length-limited modules
    public float EffectiveProjectileModuleCount { get; set; } // Compensate for length-limited modules

    // Power
    public float GPRecoil { get; set; }
    public float MaxDraw { get; set; }
    public float RailDraw { get; set; } = 0;
    public float Velocity { get; set; }

    // Reload and Cooldown
    public float ReloadTime { get; set; }
    public float BeltReloadTime { get; set; } = 0; // Beltfed Loader
    public float CooldownTime { get; set; }

    // Damage
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

        EffectiveBodyModuleCount = BodyLength / Gauge;
        EffectiveProjectileModuleCount = ProjectileLength / Gauge;
    }

    public void CalculateGPRecoil()
    {
        GPRecoil = GaugeCoefficient * GPCasingCount * 2500;
    }

    public void CalculateMaxDraw()
    {
        MaxDraw = (float)(GaugeCoefficient * ((ProjectileLength / Gauge) + (0.5 * RGCasingCount)) * 12500);
    }

    public void CalculateReloadTime()
    {
        ReloadTime = (float)(Math.Pow((Gauge * Gauge * Gauge / 125000000), 0.45)
            * (2 + EffectiveProjectileModuleCount + 0.25 * (RGCasingCount + GPCasingCount))
            * 17.5);

        if (Gauge >= 100)
        {
            BeltReloadTime = (float)(ReloadTime * 0.75 * Math.Pow(Gauge / 1000, 0.45));
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

    public void CalculateVelocity()
    {
        // Calculate weighted velocity modifier
        float[] VelocityModifiers;
        for (int i = 0; i < ShellModuleCounts.Length; i++)
        {
            VelocityModifiers
        }
        Velocity = (float)Math.Sqrt((RailDraw + GPRecoil) * 85 / (GaugeCoefficient * EffectiveProjectileModuleCount));
    }

}

namespace aps_calc
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize shell
            Shell TestShell = new Shell();

            TestShell.Gauge = 250;
            TestShell.GPCasingCount = 3;
            TestShell.RGCasingCount = 3;
            TestShell.ShellModuleCounts[2] = 4;
            TestShell.ShellModuleCounts[4] = 1;
            TestShell.BaseModule = Module.BaseBleeder;
            TestShell.HeadModule = Module.APHead;

            Console.WriteLine("Gauge: " + TestShell.Gauge);
            Console.WriteLine("GP Casings: " + TestShell.GPCasingCount);
            Console.WriteLine("RG Casings: " + TestShell.RGCasingCount);
            Console.WriteLine("Base: " + TestShell.BaseModule.Name);
            Console.WriteLine("Head: " + TestShell.HeadModule.Name);

            TestShell.CalculateLengths();
            Console.WriteLine("Casing Length: " + TestShell.CasingLength);
            Console.WriteLine("Body Length: " + TestShell.BodyLength);
            Console.WriteLine("Projectile Length: " + TestShell.ProjectileLength);
            Console.WriteLine("Total Length: " + TestShell.TotalLength);
            TestShell.CalculateGPRecoil();
            Console.WriteLine("GP Recoil " + TestShell.GPRecoil);
            TestShell.CalculateMaxDraw();
            Console.WriteLine("Max Draw " + TestShell.MaxDraw);
            TestShell.CalculateReloadTime();
            Console.WriteLine("Reload Time " + TestShell.ReloadTime);
            TestShell.CalculateChemDamage();
            Console.WriteLine("Chem Damage: " + TestShell.ChemDamage);
            TestShell.CalculateVelocity();
            Console.WriteLine("Velocity " + TestShell.Velocity);


            /*
            bool stop = false;
            while (!stop)
            {
                int GaugeTest = Convert.ToInt32(value);
                if (GaugeTest < 18 || GaugeTest > 500)
                {
                    Console.WriteLine("Gauge must be an integer between 18 and 500");
                }
                else
                {
                    _gauge = value;
                    stop = true;
                }
            }
            */
        }
    }
}
