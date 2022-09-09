using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagedDoom;
using UnityEngine;

namespace Doom
{
    public class DoomShipLogMode : ShipLogMode
    {
        public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
        {

        }

        public override bool AllowCancelInput()
        {
            return true;
        }

        public override bool AllowModeSwap()
        {
            return true;
        }

        public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
        {
            Doom.Instance.Focus();
        }

        public override void ExitMode()
        {
            Doom.Instance.Unfocus();
        }

        public override string GetFocusedEntryID()
        {
            return "";
        }

        public override void OnEnterComputer()
        {

        }

        public override void OnExitComputer()
        {

        }

        public override void UpdateMode()
        {

        }
    }
}
