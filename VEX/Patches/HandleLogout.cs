using HarmonyLib;

namespace VEX.Patches
{
	[HarmonyPatch(typeof(Game), "Logout")]
	internal static class HandleLogout
	{
		public static void Prefix()
		{
			if (Commands.vexDebug)
			{
				Commands.DebugToggle(out _);
			}
		}
	}
}
