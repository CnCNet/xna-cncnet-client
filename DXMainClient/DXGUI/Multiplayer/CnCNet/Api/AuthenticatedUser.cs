using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.CnCNet.Api
{
    class AuthenticatedUser
    {
        private string name;
        private string email;
        private string clan;

        public string Username 
        {
            get { return name; }
            set { name = value; }
        }

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        public string Clan
        {
            get { return clan; }
            set { clan = value; }
        }
    }
}
