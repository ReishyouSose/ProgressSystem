﻿using ProgressSystem.Common.Configs;
using ProgressSystem.Common.Systems;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.UI.DeveloperMode.AchEditor;
using ProgressSystem.UI.PlayerMode;
using Terraria.GameInput;

namespace ProgressSystem.Common.Players
{
    public class PSPlayer : ModPlayer
    {
        public static PSPlayer? Instance { get; private set; }
        public int maxLife;
        public int maxMana;
        public int defense;
        public float moveSpeed;
        public float moveAccel;
        public float damage;
        public float endurance;
        public int crit;
        public override void OnEnterWorld()
        {
            Instance = this;
            NetHandler.SyncAchievementDataOnEnterWorld();
        }

        private static bool editorInitialized;
        private static bool progressPanelInitialized;
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (KeyBinds.Check.JustPressed && !Main.gameMenu)
            {
                static void OpenEditor()
                {
                    Main.playerInventory = false;
                    AchEditor.Ins.OnInitialization();
                    if (!editorInitialized)
                    {
                        editorInitialized = true;
                        AchEditor.Ins.OnInitialization();
                    }
                    AchEditor.Ins.Info.IsVisible = true;
                }
                static void OpenPanel()
                {
                    Main.playerInventory = false;
                    ProgressPanel.Ins.OnInitialization();
                    if (!progressPanelInitialized)
                    {
                        progressPanelInitialized = true;
                        ProgressPanel.Ins.OnInitialization();
                    }
                    ProgressPanel.Ins.Info.IsVisible = true;
                }
                static void CloseEditor() => AchEditor.Ins.Info.IsVisible = false;
                static void ClosePanel() => ProgressPanel.Ins.Info.IsVisible = false;
                if (ProgressPanel.Ins.Info.IsVisible)
                {
                    ClosePanel();
                    if (ClientConfig.Instance.DeveloperMode)
                    {
                        OpenEditor();
                    }
                }
                else if (AchEditor.Ins.Info.IsVisible)
                {
                    CloseEditor();
                    OpenPanel();
                }
                else
                {
                    OpenPanel();
                }
            }
        }
    }
}
