using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ClientCore.Enums;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchStatusOverlay : INItializableWindow
    {
        private int DefaultInternalWidth;

        private const int BUTTON_WIDTH = 92;
        private const int BUTTON_GAP = 10;
        private const int BUTTON_HEIGHT = 21;

        private XNAPanel statusOverlayBox { get; set; }

        private XNALabel statusMessage { get; set; }

        private XNAPanel pnlButtons { get; set; }

        private Type lastQmLoadingEventType { get; set; }

        private string currentMessage { get; set; }

        private int matchupFoundConfirmTimeLeft { get; set; }

        private QmMatchFoundTimer matchupFoundConfirmTimer { get; set; }
        

        private XNAClientProgressBar progressBar;

        private readonly QmService qmService;
        private readonly QmSettings qmSettings;

        public QuickMatchStatusOverlay(WindowManager windowManager, QmService qmService, QmSettingsService qmSettingsService) : base(windowManager)
        {
            this.qmService = qmService;
            this.qmService.QmEvent += HandleQmEvent;
            qmSettings = qmSettingsService.GetSettings();

            matchupFoundConfirmTimer = new QmMatchFoundTimer();
            matchupFoundConfirmTimer.SetElapsedAction(ReduceMatchupFoundConfirmTimeLeft);
        }

        public override void Initialize()
        {
            base.Initialize();

            statusOverlayBox = FindChild<XNAPanel>(nameof(statusOverlayBox));
            DefaultInternalWidth = statusOverlayBox.ClientRectangle.Width;

            statusMessage = FindChild<XNALabel>(nameof(statusMessage));

            pnlButtons = FindChild<XNAPanel>(nameof(pnlButtons));

            progressBar = FindChild<XNAClientProgressBar>(nameof(progressBar));
        }

        private void HandleQmEvent(object sender, QmEvent qmEvent)
        {
            switch (qmEvent)
            {
                case QmLoggingInEvent:
                    HandleLoggingInEvent();
                    break;
                case QmLoginEvent:
                    HandleLoginEvent();
                    break;
                case QmLoadingLaddersAndUserAccountsEvent:
                    HandleLoadingLaddersAndUserAccountsEvent();
                    break;
                case QmLaddersAndUserAccountsEvent:
                    HandleLaddersAndUserAccountsEvent();
                    break;
                case QmLoadingLadderMapsEvent:
                    HandleLoadingLadderMapsEvent();
                    break;
                case QmLadderMapsEvent:
                    HandleLadderMapsEvent();
                    break;
                case QmRequestingMatchEvent e:
                    HandleRequestingMatchEvent(e);
                    break;
                case QmCancelingRequestMatchEvent:
                    HandleCancelingMatchRequest();
                    break;
                case QmErrorMessageEvent:
                    Disable();
                    return;
                case QmResponseEvent e:
                    HandleRequestResponseEvent(e);
                    return;
            }

            if (qmEvent is IQmOverlayStatusEvent)
                lastQmLoadingEventType = qmEvent.GetType();
        }

        private void HandleLoggingInEvent() => SetStatus(QmStrings.LoggingInStatus);

        private void HandleLoginEvent() => CloseIfLastEventType(typeof(QmLoggingInEvent));

        private void HandleLoadingLaddersAndUserAccountsEvent() => SetStatus(QmStrings.LoadingLaddersAndAccountsStatus);

        private void HandleLaddersAndUserAccountsEvent() => CloseIfLastEventType(typeof(QmLoadingLaddersAndUserAccountsEvent));

        private void HandleLoadingLadderMapsEvent() => SetStatus(QmStrings.LoadingLadderMapsStatus);

        private void HandleLadderMapsEvent() => CloseIfLastEventType(typeof(QmLoadingLadderMapsEvent));

        private void HandleRequestingMatchEvent(QmRequestingMatchEvent e) => SetStatus(QmStrings.RequestingMatchStatus, e.CancelAction);

        private void HandleRequestResponseEvent(QmResponseEvent e)
        {
            QmResponseMessage responseMessage = e.Response.Data;
            switch (true)
            {
                case true when responseMessage is QmWaitResponse:
                    return; // need to keep the overlay open while waiting
                case true when responseMessage is QmSpawnResponse spawnResponse:
                    HandleSpawnResponseEvent(spawnResponse);
                    return;
                default:
                    CloseIfLastEventType(typeof(QmRequestingMatchEvent), typeof(QmCancelingRequestMatchEvent));
                    return;
            }
        }

        private void HandleSpawnResponseEvent(QmSpawnResponse spawnResponse)
        {
            int interval = matchupFoundConfirmTimer.GetInterval();
            int ratio = 1000 / interval;
            int max = qmSettings.MatchFoundWaitSeconds * interval / ratio;
            progressBar.Maximum = max;
            progressBar.Value = max;
            var actions = new List<Tuple<string, Action>>
            {
                new(QmStrings.MatchupFoundConfirmYes, () => AcceptMatchAsync(spawnResponse.Spawn)),
                new(QmStrings.MatchupFoundConfirmNo, () => RejectMatchAsync(spawnResponse.Spawn))
            };
            SetStatus(QmStrings.MatchupFoundConfirmMsg, actions, ProgressBarModeEnum.Determinate);
            matchupFoundConfirmTimer.SetSpawn(spawnResponse.Spawn);
            matchupFoundConfirmTimer.Start();
        }

        private void AcceptMatchAsync(QmSpawn spawn)
        {
            Disable();
            matchupFoundConfirmTimer.Stop();
            qmService.AcceptMatchAsync(spawn);
        }

        private void RejectMatchAsync(QmSpawn spawn)
        {
            Disable();
            matchupFoundConfirmTimer.Stop();
            qmService.RejectMatchAsync(spawn);
        }

        private void HandleCancelingMatchRequest() => SetStatus(QmStrings.CancelingMatchRequestStatus);

        private void CloseIfLastEventType(params Type[] lastEventType)
        {
            if (lastEventType.Any(t => t == lastQmLoadingEventType))
                Disable();
        }

        private void ReduceMatchupFoundConfirmTimeLeft()
        {
            progressBar.Value -= 1;

            if (progressBar.Value != 0)
                return;

            matchupFoundConfirmTimer.Stop();
            Disable();
            RejectMatchAsync(matchupFoundConfirmTimer.Spawn);
        }

        private void SetStatus(string message, Tuple<string, Action> button)
            => SetStatus(message, new List<Tuple<string, Action>> { button });

        private void SetStatus(string message, IEnumerable<Tuple<string, Action>> buttons = null, ProgressBarModeEnum progressBarMode = ProgressBarModeEnum.Indeterminate)
        {
            currentMessage = message;
            statusMessage.Text = message;
            progressBar.ProgressBarMode = progressBarMode;

            ResizeForText();
            AddButtons(buttons);
            Enable();
        }

        private void ResizeForText()
        {
            Vector2 textDimensions = Renderer.GetTextDimensions(statusMessage.Text, statusMessage.FontIndex);

            statusOverlayBox.Width = (int)Math.Max(DefaultInternalWidth, textDimensions.X + 60);
            statusOverlayBox.X = (Width / 2) - (statusOverlayBox.Width / 2);
        }

        private void AddButtons(IEnumerable<Tuple<string, Action>> buttons = null)
        {
            foreach (XNAControl xnaControl in pnlButtons.Children.ToList())
                pnlButtons.RemoveChild(xnaControl);

            if (buttons == null)
                return;

            var buttonDefinitions = buttons.ToList();
            int fullWidth = (BUTTON_WIDTH * buttonDefinitions.Count) + (BUTTON_GAP * (buttonDefinitions.Count - 1));
            int startX = (statusOverlayBox.Width / 2) - (fullWidth / 2);

            for (int i = 0; i < buttonDefinitions.Count; i++)
            {
                Tuple<string, Action> buttonDefinition = buttonDefinitions[i];
                var button = new XNAClientButton(WindowManager);
                button.Text = buttonDefinition.Item1;
                button.LeftClick += (_, _) => buttonDefinition.Item2();
                button.ClientRectangle = new Rectangle(startX + (i * BUTTON_WIDTH) + (i * (buttonDefinitions.Count - 1) * BUTTON_GAP), 0, BUTTON_WIDTH, BUTTON_HEIGHT);
                pnlButtons.AddChild(button);
            }
        }
    }
}