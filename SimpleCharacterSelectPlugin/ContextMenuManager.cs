using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SimpleCharacterSelectPlugin.Managers
{
    public class ContextMenuManager : IDisposable
    {
        private readonly Plugin plugin;
        private readonly IContextMenu contextMenu;

        private static readonly string[] ValidAddons =
        [
            "PartyMemberList",
            "FriendList",
            "FreeCompany",
            "LinkShell",
            "CrossWorldLinkshell",
            "_PartyList",
            "ChatLog",
            "LookingForGroup",
            "BlackList",
            "ContentMemberList",
            "SocialList",
            "ContactList",
            "CharacterInspect",
            "_Target",
            "NamePlate",
            "_NaviMap",
            "SelectString",
            "SelectIconString"
        ];

        private static readonly Dictionary<uint, string> WorldIdToName = new()
        {
            { 404, "Marilith" },
            { 410, "Rafflesia" },
            { 411, "White Rook" },
            { 100, "FictitiousWorld" },
        };

        public ContextMenuManager(Plugin plugin, IContextMenu contextMenu)
        {
            this.plugin = plugin;
            this.contextMenu = contextMenu;
            this.contextMenu.OnMenuOpened += OnMenuOpened;
        }

        public void Dispose()
        {
            this.contextMenu.OnMenuOpened -= OnMenuOpened;
        }

        private void OnMenuOpened(IMenuOpenedArgs args)
        {
            if (args.Target is MenuTargetDefault def && ValidAddons.Contains(args.AddonName))
            {
                HandleUIContextMenu(args, def);
                return;
            }

            if (args.Target is MenuTargetDefault objTarget && args.AddonName == null)
            {
                HandleGameObjectContextMenu(args, objTarget);
                return;
            }
        }

        private void HandleUIContextMenu(IMenuOpenedArgs args, MenuTargetDefault def)
        {
            if (def.TargetHomeWorld.RowId == 0)
                return;

            var name = def.TargetName;
            var worldRow = def.TargetHomeWorld;

            string worldName = worldRow.RowId > 0
                ? worldRow.Value.Name.ToString()
                : $"World-{worldRow.RowId}";
        }

        private void HandleGameObjectContextMenu(IMenuOpenedArgs args, MenuTargetDefault target)
        {

        }

        private void BlockUser(string physicalName, string csName)
        {
            plugin.Configuration.BlockedCSUsers.Add(physicalName);
            plugin.Configuration.Save();
            Plugin.Log.Info($"Blocked CS+ user: {physicalName} (CS+ name: {csName})");
        }
    }
}
