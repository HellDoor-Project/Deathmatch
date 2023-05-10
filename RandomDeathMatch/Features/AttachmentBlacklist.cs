﻿using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static TheRiptide.Translation;

namespace TheRiptide
{
    //DOUBLE-SHOT SYSTEM
    public class AttachmentBlacklistConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("put black listed attachments here, see below for all attachments")]
        public List<AttachmentName> BlackList { get; set; } = new List<AttachmentName>();

        [Description("list of all the different attachments (changing this does nothing)")]
        public List<AttachmentName> AllAttachments { get; set; } = new List<AttachmentName>();
    }

    class AttachmentBlacklist
    {
        public static AttachmentBlacklist Singleton { get; private set; }
        private Dictionary<ItemType, uint> BannedWeaponCodes = new Dictionary<ItemType,  uint>();

        AttachmentBlacklistConfig config;

        public AttachmentBlacklist()
        {
            Singleton = this;
        }

        public void Init(AttachmentBlacklistConfig config, Deathmatch plugin)
        {
            this.config = config;
            config.AllAttachments.Clear();
            foreach (AttachmentName name in Enum.GetValues(typeof(AttachmentName)))
                config.AllAttachments.Add(name);
            PluginHandler handler = PluginHandler.Get(plugin);
            handler.SaveConfig(plugin, "attachment_blacklist_config");
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            //foreach(RoomIdentifier room in RoomIdentifier.AllRoomIdentifiers)
            //{
            //    WorkstationController wc = room.GetComponentInChildren<WorkstationController>();
            //    if (wc != null)
            //    NetworkServer.UnSpawn(wc.gameObject);
            //}
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangeItem(Player player, ushort old_item, ushort new_item)
        {
            if(player.ReferenceHub.inventory.UserInventory.Items.ContainsKey(new_item))
            {
                ItemBase item = player.ReferenceHub.inventory.UserInventory.Items[new_item];
                if(item is Firearm firearm)
                    RemoveBanned(player, firearm);
            }
        }

        [PluginEvent(ServerEventType.PlayerShotWeapon)]
        void OnShotWeapon(Player player, Firearm firearm)
        {
            RemoveBanned(player, firearm);
        }

        private void RemoveBanned(Player player, Firearm firearm)
        {
            if(!BannedWeaponCodes.ContainsKey(firearm.ItemTypeId))
            {
                int bit_pos = 0;
                uint code_mask = 0;
                foreach (var a in firearm.Attachments)
                {
                    if(config.BlackList.Contains(a.Name))
                        code_mask |= (1U << bit_pos);
                    bit_pos++;
                }
                BannedWeaponCodes.Add(firearm.ItemTypeId, ~code_mask);
            }

            var s = firearm.Status;
            uint new_code = s.Attachments & BannedWeaponCodes[firearm.ItemTypeId];
            if(new_code != s.Attachments)
            {
                BitArray ba = new BitArray(BitConverter.GetBytes(~BannedWeaponCodes[firearm.ItemTypeId]));
                List<string> attachments = new List<string>();
                int index = 0;
                foreach(bool b in ba)
                {
                    if (b)
                        attachments.Add(firearm.Attachments[index].Name.ToString());
                    index++;
                    if (index >= firearm.Attachments.Length)
                        break;
                }
                BroadcastOverride.BroadcastLine(player, 1, 3.0f, BroadcastPriority.Medium, translation.AttachmentBanned.Replace("{attachment}", string.Join(", ", attachments)));
                BroadcastOverride.UpdateIfDirty(player);
                firearm.Status = new FirearmStatus(s.Ammo, s.Flags, new_code);
            }
        }
    }
}
