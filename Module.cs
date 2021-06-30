using System;
using System.Collections.Generic;
using System.Text;

namespace ApsCalc
{
    public class Module
    {
        /// <summary>
        /// Stores module information
        /// </summary>
        /// <param name="name">Module name</param>
        /// <param name="vMod">Velocity modifier</param>
        /// <param name="kdMod">Kinetic damage modifier</param>
        /// <param name="apMod">Armor pierce modifier</param>
        /// <param name="cMod">Chemical payload modifier</param>
        /// <param name="accMod">Inaccuracy modifier</param>
        /// <param name="mLength">Max length of module in mm (length equals gauge at or below this value)</param>
        /// <param name="mType">The type of module - base, middle, or head</param>
        public Module(string name, float vMod, float kdMod, float apMod, float cMod, float accMod, float mLength, Position mType)
        {
            Name = name;
            VelocityMod = vMod;
            KineticDamageMod = kdMod;
            ArmorPierceMod = apMod;
            ChemMod = cMod;
            AccuracyMod = accMod;
            MaxLength = mLength;
            ModulePosition = mType;
        }

        // Module positions.  Enum is faster than strings.
        public enum Position : int
        {
            Base,
            Middle,
            Head
        }

        public string Name { get; }
        public float VelocityMod { get; }
        public float KineticDamageMod { get; }
        public float ArmorPierceMod { get; }
        public float ChemMod { get; }
        public float AccuracyMod { get; }
        public float MaxLength { get; }
        public Position ModulePosition { get; }

        // Initialize every unique module type
        public static Module SolidBody { get; } = new Module("Solid body", 1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 500, Position.Middle);
        public static Module SabotBody { get; } = new Module("Sabot body", 1.1f, 0.8f, 1.4f, 0.25f, 1.0f, 500, Position.Middle);
        public static Module EmpBody { get; } = new Module("EMP body", 1.0f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Middle);
        public static Module FlaKBody { get; } = new Module("FlaK body", 1.0f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Middle);
        public static Module FragBody { get; } = new Module("Frag body", 1.0f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Middle);
        public static Module HEBody { get; } = new Module("HE body", 1.0f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Middle);
        public static Module FuseBody { get; } = new Module("Fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Middle);
        public static Module FinBody { get; } = new Module("Stabilizer fin body", 0.95f, 1.0f, 1.0f, 1.0f, 0.2f, 300, Position.Middle);
        public static Module GravCompensator { get; } = new Module("Grav. compensator", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Middle);
        public static Module EmpHead { get; } = new Module("EMP head", 1.3f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Head);
        public static Module FlaKHead { get; } = new Module("FlaK head", 1.3f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Head);
        public static Module FragHead { get; } = new Module("Frag head", 1.3f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Head);
        public static Module HEHead { get; } = new Module("HE head", 1.3f, 1.0f, 0.1f, 1.0f, 1.0f, 500, Position.Head);
        public static Module SpecialHead { get; } = new Module("Squash or Shaped charge head", 1.45f, 0.1f, 0.1f, 1.0f, 1.0f, 500, Position.Head);
        public static Module APHead { get; } = new Module("AP head", 1.6f, 1.0f, 1.65f, 1.0f, 1.0f, 500, Position.Head);
        public static Module SabotHead { get; } = new Module("Sabot head", 1.6f, 0.85f, 2.5f, 0.25f, 1.0f, 500, Position.Head);
        public static Module HeavyHead { get; } = new Module("Heavy head", 1.45f, 1.65f, 1.0f, 1.0f, 1.0f, 500, Position.Head);
        public static Module HollowPoint { get; } = new Module("Hollow point head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 500, Position.Head);
        public static Module SkimmerTip { get; } = new Module("Skimmer tip", 1.6f, 1.0f, 1.4f, 1.0f, 1.0f, 500, Position.Head);
        public static Module Disruptor { get; } = new Module("Disruptor conduit", 1.6f, 1.0f, 0.1f, 1f, 1.0f, 500, Position.Head);
        public static Module BaseBleeder { get; } = new Module("Base bleeder", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Base);
        public static Module Supercav { get; } = new Module("Supercavitation base", 1.0f, 1.0f, 1.0f, 0.75f, 1.0f, 100, Position.Base);
        public static Module Tracer { get; } = new Module("Visible tracer", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100, Position.Base);
        public static Module GravRam { get; } = new Module("Graviton ram", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 500, Position.Base);
        
        // List modules for reference
        public static Module[] AllModules { get; } =
        {
        SolidBody,
        SabotBody,
        EmpBody,
        FlaKBody,
        FragBody,
        HEBody,
        FuseBody,
        FinBody,
        GravCompensator,
        EmpHead,
        FlaKHead,
        FragHead,
        HEHead,
        SpecialHead,
        APHead,
        SabotHead,
        HeavyHead,
        HollowPoint,
        SkimmerTip,
        Disruptor,
        BaseBleeder,
        Supercav,
        Tracer,
        GravRam
        };
    }
}
