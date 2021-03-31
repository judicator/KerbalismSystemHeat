using System;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using KERBALISM.Planner;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatHarvesterKerbalism : ModuleSystemHeatHarvester
	{
		public static string brokerName = "SHHarvester";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismSystemHeat_Brokers_Harvester");

		// In the editor, allow the user to tweak the abundance for simulation purposes
		[UI_FloatRange(scene = UI_Scene.Editor, minValue = 0f, maxValue = 1f, stepIncrement = 0.01f)]
		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "simulated resource abundance")]
		public float simulatedAbundance = 0.1f;

		[KSPField(isPersistant = true)]
		public float calculatedAbundance = -1f;

		[KSPField(isPersistant = true)]
		public bool canHarvest = false;

		[KSPField(isPersistant = true)]
		public float systemHeatEfficiency = 0f;

		private float eff = 0f;

		public List<ResourceRatio> inputListClone;

		[KSPEvent(guiActive = false, guiName = "Toggle", guiActiveEditor = true, active = true)]
		public new void ToggleEditorThermalSim()
		{
			editorThermalSim = !editorThermalSim;
			// Update Kerbalism planner UI
			if (Lib.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}

		public new void Start()
		{
			base.Start();
			Fields["simulatedAbundance"].guiName = Localizer.Format("#LOC_KerbalismSystemHeat_SimulatedResourceAbundance", ResourceName);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			inputListClone = new List<ResourceRatio>();
			foreach (ResourceRatio res in inputList)
			{
				inputListClone.Add(res);
			}
			if (heatModule != null)
			{
				systemHeatEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (heatModule != null && simulatedAbundance >= HarvestThreshold && editorThermalSim)
			{
				foreach (ResourceRatio res in inputList)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
				}
				resourceChangeRequest.Add(new KeyValuePair<string, double>(ResourceName, Efficiency * systemHeatEfficiency * simulatedAbundance));
			}
			return brokerTitle;
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsActivated && heatModule != null && canHarvest)
			{
				calculatedAbundance = (float) KSHUtils.SampleResourceAbundance(vessel, this);
				if (calculatedAbundance >= HarvestThreshold)
				{
					ResUpdate(
						vessel,
						inputList,
						ResourceName,
						Efficiency,
						systemHeatEfficiency,
						calculatedAbundance,
						TimeWarp.fixedDeltaTime);
				}
			}
			return brokerTitle;
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			// if active
			if (Lib.Proto.GetBool(module_snapshot, "IsActivated") && Lib.Proto.GetBool(module_snapshot, "canHarvest"))
			{
				double abundance = Lib.Proto.GetFloat(module_snapshot, "calculatedAbundance");
				if (abundance >= Lib.Proto.GetFloat(module_snapshot, "HarvestThreshold"))
				{
					float systemHeatEfficiency = Lib.Proto.GetFloat(module_snapshot, "systemHeatEfficiency");
					string resName = Lib.Proto.GetString(module_snapshot, "ResourceName");
					float Efficiency = Lib.Proto.GetFloat(module_snapshot, "Efficiency");
					ResUpdate(v,
						(proto_part_module as SystemHeatHarvesterKerbalism).inputList,
						resName,
						Efficiency,
						systemHeatEfficiency,
						abundance,
						elapsed_s);
				}
			}
			// undo stock behavior by forcing last_update_time to now
			Lib.Proto.Set(module_snapshot, "lastUpdateTime", Planetarium.GetUniversalTime());
			return brokerTitle;
		}

		private static void ResUpdate(Vessel v, List<ResourceRatio> inputList, string ResourceName, float Efficiency, float systemHeatEfficiency, double abundance, double elapsed_s)
		{
			ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
				brokerName,
				KERBALISM.ResourceBroker.BrokerCategory.Harvester,
				brokerTitle));
			foreach (ResourceRatio ir in inputList)
			{
				recipe.AddInput(ir.ResourceName, ir.Ratio * elapsed_s);
			}
			recipe.AddOutput(ResourceName, Efficiency * systemHeatEfficiency * abundance * elapsed_s, dump: true);
			KERBALISM.ResourceCache.Get(v).AddRecipe(recipe);
		}

		public override void FixedUpdate()
		{
			// Temporary set Efficiency to zero in order to prevent FixedUpdate from producing resources
			eff = Efficiency;
			Efficiency = 0;
			inputList = new List<ResourceRatio>();
			base.FixedUpdate();
			inputList = inputListClone;
			Efficiency = eff;
			if (HighLogic.LoadedSceneIsFlight)
			{
				canHarvest = CanHarvest();
			}
			if (heatModule != null)
			{
				systemHeatEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
			}
			// Update Kerbalism planner UI
			if (Lib.IsEditor() && editorThermalSim)
			{
				KSHUtils.UpdateKerbalismPlannerUI();
			}
		}

		private bool CanHarvest()
		{
			CelestialBody body = vessel.mainBody;
			bool result = false;

			// check situation
			switch (HarvesterType)
			{
				case 0:
					result = vessel.Landed && CheckForImpact();
					break;
				case 1:
					result = body.ocean && (vessel.Splashed || vessel.altitude < 0.0);
					break;
				case 2:
					result = body.atmosphere && vessel.altitude < body.atmosphereDepth;
					break;
				case 3:
					result = vessel.altitude > body.atmosphereDepth || !body.atmosphere;
					break;
			}
			return result;
		}
	}
}
