/*
 * Copyright KwirkyJ (Jake Smith)
 * 
 * Available for used under the LGPL v3 license.
 * You may use, recompile, modify, and redistribute with this license
 * and attribution.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace JDiminishingRTG
{
    [KSPModule("Radioisotope Generator")]
    public class ModuleDiminishingRTG : PartModule
    {
        [KSPField(isPersistant = true)]
        public float volume = 5F;

        // TODO: efficiency curve (inverse logistic?)
        [KSPField(isPersistant = true)]
        public float efficiency = 0.5F;


        #region privateFields
        [KSPField(guiName = "Fuel type", isPersistant = true, guiActiveEditor = true)]
        private string fuelName;

        [KSPField( guiName = "Half-life", isPersistant = true, guiActive = true, guiActiveEditor = true, guiUnits = " years")]
        private float fuelHalflife = -1F; // time in Kerbin-years

        [KSPField(isPersistant = true)]
        private int fuelIndex = -1;

        [KSPField(isPersistant = true)]
        private float fuelPep = -1F;

        [KSPField(isPersistant = true)]
        private float fuelDensity = -1;

        [KSPField(isPersistant = true)]
        private float timeOfStart = -1F;

        [KSPField(guiName = "Output", guiActive = true, guiActiveEditor = true, guiUnits = " Ec/s")]
        private string guiOutput;

        private float output;

        [KSPField(guiName = "RTG mass", isPersistant = true, guiActiveEditor = true, guiUnits = " tonnes")]
        private float mass = -1F;

        private UIPartActionWindow tweakableUI;

        private static List<RTGFuelConfig> RTGFuelConfigList;

        private static bool GenerateElectricity = false;
        private static bool GenerateHeat = true;
        
        private static string PowerDensityUnits  = "W/kg"; 
        private static string PowerDensityLabel  = "pep";
        private static float  PowerDensityFactor = 1e-3F;
        
        private static string HeatUnits        = "W";
        private static string ElectricityUnits = "Ec";

        private static float HeatScale        = 1F;
        private static float ElectricityScale = 1F;
        #endregion

        public override string GetInfo ()
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append ("Output decays over time.\n\n");
            sb.Append (String.Format ("Efficiency: {0:##.##}%\n", this.efficiency * 100));
            sb.Append (String.Format ("Volume: {0:####.##} dL\n\n", this.volume));
            if (RTGFuelConfigList == null) 
                ReadCustomConfigs ();
            sb.Append ("<color=#99FF00>Available fuels:</color>");
            foreach (RTGFuelConfig c in RTGFuelConfigList) {
                sb.Append ("\n    <color=#FF6600>"+ c.resourceName + "</color>\n");
                sb.Append ("        half-life: " + c.halflife + " years\n");
                sb.Append ("        " + PowerDensityLabel + ": " + (c.pep * PowerDensityFactor) 
                           + " " + PowerDensityUnits);
            }
            return sb.ToString();
        }

        public override void OnStart (StartState state)
        {
            //if (RTGFuelConfigList == null) 
                ReadCustomConfigs ();
            if (fuelIndex < 0)
                updateFuelSetup (0);
            updateOutput ();
            if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
                part.force_activate ();
            }
        }

        #region configLoading
        private static void ReadCustomConfigs()
        {
			Debug.Log ("[JDimRTG] Reading configs...");
            ReadRTGFuelConfigs ();
            try {
                ReadJDiminishingRTGGlobalConfig ();
			    Debug.Log ("[JDimRTG] ...reading configs done.");
            } catch (Exception e) {
                Debug.LogError("[JDimRTG] Problem in reading global config!\n" + e.ToString ());
            }
        }

        private static void ReadJDiminishingRTGGlobalConfig ()
		{
			foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes ("JDIMINISHINGRTGGLOBALCONFIG")) {
				Debug.Log ("[JDimRTG] Reading global config...");
				if (n.HasValue ("GenerateHeat")) {
					GenerateHeat = bool.Parse (n.GetValue ("GenerateHeat"));
					Debug.Log ("[JDimRTG] GenerateHeat = " + GenerateHeat);
				}
				if (!GenerateHeat) {
					GenerateElectricity = true;
				} else if (n.HasValue ("GenerateElectricity")) {
					GenerateElectricity = bool.Parse (n.GetValue ("GenerateElectricity"));
					Debug.Log ("[JDimRTG] GenerateElectricity = " + GenerateElectricity);
				}

				if (n.HasValue ("PowerDensityFactor")) {
					PowerDensityFactor = float.Parse (n.GetValue ("PowerDensityFactor"));
					Debug.Log ("[JDimRTG] PowerDensityFactor = " + PowerDensityFactor);
				}
				if (n.HasValue ("PowerDensityLabel")) {
					PowerDensityLabel = n.GetValue ("PowerDensityLabel");
					Debug.Log ("[JDimRTG] PowerDensityLabel = " + PowerDensityLabel);
				}
				if (n.HasValue ("PowerDensityUnits")) {
					PowerDensityUnits = n.GetValue ("PowerDensityUnits");
					Debug.Log ("[JDimRTG] PowerDensityUnits = " + PowerDensityUnits);
				}

				if (n.HasValue ("HeatUnits")) {
					HeatUnits = n.GetValue ("HeatUnits");
					Debug.Log ("[JDimRTG] HeatUnits = " + HeatUnits);
				}
				if (n.HasValue ("ElectricityUnits")) {
					ElectricityUnits = n.GetValue ("ElectricityUnits");
					Debug.Log ("[JDimRTG] ElectricityUnits = " + ElectricityUnits);
				}

				if (n.HasValue ("HeatScale")) {
					HeatScale = float.Parse (n.GetValue ("HeatScale"));
					Debug.Log ("[JDimRTG] HeatScale = " + HeatScale);
				}
				if (n.HasValue ("ElectricityScale")) {
					ElectricityScale = float.Parse (n.GetValue ("ElectricityScale"));
					Debug.Log ("[JDimRTG] ElectricityScale = " + ElectricityScale);
				}
			}
        }

        private static void ReadRTGFuelConfigs ()
        {
			Debug.Log ("[JDimRTG] Reading RTG Fuel configs...");
            RTGFuelConfigList = new List<RTGFuelConfig> ();
            List<string> seenResources = new List<string> ();
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("RTGFUELCONFIG")) {
                try {
                    RTGFuelConfig c = new RTGFuelConfig (node);
                    if (seenResources.Contains (c.resourceName))
                        continue;
                    RTGFuelConfigList.Add (new RTGFuelConfig (node));
                } catch (Exception e) {
                    Debug.LogError ("[JDimRTG] Cannot load all RTGFUELCONFIGs!\n" + e.ToString ());
                }
            }
        }
        #endregion

        #region configurationLogic 
        // this region derived from Firespitter
        [KSPEvent(guiActiveEditor = true, guiName = "Next fuel type")]
        public void nextFuelConfiguration ()
        {
            int newFuelIndex = (fuelIndex + 1 >= RTGFuelConfigList.Count) 
                               ? 0 : fuelIndex + 1;
            updateFuelSetup (newFuelIndex);
            foreach (Part sympart in this.part.symmetryCounterparts) {
                UpdateFuelSetupInPart (sympart, newFuelIndex);
            }
            updateActivePartUI ();
        }

        private void updateActivePartUI ()
        {
            if (tweakableUI == null) {
                tweakableUI = ToolsBeta.FindActionWindow (this.part);
            }
            if (tweakableUI != null) {
                tweakableUI.displayDirty = true;
            } else {
                Debug.Log ("[JDimRTG] no UI to refresh");
            }
        }
        
        private static void UpdateFuelSetupInPart (Part p, int index)
        {
            ModuleDiminishingRTG rtg = p.GetComponent<ModuleDiminishingRTG> ();
            if (rtg == null) 
                return;
            rtg.updateFuelSetup (index);
        }

        private void updateFuelSetup (int index)
        {
            string oldFuelName = fuelName;
            RTGFuelConfig config = RTGFuelConfigList [index];
            fuelIndex = index;
            fuelName = config.resourceName;
            fuelHalflife = config.halflife;
            fuelPep = config.pep;
            fuelDensity = config.density;

            part.Resources.list.Clear ();
            if (oldFuelName != "") {
                PartResource[] partResources = part.GetComponents<PartResource> ();
                for (int i = 0; i < partResources.Length; i++) {
                    if (partResources [i].resourceName == oldFuelName) 
                        DestroyImmediate (partResources [i]);
                }
            }
            ConfigNode resourceNode = new ConfigNode ("RESOURCE");
            resourceNode.AddValue ("name", fuelName);
            resourceNode.AddValue ("maxAmount", volume);
            resourceNode.AddValue ("amount", volume);
            resourceNode.AddValue ("isTweakable", false);
            part.AddResource (resourceNode);
            part.Resources.UpdateList ();
            mass = part.mass + part.GetResourceMass ();
            updateOutput ();
        }
        #endregion

        #region operation
        public override void OnFixedUpdate ()
        {
            double now = Planetarium.GetUniversalTime ();
            timeOfStart = (timeOfStart < 0) ? (float)now : timeOfStart;
            double progress = Math.Pow (2, -(((float)now - timeOfStart) / (fuelHalflife * 9203545)));

            updateRTGFuelAmount (progress);
            updateOutput ();
            if (GenerateElectricity) {
                part.RequestResource ("ElectricCharge", -output * efficiency * TimeWarp.fixedDeltaTime);
            }
            if (GenerateHeat) {
                part.AddThermalFlux (output);
            }
            updatePartMass ();
        }

        private void updateRTGFuelAmount (double progress)
        {
            foreach (PartResource res in part.GetComponents<PartResource> ()) {
                if (res.resourceName == fuelName) 
                    res.amount = res.maxAmount * progress;
            }
        }

        private void updatePartMass ()
        {
            foreach (PartResource res in part.GetComponents<PartResource> ()) {
                if (res.resourceName == fuelName) {
                    part.mass = mass - ((float)res.amount * fuelDensity);
                }
            }
        }

        private void updateOutput ()
        {
            foreach (PartResource res in part.GetComponents<PartResource> ()) {
                if (res.resourceName == fuelName) {
                    output = fuelPep * ((float)res.amount * fuelDensity) * HeatScale;
                }
            }
            float tmpout = output;
            string units = HeatUnits;
            if (GenerateElectricity) {
                tmpout = output * efficiency * ElectricityScale;
                units = ElectricityUnits;
            }
            if (tmpout < 1) {
                Fields ["guiOutput"].guiUnits = " " + units + "/min";
                guiOutput = String.Format ("{0:##.##}", tmpout * 60F);
            } else {
                Fields ["guiOutput"].guiUnits = " " + units + "/s";
                guiOutput = String.Format ("{0:##.##}", tmpout);
            }
        }
        #endregion
    }
}

