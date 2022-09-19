//using HarmonyLib;

//namespace VEX.Patches
//{
//	[HarmonyPatch(typeof(TerrainModifier), "Awake")]
//	internal static class TerrainLight
//	{
//		public static void Prefix(ref TerrainModifier __instance)
//		{
//			if (Commands.debugTerrain && __instance.m_playerModifiction)
//			{
//				Commands.SetupLight(__instance);
//			}
//		}
//	}
//}
