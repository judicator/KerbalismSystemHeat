using System.Collections.Generic;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatConverterKerbalism: ModuleSystemHeatConverter
	{
		[KSPField(isPersistant = true)]
		public float systemHeatEfficiency = 0f;

		public List<ResourceRatio> inputListClone;
		public List<ResourceRatio> outputListClone;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			inputListClone = new List<ResourceRatio>();
			foreach (ResourceRatio res in inputList)
            {
				inputListClone.Add(res);
			}
			outputListClone = new List<ResourceRatio>();
			foreach (ResourceRatio res in outputList)
			{
				outputListClone.Add(res);
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (heatModule != null)
			{
				ResUpdate(resourceChangeRequest);
			}
			return "converter";
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if ((heatModule != null) && IsActivated)
			{
				ResUpdate(resourceChangeRequest);
			}
			return "converter";
		}

		private void ResUpdate(List<KeyValuePair<string, double>> resourceChangeRequest)
        {
			float efficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
			foreach (ResourceRatio res in inputList)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
			}
			foreach (ResourceRatio res in outputList)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, res.Ratio * efficiency));
			}
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			// note: ignore stock temperature mechanic of converters, but calculate effiency based on last calculated SystemHeat loop temperature
			// note: ignore auto shutdown
			// note: non-mandatory resources 'dynamically scale the ratios', that is exactly what mandatory resources do too (DERP ALERT)
			// note: 'undo' stock behavior by forcing lastUpdateTime to now (to minimize overlapping calculations from this and stock post-facto simulation)

			// if active
			if (Lib.Proto.GetBool(module_snapshot, "IsActivated"))
			{
				float SHEfficiency = Lib.Proto.GetFloat(module_snapshot, "systemHeatEfficiency");

				foreach (ResourceRatio res in (proto_part_module as SystemHeatConverterKerbalism).inputList)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
				}
				foreach (ResourceRatio res in (proto_part_module as SystemHeatConverterKerbalism).outputList)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, res.Ratio * SHEfficiency));
				}
			}
			// undo stock behavior by forcing last_update_time to now
			Lib.Proto.Set(module_snapshot, "lastUpdateTime", Planetarium.GetUniversalTime());
			return "converter";
		}

		public override void FixedUpdate()
		{
			inputList = new List<ResourceRatio>();
			outputList = new List<ResourceRatio>();
			base.FixedUpdate();
			inputList = inputListClone;
			outputList = outputListClone;
			if (heatModule != null)
			{
				systemHeatEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
			}
		}
	}
}
