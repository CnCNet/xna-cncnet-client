using System;
using System.Collections.Generic;
using System.Linq;
using clientdx.DXGUI.Generic;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    public class TeamStartMappingPresetMenu : XNAAdvancedContextMenu
    {
        private XNAAdvancedContextMenuItem savePresetItem;
        private XNAAdvancedContextMenuItem saveAsPresetItem;

        public TeamStartMappingPresetMenu(WindowManager windowManager) : base(windowManager)
        {
            
            DrawBorders = true;
            BackgroundTexture = AssetLoader.CreateTexture(Color.Black, 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
            savePresetItem = new XNAAdvancedContextMenuItem(windowManager)
            {
                Text = "Save"
            };

            saveAsPresetItem = new XNAAdvancedContextMenuItem(windowManager)
            {
                Text = "Save As"
            };
        }

        private void ClearItems()
        {
            var children = Children.ToList();
            foreach (XNAControl xnaControl in children)
                RemoveChild(xnaControl);
        }

        public void ReinitItems()
        {
            ClearItems();
            AddChild(savePresetItem);
            AddChild(saveAsPresetItem);
        }

        public void AddPresetList(List<TeamStartMappingPreset> presets, string headerLabel, Action<TeamStartMappingPreset> selectAction)
        {
            if (!presets.Any())
                return;

            AddChild(new XNAClientContextMenuDividerItem(WindowManager));
            AddChild(new XNAAdvancedContextMenuItem(WindowManager)
            {
                Text = headerLabel,
                Selectable = false
            });
            presets.ForEach(preset => AddChild(new TeamStartMappingPresetMenuItem(WindowManager)
            {
                Item = preset,
                Text = $" {preset.Name}",
                SelectAction = selectAction
            }));
        }

        public List<TeamStartMappingPresetMenuItem> GetPresetItems() =>
            Children
                .OfType<TeamStartMappingPresetMenuItem>()
                .ToList();

        //
        public List<TeamStartMappingPreset> GetPresets() =>
            GetPresetItems()
                .Select(i => i.Item)
                .ToList();

        public void Open(Point point, TeamStartMappingPreset currentMappingPreset)
        {
            if (Visible)
            {
                Attach();
                Disable();
                return;
            }


            Detach();


            savePresetItem.Selectable = currentMappingPreset?.CanSave ?? false;

            ClientRectangle = new Rectangle(point.X, point.Y, 100, 100);
            Enable();
        }

        public override void Draw(GameTime gameTime)
        {
            var children = Children.ToList();
            Height = 0;
            children.ForEach(child => Height += child.Height);
            
            FillRectangle(ClientRectangle, Color.Black);
            base.Draw(gameTime);
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            Disable();
        }
    }
}
