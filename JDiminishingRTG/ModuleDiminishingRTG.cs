/*
 * Copyright Jake "KwirkyJ" Smith) <kwirkyj.smith0@gmail.com>
 * 
 * Available for use under the LGPL v3 license.
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
        [KSPField(guiName = "Fuel type", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        private string fuelName;

        [KSPField(guiName = "Half-life", isPersistant = true, guiActive = true, guiActiveEditor = true, guiUnits = " years")]
        private float fuelHalflife = -1F; // time in Kerbin-years

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

		//[SerializeField] // changes a bug
		[KSPField (guiName = "Fuel type", isPersistant = true, guiActiveEditor = true)]
		[UI_ChooseOption (options = new[] {"none"}, affectSymCounterparts = UI_Scene.Editor, scene = UI_Scene.Editor)]//, suppressEditorShipModified = true)]
		private int fuelSelectorIndex = 0;
		#endregion

		#region staticFields
		private static bool AreConfigsRead = false;

        private static List<RTGFuelConfig> RTGFuelConfigList;

        private static bool GenerateElectricity = false;
        private static bool GenerateHeat        = true;
        
        private static string PowerDensityUnits  = "W/kg"; 
        private static string PowerDensityLabel  = "pep";
        private static float  PowerDensityFactor = 1e-3F;
        
        private static string HeatUnits        = "W";
        private static string ElectricityUnits = "Ec";

        private static float HeatScale        = 1F;
        private static float ElectricityScale = 1F;

		private static string[] FuelNames = new string[] {"None"};
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
			if (!AreConfigsRead) {
				ReadCustomConfigs ();
				PopulateFuelNames();
			    AreConfigsRead = true;
			}
			if (HighLogic.LoadedScene == GameScenes.EDITOR) {
				UI_Control c = this.Fields["fuelSelectorIndex"].uiControlEditor;
				c.onFieldChanged = updateFuelSetup;
				UI_ChooseOption o = (UI_ChooseOption)c;
				o.options = FuelNames;
				this.updateFuelSetup (this.fuelSelectorIndex);
			}
            this.updateUIOutput (this.fuelPep, this.efficiency);
            if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				this.updateFuelSetup (this.fuelSelectorIndex);
                this.part.force_activate ();
            }
        }

		private static void PopulateFuelNames ()
		{
			int count = RTGFuelConfigList.Count;
			string[] names = new string[count];
			for (int i = 0; i < count; i++) {
				RTGFuelConfig r = RTGFuelConfigList[i];
				names[i] = r.resourceName;
			}
			FuelNames = names;
		}

        #region configLoading
        //SEE http://docuwiki-kspapi.rhcloud.com/#/classes/UI_ChooseOption
        //    http://forum.kerbalspaceprogram.com/index.php?/topic/135891-ui_chooseoption-oddities-when-displaying-long-names/
        private static void ReadCustomConfigs()
        {
			Debug.Log ("[JDimRTG] Reading configs...");
			RTGFuelConfigList = GetRTGFuelConfigs (GameDatabase.Instance.GetConfigNodes("RTGFUELCONFIG"));
            try {
				foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes ("JDIMINISHINGRTGGLOBALCONFIG")) {
					ReadJDiminishingRTGGlobalConfig (n);
				}
                Debug.Log ("[JDimRTG] ...reading configs done.");
            } catch (Exception e) {
                Debug.LogError("[JDimRTG] Problem in reading global config!\n" + e.ToString ());
            }
        }

        private static List<RTGFuelConfig> GetRTGFuelConfigs (ConfigNode[] database_rtgfuelnodes)
		{
			Debug.Log ("[JDimRTG] Reading RTG Fuel configs...");
			List<RTGFuelConfig> config_list = new List<RTGFuelConfig> ();
			List<string> seen_resources = new List<string> ();
			foreach (ConfigNode node in database_rtgfuelnodes) {
				try {
					RTGFuelConfig c = new RTGFuelConfig (node);
					if (!seen_resources.Contains (c.resourceName)) {
					    config_list.Add (c);
					    seen_resources.Add (c.resourceName);
					}
				} catch (Exception e) {
					Debug.LogError ("[JDimRTG] Could not load RTGFUELCONFIG:\n" + e.ToString ());
				}
			}
			return config_list;
        }

        private static void ReadJDiminishingRTGGlobalConfig (ConfigNode n)
		{
			Debug.Log ("[JDimRTG] Reading RTG global config...");
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
        #endregion

        #region configurationLogic
		public void updateFuelSetup (BaseField field, object oldValue)
		{
			this.updateFuelSetup (this.fuelSelectorIndex);
		}

        private void updateFuelSetup (int index)
        {
			foreach (RTGFuelConfig possibleFuel in RTGFuelConfigList) {
				List<PartResource> resources = this.part.Resources.list;
				int i = 0;
				while (resources[i] != null) {
					PartResource r = resources[i];
					if (r.resourceName == possibleFuel.resourceName) {
						resources.Remove (r);
						Destroy (r);
					} else {
						i++;
					}
				}
            }

            RTGFuelConfig config = RTGFuelConfigList[index];
            this.fuelName     = config.resourceName;
            this.fuelHalflife = config.halflife;
            this.fuelPep      = config.pep;
            this.fuelDensity  = config.density;

            ConfigNode resourceNode = new ConfigNode ("RESOURCE");
            resourceNode.AddValue ("name", this.fuelName);
            resourceNode.AddValue ("maxAmount", this.volume);
            resourceNode.AddValue ("amount", this.volume);
            resourceNode.AddValue ("isTweakable", false);
            this.part.AddResource (resourceNode);

            this.mass = this.part.mass + this.part.GetResourceMass ();
            this.updateUIOutput (this.fuelPep, this.volume * this.fuelDensity);
        }
        #endregion

        #region operation
        public override void OnUpdate ()
        {
            this.updateUIOutput(this.output, this.efficiency);
        }
        
        private void updateUIOutput (float output, float efficiency) {
            float tmpout = output;
            string units = HeatUnits;
            if (GenerateElectricity) {
                tmpout = output * efficiency * ElectricityScale;
                units = ElectricityUnits;
            }
            if (tmpout < 1) {
                units = units + "/min";
                tmpout = tmpout * 60F;
            } else {
                units = units + "/s";
            }
            Fields ["guiOutput"].guiUnits = " " + units;
            this.guiOutput = String.Format ("{0:##.##}", tmpout);
        }
        
        public override void OnFixedUpdate ()
        {
            float now = (float)Planetarium.GetUniversalTime ();
            this.timeOfStart = (this.timeOfStart < 0) ? now : this.timeOfStart;
            
            PartResource r = this.getRTGResource(this.fuelName);
            if (r == null) { 
                Debug.LogError ("[JDimRTG] Module resource '" + this.fuelName + "' has no matching PartResource");
                return;
            }
            
            double progress = Math.Pow (2, -((now - this.timeOfStart) / (this.fuelHalflife * 9203545)));
            r.amount = r.maxAmount * progress;
			this.output = getOutput (this.fuelPep, (float)r.amount * this.fuelDensity);
			this.part.mass = this.mass;
            if (GenerateElectricity) {
                this.part.RequestResource ("ElectricCharge", -this.output * this.efficiency * TimeWarp.fixedDeltaTime);
            }
            if (GenerateHeat) {
                this.part.AddThermalFlux (this.output);
            }
        }
        
       private PartResource getRTGResource (string resname) {
            foreach (PartResource r in this.part.Resources.list) {
                if (r.resourceName == resname) {
                    return r;
                }
            }
            return null;
        }

		private float getOutput (float pep, float res_mass) {
			return pep * res_mass * HeatScale;
		}
        #endregion
    }
}

