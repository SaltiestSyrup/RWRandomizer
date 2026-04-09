using System.Collections.Generic;
using System.Linq;
using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

/// <summary>
/// Simple display of items pending delivery
/// </summary>
public class PendingItemsDisplay : RectangularMenuObject
{
    private const int BASE_ELEMENTS_PER_ROW = 15;
    private const int MAXIMUM_ROWS = 9;
    public readonly int elementsPerRow = BASE_ELEMENTS_PER_ROW;

    public RoundedRect roundedRect;
    public MenuLabel label;
    public BorderlessSymbolButton[] buttons;

    public List<int> selectedIndices = [];

    public PendingItemsDisplay(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
    {
        Unlock.Item[] pendingItems = [.. Plugin.RandoManager.itemDeliveryQueue];
        buttons = new BorderlessSymbolButton[pendingItems.Length];

        int maxRows = Mathf.FloorToInt((pos.y - 57f) / 30f);
        if (pendingItems.Length > BASE_ELEMENTS_PER_ROW * maxRows)
        {
            elementsPerRow = Mathf.CeilToInt((float)pendingItems.Length / maxRows);
        }

        size = new Vector2((elementsPerRow * 30f) + 10f, ((pendingItems.Length - 1) / elementsPerRow * 30f) + 57f);

        myContainer = new FContainer();
        owner.Container.AddChild(myContainer);

        roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
        {
            fillAlpha = 1f
        };
        subObjects.Add(roundedRect);

        label = new MenuLabel(menu, this, "Pending items (Click to retrieve)", new Vector2(10.01f, -13.01f), default, false, null);
        label.label.color = RWMenu.MenuRGB(RWMenu.MenuColors.White);
        label.label.alignment = FLabelAlignment.Left;
        subObjects.Add(label);

        for (int i = 0; i < pendingItems.Length; i++)
        {
            buttons[i] = new(menu, this, ItemToFSprite(pendingItems[i]), $"OBJ_{i}",
                new((30f * (i % elementsPerRow)) + 5f, -(30f * Mathf.FloorToInt(i / elementsPerRow)) - 50f));
            subObjects.Add(buttons[i]);
        }
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message.StartsWith("OBJ_"))
        {
            if (!int.TryParse(message.Substring(4), out int index)) return;

            selectedIndices.Add(index);
            buttons[index].buttonBehav.greyedOut = true;
        }
    }

    public static FSprite ItemToFSprite(Unlock.Item item)
    {
        string spriteName;
        float spriteScale = 1f;
        Color spriteColor;

        IconSymbol.IconSymbolData iconData;

        if (item.id is "FireSpear" or "ExplosiveSpear")
        {
            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
        }
        else if (item.id is "ElectricSpear")
        {
            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
        }
        else if (item.id is "PoisonSpear")
        {
            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 4);
        }
        else if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(item.type.value))
        {
            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(item.type.value), 0);
            if (item.id is "RotFruit") iconData.intData = 1;
        }
        else
        {
            return new FSprite("Futile_White", true);
        }

        spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
        spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);

        try
        {
            return new FSprite(spriteName, true)
            {
                scale = spriteScale,
                color = spriteColor,
            };
        }
        catch
        {
            Plugin.Log.LogError($"Failed to load sprite '{spriteName}'");
            return new FSprite("Futile_White", true);
        }
    }
}