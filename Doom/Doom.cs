using OWML.Common;
using OWML.ModHelper;
using UnityDoom;
using UnityEngine;

namespace Doom
{
    public class Doom : ModBehaviour
    {
        private void Start()
        {
            ModHelper.Events.Player.OnPlayerAwake += Player_OnPlayerAwake;
        }

        private void Player_OnPlayerAwake(PlayerBody obj)
        {
            ModHelper.Events.Unity.RunWhen(() => !!Locator.GetShipBody(), () =>
            {
                var refTransform = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogSplashScreen").transform;
                var go = new GameObject("ShipLogDoom");
                go.transform.SetParent(refTransform.parent, false);
                go.transform.localPosition = refTransform.localPosition;
                go.transform.localRotation = refTransform.localRotation;
                go.transform.localScale = new Vector3(1f, 0.85f, 1f);
                go.transform.position += -go.transform.forward * 0.001f;
                var doom = go.AddComponent<UnityDoomPlayer>();
                doom.staticFileDirectory = ModHelper.Manifest.ModFolderPath;
                doom.play = true;
            });
        }
    }
}
