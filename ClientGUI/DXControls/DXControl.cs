using ClientCore;
using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DXControls
{
    /// <summary>
    /// The base class for a XNA-based UI control.
    /// </summary>
    public class DXControl : DrawableGameComponent
    {
        public DXControl(Game game) : base(game)
        {

        }

        public delegate void MouseEnterEventHandler(object sender, EventArgs e);
        public event MouseEnterEventHandler MouseEnter;

        public delegate void MouseLeaveEventHandler(object sender, EventArgs e);
        public event MouseLeaveEventHandler MouseLeave;

        public delegate void MouseMoveEventHandler(object sender, EventArgs e);
        public event MouseMoveEventHandler MouseMove;

        public delegate void MouseOnControlEventHandler(object sender, MouseEventArgs e);
        public event MouseOnControlEventHandler MouseOnControl;

        public delegate void ScrollWheelEventHandler(object sender, EventArgs e);
        public event ScrollWheelEventHandler MouseScrolled;

        public delegate void MouseClickEventHandler(object sender, EventArgs e);
        public event MouseClickEventHandler LeftClick;
        public event MouseClickEventHandler RightClick;

        public DXControl Parent;

        public List<DXControl> Children = new List<DXControl>();

        public string Name { get; set; }

        /// <summary>
        /// The display rectangle of the control inside its parent.
        /// </summary>
        public Rectangle ClientRectangle { get; set; }

        public Rectangle WindowRectangle()
        {
            return new Rectangle(GetLocationX(), GetLocationY(), ClientRectangle.Width, ClientRectangle.Height);
        }

        public int GetLocationX()
        {
            if (Parent != null)
                return ClientRectangle.X + Parent.GetLocationX();

            return ClientRectangle.X;
        }

        public int GetLocationY()
        {
            if (Parent != null)
                return ClientRectangle.Y + Parent.GetLocationY();

            return ClientRectangle.Y;
        }

        Color remapColor = Color.White;
        public Color RemapColor
        {
            get { return remapColor; }
            set { remapColor = value; }
        }

        bool CursorOnControl = false;

        float alpha = 1.0f;
        public float Alpha
        {
            get
            { 
                if (Parent != null)
                    return alpha * Parent.Alpha;

                return alpha;
            }
            set
            {
                if (value > 1.0f)
                    alpha = 1.0f;
                else if (value < 0.0)
                    alpha = 0.0f;
                else
                    alpha = value;
            }
        }

        public CursorImage CursorImage;

        public bool HasExclusiveCursorAccess { get; set; }

        public virtual string Text { get; set; }

        public object Tag { get; set; }

        public bool Killed { get; set; }

        bool _ignoreInputOnFrame = false;
        public bool IgnoreInputOnFrame
        {
            get
            {
                if (Parent == null)
                    return _ignoreInputOnFrame;
                else
                    return _ignoreInputOnFrame || Parent.IgnoreInputOnFrame;
            }
            set
            {
                _ignoreInputOnFrame = true;
            }
        }


        public Color GetRemapColor()
        {
            return GetColorWithAlpha(RemapColor);
        }

        public Color GetColorWithAlpha(Color baseColor)
        {
            return new Color(baseColor.R, baseColor.G, baseColor.B, (int)(Alpha * 255));
        }

        public void AddChild(DXControl child)
        {
            child.Parent = this;
            child.Initialize();
            Children.Add(child);
        }

        public void SetWindowAttributes()
        {
            IniFile iniFile = new IniFile(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + Name + ".ini");

            GetAttributes(iniFile);
        }

        public void GetAttributes(IniFile iniFile)
        {
            foreach (DXControl child in Children)
                child.GetAttributes(iniFile);

            List<string> keys = iniFile.GetSectionKeys(Name);

            if (keys == null)
                return;

            foreach (string key in keys)
                ParseAttributeFromINI(iniFile, key);
        }

        public void CenterOnParent()
        {
            if (Parent == null)
            {
                Logger.Log("Error: CenterOnParent called for a control which has no parent!");
                return;
            }

            Rectangle parentRectangle = Parent.ClientRectangle;

            ClientRectangle = new Rectangle((parentRectangle.Width - ClientRectangle.Width) / 2,
                (parentRectangle.Height - ClientRectangle.Height) / 2, ClientRectangle.Width, ClientRectangle.Height);
        }

        /// <summary>
        /// Gets the cursor's location relative to this control's location.
        /// </summary>
        /// <returns>A point that represents the cursor's location relative to this control's location.</returns>
        public Point GetCursorPoint()
        {
            return new Point(Cursor.Instance().Location.X - WindowRectangle().X, Cursor.Instance().Location.Y - WindowRectangle().Y);
        }

        protected virtual void ParseAttributeFromINI(IniFile iniFile, string key)
        {
            switch (key)
            {
                case "Size":
                    string[] size = iniFile.GetStringValue(Name, "Size", "10,10").Split(',');
                    ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                        Int32.Parse(size[0]), Int32.Parse(size[1]));
                    break;
                case "Location":
                    string[] location = iniFile.GetStringValue(Name, "Location", "10,10").Split(',');
                    ClientRectangle = new Rectangle(Int32.Parse(location[0]), Int32.Parse(location[1]),
                        ClientRectangle.Width, ClientRectangle.Height);
                    break;
                case "RemapColor":
                    string[] colors = iniFile.GetStringValue(Name, "RemapColor", "255,255,255").Split(',');
                    RemapColor = new Color(Int32.Parse(colors[0]), Int32.Parse(colors[1]), Int32.Parse(colors[2]), 255);
                    break;
                case "Text":
                    Text = iniFile.GetStringValue(Name, "Text", " ");
                    break;
                case "Visible":
                    Visible = iniFile.GetBooleanValue(Name, "Visible", true);
                    Enabled = Visible;
                    break;
                case "DistanceFromRightBorder":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(Parent.ClientRectangle.Width - iniFile.GetIntValue(Name, "DistanceFromRightBorder", 0),
                            ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
                    }
                    break;
                case "DistanceFromBottomBorder":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, Parent.ClientRectangle.Height - iniFile.GetIntValue(Name, "DistanceFromBottomBorder", 0),
                            ClientRectangle.Width, ClientRectangle.Height);
                    }
                    break;
            }
        }

        public virtual void Kill()
        {
            foreach (DXControl child in Children)
                child.Kill();

            Killed = true;
        }

        public virtual void RefreshSize()
        {
            foreach (DXControl child in Children)
                child.RefreshSize();
        }

        public override void Update(GameTime gameTime)
        {
            Rectangle rectangle = WindowRectangle();

            Cursor cursor = Cursor.Instance();

            if (IgnoreInputOnFrame)
            {
                _ignoreInputOnFrame = false;
                return;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Enabled)
                {
                    Children[i].Update(gameTime);
                }
            }

            if (rectangle.Contains(cursor.Location) &&
                (!cursor.ExclusiveAccessArea.Contains(cursor.Location) || HasExclusiveCursorAccess))
            {
                if (!CursorOnControl)
                    OnMouseEnter();

                Cursor.Instance().CursorImage = CursorImage;

                CursorOnControl = true;

                MouseEventArgs mouseEventArgs = new MouseEventArgs(cursor.Location - rectangle.Location);

                OnMouseOnControl(mouseEventArgs);

                if (Cursor.Instance().HasMoved)
                    OnMouseMove();

                if (cursor.LeftClicked)
                {
                    if (HasExclusiveCursorAccess && !cursor.ExclusiveAccessArea.Contains(cursor.Location))
                    {
                        HasExclusiveCursorAccess = false;
                    }

                    OnLeftClick();
                }

                if (cursor.RightClicked)
                {
                    if (HasExclusiveCursorAccess && !cursor.ExclusiveAccessArea.Contains(cursor.Location))
                    {
                        HasExclusiveCursorAccess = false;
                    }

                    OnRightClick();
                }

                if (cursor.ScrollWheelValue != 0)
                {
                    if (HasExclusiveCursorAccess && !cursor.ExclusiveAccessArea.Contains(cursor.Location))
                    {
                        HasExclusiveCursorAccess = false;
                    }

                    OnMouseScrolled();
                }
            }
            else
            {
                if (CursorOnControl)
                    OnMouseLeave();

                CursorOnControl = false;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    Children[i].Draw(gameTime);
                }
            }
        }

        public virtual void OnMouseEnter()
        {
            if (MouseEnter != null)
                MouseEnter(this, EventArgs.Empty);
        }

        public virtual void OnMouseLeave()
        {
            if (MouseLeave != null)
                MouseLeave(this, EventArgs.Empty);
        }

        public virtual void OnLeftClick()
        {
            if (LeftClick != null)
                LeftClick(this, EventArgs.Empty);
        }

        public virtual void OnRightClick()
        {
            if (RightClick != null)
                RightClick(this, EventArgs.Empty);
        }

        public virtual void OnMouseMove()
        {
            if (MouseMove != null)
                MouseMove(this, EventArgs.Empty);
        }

        public virtual void OnMouseOnControl(MouseEventArgs eventArgs)
        {
            if (MouseOnControl != null)
                MouseOnControl(this, eventArgs);
        }

        public virtual void OnMouseScrolled()
        {
            if (MouseScrolled != null)
                MouseScrolled(this, EventArgs.Empty);
        }
    }
}
