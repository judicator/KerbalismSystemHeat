using System.Collections.Generic;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatHarvesterKerbalism : ModuleSystemHeatHarvester
	{
		// In the editor, allow the user to tweak the abundance for simulation purposes
		[UI_FloatRange(scene = UI_Scene.Editor, minValue = 0f, maxValue = 1f, stepIncrement = 0.01f)]
		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KerbalistSystemHeat_SimulatedResourceAbundance")]
		public float simulatedAbundance = 0.1f;

		[KSPField(isPersistant = true)]
		public float calculatedAbundance = -1f;

		[KSPField(isPersistant = true)]
		public bool canHarvest = false;

		[KSPField(isPersistant = true)]
		public float systemHeatEfficiency = 0f;

		private float eff = 0f;

		public List<ResourceRatio> inputListClone;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			inputListClone = new List<ResourceRatio>();
			foreach (ResourceRatio res in inputList)
			{
				inputListClone.Add(res);
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (heatModule != null)
			{
				ResUpdate(resourceChangeRequest, simulatedAbundance);
			}
			return "harvester";
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsActivated && heatModule != null && canHarvest)
			{
				double abundance = KSHUtils.SampleResourceAbundance(vessel, this);
				calculatedAbundance = (float) abundance;
				ResUpdate(resourceChangeRequest, abundance);
			}
			return "harvester";
		}

		private void ResUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, double resAbundance)
        {
			foreach (ResourceRatio res in inputList)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
			}
			if (resAbundance >= HarvestThreshold)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>(ResourceName, Efficiency * systemHeatEfficiency * resAbundance));
			}
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			// if active
			if (Lib.Proto.GetBool(module_snapshot, "IsActivated") && Lib.Proto.GetBool(module_snapshot, "canHarvest"))
			{
				double abundance = Lib.Proto.GetFloat(module_snapshot, "calculatedAbundance");
				string resName = Lib.Proto.GetString(module_snapshot, "ResourceName");

				if (abundance >= Lib.Proto.GetFloat(module_snapshot, "HarvestThreshold"))
				{
					float systemHeatEfficiency = Lib.Proto.GetFloat(module_snapshot, "systemHeatEfficiency");
					float Efficiency = Lib.Proto.GetFloat(module_snapshot, "Efficiency");
					foreach (ResourceRatio res in (proto_part_module as SystemHeatHarvesterKerbalism).inputList)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
					}
					resourceChangeRequest.Add(new KeyValuePair<string, double>(resName, Efficiency * systemHeatEfficiency * abundance));
				}
			}
			// undo stock behavior by forcing last_update_time to now
			Lib.Proto.Set(module_snapshot, "lastUpdateTime", Planetarium.GetUniversalTime());
			return "converter";
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
			if (!HighLogic.LoadedSceneIsEditor)
			{
				canHarvest = CanHarvest();
			}
			if (heatModule != null)
			{
				systemHeatEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
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
