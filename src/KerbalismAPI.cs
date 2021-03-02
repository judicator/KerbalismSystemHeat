using System;

namespace KerbalismSystemHeat
{
	public static class KerbalismAPI
	{
		public static bool KerbalismFound = false;
		private static bool initialized = false;

		// delegate for the following API method (return value -> use Func) :
		// public static double ResourceAmount(Vessel v, string resource_name)
		public static Func<Vessel, string, double> ResourceAmount;

		// You will need to call this method only once
		public static void Init()
		{
			if (KerbalismFound)
			{
				if (!initialized)
				{
					Type apiType = Type.GetType("KERBALISM.API");
					ResourceAmount = (Func<Vessel, string, double>)Delegate.CreateDelegate(typeof(Func<Vessel, string, double>), apiType.GetMethod("ResourceAmount"));
					initialized = true;
				}
			}
		}
	}
}
