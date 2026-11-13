#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientCore.Extensions;

namespace ClientGUI
{
    /// <summary>
    /// An enhanced drop-down control with scrollable item list, tooltip support,
    /// and custom rendering of hover highlight and scroll bar.
    /// Prevents accidental item selection while dragging the scroll thumb or right after releasing it.
    /// </summary>
    public class XNAClientDropDown : XNADropDown, IToolTipContainer
    {
        /// <summary>The tooltip control attached to this drop-down.</summary>
        public ToolTip ToolTip { get; private set; } = null!;

        private string _initialToolTipText = string.Empty;

        /// <summary>
        /// Gets or sets the tooltip text displayed when the mouse hovers over the control.
        /// </summary>
        public string ToolTipText
        {
            get => Initialized ? ToolTip.Text : _initialToolTipText;
            set
            {
                if (Initialized && ToolTip != null)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }

        public XNAClientDropDown(WindowManager windowManager) : base(windowManager) { }

        /// <summary>
        /// Initializes the drop-down with a click sound effect, tooltip,
        /// and subscribes to the selected index changed event.
        /// </summary>
        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");
            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
            SelectedIndexChanged += XNAClientDropDown_SelectedIndexChanged;
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "ToolTip":
                    ToolTipText = value.FromIniString();
                    break;
                case "MaxVisibleItems":
                    MaxVisibleItems = Conversions.IntFromString(value, MaxVisibleItems);
                    break;
                default:
                    base.ParseControlINIAttribute(iniFile, key, value);
                    break;
            }
        }

        /// <summary>
        /// Opens the dropdown and ensures the selected item is visible
        /// within the scroll area after expansion.
        /// </summary>
        public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
        {
            bool wasClosed = DropDownState == DropDownState.CLOSED && AllowDropDown;
            base.OnMouseLeftDown(inputEventArgs);

            if (wasClosed && DropDownState != DropDownState.CLOSED)
            {
                EnsureSelectedIndexVisible(resetOnNoSelection: true);
                AdjustDropDownHeight();
            }

            UpdateToolTipBlock();
        }

        /// <summary>
        /// Handles left click when open: selects items in the scrollable area,
        /// ignores clicks on the scroll bar, and delegates to the base class otherwise.
        /// </summary>
        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            if (DropDownState == DropDownState.CLOSED)
            {
                base.OnLeftClick(inputEventArgs);
                return;
            }

            if (_skipNextItemSelection)
            {
                _skipNextItemSelection = false;
                return;
            }

            Point cursorPoint = GetCursorPoint();

            if (_isScrollable)
            {
                int sbWidth = _dropDownScrollBar.ScrollBarWidth;
                Rectangle itemsRect = DropDownState == DropDownState.OPENED_DOWN
                    ? new Rectangle(0, DropDownTexture.Height, Width - sbWidth, Height - DropDownTexture.Height)
                    : new Rectangle(0, 0, Width - sbWidth, Height - DropDownTexture.Height);

                if (!_isDraggingScrollBar && itemsRect.Contains(cursorPoint))
                {
                    int relativeY = cursorPoint.Y - itemsRect.Y - 1;
                    int clickedIndex = relativeY / ItemHeight + _scrollOffset;

                    if (clickedIndex >= 0 && clickedIndex < Items.Count && Items[clickedIndex].Selectable)
                    {
                        SelectedIndex = clickedIndex;
                        ClickSoundEffect?.Play();
                        CloseDropDown();
                        return;
                    }
                }

                Rectangle sbBounds = new Rectangle(
                    Width - sbWidth,
                    DropDownState == DropDownState.OPENED_DOWN ? DropDownTexture.Height + 1 : 1,
                    sbWidth,
                    Height - DropDownTexture.Height - 2);

                if (sbBounds.Contains(cursorPoint))
                    return;
            }

            base.OnLeftClick(inputEventArgs);
        }

        protected override void CloseDropDown()
        {
            base.CloseDropDown();
            UpdateToolTipBlock();
        }

        /// <summary>
        /// Per-frame update: handles hover index, scroll bar dragging,
        /// and tracks previous mouse state for close-on-click-outside.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (DropDownState == DropDownState.CLOSED)
            {
                _previousMouseState = Mouse.GetState();
                return;
            }

            Point cursorPoint = GetCursorPoint();
            MouseState mouseState = Mouse.GetState();

            if (_isDraggingScrollBar)
                _correctHoveredIndex = -1;
            else
                _correctHoveredIndex = GetHoveredIndexWithScroll(cursorPoint);

            if (_isScrollable)
            {
                Rectangle sbBounds = new Rectangle(
                    Width - _dropDownScrollBar.ScrollBarWidth,
                    DropDownState == DropDownState.OPENED_DOWN ? DropDownTexture.Height + 1 : 1,
                    _dropDownScrollBar.ScrollBarWidth,
                    Height - DropDownTexture.Height - 2);

                if (mouseState.LeftButton == ButtonState.Pressed && sbBounds.Contains(cursorPoint))
                {
                    if (!_isDraggingScrollBar)
                    {
                        _isDraggingScrollBar = true;
                        _dropDownScrollBar.HandleTrackClick(cursorPoint);
                    }
                }

                if (_isDraggingScrollBar)
                {
                    Point sbLocal = new Point(cursorPoint.X - sbBounds.X, cursorPoint.Y - sbBounds.Y);
                    _dropDownScrollBar.HandleTrackClick(new Point(sbLocal.X + _dropDownScrollBar.ScrollBarWidth / 2, sbLocal.Y));
                    _scrollOffset = _dropDownScrollBar.ViewTop;
                }

                if (mouseState.LeftButton == ButtonState.Released && _isDraggingScrollBar)
                {
                    _skipNextItemSelection = true;
                    _isDraggingScrollBar = false;
                }
            }

            _previousMouseState = mouseState;
        }

        /// <summary>
        /// Draws items with pulsing hover animation and selection highlighting.
        /// </summary>
        protected override void DrawItem(int index, int y)
        {
            if (_isScrollable)
            {
                int visibleIndex = index - _scrollOffset;
                if (visibleIndex < 0 || visibleIndex >= _maxVisibleItems)
                    return;

                int baseY = DropDownState == DropDownState.OPENED_DOWN
                    ? DropDownTexture.Height + 1
                    : 1;
                y = baseY + visibleIndex * ItemHeight;
            }

            XNADropDownItem item = Items[index];
            bool isHovered = _correctHoveredIndex == index;
            bool isSelected = index == SelectedIndex;

            int sbWidth = _isScrollable ? _dropDownScrollBar.ScrollBarWidth + 1 : 2;
            Rectangle itemRect = new Rectangle(1, y, Width - sbWidth, ItemHeight);

            if (isSelected)
                FillRectangle(itemRect, FocusColor);
            else if (isHovered)
            {
                float t = (float)(Math.Sin(_hoverAnimationTime * 2.0) * 0.5 + 0.5);
                FillRectangle(itemRect, Color.Lerp(BackColor, FocusColor, t));
            }
            else
                FillRectangle(itemRect, BackColor);

            int x = 2;
            if (item.Texture != null)
            {
                DrawTexture(item.Texture, new Rectangle(1, y + 1, item.Texture.Width, item.Texture.Height), Color.White);
                x += item.Texture.Width + 1;
            }

            Color textColor = item.Selectable ? GetItemTextColor(item) : DisabledItemColor;
            if (!string.IsNullOrEmpty(item.Text))
            {
                Vector2 textPosition = new Vector2(x, y + 1);
                DrawStringWithShadow(item.Text, FontIndex, textPosition, textColor);
            }
        }

        private void XNAClientDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DropDownState != DropDownState.CLOSED && _isScrollable)
                EnsureSelectedIndexVisible(resetOnNoSelection: false);
        }

        protected void UpdateToolTipBlock()
        {
            if (ToolTip == null)
                return;
            ToolTip.Blocked = DropDownState != DropDownState.CLOSED;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                SelectedIndexChanged -= XNAClientDropDown_SelectedIndexChanged;
            base.Dispose(disposing);
        }
    }
}
