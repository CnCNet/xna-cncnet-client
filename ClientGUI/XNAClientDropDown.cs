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
    /// and custom rendering of hover underline and scroll bar.
    /// Prevents accidental item selection while dragging the scroll thumb or right after releasing it.
    /// </summary>
    public class XNAClientDropDown : XNADropDown, IToolTipContainer
    {
        // ---------- Constants ----------
        private const int DefaultScrollBarWidth = 3;
        private const int MinScrollThumbHeight = 15;
        private const int UnderlineThickness = 2;
        private const float ScrollRoundingFactor = 0.99f;
        private const int ScrollWheelOverflowThreshold = 1000;

        // ---------- Fields ----------
        private int _scrollOffset;
        private int _maxVisibleItems;
        private Rectangle _scrollBarArea;
        private bool _isDraggingScrollBar;
        private bool _skipNextItemSelection;
        private int _scrollThumbHeight;
        private int _scrollBarWidth = DefaultScrollBarWidth;
        private MouseState _previousMouseState;
        private int _correctHoveredIndex = -1;
																	 

        /// <summary>Indicates whether the drop-down is scrollable (i.e., has more items than MaxVisibleItems and MaxVisibleItems > 0).</summary>
        private bool _isScrollable => _maxVisibleItems > 0 && Items.Count > _maxVisibleItems;

        // ---------- Properties ----------
        // Non-nullable ToolTip to satisfy IToolTipContainer, initialized in Initialize()
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


        /// <summary>
        /// Gets or sets the maximum number of items visible when the dropdown is open.
        /// Use 0 to show all items without scrolling. Negative values are coerced to 0.
        /// </summary>
        public int MaxVisibleItems
        {
            get => _maxVisibleItems;
			   
			 
							  
																										  
            set => _maxVisibleItems = Math.Max(0, value);
			 
        }

        // ---------- Constructor ----------
        public XNAClientDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        // ---------- Initialization ----------
        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");
            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
            _previousMouseState = Mouse.GetState();
            SelectedIndexChanged += XNAClientDropDown_SelectedIndexChanged;
        }

        // ---------- INI Parsing ----------
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

        // ---------- Mouse Handling ----------
        public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
        {
            bool wasClosed = DropDownState == DropDownState.CLOSED && AllowDropDown;
            base.OnMouseLeftDown(inputEventArgs);

            if (wasClosed && DropDownState != DropDownState.CLOSED)
            {
                EnsureSelectedIndexVisible(resetOnNoSelection: true);
                AdjustDropDownHeight();
                CalculateScrollBar();
            }

            UpdateToolTipBlock();
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            if (DropDownState == DropDownState.CLOSED)
            {
                base.OnLeftClick(inputEventArgs);
                return;
            }

            // If we just finished dragging the scroll bar, ignore this click entirely.
            // This prevents accidental item selection or dropdown closing.
            if (_skipNextItemSelection)
            {
                _skipNextItemSelection = false;
                return;
            }

            Point cursorPoint = GetCursorPoint();

            if (_isScrollable)
            {
                Rectangle itemsRect = DropDownState == DropDownState.OPENED_DOWN
                    ? new Rectangle(0, DropDownTexture.Height, Width - _scrollBarWidth, Height - DropDownTexture.Height)
                    : new Rectangle(0, 0, Width - _scrollBarWidth, Height - DropDownTexture.Height);

                // Prevent selection while dragging
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

                // Click on the scroll bar area should not close the dropdown
                if (_scrollBarArea.Contains(cursorPoint))
                    return;
            }

            base.OnLeftClick(inputEventArgs);
        }

        // ---------- Dropdown State ----------
        protected override void CloseDropDown()
        {
            base.CloseDropDown();
            UpdateToolTipBlock();
            _isDraggingScrollBar = false;
            _skipNextItemSelection = false;
            _correctHoveredIndex = -1;
        }

        // ---------- Scroll Bar Calculation ----------
        private void CalculateScrollBar()
        {
            if (!_isScrollable || DropDownState == DropDownState.CLOSED)
                return;

            if (DropDownState == DropDownState.OPENED_DOWN)
            {
                _scrollBarArea = new Rectangle(
                    Width - _scrollBarWidth,
                    DropDownTexture.Height + 1,
                    _scrollBarWidth,
                    Height - DropDownTexture.Height - 2
                );
            }
            else // OPENED_UP
            {
                _scrollBarArea = new Rectangle(
                    Width - _scrollBarWidth,
                    1,
                    _scrollBarWidth,
                    Height - DropDownTexture.Height - 2
                );
            }

            _scrollThumbHeight = Math.Max(MinScrollThumbHeight,
                (int)((float)_maxVisibleItems / Items.Count * _scrollBarArea.Height));
        }

        // ---------- Update Logic ----------
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (DropDownState == DropDownState.CLOSED)
            {
                _correctHoveredIndex = -1;
                _previousMouseState = Mouse.GetState();
						 
            }

            Point cursorPoint = GetCursorPoint();
            MouseState mouseState = Mouse.GetState();

            // Disable hover index calculation while dragging the scroll bar
            if (_isDraggingScrollBar)
                _correctHoveredIndex = -1;
            else
                _correctHoveredIndex = GetHoveredIndexWithScroll(cursorPoint);

            // ---------- Scroll wheel with overflow protection ----------
            int scrollDelta = _previousMouseState.ScrollWheelValue - mouseState.ScrollWheelValue;
			
			// Use long to avoid Math.Abs(int.MinValue) overflow
            if (Math.Abs((long)scrollDelta) <= ScrollWheelOverflowThreshold && scrollDelta != 0)
            {
                bool scrollDown = scrollDelta > 0;

                if (DropDownState != DropDownState.CLOSED)
                {
                    if (_isScrollable)
                    {
                        int maxScrollOffset = Math.Max(0, Items.Count - _maxVisibleItems);
                        if (scrollDown)
                            _scrollOffset = Math.Min(_scrollOffset + 1, maxScrollOffset);
                        else
                            _scrollOffset = Math.Max(_scrollOffset - 1, 0);
                    }
                }
                else // Dropdown closed – change selected item only if mouse is over the control
                {
                    if (new Rectangle(0, 0, Width, Height).Contains(cursorPoint))
                    {
                        if (scrollDown && SelectedIndex < Items.Count - 1)
                            SelectedIndex++;
                        else if (!scrollDown && SelectedIndex > 0)
                            SelectedIndex--;
                    }
                }
            }

            // ---------- Scrollbar dragging & track click ----------
            if (_isScrollable)
            {
                // Start dragging when pressing left button inside the scroll bar area
                if (mouseState.LeftButton == ButtonState.Pressed && _scrollBarArea.Contains(cursorPoint))
                {
                    if (!_isDraggingScrollBar)
                        _isDraggingScrollBar = true;
                }

                // Continue updating position while dragging, even if cursor leaves the narrow bar
                if (_isDraggingScrollBar)
                {
                    float relativeY = (cursorPoint.Y - _scrollBarArea.Y) / (float)_scrollBarArea.Height;
                    relativeY = Math.Clamp(relativeY, 0f, 1f);
                    int maxScrollOffset = Math.Max(0, Items.Count - _maxVisibleItems);
                    _scrollOffset = (int)(relativeY * (maxScrollOffset + ScrollRoundingFactor));
                    _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScrollOffset);
                }

                // End dragging when left button is released
                if (mouseState.LeftButton == ButtonState.Released && _isDraggingScrollBar)
                {
                    _skipNextItemSelection = true;
                    _isDraggingScrollBar = false;
                }
            }

            _previousMouseState = mouseState;
        }

        // ---------- Hover Index ----------
        /// <summary>
        /// Determines which item the cursor is currently hovering over,
        /// taking the current scroll offset into account.
        /// </summary>
        private int GetHoveredIndexWithScroll(Point cursorPoint)
        {
            if (DropDownState == DropDownState.CLOSED)
                return -1;

            // If no scrolling is active, simply compute index without offset
            if (!_isScrollable)
            {
                Rectangle itemsArea = DropDownState == DropDownState.OPENED_DOWN
                    ? new Rectangle(0, DropDownTexture.Height + 1, Width, Height - DropDownTexture.Height - 2)
                    : new Rectangle(0, 1, Width, Height - DropDownTexture.Height - 2);

                if (!itemsArea.Contains(cursorPoint))
                    return -1;

                int relativeY = cursorPoint.Y - itemsArea.Y;
                int index = relativeY / ItemHeight;
                if (index >= 0 && index < Items.Count && Items[index].Selectable)
                    return index;
                return -1;
            }

            // Scrolled case: account for scroll offset and reserved scrollbar width
            Rectangle itemsAreaScrolled = DropDownState == DropDownState.OPENED_DOWN
                ? new Rectangle(0, DropDownTexture.Height + 1, Width - _scrollBarWidth, Height - DropDownTexture.Height - 2)
                : new Rectangle(0, 1, Width - _scrollBarWidth, Height - DropDownTexture.Height - 2);

            if (!itemsAreaScrolled.Contains(cursorPoint))
                return -1;

            int relativeYScrolled = cursorPoint.Y - itemsAreaScrolled.Y;
            int visibleIndex = relativeYScrolled / ItemHeight;

            if (visibleIndex < 0 || visibleIndex >= _maxVisibleItems)
                return -1;

            int realIndex = visibleIndex + _scrollOffset;
            if (realIndex >= 0 && realIndex < Items.Count && Items[realIndex].Selectable)
                return realIndex;

            return -1;
        }

        // ---------- Drawing ----------
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

            Rectangle itemRect = new Rectangle(1, y, Width - (_isScrollable ? _scrollBarWidth + 1 : 2), ItemHeight);
            FillRectangle(itemRect, isSelected ? FocusColor : BackColor);

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

                if (isHovered && item.Selectable)
                {
                    Vector2 textSize = Renderer.GetTextDimensions(item.Text, FontIndex);
                    int underlineY = y + ItemHeight - 3;
                    FillRectangle(new Rectangle(
                        (int)textPosition.X,
                        underlineY,
                        (int)textSize.X,
                        UnderlineThickness),
                        FocusColor);
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (DropDownState != DropDownState.CLOSED && _isScrollable)
                DrawScrollBar();
        }

        /// <summary>
        /// Draws the scroll bar track and thumb when the drop‑down is open and scrollable.
        /// </summary>
        private void DrawScrollBar()
        {

            if (_scrollBarArea.Height <= 0 || _scrollThumbHeight <= 0)
                return;

            FillRectangle(_scrollBarArea, BackColor);

            int maxScrollOffset = Math.Max(1, Items.Count - _maxVisibleItems);
            float scrollPercentage = _scrollOffset / (float)maxScrollOffset;
            int thumbY = _scrollBarArea.Y + (int)(scrollPercentage * (_scrollBarArea.Height - _scrollThumbHeight));
            thumbY = Math.Min(_scrollBarArea.Y + _scrollBarArea.Height - _scrollThumbHeight, thumbY);

            Rectangle thumbRect = new Rectangle(
                _scrollBarArea.X,
                thumbY,
                _scrollBarWidth,
                _scrollThumbHeight
            );

            FillRectangle(thumbRect, FocusColor);
            DrawRectangle(thumbRect, BorderColor, 1);
        }

        // ---------- Dropdown Layout ----------
        private void AdjustDropDownHeight()
        {
            int originalHeight = Height;
            int visibleItemCount = _maxVisibleItems > 0
                ? Math.Min(_maxVisibleItems, Items.Count)
                : Items.Count;

            int newHeight = DropDownTexture.Height + 2 + ItemHeight * visibleItemCount;

            if (DropDownState == DropDownState.OPENED_UP)
                Y -= (newHeight - originalHeight);

            Height = newHeight;
        }

        /// <summary>
        /// Ensures that the selected index is visible within the scroll area.
        /// </summary>
        /// <param name="resetOnNoSelection">If true and no item is selected, resets scroll offset to 0.</param>
        private void EnsureSelectedIndexVisible(bool resetOnNoSelection = false)
        {
            if (!_isScrollable || SelectedIndex < 0)
            {
                if (resetOnNoSelection)
                    _scrollOffset = 0;
                return;
            }

            if (SelectedIndex < _scrollOffset)
                _scrollOffset = SelectedIndex;
            else if (SelectedIndex >= _scrollOffset + _maxVisibleItems)
                _scrollOffset = SelectedIndex - _maxVisibleItems + 1;
        }

        // ---------- Event Handlers ----------
        private void XNAClientDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DropDownState != DropDownState.CLOSED && _isScrollable)
                EnsureSelectedIndexVisible(resetOnNoSelection: false);
        }

        // ---------- Tooltip ----------
        protected void UpdateToolTipBlock()
        {
            if (ToolTip == null)
                return;

            ToolTip.Blocked = DropDownState != DropDownState.CLOSED;
        }

        // ---------- Resource Cleanup ----------
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                SelectedIndexChanged -= XNAClientDropDown_SelectedIndexChanged;
            base.Dispose(disposing);
        }
    }
}