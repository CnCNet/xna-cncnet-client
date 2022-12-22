using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using ClientCore.Extensions;

namespace ClientGUI
{
    public enum PlayerSlotState
    {
        Empty,
        Unavailable,
        AI,
        NotReady,
        Ready,
        InGame,
        Warning,
        Error
    }

    public class XNAPlayerSlotIndicator : XNAIndicator<PlayerSlotState>
    {
        public static new Dictionary<PlayerSlotState, Texture2D> Textures { get; set; }

        public ToolTip ToolTip { get; private set; }

        public XNAPlayerSlotIndicator(WindowManager windowManager) : base(windowManager, Textures) { }

        public static void LoadTextures()
        {
            Textures = new Dictionary<PlayerSlotState, Texture2D>()
            {
                { PlayerSlotState.Empty, AssetLoader.LoadTextureUncached("statusEmpty.png") },
                { PlayerSlotState.Unavailable, AssetLoader.LoadTextureUncached("statusUnavailable.png") },
                { PlayerSlotState.AI, AssetLoader.LoadTextureUncached("statusAI.png") },
                { PlayerSlotState.NotReady, AssetLoader.LoadTextureUncached("statusClear.png") },
                { PlayerSlotState.Ready, AssetLoader.LoadTextureUncached("statusOk.png") },
                { PlayerSlotState.InGame, AssetLoader.LoadTextureUncached("statusInProgress.png") },
                { PlayerSlotState.Warning, AssetLoader.LoadTextureUncached("statusWarning.png") },
                { PlayerSlotState.Error, AssetLoader.LoadTextureUncached("statusError.png") }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this);
        }

        public override void SwitchTexture(PlayerSlotState key)
        {
            base.SwitchTexture(key);

            switch (key)
            {
                case PlayerSlotState.Empty:
                    ToolTip.Text = "The slot is empty.".L10N("Client:ClientGUI:SlotEmpty");
                    break;

                case PlayerSlotState.Unavailable:
                    ToolTip.Text = "The slot is unavailable.".L10N("Client:ClientGUI:SlotUnavailable");
                    break;

                case PlayerSlotState.AI:
                    ToolTip.Text = "The player is computer-controlled.".L10N("Client:ClientGUI:PlayerIsComputer");
                    break;

                case PlayerSlotState.NotReady:
                    ToolTip.Text = "The player isn't ready.".L10N("Client:ClientGUI:PlayerIsNotReady");
                    break;

                case PlayerSlotState.Ready:
                    ToolTip.Text = "The player is ready.".L10N("Client:ClientGUI:PlayerIsReady");
                    break;

                case PlayerSlotState.InGame:
                    ToolTip.Text = "The player is in game.".L10N("Client:ClientGUI:PlayerIsInGame");
                    break;

                case PlayerSlotState.Warning:
                    ToolTip.Text = "The player has some issue(s) that may impact gameplay.".L10N("Client:ClientGUI:PlayerHasIssue");
                    break;

                case PlayerSlotState.Error:
                    ToolTip.Text = "There's a critical issue with the player.".L10N("Client:ClientGUI:PlayerHasCriticalIssue");
                    break;
            }
        }
    }
}
