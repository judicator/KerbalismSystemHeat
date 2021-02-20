using System.Collections.Generic;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatFissionReactorKerbalismUpdater: PartModule
	{
		// This should correspond to the related ModuleSystemHeatFissionReactor
		[KSPField(isPersistant = true)]
		public string reactorModuleID;

		protected const string reactorModuleName = "ModuleSystemHeatFissionReactor";
		protected ModuleSystemHeatFissionReactor reactorModule;

		protected static bool resourcesListParsed = false;
		protected static List<ResourceRatio> inputs;
		protected static List<ResourceRatio> outputs;

		public virtual void Start()
		{
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
			{
				reactorModule = FindReactorModule(this.part, reactorModuleID);
				if (inputs == null || inputs.Count == 0)
				{
					ConfigNode node = ModuleUtils.GetModuleConfigNode(part, moduleName);
					if (node != null) {
						OnLoad(node);
					}
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			ParseResourcesList(this.part);
		}

		// Fetch input/output resources list from reactor ConfigNode
		protected static void ParseResourcesList(Part part)
		{
			if (!resourcesListParsed)
			{
				ConfigNode node = ModuleUtils.GetModuleConfigNode(part, reactorModuleName);
				if (node != null) {
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

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactorModuleSnapshot = FindReactorModuleSnapshot(part_snapshot, Proto.GetString(module_snapshot, "reactorModuleID"));
			if (reactorModuleSnapshot != null)
			{
				string title = "fission reactor";
				if (reactorModuleSnapshot.moduleName == "ModuleSystemHeatFissionEngine")
				{
					title = "fission engine";
				}

				if (Proto.GetBool(reactorModuleSnapshot, "Enabled"))
				{
					float curECGeneration = Proto.GetFloat(reactorModuleSnapshot, "CurrentElectricalGeneration");
					if (curECGeneration > 0)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", curECGeneration));
					}

					float fuelThrottle = Proto.GetFloat(reactorModuleSnapshot, "CurrentReactorThrottle") / 100f;
					if (fuelThrottle > 0)
					{
						ParseResourcesList(proto_part);
						foreach (ResourceRatio ratio in inputs)
						{
							resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, - fuelThrottle * ratio.Ratio));
						}
						foreach (ResourceRatio ratio in outputs)
						{
							resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, fuelThrottle * ratio.Ratio));
						}
					}
				}
				// Set LastUpdate to current time - this is done to prevent double resources consumption
				// Background resource consumption is already handled earlier in this method,
				// no need to do it again on craft activation in ModuleSystemHeatFissionReactor.DoCatchup()
				Proto.Set(reactorModuleSnapshot, "LastUpdateTime", Planetarium.GetUniversalTime());
				return title;
			}
			return "ERR: no reactor";
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (reactorModule != null)
			{
				string title = "fission reactor";
				if (reactorModule.moduleName == "ModuleSystemHeatFissionEngine")
				{
					title = "fission engine";
				}

				if (reactorModule.GeneratesElectricity)
				{
					float curECGeneration = reactorModule.ElectricalGeneration.Evaluate(reactorModule.CurrentReactorThrottle);
					if (curECGeneration > 0)
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", curECGeneration));
					}
				}

				float fuelThrottle = reactorModule.CurrentReactorThrottle / 100f;
				if (fuelThrottle > 0)
				{
					ParseResourcesList(this.part);
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
			return "ERR: no reactor";
		}

		// Find associated Reactor module
		public static ModuleSystemHeatFissionReactor FindReactorModule(Part part, string moduleName)
		{
			ModuleSystemHeatFissionReactor reactorModule = part.FindModulesImplementing<ModuleSystemHeatFissionReactor>().Find(x => x.moduleID == moduleName);
			if (reactorModule == null)
			{
				Utils.LogError($"[{part}] No ModuleSystemHeatFissionReactor named {moduleName} was found, using first instance");
				reactorModule = part.FindModuleImplementing<ModuleSystemHeatFissionReactor>();
			} 
			if (reactorModule == null)
			{
				Utils.LogError($"[{part}] No ModuleSystemHeatFissionReactor was found.");
			}
			return reactorModule;
		}

		// Find associated Reactor module snapshot (used for unloaded vessels as they only have Modules snapshots)
		protected static ProtoPartModuleSnapshot FindReactorModuleSnapshot(ProtoPartSnapshot part_snapshot, string moduleName)
		{
			ProtoPartModuleSnapshot reactorModuleSnapshot = null;
			for (int i = 0; i < part_snapshot.modules.Count; i++)
			{
				if (part_snapshot.modules[i].moduleName == reactorModuleName)
				{
					if (part_snapshot.modules[i].moduleValues.GetValue("moduleID") == moduleName)
					{
						reactorModuleSnapshot = part_snapshot.modules[i];
					}
				}
			}
			if (reactorModuleSnapshot == null)
			{
				reactorModuleSnapshot = part_snapshot.FindModule(reactorModuleName);
			}
			if (reactorModuleSnapshot == null)
			{
				Utils.LogError($"[{part_snapshot.partName}] No {reactorModuleName} was found in Part Snapshot.");
			}
			return reactorModuleSnapshot;
		}
    }
}
