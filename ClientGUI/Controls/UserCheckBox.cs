using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.IO;
using System.Windows.Forms;
using ClientCore;

namespace ClientGUI
{
    public partial class UserCheckBox : UserControl
    {
        public UserCheckBox()
        {
            InitializeComponent();

            clearedImage = SharedUILogic.LoadImage("checkBoxClear.png");
            checkedImage = SharedUILogic.LoadImage("checkBoxChecked.png");

            if (Checked)
                button1.BackgroundImage = checkedImage;
            else
                button1.BackgroundImage = clearedImage;

            button1.Click += new EventHandler(button1_Click);
            label1.Click += new EventHandler(button1_Click);
            //this.Text = this.Tag.ToString();
        }

        public UserCheckBox(Color BaseColor, Color HoverColor, string text)
        {
            InitializeComponent();

            clearedImage = SharedUILogic.LoadImage("checkBoxClear.png");
            checkedImage = SharedUILogic.LoadImage("checkBoxChecked.png");

            baseColor = BaseColor;
            hoverColor = HoverColor;
            label1.ForeColor = BaseColor;
            label1.Text = text;
            this.Size = new System.Drawing.Size(label1.Location.X + label1.Size.Width, 15);
            button1.BackgroundImage = clearedImage;
            button1.Click += new EventHandler(button1_Click);
            label1.Click += new EventHandler(button1_Click);
        }

        public void Initialize()
        {
            button1.Size = checkedImage.Size;
            label1.Location = new Point(checkedImage.Size.Width + 1, -1 + button1.Location.Y + (checkedImage.Height - label1.Height) / 2);
            this.Size = new Size(this.Size.Width, button1.Location.Y + button1.Size.Height);
            this.FontChanged += UserCheckBox_FontChanged;
        }

        void UserCheckBox_FontChanged(object sender, EventArgs e)
        {
            label1.Location = new Point(checkedImage.Size.Width + 1, -1 + button1.Location.Y + (checkedImage.Height - label1.Height) / 2);
            this.Size = new Size(this.Size.Width, button1.Location.Y + button1.Size.Height);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!_enabled)
                return;

            if (Checked)
            {
                Checked = false;
            }
            else
            {
                Checked = true;
            }
        }

        private bool _checked = false;
        public delegate void OnCheckedChanged(object sender);
        public event OnCheckedChanged CheckedChanged;

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true)]
        [Description("The text color of the check box.")]
        public Color BaseColor
        {
            get { return baseColor; }
            set
            {
                baseColor = value;
                label1.ForeColor = value;
            }
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true)]
        [Description("The text color of the check box when the cursor is hovered over it.")]
        public Color HoverColor
        {
            get { return hoverColor; }
            set { hoverColor = value; }
        }

        private Color baseColor;
        private Color hoverColor;

        Image clearedImage;
        Image checkedImage;

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true)]
        [Description("Whether the UserCheckBox is checked.")]
        public bool Checked
        {
            get { return _checked; }
            set
            {
                _checked = value;
                if (checkedImage != null)
                {
                    if (value)
                        button1.BackgroundImage = checkedImage;
                    else
                        button1.BackgroundImage = clearedImage;
                }

                if (CheckedChanged != null)
                    CheckedChanged(this);
            }
        }

        private bool _enabled = true;

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true)]
        [Description("The text displayed in the UserCheckBox.")]
        public string LabelText
        {
            get
            {
                return label1.Text;
            }
            set
            {
                label1.Text = value;
            }
        }

        public override string Text
        {
            get
            {
                return label1.Text;
            }
            set
            {
                label1.Text = value;
            }

        }

        public bool IsEnabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// Gets or sets a value that determines if the 
        /// value of the check box should be "reversed".
        /// </summary>
        public bool Reversed
        {
            get; set;
        }

        private void label1_MouseEnter(object sender, EventArgs e)
        {
            if (_enabled)
                label1.ForeColor = hoverColor;
        }

        private void label1_MouseLeave(object sender, EventArgs e)
        {
            label1.ForeColor = baseColor;
        }

        private void button1_MouseEnter(object sender, EventArgs e)
        {
            if (_enabled)
                label1.ForeColor = hoverColor;
        }

        private void button1_MouseLeave(object sender, EventArgs e)
        {
            label1.ForeColor = baseColor;
        }
    }
}
