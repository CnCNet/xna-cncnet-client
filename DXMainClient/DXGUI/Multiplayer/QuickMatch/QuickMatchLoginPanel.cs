﻿using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchLoginPanel : INItializableWindow
    {
        public event EventHandler Exit;

        private const string LoginErrorTitle = "Login Error";

        private readonly QmService qmService;

        private XNATextBox tbEmail;
        private XNAPasswordBox tbPassword;

        public event EventHandler LoginEvent;

        public QuickMatchLoginPanel(WindowManager windowManager, QmService qmService) : base(windowManager)
        {
            this.qmService = qmService;
            this.qmService.QmEvent += HandleQmEvent;
            IniNameOverride = nameof(QuickMatchLoginPanel);
        }

        public override void Initialize()
        {
            base.Initialize();

            XNAClientButton btnLogin;
            btnLogin = FindChild<XNAClientButton>(nameof(btnLogin));
            btnLogin.LeftClick += (_, _) => Login();

            XNAClientButton btnCancel;
            btnCancel = FindChild<XNAClientButton>(nameof(btnCancel));
            btnCancel.LeftClick += (_, _) => Exit?.Invoke(this, null);

            tbEmail = FindChild<XNATextBox>(nameof(tbEmail));
            tbEmail.Text = qmService.GetCachedEmail() ?? string.Empty;
            tbEmail.EnterPressed += (_, _) => Login();

            tbPassword = FindChild<XNAPasswordBox>(nameof(tbPassword));
            tbPassword.EnterPressed += (_, _) => Login();

            EnabledChanged += InitLogin;
        }

        private void HandleQmEvent(object sender, QmEvent qmEvent)
        {
            switch (qmEvent)
            {
                case QmLoginEvent:
                    Disable();
                    return;
                case QmLogoutEvent:
                    Enable();
                    return;
            }
        }

        public void InitLogin(object sender, EventArgs eventArgs)
        {
            if (!Enabled)
                return;

            if (qmService.HasToken())
                qmService.RefreshAsync();
        }

        private void Login()
        {
            if (!ValidateForm())
                return;

            qmService.LoginAsync(tbEmail.Text, tbPassword.Password);
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrEmpty(tbEmail.Text))
            {
                XNAMessageBox.Show(WindowManager, LoginErrorTitle, "No Email specified");
                return false;
            }

            if (string.IsNullOrEmpty(tbPassword.Text))
            {
                XNAMessageBox.Show(WindowManager, LoginErrorTitle, "No Password specified");
                return false;
            }

            return true;
        }
    }
}