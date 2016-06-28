using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using ClientCore;
using Rampastring.Tools;

namespace ClientGUI
{
    /// <summary>
    /// A generic form that the client's forms are based on.
    /// Enables styling via INI files and moving the form by dragging from elsewhere
    /// than just form borders.
    /// </summary>
    public class MovableForm : Form
    {
        private bool _moving = false;
        private Point _offset;

        bool initialized = false;
        bool controlsMoved = false;

        bool enableShadow = true;

        List<PictureBox> ExtraPictureBoxes = new List<PictureBox>();

        protected override CreateParams CreateParams
        {
            get
            {
                if (enableShadow)
                {
                    const int CS_DROPSHADOW = 0x20000;
                    CreateParams cp = base.CreateParams;
                    cp.ClassStyle |= CS_DROPSHADOW;
                    return cp;
                }
                else
                    return base.CreateParams;
            }
        }

        public void MouseDownHandler(object sender, MouseEventArgs e)
        {
            _moving = true;
            _offset = new Point(e.X, e.Y);
        }

        public void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (_moving)
            {
                Point newlocation = this.Location;
                newlocation.X += e.X - _offset.X;
                newlocation.Y += e.Y - _offset.Y;
                this.Location = newlocation;
            }
        }

        public void MouseUpHandler(object sender, MouseEventArgs e)
        {
            if (_moving)
            {
                _moving = false;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            InitializeMovableForm();

            MouseMove += MouseMoveHandler;
            MouseUp += MouseUpHandler;
            MouseDown += MouseDownHandler;

            foreach (Control c in Controls)
                SetEvents(c);

            base.OnShown(e);
        }

        protected void InitializeMovableForm()
        {
            if (initialized)
                return;

            this.SuspendLayout();
            ControlDrawHandler.SuspendDrawing(this);

            initialized = true;

            int topPadding = 0;
            int leftPadding = 0;
            int bottomPadding = 0;
            int rightPadding = 0;
            string padding;

            FormBorderStyle fbs = this.FormBorderStyle;

            IniFile iniFile;
            if (File.Exists(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + this.Name + ".ini"))
            {
                iniFile = new IniFile(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + this.Name + ".ini");
                padding = iniFile.GetStringValue(this.Name, "Padding", String.Empty);
            }
            else
            {
                iniFile = new IniFile(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "MovableForm.ini");
                if (iniFile.GetSectionKeys(this.Name) != null)
                    padding = iniFile.GetStringValue(this.Name, "Padding", String.Empty);
                else
                    padding = iniFile.GetStringValue("MovableForm", "Padding", String.Empty);

                switch (iniFile.GetStringValue("MovableForm", "FormBorderStyle", String.Empty))
                {
                    case "None":
                        fbs = FormBorderStyle.None;
                        break;
                    case "SizableToolWindow":
                        fbs = FormBorderStyle.SizableToolWindow;
                        break;
                    case "Fixed3D":
                        fbs = FormBorderStyle.Fixed3D;
                        break;
                    case "FixedSingle":
                        fbs = FormBorderStyle.FixedSingle;
                        break;
                    case "FixedDialog":
                        fbs = FormBorderStyle.FixedDialog;
                        break;
                    case "FixedToolWindow":
                        fbs = FormBorderStyle.FixedToolWindow;
                        break;
                    case "Sizable":
                        break;
                    default:
                        fbs = this.FormBorderStyle;
                        break;
                }
            }

            if (iniFile.GetBooleanValue(this.Name, "IgnoreAllCode", false))
            {
                ResumeLayout();
                ControlDrawHandler.ResumeDrawing(this);
                enableShadow = false;
                return;
            }

            this.FormBorderStyle = fbs;

            if (!controlsMoved)
            {
                if (!String.IsNullOrEmpty(padding))
                {
                    string[] paddingParts = padding.Split(',');
                    topPadding = Int32.Parse(paddingParts[0]);
                    leftPadding = Int32.Parse(paddingParts[1]);
                    bottomPadding = Int32.Parse(paddingParts[2]);
                    rightPadding = Int32.Parse(paddingParts[3]);

                    AnchorStyles[] anchors = new AnchorStyles[this.Controls.Count];

                    int i = 0;
                    foreach (Control control in this.Controls)
                    {
                        control.Location = new Point(control.Location.X + leftPadding, control.Location.Y + topPadding);
                        anchors[i] = control.Anchor;
                        control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        i++;
                    }

                    this.Size = new Size(this.Size.Width + leftPadding + rightPadding, this.Size.Height + topPadding + bottomPadding);

                    i = 0;
                    foreach (Control control in this.Controls)
                    {
                        control.Anchor = anchors[i];
                        i++;
                    }
                }
            }

            string pbSectionName = "ExtraPictureBoxes";

            List<string> pbKeys = iniFile.GetSectionKeys(pbSectionName);

            if (pbKeys != null)
            {
                foreach (string keyName in pbKeys)
                {
                    string name = iniFile.GetStringValue(pbSectionName, keyName, null);

                    if (name == null)
                        throw new Exception(this.Name + ".ini: " + " Invalid data in section " + pbSectionName);

                    PictureBox pb = new PictureBox();
                    pb.Name = name;
                    pb.BorderStyle = BorderStyle.None;
                    pb.BackColor = Color.Transparent;
                    string parent = iniFile.GetStringValue(name, "Parent", String.Empty);
                    if (String.IsNullOrEmpty(parent))
                    {
                        this.Controls.Add(pb);
                        this.Controls.SetChildIndex(pb, 0);
                    }
                    else
                    {
                        Control[] c = this.Controls.Find(parent, true);
                        if (c.Length == 0)
                            throw new Exception(this.Name + ".ini: Invalid Parent= specified for " + name);
                        c[0].Controls.Add(pb);
                    }
                    ExtraPictureBoxes.Add(pb);
                }
            }

            SharedUILogic.SetControlStyle(iniFile, this);

            if (!controlsMoved)
            {
                padding = iniFile.GetStringValue(this.Name, "AfterPadding", String.Empty);

                if (!String.IsNullOrEmpty(padding))
                {
                    string[] paddingParts = padding.Split(',');
                    topPadding = Int32.Parse(paddingParts[0]);
                    leftPadding = Int32.Parse(paddingParts[1]);
                    bottomPadding = Int32.Parse(paddingParts[2]);
                    rightPadding = Int32.Parse(paddingParts[3]);

                    AnchorStyles[] anchors = new AnchorStyles[this.Controls.Count];

                    int i = 0;
                    foreach (Control control in this.Controls)
                    {
                        if (!(control is PictureBox))
                            control.Location = new Point(control.Location.X + leftPadding, control.Location.Y + topPadding);
                        anchors[i] = control.Anchor;
                        control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        i++;
                    }

                    this.Size = new Size(this.Size.Width + rightPadding, this.Size.Height + bottomPadding);

                    i = 0;
                    foreach (Control control in this.Controls)
                    {
                        control.Anchor = anchors[i];
                        i++;
                    }
                }
            }

            controlsMoved = true;

            this.ResumeLayout();
            ControlDrawHandler.ResumeDrawing(this);
        }

        void SetEvents(Control c)
        {
            if (c is Panel || c is PictureBox || c is Label || c is ProgressBar)
            {
                c.MouseMove += MouseMoveHandler;
                c.MouseUp += MouseUpHandler;
                c.MouseDown += MouseDownHandler;
            }

            foreach (Control child in c.Controls)
                SetEvents(child);
        }

        void ClearPictureBoxes()
        {
            foreach (PictureBox pb in ExtraPictureBoxes)
            {
                Controls.Remove(pb);
                pb.Dispose();
            }

            ExtraPictureBoxes.Clear();
        }

        protected void Reinitialize()
        {
            SuspendLayout();
            initialized = false;
            ClearPictureBoxes();
            InitializeMovableForm();
            ResumeLayout();
        }

        protected override void OnClosed(EventArgs e)
        {
            RemoveEvents(this);

            base.OnClosed(e);
        }

        void RemoveEvents(Control c)
        {
            if (c is Form || c is Panel || c is PictureBox || c is Label || c is ProgressBar)
            {
                c.MouseMove -= MouseMoveHandler;
                c.MouseUp -= MouseUpHandler;
                c.MouseDown -= MouseDownHandler;
            }

            foreach (Control child in c.Controls)
                RemoveEvents(child);
        }
    }
}
