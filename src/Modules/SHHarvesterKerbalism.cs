using System.Collections.Generic;
using SystemHeat;
using KSP.Localization;

namespace KerbalismSystemHeat
{
	public class SystemHeatHarvesterKerbalism : ModuleSystemHeatHarvester
	{
		// In the editor, allow the user to tweak the abundance for simulation purposes
		[UI_FloatRange(scene = UI_Scene.Editor, minValue = 0f, maxValue = 1f, stepIncrement = 0.01f)]
		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Anticipated resource abundance")]
		public float simulatedAbundance = 0.1f;

		[KSPField(isPersistant = true)]
		public float lastCalculatedBGAbundance = -1f;

		[KSPField(isPersistant = true)]
		public bool canHarvest = false;

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (heatModule != null)
			{
				float shEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
				foreach (ResourceRatio res in inputList)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
				}
				if (simulatedAbundance >= HarvestThreshold)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(ResourceName, Efficiency * shEfficiency * simulatedAbundance));
				}
			}
			return "harvester";
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsActivated && heatModule != null && canHarvest)
			{
				double abundance = SampleAbundance(vessel, this);
				if (abundance >= HarvestThreshold)
                {
					float shEfficiency = systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
					foreach (ResourceRatio res in inputList)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
					}
					resourceChangeRequest.Add(new KeyValuePair<string, double>(ResourceName, Efficiency * shEfficiency * abundance));
				}
			}
			return "harvester";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			// if active
			if (Proto.GetBool(module_snapshot, "IsActivated"))
			{
				// determine if vessel is full of all output resources
				// note: comparing against previous amount
				bool full = true;
				foreach (var or in (proto_part_module as SystemHeatHarvesterKerbalism).outputList)
				{
					double resLevel = KerbalismAPI.ResourceAmount(v, or.ResourceName);
					full &= (resLevel >= (proto_part_module as SystemHeatHarvesterKerbalism).FillAmount - double.Epsilon);
				}

				// if not full
				if (!full && Proto.GetBool(module_snapshot, "canHarvest"))
				{
					bool isExosphereHarvester = Proto.GetInt(module_snapshot, "HarvesterType") == 3;
					double abundance = Proto.GetFloat(module_snapshot, "lastCalculatedBGAbundance");
					if (abundance == -1f || isExosphereHarvester)
                    {
						abundance = SampleAbundance(v, proto_part_module as SystemHeatHarvesterKerbalism);
						Proto.Set(module_snapshot, "lastCalculatedBGAbundance", abundance);
					}
					if (abundance >= Proto.GetFloat(module_snapshot, "HarvestThreshold"))
					{
						ProtoPartModuleSnapshot sh = FindSystemHeatModuleSnapshot(part_snapshot, (proto_part_module as SystemHeatConverterKerbalism).systemHeatModuleID);
						string resName = Proto.GetString(module_snapshot, "ResourceName");
						float Efficiency = Proto.GetFloat(module_snapshot, "Efficiency");
						if (sh != null)
						{
							float shEfficiency = (proto_part_module as SystemHeatConverterKerbalism).systemEfficiency.Evaluate(Proto.GetFloat(sh, "currentLoopTemperature"));
							foreach (var res in (proto_part_module as SystemHeatConverterKerbalism).inputList)
							{
								resourceChangeRequest.Add(new KeyValuePair<string, double>(res.ResourceName, -res.Ratio));
							}
							resourceChangeRequest.Add(new KeyValuePair<string, double>(resName, Efficiency * shEfficiency * abundance));
						}
					}
				}

				// undo stock behavior by forcing last_update_time to now
				Proto.Set(module_snapshot, "lastUpdateTime", Planetarium.GetUniversalTime());
			}
			return "converter";
		}

		public override void FixedUpdate()
		{
			if (heatModule != null)
			{
				if (HighLogic.LoadedSceneIsFlight)
				{
					GenerateHeatFlight();
					UpdateSystemHeatFlight();
				}
				if (HighLogic.LoadedSceneIsEditor)
				{
					GenerateHeatEditor();
					Fields["HarvesterEfficiency"].guiActiveEditor = editorThermalSim;
				}
				HarvesterEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Field_Efficiency_Value", (systemEfficiency.Evaluate(heatModule.currentLoopTemperature) * 100f).ToString("F1"));
			}
			lastCalculatedBGAbundance = -1f;
			canHarvest = CanHarvest();
		}

		private static double SampleAbundance(Vessel v, SystemHeatHarvesterKerbalism harvester)
		{
			// get abundance
			AbundanceRequest request = new AbundanceRequest
			{
				ResourceType = (HarvestTypes) harvester.HarvesterType,
				ResourceName = harvester.ResourceName,
				BodyId = v.mainBody.flightGlobalsIndex,
				Latitude = v.latitude,
				Longitude = v.longitude,
				Altitude = v.altitude,
				CheckForLock = false
			};
			return ResourceMap.Instance.GetAbundance(request);
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
