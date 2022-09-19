using System;
using UnityEngine;

namespace VEX
{
    // Token: 0x02000003 RID: 3
    internal class RestoreData
    {
        // Token: 0x06000004 RID: 4 RVA: 0x000020EF File Offset: 0x000002EF
        public RestoreData(Vector3 position, GameObject prefab, long creationTime)
        {
            this.position = position;
            this.prefab = prefab;
            this.creationTime = creationTime;
        }

        // Token: 0x04000002 RID: 2
        public readonly Vector3 position;

        // Token: 0x04000003 RID: 3
        public readonly GameObject prefab;

        // Token: 0x04000004 RID: 4
        public readonly long creationTime;
    }
}
