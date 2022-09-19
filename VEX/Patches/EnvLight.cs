using HarmonyLib;
using UnityEngine;

namespace VEX.Patches
{
	[HarmonyPatch(typeof(EnvMan), "SetEnv")]
	internal static class EnvLight
	{
		private static int _SunFogColor = Shader.PropertyToID("_SunFogColor");
		private static int _AmbientColor = Shader.PropertyToID("_AmbientColor");

		public static void Postfix()
		{
			if (Commands.vexDebug)
			{
				RenderSettings.ambientLight = Color.black;
				RenderSettings.fogColor = Color.black;
				Shader.SetGlobalColor(_SunFogColor, Color.black);
				Shader.SetGlobalColor(_AmbientColor, Color.black);
			}
		}
	}
}
