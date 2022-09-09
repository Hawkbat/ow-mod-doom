using ManagedDoom;
using ManagedDoom.Unity;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace Doom
{
    public class Doom : ModBehaviour
    {
        public static Doom Instance;

        public UnityDoom DoomGame;

        DoomShipLogMode doomMode;
        ShipLogController shipLog;
        ScreenPrompt doomPrompt;

        private void Start()
        {
            Instance = this;

            ConfigUtilities.OverrideExeDirectory = ModHelper.Manifest.ModFolderPath;

            GlobalMessenger.AddListener("EnterShipComputer", () =>
            {
                if (DoomGame == null) return;
                DoomGame.Visible = false;
            });

            GlobalMessenger.AddListener("ExitShipComputer", () =>
            {
                if (DoomGame == null) return;
                DoomGame.Visible = true;
                DoomGame.AllowInput = false;
            });

            LoadManager.OnCompleteSceneLoad += LoadManager_OnCompleteSceneLoad;
            ModHelper.Events.Player.OnPlayerAwake += Player_OnPlayerAwake;
        }

        private void LoadManager_OnCompleteSceneLoad(OWScene originalScene, OWScene loadScene)
        {
            DoomGame = null;
        }

        private void Player_OnPlayerAwake(PlayerBody obj)
        {
            ModHelper.Events.Unity.RunWhen(() => !!Locator.GetShipBody(), () =>
            {
                var refTransform = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogCanvas").transform;

                shipLog = refTransform.gameObject.GetComponentInParent<ShipLogController>();

                var go = new GameObject("ShipLogDoom");
                doomMode = go.AddComponent<DoomShipLogMode>();
                go.transform.SetParent(refTransform.parent, false);
                go.transform.localPosition = refTransform.localPosition;
                go.transform.localRotation = refTransform.localRotation;
                go.transform.localScale = new Vector3(1f, 0.85f, 1f);
                go.transform.position += -go.transform.forward * 0.001f;
                go.layer = LayerMask.NameToLayer("UI");

                doomPrompt = new ScreenPrompt(InputLibrary.markEntryOnHUD, "DOOM", 0, ScreenPrompt.DisplayState.Normal, false);
                Locator.GetPromptManager().AddScreenPrompt(doomPrompt, shipLog._upperRightPromptList, TextAnchor.MiddleRight, -1, true);

                DoomGame = new UnityDoom(new CommandLineArgs(new string[0]), go.transform);

                doomMode.Initialize(null, null, null);
            });
        }

        private void Update()
        {
            if (DoomGame != null) DoomGame.Run(Time.unscaledDeltaTime);
            if (shipLog && OWInput.GetInputMode() == InputMode.ShipComputer)
            {
                doomPrompt.SetVisibility(shipLog._currentMode == shipLog._mapMode);
                if (doomMode && shipLog._currentMode == shipLog._mapMode && OWInput.IsPressed(InputLibrary.markEntryOnHUD, InputMode.All))
                {
                    shipLog._currentMode.ExitMode();
                    shipLog._currentMode = doomMode;
                    shipLog._currentMode.EnterMode();
                }
            }
        }

        public void Focus()
        {
            if (DoomGame == null) return;
            DoomGame.Visible = true;
            DoomGame.AllowInput = true;
        }

        public void Unfocus()
        {
            if (DoomGame == null) return;
            DoomGame.Visible = false;
            DoomGame.AllowInput = false;
        }
    }
}
