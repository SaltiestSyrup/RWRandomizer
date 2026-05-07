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
    private readonly Vector2Int dimensions = new(10, 6);
    private readonly int elementsPerPage;

    private RoundedRect roundedRect;
    private MenuLabel label;
    private LevelSelector.ScrollButton pageLeftButton;
    private LevelSelector.ScrollButton pageRightButton;
    private BorderlessSymbolButton[] buttons;

    public List<int> selectedIndices = [];
    private int currentPage = 0;

    public PendingItemsDisplay(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
    {
        elementsPerPage = dimensions.x * dimensions.y;
        Unlock.Item[] pendingItems = [.. Plugin.RandoManager.itemDeliveryQueue];
        buttons = new BorderlessSymbolButton[pendingItems.Length];

        size = new Vector2(dimensions.x * 30f + 10f, (dimensions.y - 1) * 30f + 62f);

        myContainer = new FContainer();
        owner.Container.AddChild(myContainer);

        roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
        {
            fillAlpha = 1f
        };
        subObjects.Add(roundedRect);

        pageLeftButton = new LevelSelector.ScrollButton(menu, this, "PAGE_LEFT",
            new Vector2(size.x - 62.5f, -30f), 3);
        subObjects.Add(pageLeftButton);
        pageRightButton = new LevelSelector.ScrollButton(menu, this, "PAGE_RIGHT",
            new Vector2(size.x - 32.5f, -30f), 1);
        subObjects.Add(pageRightButton);

        label = new MenuLabel(menu, this, "Pending items (Click to retrieve)", new Vector2(10.01f, -15.01f), default, false);
        label.label.color = RWMenu.MenuRGB(RWMenu.MenuColors.White);
        label.label.alignment = FLabelAlignment.Left;
        subObjects.Add(label);

        for (int i = 0; i < pendingItems.Length; i++)
        {
            int j = i % elementsPerPage;
            buttons[i] = new BorderlessSymbolButton(menu, this, ItemToFSprite(pendingItems[i]), $"OBJ_{i}",
                new Vector2(30f * (j % dimensions.x) + 5f, -(30f * Mathf.FloorToInt(j / dimensions.x)) - 55f));
            subObjects.Add(buttons[i]);
        }
        SwitchToPage(0);
    }

    private void SwitchToPage(int newPage)
    {
        currentPage = Mathf.Clamp(newPage, 0, (buttons.Length - 1) / elementsPerPage);
        pageLeftButton.buttonBehav.greyedOut = currentPage == 0;
        pageRightButton.buttonBehav.greyedOut = currentPage == (buttons.Length - 1) / elementsPerPage;
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i >= currentPage * elementsPerPage && i < (currentPage + 1) * elementsPerPage)
                buttons[i].Show();
            else
                buttons[i].Hide();
        }
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        switch (message)
        {
            case "PAGE_LEFT":
                SwitchToPage(currentPage - 1);
                break;
            case "PAGE_RIGHT":
                SwitchToPage(currentPage + 1);
                break;
            default:
                if (message.StartsWith("OBJ_") && int.TryParse(message.Substring(4), out int index))
                {
                    selectedIndices.Add(index);
                    buttons[index].Disable();
                }
                break;
        }
    }

    private static FSprite ItemToFSprite(Unlock.Item item)
    {
        IconSymbol.IconSymbolData iconData;

        switch (item.id)
        {
            case "FireSpear" or "ExplosiveSpear":
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
                break;
            case "ElectricSpear":
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
                break;
            case "PoisonSpear":
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 4);
                break;
            default:
            {
                if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(item.type.value))
                {
                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(item.type.value), 0);
                    if (item.id is "RotFruit") iconData.intData = 1;
                }
                else
                {
                    return new FSprite("Futile_White", true);
                }

                break;
            }
        }

        float spriteScale = 1f;
        string spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
        Color spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);

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