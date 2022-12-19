using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oku
{
    public static class OkuConstDefs
    {
        public const string AssetBundleName = "com.oku.utils";

        public const string PilotPrefabBundlePath = "assets/oku/prefabs/pilot.prefab";

        //
        //var okuAb = AssetBundle.GetAllLoadedAssetBundles()
        //    .First(ab => ab.name.Equals(OkuConstDefs.AssetBundleName));
        //var pilotPrefab = okuAb.LoadAsset<GameObject>(OkuConstDefs.PilotPrefabBundlePath);
        private static AssetBundle _okuAb;
        public static AssetBundle GetCoreAssetBundle()
        {
            if (_okuAb == null)
            {
                _okuAb = AssetBundle.GetAllLoadedAssetBundles()
                .First(ab => ab.name.Equals(AssetBundleName));
            }

            return _okuAb;
        }

        private static GameObject _pilotPrefab;

        public static GameObject GetPilotPrefab()
        {
            if (_pilotPrefab == null)
            {
                _pilotPrefab = GetCoreAssetBundle().LoadAsset<GameObject>(PilotPrefabBundlePath);
            }

            return _pilotPrefab;
        }
    }
}
