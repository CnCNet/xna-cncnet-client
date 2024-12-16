using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DTAClient.Domain.Singleplayer;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic.Campaign
{

    public class VariableCheckbox : XNACheckBox
    {
        private string _variable;
        public string Variable
        {
            get
            {
                return _variable;
            }
            set
            {
                Checked = CampaignHandler.Instance.Variables[value] > 0;
                _variable = value;
            }
        }
        public VariableCheckbox(WindowManager windowManager) : base(windowManager)
        {
            AllowChecking = true;
        }
        public override void OnLeftClick()
        {
            base.OnLeftClick();

            if (CampaignHandler.Instance.Variables.ContainsKey(Variable))
                CampaignHandler.Instance.Variables[Variable] = Checked ? 1 : 0;
        }
    }
}
