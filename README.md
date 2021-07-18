# Kerbalism SystemHeat

Kerbalism support for SystemHeat.


## What does this mod do?

This mod implements Kerbalism resource system support for SystemHeat partmodules.

This includes:

### Background resource production/consumption for unloaded vessels

Implemented for: radiators, converters, harvesters, fission reactors and fission engines (ones producing EC).

If fission reactor/engine is set to non-manual mode, mod will automatically adjust it's throttle (respecting min and max throttle values) in order to satisfy vessel electricity consumption.

KerbalismSystemHeat does not touch heat simulation in any manner. Heat balance of vessel (including heatloops temperatures, accumulated heat flux, and so on) will be saved until vessel goes "off rails" again (i.e. is loaded and/or activated).

### Kerbalism resource production/consumption system for active vessels

Implemented for: radiators, converters and harvesters.

Fission reactors/engines do not use Kerbalism resource system on active vessels, and are still subject for "incoherent behavior at high warp speed" warning from Kerbalism.

### Kerbalism planner support in VAB/Hangar

Implemented for: radiators, converters, harvesters, fission reactors and fission engines.

Planner simulation for converters and harvesters is activated by turning SystemHeat heat simulation on in PAW menu. Please note that harvesters and converters efficiency now depends only on temperature of heat loop they belong to.

"Simulated resource abundance" parameter added to harvesters for player's convenience (similarly to Kerbalism Harvester partmodule).

### Some rework for radioactivity of fission reactors and engines

Kerbalism emulates radioactivity from fission reactors and engines by adding special "Emitter" module for such parts. However, Emitter partmodule doesn't care if reactor/engine was ever started, and "emits" constantly.
This mod changes this in more realistic manner: fission reactor/engine will start emit radiation only then they has started, and after they has been shutdown, emission will slowly decay to some minimum value.


## Kerbalism profiles support

Default and ScienceOnly profiles have been tested so far.

Others profiles should work too, as this mod does not interfere with them.

Players, using Default Kerbalism profile, may notice that all converters and harvesters have not been switched to SystemHeat modules, and still use Kerbalism processes (and do not produce any heat, as a result).

This is intended, at least until I figure out how to properly integrate Kerbalism Configure partmodule with SystemHeat modules.


## Dependencies

* [Kerbalism (3.14)](https://github.com/Kerbalism/Kerbalism)
* [SystemHeat (0.4.3)](https://github.com/post-kerbin-mining-corporation/SystemHeat)
* SystemHeatFissionReactors (from SystemHeat/Extras)
* SystemHeatFissionEngines (from SystemHeat/Extras)
* [Module manager (last version preferred)](https://github.com/sarbian/ModuleManager)


## Mods support

* [USI Core](https://github.com/UmbraSpaceIndustries/USI_Core) - USI reactors will be switched to SystemHeat modules


## Installation

Please remove mod folders (zKerbalismSystemHeat or KerbalismSystemHeat) from GameData folder inside your Kerbal Space Program folder before installation.

Then place the GameData folder from downloaded archive inside your Kerbal Space Program folder.


## Licensing

The MIT License (MIT)

Copyright (c) 2021 Alexander Rogov

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
