using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace VEX.Patches
{
	[HarmonyPatch(typeof(Player), "Update")]
	internal static class DebugToggle
	{
		private static readonly MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");

		public static void Prefix(ref Player __instance)
		{
			if (__instance == Player.m_localPlayer && (bool)TakeInput.Invoke(__instance, new object[0]) && Input.GetKeyDown(Settings.debugToggle.Value.MainKey))
			{
				Commands.DebugToggle(out _);
			}
		}
	}
}
