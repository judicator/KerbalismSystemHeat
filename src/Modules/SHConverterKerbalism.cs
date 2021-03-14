using System;
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

		private DateTime lastPlannerUIUpdate = DateTime.UtcNow;

		[KSPEvent(guiActive = false, guiName = "Toggle", guiActiveEditor = true, active = true)]
		public new void ToggleEditorThermalSim()
		{
			editorThermalSim = !editorThermalSim;
			// Update Kerbalism planner UI
			if (Lib.IsEditor())	GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}

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
			if (heatModule != null)
			{
				systemHeatEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (heatModule != null && editorThermalSim)
			{
				foreach (ResourceRatio res in inputList)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
				}
				foreach (ResourceRatio res in outputList)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, res.Ratio * systemHeatEfficiency));
				}
			}
			return "converter";
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if ((heatModule != null) && IsActivated)
			{
				ResUpdate(vessel, inputList, outputList, systemHeatEfficiency, TimeWarp.fixedDeltaTime);
			}
			return "converter";
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
				ResUpdate(v,
					(proto_part_module as SystemHeatConverterKerbalism).inputList,
					(proto_part_module as SystemHeatConverterKerbalism).outputList,
					SHEfficiency,
					elapsed_s
					);
			}
			// undo stock behavior by forcing last_update_time to now
			Lib.Proto.Set(module_snapshot, "lastUpdateTime", Planetarium.GetUniversalTime());
			return "converter";
		}

		private static void ResUpdate(Vessel v, List<ResourceRatio> inputList, List<ResourceRatio> outputList, float systemHeatEfficiency, double elapsed_s)
		{
			ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate("SHConverter", KERBALISM.ResourceBroker.BrokerCategory.Converter, "converter"));
			foreach (ResourceRatio ir in inputList)
			{
				recipe.AddInput(ir.ResourceName, ir.Ratio * elapsed_s);
			}
			foreach (ResourceRatio or in outputList)
			{
				recipe.AddOutput(or.ResourceName, or.Ratio * systemHeatEfficiency * elapsed_s, or.DumpExcess);
			}
			KERBALISM.ResourceCache.Get(v).AddRecipe(recipe);
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
			DateTime timeStamp = DateTime.UtcNow;
			// Update Kerbalism planner UI twice a second if thermal simulation is on
			if (Lib.IsEditor() && editorThermalSim && (timeStamp - lastPlannerUIUpdate).TotalMilliseconds >= 500.0)
			{
				lastPlannerUIUpdate = timeStamp;
				UI.Update(Kerbalism.Callbacks.visible);
				UI.On_gui(Kerbalism.Callbacks.visible);
			}
		}
	}
}
