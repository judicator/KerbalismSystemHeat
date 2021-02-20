using System;
using System.Collections.Generic;

namespace KerbalismSystemHeat
{
	// Copy/paste from Kerbalism/Lib.cs
	#region PROTO
	public static class Proto
	{
		public static bool GetBool( ProtoPartModuleSnapshot m, string name, bool def_value = false )
		{
			bool v;
			string s = m.moduleValues.GetValue( name );
			return s != null && bool.TryParse( s, out v ) ? v : def_value;
		}

		public static float GetFloat( ProtoPartModuleSnapshot m, string name, float def_value = 0.0f )
		{
			// note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
			float v;
			string s = m.moduleValues.GetValue( name );
			return s != null && float.TryParse( s, out v ) && !float.IsNaN( v ) && !float.IsInfinity( v ) ? v : def_value;
		}

		public static string GetString( ProtoPartModuleSnapshot m, string name, string def_value = "" )
		{
			string s = m.moduleValues.GetValue( name );
			return s ?? def_value;
		}

		///<summary>set a value in a proto module</summary>
		public static void Set<T>( ProtoPartModuleSnapshot module, string value_name, T value )
		{
			module.moduleValues.SetValue( value_name, value.ToString(), true );
		}
	}
	#endregion
}
