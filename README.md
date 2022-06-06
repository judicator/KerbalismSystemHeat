# Kerbalism SystemHeat

"Middleman" mod, which implements Kerbalism resource system support for [Nertea's SystemHeat](https://forum.kerbalspaceprogram.com/index.php?/topic/193909-*).


## What partmodules and features of SystemHeat mod are supported and how?

### Background resource production/consumption for unloaded vessels

Implemented for: radiators, converters, harvesters, fission reactors and fission engines.

Mod will automatically adjust fission reactor's throttle (respecting min (and max, if reactor in manual mode) throttle values) in order to satisfy vessel electricity consumption. Same also applies to trimodal fission engines (ones producing EC).

KerbalismSystemHeat does not touch heat simulation in any manner. Heat balance of vessel (including heatloops temperatures, accumulated heat flux, and so on) will be saved until vessel goes "off rails" again (i.e. is loaded and/or activated).

### Kerbalism resource production/consumption system for active vessels

Implemented for: radiators, converters and harvesters.

Fission reactors/engines do not use Kerbalism resource system on active vessels, and are still subject for "incoherent behavior at high warp speed" warning from Kerbalism.

### Kerbalism planner support in VAB/Hangar

Implemented for: radiators, converters, harvesters, fission reactors and fission engines.

Planner simulation for converters and harvesters is activated by turning SystemHeat heat simulation on in PAW menu. Please note that harvesters and converters efficiency now depends only on temperature of heat loop they belong to.

"Simulated resource abundance" parameter added to harvesters for player's convenience (similarly to Kerbalism Harvester partmodule).


## Kerbalism profiles support

Default and ScienceOnly profiles have been tested so far.

Others profiles should work too, as this mod does not interfere with them.

Players, using Default Kerbalism profile, may notice that all converters and harvesters have not been switched to SystemHeat modules, and still use Kerbalism processes (and do not produce any heat, as a result).

This is intended, at least until I figure out how to properly integrate Kerbalism Configure partmodule with SystemHeat modules.


## Dependencies

* [Kerbalism (3.14)](https://github.com/Kerbalism/Kerbalism)
* [SystemHeat (0.5.6)](https://github.com/post-kerbin-mining-corporation/SystemHeat)
* [Module manager (last version preferred)](https://github.com/sarbian/ModuleManager)
* SystemHeatFissionReactors (from SystemHeat/Extras)
* SystemHeatFissionEngines (from SystemHeat/Extras)


## Supported KSP versions

KerbalismSystemHeat have been tested in KSP versions from 1.8.1 to 1.12.3.


## Mods support

* Atomic Age (either old one or as part of SpaceTux Industries Recycled Parts) - SystemHeat support for wrap-around radiators and nuclear engines
* [Heat Control](https://github.com/post-kerbin-mining-corporation/HeatControl) - SystemHeat support for heat exchangers (radiators are already suppported by SystemHeat)
* [Missing History](https://github.com/UmbraSpaceIndustries/USI_Core) - SystemHeat support for nuclear engines
* [Near Future Aeronautics](https://github.com/post-kerbin-mining-corporation/NearFutureAeronautics) - SystemHeat support for nuclear engines
* [Near Future Electrics](https://github.com/post-kerbin-mining-corporation/NearFutureElectrical) - if SystemHeatConverters is installed, nuclear recycler will use SystemHeat mechanics
* [USI FTT](https://github.com/UmbraSpaceIndustries/FTT) - SystemHeat support for nuclear reactors
* [USI Core](https://github.com/UmbraSpaceIndustries/USI_Core) - SystemHeat support for nuclear reactors and nuclear materials containers


## Installation

Please remove mod folders (`zKerbalismSystemHeat` or `KerbalismSystemHeat`) from `GameData` folder inside your Kerbal Space Program folder before installation.

Then place the `GameData` folder from downloaded archive inside your Kerbal Space Program folder.


## Optional patch

There is an optional patch in `Extras/SystemHeatFissionReactorsLowerMinThrust`. It changes minimum throttle for fission reactor from default 25% to more reasonable 10%.

If you want to install it, just copy `SystemHeatFissionReactorsLowerMinThrust` folder to your `GameData`.


## Licensing

The MIT License (MIT)

Copyright (c) 2022 Alexander Rogov

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
