using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class BorderlessSymbolButton : ButtonTemplate
{
    public string signalText;
    public FSprite symbolSprite;
    private readonly Color baseColor;
    private readonly Color greyColor;
    private readonly float baseScale;

    public BorderlessSymbolButton(RWMenu menu, MenuObject owner, string symbolName, string signalText, Vector2 pos) : base(menu, owner, pos, new(24f, 24f))
    {
        this.signalText = signalText;

        symbolSprite = new FSprite(symbolName, true);
        baseColor = RWMenu.MenuRGB(RWMenu.MenuColors.MediumGrey);
        greyColor = RWMenu.MenuRGB(RWMenu.MenuColors.VeryDarkGrey);
        baseScale = 1f;
        Container.AddChild(symbolSprite);
    }

    public BorderlessSymbolButton(RWMenu menu, MenuObject owner, FSprite sprite, string signalText, Vector2 pos) : base(menu, owner, pos, new(24f, 24f))
    {
        this.signalText = signalText;

        symbolSprite = sprite;
        baseColor = sprite.color;
        greyColor = RWMenu.MenuRGB(RWMenu.MenuColors.VeryDarkGrey);
        baseScale = sprite.scale;
        Container.AddChild(symbolSprite);
    }

    public override void Update()
    {
        base.Update();
        symbolSprite.scale = baseScale * (1 + buttonBehav.sizeBump * 0.2f);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        float cycle = 0.5f - 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * Mathf.PI * 2f);
        cycle *= buttonBehav.sizeBump;

        symbolSprite.color = buttonBehav.greyedOut ? greyColor : Color.Lerp(baseColor, greyColor, cycle);
        symbolSprite.x = DrawX(timeStacker) + DrawSize(timeStacker).x / 2;
        symbolSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2;
    }

    public override void RemoveSprites()
    {
        symbolSprite.RemoveFromContainer();
        base.RemoveSprites();
    }

    public override void Clicked() => Singal(this, signalText);
}