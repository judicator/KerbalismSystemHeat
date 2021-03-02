using System.Collections.Generic;
using KSP.Localization;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatConverterKerbalism: ModuleSystemHeatConverter
	{
		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (heatModule != null)
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
			return "converter";
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsActivated)
			{
				bool full = true;
				foreach (var or in outputList)
				{
					double orAmount;
					if (availableResources.TryGetValue(or.ResourceName, out orAmount))
					{
						full &= (orAmount >= FillAmount - double.Epsilon);
					}
				}
				if (!full && heatModule != null)
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
			if (Proto.GetBool(module_snapshot, "IsActivated"))
			{
				// determine if vessel is full of all output resources
				// note: comparing against previous amount
				bool full = true;
				foreach (var or in (proto_part_module as SystemHeatConverterKerbalism).outputList)
				{
					double resLevel = KerbalismAPI.ResourceAmount(v, or.ResourceName);
					full &= (resLevel >= (proto_part_module as SystemHeatConverterKerbalism).FillAmount - double.Epsilon);
				}

				// if not full
				if (!full)
				{
					ProtoPartModuleSnapshot sh = FindSystemHeatModuleSnapshot(part_snapshot, (proto_part_module as SystemHeatConverterKerbalism).systemHeatModuleID);
					if (sh != null)
					{
						float efficiency = (proto_part_module as SystemHeatConverterKerbalism).systemEfficiency.Evaluate(Proto.GetFloat(sh, "currentLoopTemperature"));
						foreach (var res in (proto_part_module as SystemHeatConverterKerbalism).inputList)
						{
							resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
						}
						foreach (var res in (proto_part_module as SystemHeatConverterKerbalism).outputList)
						{
							resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, res.Ratio * efficiency));
						}
					}
				}

				// undo stock behavior by forcing last_update_time to now
				Proto.Set(module_snapshot, "lastUpdateTime", Planetarium.GetUniversalTime());
			}
			return "converter";
		}

		// Do all the same things as in base class, but do not call stock ModuleResourceConverter.FixedUpdate
		public override void FixedUpdate()
		{
			if (heatModule != null)
			{
				if (HighLogic.LoadedSceneIsFlight)
				{
					GenerateHeatFlight();
					UpdateSystemHeatFlight();

					Fields["ConverterEfficiency"].guiActive = base.ModuleIsActive();
				}
				if (HighLogic.LoadedSceneIsEditor)
				{
					GenerateHeatEditor();

					Fields["ConverterEfficiency"].guiActiveEditor = editorThermalSim;

				}
				ConverterEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency_Value", (systemEfficiency.Evaluate(heatModule.currentLoopTemperature) * 100f).ToString("F1"));
			}
		}

		// Find associated ModuleSystemHeat module snapshot (used for unloaded vessels as they only have Modules snapshots)
		protected static ProtoPartModuleSnapshot FindSystemHeatModuleSnapshot(ProtoPartSnapshot part_snapshot, string moduleName)
		{
			ProtoPartModuleSnapshot SHModuleSnapshot = null;
			for (int i = 0; i < part_snapshot.modules.Count; i++)
			{
				if (part_snapshot.modules[i].moduleName == "ModuleSystemHeat")
				{
					if (part_snapshot.modules[i].moduleValues.GetValue("moduleID") == moduleName)
					{
						SHModuleSnapshot = part_snapshot.modules[i];
					}
				}
			}
			if (SHModuleSnapshot == null)
			{
				Utils.LogError($"[{part_snapshot.partName}] No ModuleSystemHeat named {moduleName} was found, using first instance");
				SHModuleSnapshot = part_snapshot.FindModule("ModuleSystemHeat");
			}
			if (SHModuleSnapshot == null)
			{
				Utils.LogError($"[{part_snapshot.partName}] No ModuleSystemHeat was found in Part Snapshot.");
			}
			return SHModuleSnapshot;
		}
    }
}
