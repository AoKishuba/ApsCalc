# aps-calc
From the Depths APS Shell calculator

Known Issues:
  - Does not support Graviton Ram because I haven't taken the time to get the AP, KD, and Velocity modifiers for that module
  - Does not properly save shell configurations while running tests
  - Does not properly print number of shells tested

Limitations:
  - Does not calculate HE, frag, FlaK, or EMP damage; uses the multipier instead, because all those warheads scale the same way, so the most efficient HE configuration is also the most efficient configuration for FlaK, frag, and EMP
  - Shells without a dedicated "head" module are not currently supported (and could break the module)
  - Does not calculate accuracy

Planned Features:
  - Integration with my armor layout pierce calculator, for testing pendepth shells against a given armor configuration and only saving shells which pen
