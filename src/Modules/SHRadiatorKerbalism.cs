using System;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatRadiatorKerbalism: ModuleSystemHeatRadiator
	{
		[KSPField(isPersistant = true)]
		public float scale = 1f;

		[KSPField(isPersistant = true)]
		public float scaleEmissionPower = 2f;

		[KSPField(isPersistant = false)]
		public FloatCurve refTemperatureCurve = new FloatCurve();

		public static string radiatorTitle = Localizer.Format("#LOC_KerbalismSystemHeat_Radiator");

		public List<ModuleResource> inputResourcesClone;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			inputResourcesClone = resHandler.inputResources.ConvertAll(p => p);
			refTemperatureCurve.Load(node.GetNode("temperatureCurve"));
		}

		// Tweakscale support
		[KSPEvent]
		void OnPartScaleChanged(BaseEventDetails data)
		{
			scale = data.Get<float>("factorAbsolute");
			temperatureCurve = new FloatCurve();
			for (int i = 0; i < refTemperatureCurve.Curve.length; i++)
			{
				temperatureCurve.Add(refTemperatureCurve.Curve.keys[i].time, refTemperatureCurve.Curve.keys[i].value * (float) Math.Pow(scale, scaleEmissionPower));
			}
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
				resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate * Math.Pow(scale, scaleEmissionPower)));
			}
			return radiatorTitle;
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			if (Lib.Proto.GetBool(module_snapshot, "IsCooling"))
			{
				float scale = Lib.Proto.GetFloat(module_snapshot, "scale");
				float scaleEmissionPower = Lib.Proto.GetFloat(module_snapshot, "scaleECConsumptionPower");
				foreach (ModuleResource res in ((proto_part_module as SystemHeatRadiatorKerbalism).resHandler.inputResources))
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate * Math.Pow(scale, scaleEmissionPower)));
				}
			}
			return radiatorTitle;
		}

		// Simulate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsCooling)
			{
				foreach (ModuleResource res in resHandler.inputResources)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate * Math.Pow(scale, scaleEmissionPower)));
				}
			}
			return radiatorTitle;
		}

		public override void FixedUpdate()
		{
			// Temporary set input resources list to empty to prevent resources consumption in FixedUpdate
			// Input resources consumption is handled by ResourceUpdate
			resHandler.inputResources = new List<ModuleResource>();
			base.FixedUpdate();
			resHandler.inputResources = inputResourcesClone;
		}
	}
}
