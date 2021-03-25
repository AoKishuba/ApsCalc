﻿using System;
using System.Collections.Generic;
using System.Text;

namespace aps_calc
{
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
        ChemHead,
        SpecialHead,
        APHead,
        SabotHead,
        HeavyHead,
        SkimmerTip,
        Disruptor,
        BaseBleeder,
        Supercav,
        Tracer
        };
    }
}