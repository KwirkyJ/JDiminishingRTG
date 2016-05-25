# JDiminishingRTG
A better KSP RTG

Release thread URL: http://forum.kerbalspaceprogram.com/threads/115938

* An improvement on stock RTG behavior with its eternal power supply.
* Implements a configurable, robust mechanism for Radioisotope decay 
  and power generation.
* Parts with ModuleDiminishinRTG will be able to configure their isotope
  fuel from within the VAB, a single input being reflected across all symmetry
  companions.
* Parts should behave consistently between vessel docking/undocking
* Decay operates in background (all unfocused vessels)
* Inspired by "Realistic RTGs" plugin by Gribbleschnibit8
  http://forum.kerbalspaceprogram.com/threads/96030/
* ModuleManager redistributed under CC SA license.
* JSeebeckUtils redistributed under LGPLv3 license. 

###Installation instructions:
  * Extract all GameData contents into your KSP/GameData folder.

###Known Issues:
  * Reverting to VAB (or loading craft from file will not display Output. 
    Re-cycling the configuration will fix.
  * Adding symmetry companions will not share the setup of the original.
    Like above, re-cycling the configuration to the desired setting will fix. (To produce error, detach and re-attach parts that have symmetry companions)
  * Part cost is not updated with change of resources. This is a failure with the part cost mechanic as provided by Squad, and I maintain that it is their duty to make it work properly.

###Extenders/modders:
  * Plugin and source available under LGPLv3
    You are free to run, modify, recompile, and redistribute with 
    attribution and same license.
  * Assets (cfg files) made available under CC BY NC 4.0
    You may share and modify-and-share so long as credit is given 
    and is not sold.
  * All RTGFUELCONFIG nodes ABSOLUTELY MUST have a resource definition 
      of the same name.
  * Adding/removing RTGFUELCONFIG nodes may have a deleterious effect to .craft
      files and persistent saves.
  * If more than one RTGFUELCONFIG has the same name, only one will be retained
      with undefined precedence.

##CHANGELOG
* 3 May 2015 (v1.3a)
    * Fixed failure to properly lead global config node, resulting in undesired behaviors.

* 2 May 2015 (v1.3)
    * Rebuilt on KSP v1.0.2
    * Added global settings configuration
    * Added thermal component

* 28 Apr 2015 (v1.2)
    * Rebuild on KSP v1.0
    * Adjusted half-lives to something more sane.

* 19 Apr 2015 (v1.1)
    * Balance tweak of radioisotopes -- ships in flight and .craft files will still function, but will retain their former performance.
    * Removed debug lines from some code sections.
    * Enabled Ec/min display when output is low.
    * Fixed bug where output was function of mass of all resources in part.
    * Fixed bug of mass being held constant as non-RTG fuels were added/removed.
    * Renamed displayed field from "Mass" to "RTG mass."
    * Added source files to release.

* 18 Apr 2015 (v1.0)
    * Initial release (KSPv0.90.0)

