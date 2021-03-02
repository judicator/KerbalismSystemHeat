using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public class SystemHeatRadiatorKerbalism: ModuleSystemHeatRadiator
	{
		public override void Start()
		{
			base.Start();
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
			if (Proto.GetBool(module_snapshot, "IsCooling"))
			{
				try
				{
					foreach (ModuleResource res in (proto_part_module.resHandler.inputResources))
					{
						resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate));
					}
				}
				catch (Exception e)
				{
					Utils.LogError($"[{proto_part}] Cannot parse SystemHeatRadiatorKerbalism prefab resource list: {e}.");
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
			if (heatModule != null)
			{
				//Utils.Log($"{0} {temperatureCurve.Evaluate(0f)}\n{350f} {temperatureCurve.Evaluate(350f)}\n{1000} {temperatureCurve.Evaluate(1000f)}\n");
				if (HighLogic.LoadedSceneIsFlight)
				{
					if (base.IsCooling)
					{
						float flux = -temperatureCurve.Evaluate(heatModule.LoopTemperature);
						if (heatModule.LoopTemperature >= heatModule.nominalLoopTemperature)
							heatModule.AddFlux(moduleID, 0f, flux);
						else
							heatModule.AddFlux(moduleID, 0f, 0f);
						RadiatorEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Running", (-flux / temperatureCurve.Evaluate(temperatureCurve.Curve.keys[temperatureCurve.Curve.keys.Length - 1].time) * 100f).ToString("F0"));

						if (scalarModule != null)
						{
							scalarModule.SetScalar(Mathf.MoveTowards(scalarModule.GetScalar, Mathf.Clamp01((heatModule.currentLoopTemperature - draperPoint) / maxTempAnimation), TimeWarp.fixedDeltaTime * heatAnimationRate));
						}
					}
					else
					{
						heatModule.AddFlux(moduleID, 0f, 0f);
						RadiatorEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Offline");
						if (scalarModule != null)
							scalarModule.SetScalar(Mathf.MoveTowards(scalarModule.GetScalar, 0f, TimeWarp.fixedDeltaTime * heatAnimationRate));
					}
				}
				if (HighLogic.LoadedSceneIsEditor)
				{
					float flux = -1.0f * temperatureCurve.Evaluate(heatModule.LoopTemperature);
					if (heatModule.LoopTemperature >= heatModule.nominalLoopTemperature)
						heatModule.AddFlux(moduleID, 0f, flux);
					else
						heatModule.AddFlux(moduleID, 0f, 0f);
					//Utils.Log($"BLAH {heatModule.LoopTemperature} {flux} {temperatureCurve.Evaluate(heatModule.LoopTemperature)}");
					RadiatorEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Running", ((-temperatureCurve.Evaluate(heatModule.LoopTemperature) / temperatureCurve.Evaluate(temperatureCurve.Curve.keys[temperatureCurve.Curve.keys.Length - 1].time)) * 100f).ToString("F0"));
				}
			}
		}
	}
}
