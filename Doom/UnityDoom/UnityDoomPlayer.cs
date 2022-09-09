using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ManagedDoom;

namespace UnityDoom {
    public class UnityDoomPlayer : MonoBehaviour
    {
        public bool play = false;

        public string commandLineArgs = "";
        public string staticFileDirectory = "";
        public bool debugLogs = false;

        private ManagedDoom.Unity.UnityDoom doom;

        private void Update()
        {
            if (play) StartGame();
            if (play) RunGame();
            if (!play) CloseGame();
        }

        private void StartGame()
        {
            if (doom != null) return;
            
            ManagedDoom.Logger.Enabled = debugLogs;
            
            if (!string.IsNullOrEmpty(staticFileDirectory))
                ConfigUtilities.OverrideExeDirectory = staticFileDirectory;
            else
                ConfigUtilities.OverrideExeDirectory = Application.streamingAssetsPath;

            string[] args = commandLineArgs.Split(' ');

            doom = new ManagedDoom.Unity.UnityDoom(new CommandLineArgs(args), transform);
        }

        private void RunGame()
        {
            if (doom == null) return;
            play = doom.Run();
        }

        private void CloseGame()
        {
            if (doom != null) return;
            play = false;
            doom.Dispose();
            doom = null;
        }
    }
}
