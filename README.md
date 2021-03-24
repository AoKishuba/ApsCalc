# aps-calc
From the Depths APS Shell calculator.  WORK IN PROGRESS

Known Issues:
  - Does not support Graviton Ram because I haven't taken the time to get the AP, KD, and Velocity modifiers for that module

Limitations:
  - Does not calculate HE, frag, FlaK, or EMP damage; uses the multipier instead, because all those warheads scale the same way, so the most efficient HE configuration is also the most efficient configuration for FlaK, frag, and EMP
  - Does not calculate accuracy

Planned Features:
  - Integration with my armor layout pierce calculator, for testing pendepth shells against a given armor configuration and only saving shells which pen
  - NEAR-TOTAL REWRITE, with values being calculated in a cascade
    - For example, get the range of gauges to be tested, then generate all possible values of GaugeCoefficient and store so they can be looked up rather than calculated
