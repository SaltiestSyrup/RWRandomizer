using Menu;
using RWCustom;
using UnityEngine;

namespace RainWorldRandomizer
{
    /// <summary>
    /// Mostly copies functionality of EndgameTokens, but allows for full control over their behavior
    /// </summary>
    public class FakeEndgameToken : PositionedMenuObject
    {
        public EndgameTokens Tokens
        {
            get
            {
                return owner as EndgameTokens;
            }
        }

        public WinState.EndgameID id;
        public int index;
        private FSprite symbolSprite;
        private FSprite circleSprite;
        private FSprite glowSprite;

        public float fade;
        public float lastFade;
        private float getToX;
        private float getToY;
        private float superGlow;
        private bool activated;

        public FakeEndgameToken(Menu.Menu menu, MenuObject owner, Vector2 pos, WinState.EndgameID id, FContainer container, int index) : base(menu, owner, pos)
        {
            this.id = id;
            this.index = index;
            myContainer = container;
            symbolSprite = new FSprite(id.ToString() + "A", true);
            container.AddChild(symbolSprite);
            circleSprite = new FSprite("EndGameCircle", true);
            container.AddChild(circleSprite);
            glowSprite = new FSprite("Futile_White", true)
            {
                shader = menu.manager.rainWorld.Shaders["FlatLight"]
            };
            container.AddChild(glowSprite);

            float x = 20f + 40f * (index % 5);
            float y = 15f + 40f * Mathf.Floor(index / 5f);
            this.pos = new Vector2(x, y);
        }

        public override void Update()
        {
            base.Update();
            lastFade = fade;
            getToX = 20f + 40f * (index % 5);
            getToY = 15f + 40f * Mathf.Floor(index / 5f);
            pos.x = Custom.LerpAndTick(pos.x, getToX, 0.05f, 0.033333335f);
            pos.y = Custom.LerpAndTick(pos.y, getToY, 0.05f, 0.033333335f);

            fade = Custom.LerpAndTick(fade, 1f, 0.05f, 0.033333335f);

            if (activated)
            {
                superGlow = Custom.LerpAndTick(superGlow, 1f, 0.07f, 0.0125f);
            }
        }

        public void Activate()
        {
            symbolSprite.RemoveFromContainer();
            circleSprite.RemoveFromContainer();
            glowSprite.RemoveFromContainer();
            owner.Container.AddChild(symbolSprite);
            owner.Container.AddChild(circleSprite);
            owner.Container.AddChild(glowSprite);
            activated = true;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 vector = DrawPos(timeStacker);
            float num = Mathf.Lerp(lastFade, fade, timeStacker);
            float num2 = Mathf.Lerp(0f, 1f, superGlow);
            Color color = Color.Lerp(
                Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey),
                Color.Lerp(
                    Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey),
                    Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White),
                    superGlow),
                num2);
            symbolSprite.x = vector.x;
            symbolSprite.y = vector.y;
            circleSprite.x = vector.x;
            circleSprite.y = vector.y;
            glowSprite.x = vector.x;
            glowSprite.y = vector.y;
            symbolSprite.color = color;
            circleSprite.color = color;
            glowSprite.color = color;
            symbolSprite.alpha = num;
            circleSprite.alpha = num;
            glowSprite.scale = Mathf.Lerp(3f, 5f + num2, num) + superGlow * Mathf.Lerp(0.75f, 1f, Random.value);
            glowSprite.alpha = Mathf.Lerp(0f, 0.3f, num2) * num;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            symbolSprite.RemoveFromContainer();
            circleSprite.RemoveFromContainer();
            glowSprite.RemoveFromContainer();
        }
    }
}
