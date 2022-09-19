using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib.Tools;
using System.Reflection;
using System.Runtime.InteropServices;
using Steamworks;

namespace VEX.Patches
{
    public class NPZ : MonoBehaviour
    {
        public static bool MOD_ENABLED = false;

        public Character m_character;
        public ZNetView m_nview;
        public int m_activeAreaRadius = 2;

        public Vector2i sector {
            get {
                return m_nview.GetZDO().m_sector;
            }
        }

        public Vector3 pos {
            get {
                return m_nview.GetZDO().m_position;
            }
        }

        public bool IsValid()
        {
            return m_nview.GetZDO() != null && m_nview.GetZDO().m_owner != 0L;
        }

        public bool IsStatic()
        {
            return m_character == null;
        }

        protected virtual void Awake()
        {
            NPZ.npzs.Add(this);
            this.m_character = base.GetComponent<Character>();
            this.m_nview = this.GetComponent<ZNetView>();
        }

        protected virtual void OnDestroy()
        {
            NPZ.npzs.Remove(this);
        }

        public static List<NPZ> npzs = new List<NPZ>();

        //TODO: probably should cache this...
        public static Dictionary<int,Dictionary<int,int>> zones
        {
            get {
                Dictionary<int, Dictionary<int, int>> zones = new Dictionary<int, Dictionary<int, int>>();

                foreach (var npz in npzs)
                {
                    if (!npz.IsValid())
                        continue;

                    Vector2i zone = npz.sector;

                    if(!zones.ContainsKey(zone.x))
                    {
                        zones[zone.x] = new Dictionary<int, int>();
                    }

                    if (zones[zone.x].ContainsKey(zone.y))
                    {
                        zones[zone.x][zone.y] = Math.Max(zones[zone.x][zone.y], npz.m_activeAreaRadius);
                    }
                    else
                    {
                        zones[zone.x].Add(zone.y, npz.m_activeAreaRadius);
                    }
                }

                return zones;
            }
        }
    }
}
