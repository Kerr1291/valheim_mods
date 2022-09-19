using System;
using VEX.Patches;
using UnityEngine;

namespace VEX
{
    // Token: 0x02000002 RID: 2
    internal class LightManager : MonoBehaviour
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public void Awake()
        {
            this.light = base.GetComponent<Light>();
        }

        // Token: 0x06000002 RID: 2 RVA: 0x00002060 File Offset: 0x00000260
        public void Update()
        {
            if (this.light == null || Player.m_localPlayer == null)
            {
                UnityEngine.Object.Destroy(this);
                return;
            }
            this.light.intensity = Settings.lightStrength.Value;
            this.light.enabled = (Commands.vexDebug && Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) <= Settings.lightDistance.Value);
        }

        // Token: 0x04000001 RID: 1
        private Light light;
    }
}
