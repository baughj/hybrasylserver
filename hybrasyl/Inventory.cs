﻿/*
 * This file is part of Project Hybrasyl.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but
 * without ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the Affero General Public License
 * for more details.
 *
 * You should have received a copy of the Affero General Public License along
 * with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * (C) 2013 Justin Baugh (baughj@hybrasyl.com)
 * (C) 2015 Project Hybrasyl (info@hybrasyl.com)
 *
 * Authors:   Justin Baugh  <baughj@hybrasyl.com>
 *            Kyle Speck    <kojasou@hybrasyl.com>
 */

using Hybrasyl.Enums;
using Hybrasyl.Objects;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hybrasyl
{

    public class Exchange
    {
        public static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Inventory _sourceItems;
        private Inventory _targetItems;
        private uint _sourceGold;
        private uint _targetGold;
        private int _sourceSize;
        private int _targetSize;
        private User _source;
        private User _target;
        private bool _active;
        private int _sourceWeight;
        private int _targetWeight;
        private bool _sourceConfirmed;
        private bool _targetConfirmed;

        public Exchange(User source, User target)
        {
            _source = source;
            _target = target;
            _sourceItems = new Inventory(60);
            _targetItems = new Inventory(60);
            _sourceGold = 0;
            _targetGold = 0;
            _sourceWeight = 0;
            _targetWeight = 0;
            _sourceSize = source.Inventory.EmptySlots;
            _targetSize = target.Inventory.EmptySlots;
        }

        public static bool StartConditionsValid(User source, User target)
        {
            return source.Map == target.Map && source.IsInViewport(target) &&
                   target.IsInViewport(source) &&
                   source.Status == PlayerStatus.Alive &&
                   target.Status == PlayerStatus.Alive && target.Distance(source) <= Constants.EXCHANGE_DISTANCE;

        }

        public bool ConditionsValid => _source.Map == _target.Map && _source.IsInViewport(_target) &&
                                       _target.IsInViewport(_source) &&
                                       _source.Status.HasFlag(PlayerStatus.InExchange) &&
                                       _target.Status.HasFlag(PlayerStatus.InExchange) &&
                                       _active;

        public bool AddItem(User giver, byte slot, byte quantity = 1)
        {
            Item toAdd;
            // Some sanity checks

            // Check if our "exchange" is full
            if (_sourceItems.IsFull || _targetItems.IsFull)
            {
                _source.SendMessage("Maximum exchange size reached. No more items can be added.", MessageTypes.SYSTEM);
                _target.SendMessage("Maximum exchange size reached. No more items can be added.", MessageTypes.SYSTEM);
                return false;
            }
            // Check if either participant's inventory would be full as a result of confirmation
            if (_sourceItems.Count == _sourceSize || _targetItems.Count == _targetSize)
            {
                _source.SendMessage("Inventory full.", MessageTypes.SYSTEM);
                _target.SendMessage("Inventory full.", MessageTypes.SYSTEM); 
                return false;
            }

            // OK - we have room, now what?
            var theItem = giver.Inventory[slot];

            // Further checks!
            // Is the item exchangeable?

            if (!theItem.Exchangeable)
            {
                giver.SendMessage("You can't trade this.", MessageTypes.SYSTEM);
                return false;
            }

            // Weight check

            if (giver == _source && _targetWeight + theItem.Weight > _target.MaximumWeight)
            {
                _source.SendSystemMessage("It's too heavy.");
                _target.SendSystemMessage("They can't carry any more.");
                return false;
            }

            if (giver == _target && _sourceWeight + theItem.Weight > _source.MaximumWeight)
            {
                _target.SendSystemMessage("It's too heavy.");
                _source.SendSystemMessage("They can't carry any more.");
                return false;
            }

            // Is the item stackable?

            if (theItem.Stackable && theItem.Count > 1)
            {
                var targetItem = giver == _target ? _source.Inventory.Find(theItem.Name) : _target.Inventory.Find(theItem.Name);

                // Check to see that giver has sufficient number of whatever, and also that the quantity is a positive number
                if (quantity <= 0)
                {
                    giver.SendSystemMessage("You can't give zero of something, chief.");
                    return false;
                }

                if (quantity > theItem.Count)
                {
                    giver.SendSystemMessage($"You don't have that many {theItem.Name} to give!");
                    return false;
                }

                // Check if the recipient already has this item - if they do, ensure the quantity proposed for trade
                // wouldn't put them over maxcount for the item in question

                if (targetItem != null && targetItem.Count + quantity > theItem.MaximumStack)
                {
                    if (giver == _target)
                    {
                        _target.SendSystemMessage($"They can't carry any more {theItem.Name}");
                        _source.SendSystemMessage($"You can't carry any more {theItem.Name}.");
                    }
                    else
                    {
                        _source.SendSystemMessage($"They can't carry any more {theItem.Name}");
                        _target.SendSystemMessage($"You can't carry any more {theItem.Name}.");
                    }
                    return false;
                }
                theItem.Count -= quantity;
                giver.SendItemUpdate(theItem, slot);
                toAdd = new Item(theItem);
                toAdd.Count = quantity;
            }
            else if (!theItem.Stackable || theItem.Count == 1)
            {
                // Item isn't stackable or is a stack of one
                // Remove the item entirely from giver
                toAdd = theItem;
                giver.RemoveItem(slot);
            }
            else
            {
                Logger.WarnFormat("exchange: Hijinx occuring: participants are {0} and {1}",
                    _source.Name, _target.Name);
                _active = false;
                return false;
            }

            // Now add the item to the active exchange and make sure we update weight
            if (giver == _source)
            {
                var exchangeSlot = (byte)_sourceItems.Count;
                _sourceItems.AddItem(toAdd);
                _source.SendExchangeUpdate(toAdd, exchangeSlot);
                _target.SendExchangeUpdate(toAdd, exchangeSlot, false);
                _targetWeight += toAdd.Weight;
            }
            if (giver == _target)
            {
                var exchangeSlot = (byte) _targetItems.Count;
                _targetItems.AddItem(toAdd);
                _target.SendExchangeUpdate(toAdd, exchangeSlot);
                _source.SendExchangeUpdate(toAdd, exchangeSlot, false);
                _sourceWeight += toAdd.Weight;

            }

            return true;
        }

        public bool AddGold(User giver, uint amount)
        {
            if (giver == _source)
            {
                if (amount > uint.MaxValue - _sourceGold)
                {
                    _source.SendMessage("No more gold can be added to this exchange.", MessageTypes.SYSTEM);
                    return false;
                }
                if (amount > _source.Gold)
                {
                    _source.SendMessage("You don't have that much gold.", MessageTypes.SYSTEM);
                    return false;
                }
                _sourceGold += amount;
                _source.SendExchangeUpdate(amount);
                _target.SendExchangeUpdate(amount, false);
                _source.Gold -= amount;
                _source.UpdateAttributes(StatUpdateFlags.Experience);

            }
            else if (giver == _target)
            {
                if (amount > uint.MaxValue - _targetGold)
                {
                    _target.SendMessage("No more gold can be added to this exchange.", MessageTypes.SYSTEM);
                    return false;
                }
                _targetGold += amount;
                _target.SendExchangeUpdate(amount);
                _source.SendExchangeUpdate(amount, false);
                _target.Gold -= amount;
                _target.UpdateAttributes(StatUpdateFlags.Experience);
            }
            else
                return false;

            return true;

        }

        public bool StartExchange()
        {
            Logger.InfoFormat("Starting exchange between {0} and {1}", _source.Name, _target.Name);
            _active = true;
            _source.Status |= PlayerStatus.InExchange;
            _target.Status |= PlayerStatus.InExchange;
            _source.ActiveExchange = this;
            _target.ActiveExchange = this;
            // Send "open window" packet to both clients
            _target.SendExchangeInitiation(_source);
            _source.SendExchangeInitiation(_target);
            return true;
        }

        /// <summary>
        /// Cancel the exchange, returning all items from the window back to each player.
        /// </summary>
        /// <returns>Boolean indicating success. Better hope this is always true.</returns>
        public bool CancelExchange(User requestor)
        {
            foreach (var item in _sourceItems)
            {
                _source.AddItem(item);
            }
            foreach (var item in _targetItems)
            {
                _target.AddItem(item);
            }
            _source.AddGold(_sourceGold);
            _target.AddGold(_targetGold);
            _source.SendExchangeCancellation(requestor == _source);
            _target.SendExchangeCancellation(requestor == _target);
            _source.ActiveExchange = null;
            _target.ActiveExchange = null;
            _source.Status &= ~PlayerStatus.InExchange;
            _target.Status &= ~PlayerStatus.InExchange;
            return true;
        }

        /// <summary>
        /// Perform the exchange once confirmation from both sides is received.
        /// </summary>
        /// <returns></returns>
        public void PerformExchange()
        {
            Logger.Info("Performing exchange");
            foreach (var item in _sourceItems)
            {
                _target.AddItem(item);
            }
            foreach (var item in _targetItems)
            {
                _source.AddItem(item);
            }
            _source.AddGold(_targetGold);
            _target.AddGold(_sourceGold);

            _source.ActiveExchange = null;
            _target.ActiveExchange = null;
            _source.Status &= ~PlayerStatus.InExchange;
            _target.Status &= ~PlayerStatus.InExchange;
        }

        /// <summary>
        /// Confirm the exchange. Once both sides confirm, perform the exchange.
        /// </summary>
        /// <returns>Boolean indicating success.</returns>
        public void ConfirmExchange(User requestor)
        {
            if (_source == requestor)
            {
                Logger.InfoFormat("Exchange: source ({0}) confirmed", _source.Name);
                _sourceConfirmed = true;
                _target.SendExchangeConfirmation(false);
            }
            if (_target == requestor)
            {
                Logger.InfoFormat("Exchange: target ({0}) confirmed", _target.Name);
                _targetConfirmed = true;
                _source.SendExchangeConfirmation(false);
            }
            if (_sourceConfirmed && _targetConfirmed)
            {
                Logger.Info("Exchange: Both sides confirmed");
                _source.SendExchangeConfirmation();
                _target.SendExchangeConfirmation();
                PerformExchange();
            }
        }
    }

    public class InventoryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            var inventory = (Inventory) value;
            var output = new object[inventory.Size];
            for (byte i = 0; i < inventory.Size; i++)
            {
                var itemInfo = new Dictionary<string, object>();
                if (inventory[i] != null)
                {
                    itemInfo["Name"] = inventory[i].Name;
                    itemInfo["Count"] = inventory[i].Count;
                    output[i] = itemInfo;
                }               
            }
            var ja = Newtonsoft.Json.Linq.JArray.FromObject(output);
            serializer.Serialize(writer, ja);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jArray = JArray.Load(reader);
            var inv = new Inventory(jArray.Count);

            for (byte i = 0; i < jArray.Count; i++)
            {
                XSD.ItemType itmType = null;
                Dictionary<string, object> item;
                if (TryGetValue(jArray[i], out item))
                {                   
                    itmType = World.Items.Where(x => x.Value.Name == (string)item.FirstOrDefault().Value).FirstOrDefault().Value;
                    if (itmType != null)
                    {
                        inv[i] = new Item(itmType.Id, Game.World)
                        {
                            Count = item.ContainsKey("Count") ? Convert.ToInt32(item["Count"]) : 1
                        };
                            //this will need to be expanded later based on item properties being saved back to the database.
                    }
                }
            }

            return inv;
        }


        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Inventory);
        }

        public bool TryGetValue(Newtonsoft.Json.Linq.JToken token, out Dictionary<string, object> item)
        {
            item = null;
            if (!token.HasValues) return false;

            item = token.ToObject<Dictionary<string, object>>();
            return true;
        }
    }


    [JsonConverter(typeof(InventoryConverter))]
    public class Inventory : IEnumerable<Item>
    {
        public DateTime LastSaved { get; set; }

        private Item[] _items;
        private Dictionary<int, List<Item>> _inventoryIndex;

        public static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public int Size { get; private set; }
        public int Count { get; private set; }
        public int Weight { get; private set; }

        #region Equipment Properties

        public Item Weapon => _items[ServerItemSlots.Weapon];

        public Item Armor => _items[ServerItemSlots.Armor];

        public Item Shield => _items[ServerItemSlots.Shield];

        public Item Helmet => _items[ServerItemSlots.Helmet];

        public Item Earring => _items[ServerItemSlots.Earring];

        public Item Necklace => _items[ServerItemSlots.Necklace];

        public Item LRing => _items[ServerItemSlots.LHand];

        public Item RRing => _items[ServerItemSlots.RHand];

        public Item LGauntlet => _items[ServerItemSlots.LArm];

        public Item RGauntlet => _items[ServerItemSlots.RArm];

        public Item Belt => _items[ServerItemSlots.Waist];

        public Item Greaves => _items[ServerItemSlots.Leg];

        public Item Boots => _items[ServerItemSlots.Foot];

        public Item FirstAcc => _items[ServerItemSlots.FirstAcc];

        public Item Overcoat => _items[ServerItemSlots.Trousers];

        public Item DisplayHelm => _items[ServerItemSlots.Coat];

        public Item SecondAcc => _items[ServerItemSlots.SecondAcc];

        public Item ThirdAcc => _items[ServerItemSlots.ThirdAcc];

        #endregion Equipment Properties

        public bool IsFull => Count == Size;
        public int EmptySlots => Size - Count;

        public void RecalculateWeight()
        {
            Weight = 0;
            foreach (var item in this)
            {
                Weight += item.Weight;
            }
        }
        public Item this[byte slot]
        {
            get
            {
                var index = slot - 1;
                if (index < 0 || index >= Size)
                    return null;
                return _items[index];
            }
            internal set
            {
                var index = slot - 1;
                if (index < 0 || index >= Size)
                    return;
                if (value == null)
                    _RemoveFromIndex(_items[index]);
                else
                    _AddToIndex(value);
                _items[index] = value;
                
            }   
        }

        private void _AddToIndex(Item item)
        {
            List<Item> itemList;
            if (_inventoryIndex.TryGetValue(item.TemplateId, out itemList))
            {
                itemList.Add(item);
            }
            else 
                _inventoryIndex[item.TemplateId] = new List<Item> {item};
        }

        private void _RemoveFromIndex(Item item)
        {
            List<Item> itemList;
            if (_inventoryIndex.TryGetValue(item.TemplateId, out itemList))
            {
                _inventoryIndex[item.TemplateId] = itemList.Where(x => x.Id != item.Id).ToList();
                if (_inventoryIndex[item.TemplateId].Count == 0)
                    _inventoryIndex.Remove(item.TemplateId);
            }
        }

        public bool TryGetValue(string name, out Item item)
        {
            item = null;
            List<Item> itemList;
            XSD.ItemType theItem;
            if (!Game.World.TryGetItemTemplate(name, out theItem) ||
                !_inventoryIndex.TryGetValue(theItem.Id, out itemList)) return false;
            item = itemList.First();
            return true;
        }

        public bool TryGetValue(int templateId, out Item item)
        {
            item = null;
            List<Item> itemList;
            if (!_inventoryIndex.TryGetValue(templateId, out itemList)) return false;
            item = itemList.First();
            return true;

        }

        public Inventory(int size)
        {
            _items = new Item[size];
            Size = size;
            _inventoryIndex = new Dictionary<int, List<Item>>();
        }

        public bool Contains(int id)
        {
            return _inventoryIndex.ContainsKey(id);
        }

        public bool Contains(string name)
        {
            XSD.ItemType theItem;
            return Game.World.TryGetItemTemplate(name, out theItem) && _inventoryIndex.ContainsKey(theItem.Id);
        }

        public int FindEmptyIndex()
        {
            for (var i = 0; i < Size; ++i)
            {
                if (_items[i] == null)
                    return i;
            }
            return -1;
        }
        public byte FindEmptySlot()
        {
            return (byte)(FindEmptyIndex() + 1);
        }

        public int IndexOf(int id)
        {
            for (var i = 0; i < Size; ++i)
            {
                if (_items[i] != null && _items[i].TemplateId == id)
                    return i;
            }
            return -1;
        }
        public int IndexOf(string name)
        {
            for (var i = 0; i < Size; ++i)
            {
                if (_items[i] != null && _items[i].Name == name)
                    return i;
            }
            return -1;
        }

        public byte SlotOf(int id)
        {
            return (byte)(IndexOf(id) + 1);
        }
        public byte SlotOf(string name)
        {
            return (byte)(IndexOf(name) + 1);
        }

        public Item Find(int id)
        {
            return _inventoryIndex.ContainsKey(id) ? _inventoryIndex[id].First() : null;
        }

        public Item Find(string name)
        {
            XSD.ItemType theItem;
            return Game.World.TryGetItemTemplate(name, out theItem) && _inventoryIndex.ContainsKey(theItem.Id)
                ? _inventoryIndex[theItem.Id].First()
                : null;
        }

        public bool AddItem(Item item)
        {
            if (IsFull) return false;
            var slot = FindEmptySlot();
            return Insert(slot, item);
        }

        public bool Insert(byte slot, Item item)
        {
            var index = slot - 1;
            if (index < 0 || index >= Size || _items[index] != null)
                return false;
            _items[index] = item;
            Count += 1;
            Weight += item.Weight;
            _AddToIndex(item);

            return true;
        }

        public bool Remove(byte slot)
        {
            var index = slot - 1;
            if (index < 0 || index >= Size || _items[index] == null)
                return false;
            var item = _items[index];
            _items[index] = null;
            Count -= 1;
            Weight -= item.Weight;
            _RemoveFromIndex(item);

            return true;
        }

        public bool Swap(byte slot1, byte slot2)
        {
            int index1 = slot1 - 1, index2 = slot2 - 1;
            if (index1 < 0 || index1 >= Size || index2 < 0 || index2 >= Size)
                return false;
            var item = _items[index1];
            _items[index1] = _items[index2];
            _items[index2] = item;
            return true;
        }

        public void Clear()
        {
            for (var i = 0; i < Size; ++i)
                _items[i] = null;
            Count = 0;
            Weight = 0;
            _inventoryIndex.Clear();
        }

        public bool Increase(byte slot, int amount)
        {
            var index = slot - 1;
            if (index < 0 || index >= Size || _items[index] == null)
                return false;
            var item = _items[index];
            if (item.Count + amount > item.MaximumStack)
                return false;
            item.Count += amount;
            return true;
        }

        public bool Decrease(byte slot, int amount)
        {
            var index = slot - 1;
            if (index < 0 || index >= Size || _items[index] == null)
                return false;
            var item = _items[index];
            if (item.Count < amount)
                return false;
            item.Count -= amount;
            if (item.Count != 0) return true;
            _items[index] = null;
            Count -= 1;
            Weight -= item.Weight;
            return true;
        }

        public IEnumerator<Item> GetEnumerator()
        {
            for (var i = 0; i < Size; ++i)
            {
                if (_items[i] != null)
                    yield return _items[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<Tuple<ushort,byte>> GetEquipmentDisplayList()
        {
            var returnList = new List<Tuple<ushort, byte>>();

            for (var x = 0; x < 18; ++x)
            {
                // This is fucking bullshit. Why would you even do this? HEY I KNOW KOREAN INTERN DESIGNING
                // THIS PROTOCOL, LET'S RANDOMLY SWAP ITEM SLOTS FOR NO REASON!!11`1`
                if (x == ServerItemSlots.Foot)
                    returnList.Add(_items[ServerItemSlots.FirstAcc] == null
                        ? new Tuple<ushort, byte>(0, 0)
                        : new Tuple<ushort, byte>((ushort)(0x8000 + _items[ServerItemSlots.FirstAcc].EquipSprite), _items[ServerItemSlots.FirstAcc].Color));
                else if (x == ServerItemSlots.FirstAcc)
                    returnList.Add(_items[ServerItemSlots.Foot] == null
                        ? new Tuple<ushort, byte>(0, 0)
                        : new Tuple<ushort, byte>((ushort)(0x8000 + _items[ServerItemSlots.Foot].EquipSprite), _items[ServerItemSlots.Foot].Color));
                else
                    returnList.Add(_items[x] == null
                        ? new Tuple<ushort, byte>(0, 0)
                        : new Tuple<ushort, byte>((ushort) (0x8000 + _items[x].EquipSprite), _items[x].Color));
            }

            return returnList;
        }

    }
}
