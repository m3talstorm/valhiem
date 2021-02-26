﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
	public Inventory(string name, Sprite bkg, int w, int h)
	{
		this.m_bkg = bkg;
		this.m_name = name;
		this.m_width = w;
		this.m_height = h;
	}

	private bool AddItem(ItemDrop.ItemData item, int amount, int x, int y)
	{
		amount = Mathf.Min(amount, item.m_stack);
		if (x < 0 || y < 0 || x >= this.m_width || y >= this.m_height)
		{
			return false;
		}
		ItemDrop.ItemData itemAt = this.GetItemAt(x, y);
		bool result;
		if (itemAt != null)
		{
			if (itemAt.m_shared.m_name != item.m_shared.m_name || (itemAt.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality))
			{
				return false;
			}
			int num = itemAt.m_shared.m_maxStackSize - itemAt.m_stack;
			if (num <= 0)
			{
				return false;
			}
			int num2 = Mathf.Min(num, amount);
			itemAt.m_stack += num2;
			item.m_stack -= num2;
			result = (num2 == amount);
			ZLog.Log(string.Concat(new object[]
			{
				"Added to stack",
				itemAt.m_stack,
				" ",
				item.m_stack
			}));
		}
		else
		{
			ItemDrop.ItemData itemData = item.Clone();
			itemData.m_stack = amount;
			itemData.m_gridPos = new Vector2i(x, y);
			this.m_inventory.Add(itemData);
			item.m_stack -= amount;
			result = true;
		}
		this.Changed();
		return result;
	}

	public bool CanAddItem(GameObject prefab, int stack = -1)
	{
		ItemDrop component = prefab.GetComponent<ItemDrop>();
		return !(component == null) && this.CanAddItem(component.m_itemData, stack);
	}

	public bool CanAddItem(ItemDrop.ItemData item, int stack = -1)
	{
		if (this.HaveEmptySlot())
		{
			return true;
		}
		if (stack <= 0)
		{
			stack = item.m_stack;
		}
		return this.FindFreeStackSpace(item.m_shared.m_name) >= stack;
	}

	public bool AddItem(ItemDrop.ItemData item)
	{
		bool result = true;
		if (item.m_shared.m_maxStackSize > 1)
		{
			int i = 0;
			while (i < item.m_stack)
			{
				ItemDrop.ItemData itemData = this.FindFreeStackItem(item.m_shared.m_name, item.m_quality);
				if (itemData != null)
				{
					itemData.m_stack++;
					i++;
				}
				else
				{
					int stack = item.m_stack - i;
					item.m_stack = stack;
					Vector2i vector2i = this.FindEmptySlot(this.TopFirst(item));
					if (vector2i.x >= 0)
					{
						item.m_gridPos = vector2i;
						this.m_inventory.Add(item);
						break;
					}
					result = false;
					break;
				}
			}
		}
		else
		{
			Vector2i vector2i2 = this.FindEmptySlot(this.TopFirst(item));
			if (vector2i2.x >= 0)
			{
				item.m_gridPos = vector2i2;
				this.m_inventory.Add(item);
			}
			else
			{
				result = false;
			}
		}
		this.Changed();
		return result;
	}

	private bool TopFirst(ItemDrop.ItemData item)
	{
		return item.IsWeapon() || (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);
	}

	public void MoveAll(Inventory fromInventory)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
		List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData itemData in list)
		{
			if (this.AddItem(itemData, itemData.m_stack, itemData.m_gridPos.x, itemData.m_gridPos.y))
			{
				fromInventory.RemoveItem(itemData);
			}
			else
			{
				list2.Add(itemData);
			}
		}
		foreach (ItemDrop.ItemData item in list2)
		{
			if (!this.AddItem(item))
			{
				break;
			}
			fromInventory.RemoveItem(item);
		}
		this.Changed();
		fromInventory.Changed();
	}

	public void MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item)
	{
		if (this.AddItem(item))
		{
			fromInventory.RemoveItem(item);
		}
		this.Changed();
		fromInventory.Changed();
	}

	public bool MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)
	{
		bool result = this.AddItem(item, amount, x, y);
		if (item.m_stack == 0)
		{
			fromInventory.RemoveItem(item);
			return result;
		}
		fromInventory.Changed();
		return result;
	}

	public bool RemoveItem(int index)
	{
		if (index < 0 || index >= this.m_inventory.Count)
		{
			return false;
		}
		this.m_inventory.RemoveAt(index);
		this.Changed();
		return true;
	}

	public bool ContainsItem(ItemDrop.ItemData item)
	{
		return this.m_inventory.Contains(item);
	}

	public bool RemoveOneItem(ItemDrop.ItemData item)
	{
		if (!this.m_inventory.Contains(item))
		{
			return false;
		}
		if (item.m_stack > 1)
		{
			item.m_stack--;
			this.Changed();
		}
		else
		{
			this.m_inventory.Remove(item);
			this.Changed();
		}
		return true;
	}

	public bool RemoveItem(ItemDrop.ItemData item)
	{
		if (!this.m_inventory.Contains(item))
		{
			ZLog.Log("Item is not in this container");
			return false;
		}
		this.m_inventory.Remove(item);
		this.Changed();
		return true;
	}

	public bool RemoveItem(ItemDrop.ItemData item, int amount)
	{
		amount = Mathf.Min(item.m_stack, amount);
		if (amount == item.m_stack)
		{
			return this.RemoveItem(item);
		}
		if (!this.m_inventory.Contains(item))
		{
			return false;
		}
		item.m_stack -= amount;
		this.Changed();
		return true;
	}

	public void RemoveItem(string name, int amount)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name)
			{
				int num = Mathf.Min(itemData.m_stack, amount);
				itemData.m_stack -= num;
				amount -= num;
				if (amount <= 0)
				{
					break;
				}
			}
		}
		this.m_inventory.RemoveAll((ItemDrop.ItemData x) => x.m_stack <= 0);
		this.Changed();
	}

	public bool HaveItem(string name)
	{
		using (List<ItemDrop.ItemData>.Enumerator enumerator = this.m_inventory.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_shared.m_name == name)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void GetAllPieceTables(List<PieceTable> tables)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_buildPieces != null && !tables.Contains(itemData.m_shared.m_buildPieces))
			{
				tables.Add(itemData.m_shared.m_buildPieces);
			}
		}
	}

	public int CountItems(string name)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name)
			{
				num += itemData.m_stack;
			}
		}
		return num;
	}

	public ItemDrop.ItemData GetItem(int index)
	{
		return this.m_inventory[index];
	}

	public ItemDrop.ItemData GetItem(string name)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name)
			{
				return itemData;
			}
		}
		return null;
	}

	public ItemDrop.ItemData GetAmmoItem(string ammoName)
	{
		int num = 0;
		ItemDrop.ItemData itemData = null;
		foreach (ItemDrop.ItemData itemData2 in this.m_inventory)
		{
			if ((itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) && itemData2.m_shared.m_ammoType == ammoName)
			{
				int num2 = itemData2.m_gridPos.y * this.m_width + itemData2.m_gridPos.x;
				if (num2 < num || itemData == null)
				{
					num = num2;
					itemData = itemData2;
				}
			}
		}
		return itemData;
	}

	private int FindFreeStackSpace(string name)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_stack < itemData.m_shared.m_maxStackSize)
			{
				num += itemData.m_shared.m_maxStackSize - itemData.m_stack;
			}
		}
		return num;
	}

	private ItemDrop.ItemData FindFreeStackItem(string name, int quality)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_quality == quality && itemData.m_stack < itemData.m_shared.m_maxStackSize)
			{
				return itemData;
			}
		}
		return null;
	}

	public int NrOfItems()
	{
		return this.m_inventory.Count;
	}

	public float SlotsUsedPercentage()
	{
		return (float)this.m_inventory.Count / (float)(this.m_width * this.m_height) * 100f;
	}

	public void Print()
	{
		for (int i = 0; i < this.m_inventory.Count; i++)
		{
			ItemDrop.ItemData itemData = this.m_inventory[i];
			ZLog.Log(string.Concat(new object[]
			{
				i.ToString(),
				": ",
				itemData.m_shared.m_name,
				"  ",
				itemData.m_stack,
				" / ",
				itemData.m_shared.m_maxStackSize
			}));
		}
	}

	public int GetEmptySlots()
	{
		return this.m_height * this.m_width - this.m_inventory.Count;
	}

	public bool HaveEmptySlot()
	{
		return this.m_inventory.Count < this.m_width * this.m_height;
	}

	private Vector2i FindEmptySlot(bool topFirst)
	{
		if (topFirst)
		{
			for (int i = 0; i < this.m_height; i++)
			{
				for (int j = 0; j < this.m_width; j++)
				{
					if (this.GetItemAt(j, i) == null)
					{
						return new Vector2i(j, i);
					}
				}
			}
		}
		else
		{
			for (int k = this.m_height - 1; k >= 0; k--)
			{
				for (int l = 0; l < this.m_width; l++)
				{
					if (this.GetItemAt(l, k) == null)
					{
						return new Vector2i(l, k);
					}
				}
			}
		}
		return new Vector2i(-1, -1);
	}

	public ItemDrop.ItemData GetOtherItemAt(int x, int y, ItemDrop.ItemData oldItem)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData != oldItem && itemData.m_gridPos.x == x && itemData.m_gridPos.y == y)
			{
				return itemData;
			}
		}
		return null;
	}

	public ItemDrop.ItemData GetItemAt(int x, int y)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_gridPos.x == x && itemData.m_gridPos.y == y)
			{
				return itemData;
			}
		}
		return null;
	}

	public List<ItemDrop.ItemData> GetEquipedtems()
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_equiped)
			{
				list.Add(itemData);
			}
		}
		return list;
	}

	public void GetWornItems(List<ItemDrop.ItemData> worn)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_useDurability && itemData.m_durability < itemData.GetMaxDurability())
			{
				worn.Add(itemData);
			}
		}
	}

	public void GetValuableItems(List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_value > 0)
			{
				items.Add(itemData);
			}
		}
	}

	public List<ItemDrop.ItemData> GetAllItems()
	{
		return this.m_inventory;
	}

	public void GetAllItems(string name, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name)
			{
				items.Add(itemData);
			}
		}
	}

	public void GetAllItems(ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_itemType == type)
			{
				items.Add(itemData);
			}
		}
	}

	public int GetWidth()
	{
		return this.m_width;
	}

	public int GetHeight()
	{
		return this.m_height;
	}

	public string GetName()
	{
		return this.m_name;
	}

	public Sprite GetBkg()
	{
		return this.m_bkg;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(this.currentVersion);
		pkg.Write(this.m_inventory.Count);
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_dropPrefab == null)
			{
				ZLog.Log("Item missing prefab " + itemData.m_shared.m_name);
				pkg.Write("");
			}
			else
			{
				pkg.Write(itemData.m_dropPrefab.name);
			}
			pkg.Write(itemData.m_stack);
			pkg.Write(itemData.m_durability);
			pkg.Write(itemData.m_gridPos);
			pkg.Write(itemData.m_equiped);
			pkg.Write(itemData.m_quality);
			pkg.Write(itemData.m_variant);
			pkg.Write(itemData.m_crafterID);
			pkg.Write(itemData.m_crafterName);
		}
	}

	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		int num2 = pkg.ReadInt();
		this.m_inventory.Clear();
		for (int i = 0; i < num2; i++)
		{
			string text = pkg.ReadString();
			int stack = pkg.ReadInt();
			float durability = pkg.ReadSingle();
			Vector2i pos = pkg.ReadVector2i();
			bool equiped = pkg.ReadBool();
			int quality = 1;
			if (num >= 101)
			{
				quality = pkg.ReadInt();
			}
			int variant = 0;
			if (num >= 102)
			{
				variant = pkg.ReadInt();
			}
			long crafterID = 0L;
			string crafterName = "";
			if (num >= 103)
			{
				crafterID = pkg.ReadLong();
				crafterName = pkg.ReadString();
			}
			if (text != "")
			{
				this.AddItem(text, stack, durability, pos, equiped, quality, variant, crafterID, crafterName);
			}
		}
		this.Changed();
	}

	public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Invalid item " + name);
			return null;
		}
		if (this.FindEmptySlot(this.TopFirst(component.m_itemData)).x == -1)
		{
			return null;
		}
		ItemDrop.ItemData result = null;
		int i = stack;
		while (i > 0)
		{
			ZNetView.m_forceDisableInit = true;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab);
			ZNetView.m_forceDisableInit = false;
			ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
			if (component2 == null)
			{
				ZLog.Log("Missing itemdrop in " + name);
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			int num = Mathf.Min(i, component2.m_itemData.m_shared.m_maxStackSize);
			i -= num;
			component2.m_itemData.m_stack = num;
			component2.m_itemData.m_quality = quality;
			component2.m_itemData.m_variant = variant;
			component2.m_itemData.m_durability = component2.m_itemData.GetMaxDurability();
			component2.m_itemData.m_crafterID = crafterID;
			component2.m_itemData.m_crafterName = crafterName;
			this.AddItem(component2.m_itemData);
			result = component2.m_itemData;
			UnityEngine.Object.Destroy(gameObject);
		}
		return result;
	}

	private bool AddItem(string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return false;
		}
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab);
		ZNetView.m_forceDisableInit = false;
		ItemDrop component = gameObject.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Missing itemdrop in " + name);
			UnityEngine.Object.Destroy(gameObject);
			return false;
		}
		component.m_itemData.m_stack = Mathf.Min(stack, component.m_itemData.m_shared.m_maxStackSize);
		component.m_itemData.m_durability = durability;
		component.m_itemData.m_equiped = equiped;
		component.m_itemData.m_quality = quality;
		component.m_itemData.m_variant = variant;
		component.m_itemData.m_crafterID = crafterID;
		component.m_itemData.m_crafterName = crafterName;
		this.AddItem(component.m_itemData, component.m_itemData.m_stack, pos.x, pos.y);
		UnityEngine.Object.Destroy(gameObject);
		return true;
	}

	public void MoveInventoryToGrave(Inventory original)
	{
		this.m_inventory.Clear();
		this.m_width = original.m_width;
		this.m_height = original.m_height;
		foreach (ItemDrop.ItemData itemData in original.m_inventory)
		{
			if (!itemData.m_shared.m_questItem && !itemData.m_equiped)
			{
				this.m_inventory.Add(itemData);
			}
		}
		original.m_inventory.RemoveAll((ItemDrop.ItemData x) => !x.m_shared.m_questItem && !x.m_equiped);
		original.Changed();
		this.Changed();
	}

	private void Changed()
	{
		this.UpdateTotalWeight();
		if (this.m_onChanged != null)
		{
			this.m_onChanged();
		}
	}

	public void RemoveAll()
	{
		this.m_inventory.Clear();
		this.Changed();
	}

	private void UpdateTotalWeight()
	{
		this.m_totalWeight = 0f;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			this.m_totalWeight += itemData.GetWeight();
		}
	}

	public float GetTotalWeight()
	{
		return this.m_totalWeight;
	}

	public void GetBoundItems(List<ItemDrop.ItemData> bound)
	{
		bound.Clear();
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_gridPos.y == 0)
			{
				bound.Add(itemData);
			}
		}
	}

	public bool IsTeleportable()
	{
		using (List<ItemDrop.ItemData>.Enumerator enumerator = this.m_inventory.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.m_shared.m_teleportable)
				{
					return false;
				}
			}
		}
		return true;
	}

	private int currentVersion = 103;

	public Action m_onChanged;

	private string m_name = "";

	private Sprite m_bkg;

	private List<ItemDrop.ItemData> m_inventory = new List<ItemDrop.ItemData>();

	private int m_width = 4;

	private int m_height = 4;

	private float m_totalWeight;
}
