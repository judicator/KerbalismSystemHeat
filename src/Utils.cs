using System;
using System.Reflection;
using UnityEngine;
using KERBALISM.Planner;

namespace KerbalismSystemHeat
{
	public static class KSHUtils
	{
		const double plannerUIUpdateDelay = 500.0;
		private static DateTime lastPlannerUIUpdate = DateTime.UtcNow;

		public static void Log(string msg)
		{
			Debug.Log("[KerbalismSystemHeat] " + msg);
		}

		public static void LogWarning(string msg)
		{
			Debug.LogWarning("[KerbalismSystemHeat] " + msg);
		}

		public static void LogError(string msg)
		{
			Debug.LogError("[KerbalismSystemHeat] " + msg);
		}

		public static void UpdateKerbalismPlannerUI()
		{
			DateTime timeStamp = DateTime.UtcNow;
			if ((timeStamp - lastPlannerUIUpdate).TotalMilliseconds >= plannerUIUpdateDelay)
			{
				lastPlannerUIUpdate = timeStamp;
				// Dirty hack - calling internal method from another assembly via reflection
				// Wish I could find another way to update Planner UI...
				// And no, onEditorShipModified event will not do the job, as it will also restart heat simulation process
				string className = typeof(Planner).AssemblyQualifiedName;
				ReflectionStaticCall(className, "RefreshPlanner");
			}
		}

		public static void ReflectionStaticCall(string ClassName, string MethodName)
		{
			var staticClass = Type.GetType(ClassName);
			if (staticClass != null)
			{
				try
				{
					staticClass.GetMethod(MethodName, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
				}
				catch (Exception ex)
				{
					LogError("Static class method " + ClassName + "." + MethodName + " reflection call failed. Exception: " + ex.Message + "\n" + ex.ToString());
				}
			}
		}

		public static bool VesselInClosedOrbit(Vessel v)
		{
			bool result = false;
			if (!double.IsNaN(v.orbit.eccentricity) && !double.IsNaN(v.orbit.ApA) && !double.IsNaN(v.orbit.PeA))
			{
				result = v.orbit.eccentricity < 1;
			}
			return result;
		}

		public static double SampleResourceAbundance(Vessel v, ModuleResourceHarvester harvester)
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
	}
}
