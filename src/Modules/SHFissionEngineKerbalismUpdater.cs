using System;
using System.Collections.Generic;
using System.Linq;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatFissionEngineKerbalismUpdater : PartModule
	{
		// This should correspond to the related ModuleSystemHeatFissionEngine
		[KSPField(isPersistant = true)]
		public string engineModuleID;

		protected const string engineModuleName = "ModuleSystemHeatFissionEngine";
		protected ModuleSystemHeatFissionEngine engineModule;

		protected bool resourcesListParsed = false;
		protected List<ResourceRatio> inputs;
		protected List<ResourceRatio> outputs;

		public virtual void Start()
		{
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
			{
				engineModule = FindEngineModule(part, engineModuleID);
				if (inputs == null || inputs.Count == 0)
				{
					ConfigNode node = ModuleUtils.GetModuleConfigNode(part, moduleName);
					if (node != null)
					{
						OnLoad(node);
					}
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			ParseResourcesList(part);
		}

		// Fetch input/output resources list from engine ConfigNode
		protected void ParseResourcesList(Part part)
		{
			if (!resourcesListParsed)
			{
				ConfigNode node = ModuleUtils.GetModuleConfigNode(part, engineModuleName);
				if (node != null)
				{
					// Load resource nodes
					ConfigNode[] inNodes = node.GetNodes("INPUT_RESOURCE");
					inputs = new List<ResourceRatio>();
					for (int i = 0; i < inNodes.Length; i++)
					{
						ResourceRatio p = new ResourceRatio();
						p.Load(inNodes[i]);
						inputs.Add(p);
					}
					ConfigNode[] outNodes = node.GetNodes("OUTPUT_RESOURCE");
					outputs = new List<ResourceRatio>();
					for (int i = 0; i < outNodes.Length; i++)
					{
						ResourceRatio p = new ResourceRatio();
						p.Load(outNodes[i]);
						outputs.Add(p);
					}
				}
				resourcesListParsed = true;
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (engineModule != null)
			{
				string title = "fission engine";

				float curECGeneration = engineModule.ElectricalGeneration.Evaluate(engineModule.CurrentReactorThrottle);
				if (curECGeneration > 0)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", curECGeneration));
				}

				float fuelThrottle = engineModule.CurrentReactorThrottle / 100f;
				if (fuelThrottle > 0)
				{
					ParseResourcesList(part);
					foreach (ResourceRatio ratio in inputs)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, -fuelThrottle * ratio.Ratio));
					}
					foreach (ResourceRatio ratio in outputs)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, fuelThrottle * ratio.Ratio));
					}
				}
				return title;
			}
			return "ERR: no engine";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot engineModuleSnapshot = FindEngineModuleSnapshot(part_snapshot, Proto.GetString(module_snapshot, "engineModuleID"));
			if (engineModuleSnapshot != null)
			{
				string title = "fission engine";

				if (Proto.GetBool(engineModuleSnapshot, "Enabled"))
				{
					float curECGeneration = Proto.GetFloat(engineModuleSnapshot, "CurrentElectricalGeneration");
					float fuelThrottle = Proto.GetFloat(engineModuleSnapshot, "CurrentReactorThrottle") / 100f;
					// Resources generation/consumption according to engine throttle parameter
					if (curECGeneration > 0)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", curECGeneration));
					}
					if (fuelThrottle > 0)
					{
						try
						{
							(proto_part_module as SystemHeatFissionEngineKerbalismUpdater).ParseResourcesList(proto_part);
							foreach (ResourceRatio ratio in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).inputs)
							{
								resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, -fuelThrottle * ratio.Ratio));
							}
							foreach (ResourceRatio ratio in (proto_part_module as SystemHeatFissionEngineKerbalismUpdater).outputs)
							{
								resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, fuelThrottle * ratio.Ratio));
							}
						}
						catch (Exception e)
						{
							Utils.LogError($"[{proto_part}] Cannot parse ModuleSystemHeatFissionEngine resource list: {e}.");
						}
					}
				}
				// Set LastUpdate to current time - this is done to prevent double resources consumption
				// Background resource consumption is already handled earlier in this method,
				// no need to do it again on craft activation in ModuleSystemHeatFissionEngine.DoCatchup()
				Proto.Set(engineModuleSnapshot, "LastUpdateTime", Planetarium.GetUniversalTime());
				return title;
			}
			return "ERR: no engine";
		}

		// Find associated Engine module
		public ModuleSystemHeatFissionEngine FindEngineModule(Part part, string moduleName)
		{
			ModuleSystemHeatFissionEngine engine = part.GetComponents<ModuleSystemHeatFissionEngine>().ToList().Find(x => x.moduleID == moduleName);

			if (engine == null)
			{
				Utils.LogError($"[{part}] No ModuleSystemHeatFissionEngine named {moduleName} was found, using first instance");
				engineModule = part.GetComponents<ModuleSystemHeatFissionEngine>().ToList().First();
			}
			if (engine == null)
			{
				Utils.LogError($"[{part}] No ModuleSystemHeatFissionEngine was found.");
			}
			return engine;
		}

		// Find associated Engine module snapshot (used for unloaded vessels as they only have Modules snapshots)
		protected static ProtoPartModuleSnapshot FindEngineModuleSnapshot(ProtoPartSnapshot part_snapshot, string moduleName)
		{
			ProtoPartModuleSnapshot engineModuleSnapshot = null;
			for (int i = 0; i < part_snapshot.modules.Count; i++)
			{
				if (part_snapshot.modules[i].moduleName == engineModuleName)
				{
					if (part_snapshot.modules[i].moduleValues.GetValue("moduleID") == moduleName)
					{
						engineModuleSnapshot = part_snapshot.modules[i];
					}
				}
			}
			if (engineModuleSnapshot == null)
			{
				engineModuleSnapshot = part_snapshot.FindModule(engineModuleName);
			}
			if (engineModuleSnapshot == null)
			{
				Utils.LogError($"[{part_snapshot.partName}] No {engineModuleName} was found in Part Snapshot.");
			}
			return engineModuleSnapshot;
		}
	}
}
