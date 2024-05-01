using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.ComponentModel;
using Terraria.GameContent;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace ProgressSystem.Common.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;
    public override void OnLoaded()
    {
        Instance = this;
    }

    public bool DontShowAnyAchievementMessage { get; set; }
    public bool DontShowOtherPlayerCompleteAchievementMessage { get; set; }
    public bool AutoReceive { get; set; }

    public enum AutoSelectRewardEnum
    {
        NotSelect,
        First,
        Random
    }
    [DrawTicks]
    public AutoSelectRewardEnum AutoSelectReward;

    #region 开发者模式
    [CustomModConfigItem(typeof(BooleanElementButOnlyVisibleInDeveloperMode))]
    [DefaultValue(true)]
    public bool DeveloperMode {
        get => _developerMode;
        set => _developerMode = value && IsTMLInDeveloperMode;
    }
    private bool _developerMode;

    public class BooleanElementButOnlyVisibleInDeveloperMode : ConfigElement<bool>
    {
        private Asset<Texture2D> _toggleTexture = null!;
        bool inDeveloperMode;

        // TODO. Display status string? (right now only on/off texture, but True/False, Yes/No, Enabled/Disabled options)
        public override void OnBind()
        {
            if (IsTMLInDeveloperMode)
            {
                inDeveloperMode = true;
            }
            else
            {
                Height.Set(0, 0);
            }
            base.OnBind();
            if (!inDeveloperMode)
            {
                return;
            }
            _toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");

            OnLeftClick += (ev, v) => Value = !Value;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!inDeveloperMode)
            {
                return;
            }
            base.DrawSelf(spriteBatch);
            CalculatedStyle dimensions = GetDimensions();
            // "Yes" and "No" since no "True" and "False" translation available
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value,
                Value ? Lang.menu[126].Value : Lang.menu[124].Value,
                new Vector2(dimensions.X + dimensions.Width - 60, dimensions.Y + 8f),
                Color.White, 0f, Vector2.Zero, new Vector2(0.8f));
            Rectangle sourceRectangle = new(Value ? ((_toggleTexture.Width() - 2) / 2 + 2) : 0, 0, (_toggleTexture.Width() - 2) / 2, _toggleTexture.Height());
            Vector2 drawPosition = new(dimensions.X + dimensions.Width - sourceRectangle.Width - 10f, dimensions.Y + 8f);
            spriteBatch.Draw(_toggleTexture.Value, drawPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
        }
    }
    #endregion
}
