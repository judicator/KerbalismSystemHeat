0.4.1
-----
 + Added optional patch (Extras/SystemHeatFissionReactorsLowerMinThrust): sets minimum throttle to fission reactors to 10% (default is 25%). Could help you save some EnrichedUranium on long journeys.

0.4.0
-----
 * Fixed issue #1, preventing patching ModuleSystemHeatFissionReactor partmodules.
 * Fixed issue #2 (throttling for fission reactors on unloaded vessels): feature has been redone.
 * Fixed support for fixed microchannel radiators from HeatControl (B9PartSwitch variants issue).
 + Implemented new feature: dynamic radioactivity for fission reactors and engines (will not emit radiation before started, emission will slowly decay after reactor/engine have been stopped).

0.3.2
-----
- Fixed USI reactors Kerbalism reliability.

0.3.1
-----
- Fixed reactors and fission engines min and max EC generation readouts.
- Updated readme.

0.3.0
-----
- Kerbalism Planner UI update is working for converters and harvesters, when thermal simulation is on.
- Some localization fixes.
- USI reactors support fixed.

0.2.0
-----
Second test release
- Implemented full support for radiators, converters and harvesters.
- Implemented partial (for unloaded vessels only) support for fission reactors and engines.  

0.1.0
-----
First release (for testers)
