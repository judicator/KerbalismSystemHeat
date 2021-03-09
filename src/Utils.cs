using UnityEngine;

namespace KerbalismSystemHeat
{
	public static class KSHUtils
	{
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
