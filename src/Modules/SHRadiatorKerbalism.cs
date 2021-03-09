using System.Collections.Generic;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatRadiatorKerbalism: ModuleSystemHeatRadiator
	{
		public List<ModuleResource> inputResourcesClone;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			inputResourcesClone = resHandler.inputResources.ConvertAll(p => p);
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			// note: IsCooling is not valid in the editor, for deployable radiators,
			// we will have to check if the related deploy module is deployed
			// we use PlannerController instead
			foreach (ModuleResource res in resHandler.inputResources)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate));
			}
			return "radiator";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			if (Lib.Proto.GetBool(module_snapshot, "IsCooling"))
			{
				foreach (ModuleResource res in ((proto_part_module as SystemHeatRadiatorKerbalism).resHandler.inputResources))
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate));
				}
			}
			return "radiator";
		}

		// Simulate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsCooling)
			{
				foreach (ModuleResource res in resHandler.inputResources)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate));
				}
			}
			return "radiator";
		}

        public override void FixedUpdate()
        {
			resHandler.inputResources = new List<ModuleResource>();
			base.FixedUpdate();
			resHandler.inputResources = inputResourcesClone;
        }
    }
}
