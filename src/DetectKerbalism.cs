using System.Reflection;
using UnityEngine;
using KSP.Localization;

namespace KerbalismSystemHeat
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class DetectKerbalism : MonoBehaviour
	{
		public void Start()
		{
			bool kerbalismFound = false;
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				// Kerbalism comes with more than one assembly. There is Kerbalism for debug builds, KerbalismBootLoader,
				// then there are Kerbalism18 or Kerbalism16_17 depending on the KSP version, and there might be ohter
				// assemblies like KerbalismContracts etc.
				// So look at the assembly name object instead of the assembly name (which is the file name and could be renamed).

				AssemblyName nameObject = new AssemblyName(a.assembly.FullName);
				string realName = nameObject.Name; // Will always return "Kerbalism" as defined in the AssemblyName property of the csproj

				if (realName.Equals("Kerbalism"))
				{
					kerbalismFound = true;
					break;
				}
			}
			if (!kerbalismFound)
			{
				PopupDialog dialog;
				dialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),
					new Vector2(0.5f, 0.5f),
					new MultiOptionDialog(
						"KerbalismSystemWarning",
						Localizer.Format("#LOC_KerbalismSystemHeat_KerbalismNotFound_Msg"),
						Localizer.Format("#LOC_KerbalismSystemHeat_KerbalismNotFound_Title"),
						HighLogic.UISkin,
						new Rect(0.5f, 0.5f, 500f, 60f),
						new DialogGUIFlexibleSpace(),
						new DialogGUIHorizontalLayout(
							new DialogGUIFlexibleSpace(),
		                    new DialogGUIButton(Localizer.Format("#LOC_KerbalistSystemHeat_Quit"), Application.Quit, 140.0f, 30.0f, true),
		                    new DialogGUIButton(Localizer.Format("#LOC_KerbalistSystemHeat_Continue"), delegate () { }, 140.0f, 30.0f, true),
							new DialogGUIFlexibleSpace()
						)
					),
					true,
					HighLogic.UISkin);
			}
			else
			{
				KerbalismAPI.KerbalismFound = true;
				KerbalismAPI.Init();
			}
		}
	}
}
