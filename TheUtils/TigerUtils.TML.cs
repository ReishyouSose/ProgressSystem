// #define MOUSE_MANAGER
// #define TIME_MANAGER

using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Chat;
using Terraria.Localization;
using Terraria.ModLoader.Core;
using Terraria.Utilities;

namespace ProgressSystem.TheUtils;

public static partial class TigerUtils
{
    public static Mod ModInstance => ProgressSystem.Instance;
    public const string ModName = nameof(ProgressSystem);
    public static Item NewItem<T>(int stack = 1, int prefix = 0) where T : ModItem => new(ModContent.ItemType<T>(), stack, prefix);
    public static T NewModItem<T>(int stack = 1, int prefix = 0) where T : ModItem => (T)new Item(ModContent.ItemType<T>(), stack, prefix).ModItem;
    public static T NewGlobalItem<T>(int type, int stack = 1, int prefix = 0) where T : GlobalItem => new Item(type, stack, prefix).GetGlobalItem<T>();
    public static T? NewGlobalItemS<T>(int type, int stack = 1, int prefix = 0) where T : GlobalItem => new Item(type, stack, prefix).TryGetGlobalItem<T>(out T? result) ? result : null;
    public static Item SampleItem(int itemID) => ContentSamples.ItemsByType[itemID];
    public static Item SampleItem<T>() where T : ModItem => ContentSamples.ItemsByType[ModContent.ItemType<T>()];
    public static T SampleModItem<T>() where T : ModItem => (T)ContentSamples.ItemsByType[ModContent.ItemType<T>()].ModItem;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetGlobalSafe<TGlobal, TResult>(int entityType, ReadOnlySpan<TGlobal> entityGlobals, out TResult? result) where TGlobal : GlobalType<TGlobal> where TResult : TGlobal
        => TryGetGlobalSafe(entityType, entityGlobals, ModContent.GetInstance<TResult>(), out result);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetGlobalSafe<TGlobal, TResult>(int entityType, ReadOnlySpan<TGlobal> entityGlobals, TResult baseInstance, out TResult? result) where TGlobal : GlobalType<TGlobal> where TResult : TGlobal
    {
        short perEntityIndex = baseInstance.PerEntityIndex;
        //只是加了下面这句中对entityGlobals长度的检查
        //TryGetModPlayer中都有的TryGetGlobal却没有
        if (entityType > 0 && perEntityIndex >= 0 && perEntityIndex < entityGlobals.Length)
        {
            result = entityGlobals[perEntityIndex] as TResult;
            return result != null;
        }

        if (GlobalTypeLookups<TGlobal>.AppliesToType(baseInstance, entityType))
        {
            result = baseInstance;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// <br/>本地的手持物品
    /// <br/>包含鼠标上的物品的处理
    /// <br/>比起<see cref="Player.HeldItem"/>, 它就算在鼠标上也可以作出修改, 而且额外带有set访问器
    /// <br/>不过在放在鼠标上的物品不能使用或者失去焦点时反而需要修改<see cref="Player.HeldItem"/>
    /// <br/>保险起见推荐在鼠标上有物品时对此(此时此值为<see cref="Main.mouseItem"/>)和<see cref="Player.HeldItem"/>一并作出修改
    /// <br/>注意不要把它设置为<see langword="null"/>
    /// <br/>另外用之前先检查一下<see cref="Item.IsAir"/>
    /// </summary>
    public static Item LocalRealHeldItem
    {
        get
        {
            return Main.mouseItem.IsNotAirS() ? Main.mouseItem : Main.LocalPlayer.HeldItem;
        }
        set
        {
            int selected = Main.LocalPlayer.selectedItem;
            if (selected == 58)
            {
                Main.mouseItem = value;
            }
            else
            {
                Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem] = value;
            }
        }
    }
    /// <summary>
    /// 以更安全的方式调用<see cref="LocalRealHeldItem"/><br/>
    /// 即使<see cref="Main.LocalPlayer"/>为<see langword="null"/>也不会报错
    /// </summary>
    public static Item? LocalRealHeldItemSafe
    {
        get
        {
            return Main.LocalPlayer == null || !Main.LocalPlayer.active ? null : LocalRealHeldItem;
        }
        set
        {
            if (Main.LocalPlayer == null)
            {
                return;
            }
            LocalRealHeldItem = value ?? new();

        }
    }

    #region 同步相关
    public static void WriteIntWithNegativeAsN1(BitWriter bitWriter, BinaryWriter binaryWriter, int value)
    {
        if (value < 0)
        {
            bitWriter.WriteBit(true);
            return;
        }
        bitWriter.WriteBit(false);
        binaryWriter.Write7BitEncodedInt(value);
    }
    public static int ReadIntWithNegativeAsN1(BitReader bitReader, BinaryReader binaryReader)
       => bitReader.ReadBit() ? -1 : binaryReader.Read7BitEncodedInt();
    #endregion
    #region Random
    public static partial class MyRandom
    {
        /// <summary>
        /// 将double转化为int
        /// 其中小数部分按概率转化为0或1
        /// </summary>
        public static int RandomD2I(double x, UnifiedRandom rand)
        {
            int floor = (int)Math.Floor(x);
            double delta = x - floor;
            return rand.NextDouble() < delta ? floor + 1 : floor;
        }
        /// <summary>
        /// 将double转化为bool
        /// 当大于1时为真, 小于0时为假
        /// 在中间则按概率
        /// </summary>
        /// <param name="x"></param>
        /// <param name="rand"></param>
        /// <returns></returns>
        public static bool RandomD2B(double x, UnifiedRandom rand)
        {
            return x > 1 - rand.NextDouble();
        }
    }
    #endregion

    public static class TMLReflection
    {
        public const BindingFlags bfall = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public const BindingFlags bfpi = BindingFlags.Public | BindingFlags.Instance;
        public const BindingFlags bfps = BindingFlags.Public | BindingFlags.Static;
        public const BindingFlags bfni = BindingFlags.NonPublic | BindingFlags.Instance;
        public const BindingFlags bfns = BindingFlags.NonPublic | BindingFlags.Static;
        private static Assembly? assembly;
        public static Assembly Assembly => assembly ??= typeof(Main).Assembly;
        public static class Types
        {
            private static Dictionary<string, Type>? allTypes;
            public static Dictionary<string, Type> AllTypes => allTypes ?? GetRight(InitTypes, allTypes)!;
            private static void InitTypes()
            {
                allTypes = [];
                Assembly.GetTypes().ForeachDo(t =>
                {
                    if (t.FullName != null)
                    {
                        allTypes.Add(t.FullName, t);
                    }
                });
            }
            public static Type UIModConfig => AllTypes["UIModConfig"];
        }
        public static class Item
        {
            public static readonly ValueDG<Type> Type = new(() => typeof(Terraria.Item));
            public static readonly ValueDG<MethodInfo> Clone = new(() => Type.Value.GetMethod(nameof(Terraria.Item.Clone), bfpi)!);
        }
        public static class Main
        {
            public static readonly ValueDG<Type> Type = new(() => typeof(Terraria.Main));
            public static readonly ValueDG<FieldInfo> MouseItem = new(() => Type.Value.GetField(nameof(Terraria.Main.mouseItem), bfps)!);
        }
        public static class Player
        {
            public static readonly ValueDG<Type> Type = new(() => typeof(Terraria.Player));
            public static readonly ValueDG<FieldInfo> Inventory = new(() => Type.Value.GetField(nameof(Terraria.Player.inventory), bfpi)!);
            public static readonly ValueDG<FieldInfo> ManaRegen = new(() => Type.Value.GetField(nameof(Terraria.Player.manaRegen), bfpi)!);
            public static readonly ValueDG<FieldInfo> ManaRegenCount = new(() => Type.Value.GetField(nameof(Terraria.Player.manaRegenCount), bfpi)!);
            public static readonly ValueDG<FieldInfo> NebulaLevelMana = new(() => Type.Value.GetField(nameof(Terraria.Player.nebulaLevelMana), bfpi)!);
            public static readonly ValueDG<FieldInfo> NebulaManaCounter = new(() => Type.Value.GetField(nameof(Terraria.Player.nebulaManaCounter), bfpi)!);
            public static readonly ValueDG<FieldInfo> StatMana = new(() => Type.Value.GetField(nameof(Terraria.Player.statMana), bfpi)!);
            public static readonly ValueDG<FieldInfo> StatManaMax = new(() => Type.Value.GetField(nameof(Terraria.Player.statManaMax), bfpi)!);
            public static readonly ValueDG<FieldInfo> StatManaMax2 = new(() => Type.Value.GetField(nameof(Terraria.Player.statManaMax2), bfpi)!);
            public static readonly ValueDG<MethodInfo> DropItemCheck = new(() => Type.Value.GetMethod(nameof(Terraria.Player.dropItemCheck), bfpi)!);
            public static readonly ValueDG<MethodInfo> ItemCheck_Shoot = new(() => Type.Value.GetMethod("ItemCheck_Shoot", bfni)!);
        }
        public static class ProjectileLoader
        {
            public static readonly ValueDG<Type> Type = new(() => typeof(Terraria.ModLoader.ProjectileLoader));
            public static readonly ValueDG<MethodInfo> OnSpawn = new(() => Type.Value.GetMethod("OnSpawn", bfns)!);
            public delegate void OnSpawnDelegate(Projectile projectile, IEntitySource source);
        }
        public static class UIModConfig
        {
            public static Type Type => Types.AllTypes["UIModConfig"];
            private static PropertyInfo? tooltip;
            private static PropertyInfo Tooltip => tooltip ??= Type.GetProperty("Tooltip", BindingFlags.Public | BindingFlags.Static)!;
            private static Action<string>? setTooltip;
            public static Action<string> SetTooltip_Func => setTooltip ??= Tooltip.SetMethod!.CreateDelegate<Action<string>>(null);
            private static Func<string>? getTooltip;
            public static Func<string> GetTooltip_Func => getTooltip ??= Tooltip.GetMethod!.CreateDelegate<Func<string>>(null);
        }
        public static class ConfigManager
        {
            public static readonly ValueDG<Type> Type = new(() => typeof(Terraria.ModLoader.Config.ConfigManager));
            public static readonly ValueDG<MethodInfo> Save = new(() => Type.Value.GetMethod("Save", bfns)!);
            public static readonly ValueDG<Action<Terraria.ModLoader.Config.ModConfig>> Save_Func = new(Save.Value.CreateDelegate<Action<Terraria.ModLoader.Config.ModConfig>>);
        }
    }
    public static T TMLInstance<T>() where T : class => ContentInstance<T>.Instance;
}

public static partial class TigerClasses
{
#if MOUSE_MANAGER
    public class MouseManager : ModSystem
    {
        public static bool OldMouseLeft;
        public static bool MouseLeft;
        public static bool MouseLeftDown => MouseLeft && !OldMouseLeft;
        public event Action? OnMouseLeftDown;
        public override void PostUpdateInput()
        {
            OldMouseLeft = MouseLeft;
            MouseLeft = Main.mouseLeft;
            if (MouseLeftDown)
            {
                OnMouseLeftDown?.Invoke();
            }
        }
    }
#endif
#if TIME_MANAGER
    public class TimeManager : ModSystem
    {
        public static UncheckedUlongTime TimeNow { get; private set; }
        static readonly Dictionary<UncheckedUlongTime, List<Action>> events = new();
        public override void PostUpdateTime()
        {
            TimeNow += 1ul;
            if (events.ContainsKey(TimeNow))
            {
                foreach (Action e in events[TimeNow])
                {
                    e.Invoke();
                }
                events.Remove(TimeNow);
            }
        }
        public static UncheckedUlongTime RegisterEvent(Action e, ulong timeDelay)
        {
            UncheckedUlongTime time = TimeNow + timeDelay;
            if (events.ContainsKey(time))
            {
                events[time].Add(e);
            }
            else
            {
                events.Add(time, new() { e });
            }
            return time;
        }
        public static UncheckedUlongTime RegisterEvent(Action e, UncheckedUlongTime time)
        {
            if (events.ContainsKey(time))
            {
                events[time].Add(e);
            }
            else
            {
                events.Add(time, new() { e });
            }
            return time;
        }
        public static bool CancelEvent(Action e, UncheckedUlongTime time)
        {
            if (!events.ContainsKey(time))
            {
                return false;
            }
            return events[time].Remove(e);
        }
    }
#endif
    /// <summary>
    /// <br/>代表获取文本的方式
    /// <br/>可以为 <see cref="string"/>, <see cref="LocalizedText"/> 或 委托
    /// <br/>如果要使用委托, 可以使用 new(delegate) 的形式获得
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct TextGetter
    {
        #region 构造函数
        public TextGetter(string stringValue) => StringValue = stringValue;
        public TextGetter(LocalizedText localizedTextValue) => LocalizedTextValue = localizedTextValue;
        public TextGetter(Func<string> stringGetterValue) => StringGetterValue = stringGetterValue;
        #endregion
        #region Vars
        private enum TextGetterType
        {
            None,
            String,
            LocalizedText,
            StringGetter
        }
        [FieldOffset(8)]
        private TextGetterType Type;
        [FieldOffset(0)]
        private string? stringValue;
        [FieldOffset(0)]
        private LocalizedText? localizedTextValue;
        [FieldOffset(0)]
        private Func<string>? stringGetterValue;
        #endregion
        #region 设置与获取值
        public readonly bool IsNone => Type == TextGetterType.None;
        public readonly string? Value => Type switch
        {
            TextGetterType.String => stringValue,
            TextGetterType.LocalizedText => localizedTextValue?.Value,
            TextGetterType.StringGetter => stringGetterValue?.Invoke(),
            _ => null
        };
        public string? StringValue
        {
            readonly get => Type == TextGetterType.String ? stringValue : null;
            set => (Type, stringValue) = (TextGetterType.String, value);
        }
        public LocalizedText? LocalizedTextValue
        {
            readonly get => Type == TextGetterType.LocalizedText ? localizedTextValue : null;
            set => (Type, localizedTextValue) = (TextGetterType.LocalizedText, value);
        }
        public Func<string>? StringGetterValue
        {
            readonly get => Type == TextGetterType.StringGetter ? stringGetterValue : null;
            set => (Type, stringGetterValue) = (TextGetterType.StringGetter, value);
        }
        #endregion
        #region 类型转换
        public static implicit operator TextGetter(string stringValue) => new(stringValue);
        public static implicit operator TextGetter(LocalizedText localizedTextValue) => new(localizedTextValue);
        public static implicit operator TextGetter(Func<string> stringGetterValue) => new(stringGetterValue);
        #endregion
        #region 运算符重载
        public static TextGetter operator |(TextGetter left, TextGetter right) => left.IsNone ? right : left;
        #endregion
    }
    /// <summary>
    /// <br/>代表 <see cref="Texture2D"/> 的获取方式
    /// <br/>可以为 <see cref="Texture2D"/>, <see cref="Asset{T}"/>, 委托, 或者图片的路径 (会转换为 <see cref="Asset{T}"/>)
    /// <br/>如果要使用委托, 可以使用 new(delegate) 的形式获得
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct Texture2DGetter
    {
        #region 构造函数
        public Texture2DGetter(string texturePath) => SetTexturePath(texturePath);
        public Texture2DGetter(Texture2D texture2DValue) => Texture2DValue = texture2DValue;
        public Texture2DGetter(Asset<Texture2D> assetOfTexture2DValue) => AssetOfTexture2DValue = assetOfTexture2DValue;
        public Texture2DGetter(Func<Texture2D> texture2DGetterValue) => Texture2DGetterValue = texture2DGetterValue;
        #endregion
        #region Vars
        private enum Texture2DGetterType
        {
            None,
            Texture2D,
            AssetOfTexture2D,
            Texture2DGetter
        }
        [FieldOffset(8)]
        private Texture2DGetterType Type;
        [FieldOffset(0)]
        private Texture2D? texture2DValue;
        [FieldOffset(0)]
        private Asset<Texture2D>? assetOfTexture2DValue;
        [FieldOffset(0)]
        private Func<Texture2D>? texture2DGetterValue;
        #endregion
        #region 设置与获取值
        public readonly bool IsNone => Type == Texture2DGetterType.None;
        public readonly Texture2D? Value => Type switch
        {
            Texture2DGetterType.Texture2D => texture2DValue,
            Texture2DGetterType.AssetOfTexture2D => assetOfTexture2DValue?.Value,
            Texture2DGetterType.Texture2DGetter => texture2DGetterValue?.Invoke(),
            _ => null
        };
        public void SetTexturePath(string texturePath)
        {
            if (ModContent.RequestIfExists<Texture2D>(texturePath, out Asset<Texture2D>? texture))
            {
                AssetOfTexture2DValue = texture;
            }
        }
        public Texture2D? Texture2DValue
        {
            readonly get => Type == Texture2DGetterType.Texture2D ? texture2DValue : null;
            set => (Type, texture2DValue) = (Texture2DGetterType.Texture2D, value);
        }
        public Asset<Texture2D>? AssetOfTexture2DValue
        {
            readonly get => Type == Texture2DGetterType.AssetOfTexture2D ? assetOfTexture2DValue : null;
            set => (Type, assetOfTexture2DValue) = (Texture2DGetterType.AssetOfTexture2D, value);
        }
        public Func<Texture2D>? Texture2DGetterValue
        {
            readonly get => Type == Texture2DGetterType.Texture2DGetter ? texture2DGetterValue : null;
            set => (Type, texture2DGetterValue) = (Texture2DGetterType.Texture2DGetter, value);
        }
        #endregion
        #region 类型转换
        public static implicit operator Texture2DGetter(string texturePath) => new(texturePath);
        public static implicit operator Texture2DGetter(Texture2D texture2DValue) => new(texture2DValue);
        public static implicit operator Texture2DGetter(Asset<Texture2D> assetOfTexture2DValue) => new(assetOfTexture2DValue);
        public static implicit operator Texture2DGetter(Func<Texture2D> texture2DGetterValue) => new(texture2DGetterValue);
        #endregion
        #region 运算符重载
        public static Texture2DGetter operator |(Texture2DGetter left, Texture2DGetter right) => left.IsNone ? right : left;
        #endregion
    }
}

public static partial class TigerExtensions
{
    #region TagCompound 拓展
    /// <summary>
    /// 若不为默认值则将值保存到 <paramref name="tag"/> 中
    /// </summary>
    public static void SetWithDefault<T>(this TagCompound tag, string key, T? value, T? defaultValue = default, bool replace = false) where T : IEquatable<T>
    {
        if ((value == null && defaultValue == null) || value?.Equals(defaultValue) == true)
        {
            return;
        }
        tag.Set(key, value, replace);
    }
    /// <summary>
    /// 若不为默认值则将值保存到 <paramref name="tag"/> 中
    /// </summary>
    /// <param name="checkDefault">检查值是否是默认值</param>
    public static void SetWithDefault<T>(this TagCompound tag, string key, T? value, Func<T?, bool> checkDefault, bool replace = false)
    {
        if (!checkDefault(value))
        {
            tag.Set(key, value, replace);
        }
    }
    /// <summary>
    /// 若不为默认值 ( ! <paramref name="isDefault"/> ) 则将值保存到 <paramref name="tag"/> 中
    /// </summary>
    public static void SetWithDefault<T>(this TagCompound tag, string key, T? value, bool isDefault, bool replace = false)
    {
        if (!isDefault)
        {
            tag.Set(key, value, replace);
        }
    }
    /// <summary>
    /// 若不为默认值则将值保存到 <paramref name="tag"/> 中
    /// </summary>
    public static void SetWithDefaultN<T>(this TagCompound tag, string key, T value, T defaultValue = default, bool replace = false) where T : struct
    {
        if (value.Equals(defaultValue) == true)
        {
            return;
        }
        tag.Set(key, value, replace);
    }

    public static Func<Item?, bool> ItemCheckDefault => i => i == null || i.IsAir;
    /// <summary>
    /// <br/>获得此值, 若不存在则返回默认值
    /// <br/>若类型不正确会报错
    /// </summary>
    public static T? GetWithDefault<T>(this TagCompound tag, string key)
    {
        return tag.TryGet(key, out T value) ? value : default;
    }
    /// <summary>
    /// <br/>获得此值, 若不存在则返回默认值(<paramref name="defaultValue"/>)
    /// <br/>若类型不正确会报错
    /// </summary>
    public static T GetWithDefault<T>(this TagCompound tag, string key, T defaultValue)
    {
        return tag.TryGet(key, out T value) ? value : defaultValue;
    }
    /// <summary>
    /// <br/>返回是否成功得到值, 返回假时得到的是默认值(返回真时也可能得到默认值(若保存的为默认值的话))
    /// <br/>若类型不正确会报错
    /// </summary>
    public static bool GetWithDefault<T>(this TagCompound tag, string key, out T? value)
    {
        if (tag.TryGet(key, out value))
        {
            return true;
        }
        value = default;
        return false;
    }
    /// <summary>
    /// <br/>返回是否成功得到值, 返回假时得到的是默认值(返回真时也可能得到默认值(若保存的为默认值的话))
    /// <br/>若类型不正确会报错
    /// </summary>
    public static bool GetWithDefault<T>(this TagCompound tag, string key, out T value, T defaultValue)
    {
        if (tag.TryGet(key, out value))
        {
            return true;
        }
        value = defaultValue;
        return false;
    }
    public static void SetWithDefaultN<T>(this TagCompound tag, string key, T? value, bool replace = false) where T : struct
    {
        if (!value.HasValue)
        {
            return;
        }
        tag.Set(key, value.Value, replace);
    }
    public static bool GetWithDefaultN<T>(this TagCompound tag, string key, out T? value) where T : struct
    {
        if (tag.TryGet(key, out T val))
        {
            value = val;
            return true;
        }
        value = null;
        return false;
    }
    public static T? GetWithDefaultN<T>(this TagCompound tag, string key) where T : struct
    {
        return tag.TryGet(key, out T val) ? val : null;
    }

    public static bool Replace<TOld, TNew>(this TagCompound tag, string oldKey, string newKey, Func<TOld?, TNew?> convert, TOld? oldDefaultValue = default, TNew? newDefaultValue = default, bool removeOldKey = true) where TNew : IEquatable<TNew>
    {
        bool result = tag.GetWithDefault(oldKey, out TOld? oldValue, oldDefaultValue);
        if (removeOldKey)
        {
            tag.Remove(oldKey);
        }
        tag.SetWithDefault(newKey, convert(oldValue), newDefaultValue, replace: true);
        return result;
    }
    public static bool Replace<T>(this TagCompound tag, string oldKey, string newKey, T? defaultValue = default, bool removeOldKey = true) where T : IEquatable<T>
    {
        bool result = tag.GetWithDefault(oldKey, out T? value, defaultValue);
        if (removeOldKey)
        {
            tag.Remove(oldKey);
        }
        tag.SetWithDefault(newKey, value, defaultValue);
        return result;
    }
    public static bool Replace(this TagCompound tag, string oldKey, string newKey, bool removeOldKey = true)
    {
        if (!tag.ContainsKey(oldKey))
        {
            return false;
        }
        tag[newKey] = tag[oldKey];
        if (removeOldKey)
        {
            tag.Remove(oldKey);
        }
        return true;
    }
    /// <summary>
    /// 需要 <paramref name="tag"/>[<paramref name="key"/>] 中是 List&lt;<typeparamref name="TElement"/>&gt;
    /// </summary>
    public static void AddElement<TElement>(this TagCompound tag, string key, TElement element)
    {
        if (tag.ContainsKey(key))
        {
            tag.Get<List<TElement>>(key).Add(element);
        }
        else
        {
            tag[key] = new List<TElement>() { element };
        }
    }
    /// <summary>
    /// 需要 <paramref name="tag"/>[<paramref name="key"/>] 中是 List&lt;<typeparamref name="TElement"/>&gt;
    /// </summary>
    public static void AddElementRange<TElement>(this TagCompound tag, string key, IEnumerable<TElement> elements)
    {
        if (tag.ContainsKey(key))
        {
            tag.Get<List<TElement>>(key).AddRange(elements);
        }
        else
        {
            tag.Add(key, new List<TElement>([.. elements]));
        }
    }
    /// <summary>
    /// 需要 <paramref name="tag"/>[<paramref name="key"/>] 中是 List&lt;<typeparamref name="TElement"/>&gt;
    /// </summary>
    public static void AddElementRange<TElement>(this TagCompound tag, string key, List<TElement> elementList)
    {
        if (tag.ContainsKey(key))
        {
            tag.Get<List<TElement>>(key).AddRange(elementList);
        }
        else
        {
            tag.Add(key, elementList);
        }
    }

    public static void SaveDictionaryData<T>(this TagCompound tag, string key, Dictionary<string, T> dictionary, Action<T, TagCompound> toTag)
    {
        tag.SaveDictionaryData(key, dictionary, t => new TagCompound().WithAction(tag => toTag(t, tag)));
    }
    public static void SaveDictionaryData<T>(this TagCompound tag, string key, Dictionary<string, T> dictionary, Func<T, TagCompound?> toTag)
    {
        TagCompound data = [.. dictionary.SelectWhere(
            p => toTag(p.Value).Transfer(
                t => t?.Count > 0 ?
                NewHolder(NewPair(p.Key, (object)t)) :
                null)
        )];
        if (data.Count > 0)
        {
            tag[key] = data;
        }
    }
    public static void LoadDictionaryData<T>(this TagCompound tag, string key, Dictionary<string, T> dictionary, Action<T, TagCompound> fromTag)
    {
        if (!tag.TryGet(key, out TagCompound dictValue))
        {
            return;
        }
        foreach ((string k, object v) in dictValue)
        {
            if (dictionary.TryGetValue(k, out T? val))
            {
                fromTag(val, (TagCompound)v);
            }
        }
    }
    public static void SaveReadOnlyDictionaryData<T>(this TagCompound tag, string key, IReadOnlyDictionary<string, T> dictionary, Action<T, TagCompound> toTag)
    {
        tag.SaveReadOnlyDictionaryData(key, dictionary, t => new TagCompound().WithAction(tag => toTag(t, tag)));
    }
    public static void SaveReadOnlyDictionaryData<T>(this TagCompound tag, string key, IReadOnlyDictionary<string, T> dictionary, Func<T, TagCompound?> toTag)
    {
        TagCompound data = [.. dictionary.SelectWhere(
            p => toTag(p.Value).Transfer(
                t => t?.Count > 0 ?
                NewHolder(NewPair(p.Key, (object)t)) :
                null)
        )];
        if (data.Count > 0)
        {
            tag[key] = data;
        }
    }
    public static void LoadReadOnlyDictionaryData<T>(this TagCompound tag, string key, IReadOnlyDictionary<string, T> dictionary, Action<T, TagCompound> fromTag)
    {
        if (!tag.TryGet(key, out TagCompound dictValue))
        {
            return;
        }
        foreach ((string k, object v) in dictValue)
        {
            if (dictionary.TryGetValue(k, out T? val))
            {
                fromTag(val, (TagCompound)v);
            }
        }
    }
    public static void SaveListData<T>(this TagCompound tag, string key, IList<T> list, Action<T, TagCompound> toTag)
    {
        tag.SaveListData(key, list, e => new TagCompound().WithAction(t => toTag(e, t)));
    }
    public static void SaveListData<T>(this TagCompound tag, string key, IList<T> list, Func<T, TagCompound?> toTag)
    {
        bool needSave = false;
        List<TagCompound?> data = list.Select(e => toTag(e).WithAction(t => needSave.AssignIf(t?.Count > 0, true))).ToList();
        if (needSave)
        {
            tag[key] = data;
        }
    }
    public static void LoadListData<T>(this TagCompound tag, string key, IList<T> list, Action<T, TagCompound> fromTag)
    {
        if (!tag.TryGet(key, out List<TagCompound?> listData))
        {
            return;
        }
        foreach (int i in Math.Min(list.Count, listData.Count))
        {
            TagCompound? ld = listData[i];
            if (ld != null)
            {
                fromTag(list[i], ld);
            }
        }
    }
    #endregion
    #region AppendItem
    public static StringBuilder AppendItem(this StringBuilder stringBuilder, Item item) =>
        stringBuilder.Append(ItemTagHandler.GenerateTag(item));
    public static StringBuilder AppendItem(this StringBuilder stringBuilder, int itemID) =>
        stringBuilder.Append(ItemTagHandler.GenerateTag(ContentSamples.ItemsByType[itemID]));
    #endregion
    #region 关于Tooltips
    public static bool AddIf(this List<TooltipLine> tooltips, bool condition, string name, string text, Color? overrideColor = null)
    {
        if (condition)
        {
            TooltipLine line = new(ModInstance, name, text);
            if (overrideColor != null)
            {
                line.OverrideColor = overrideColor;
            }
            tooltips.Add(line);
            return true;
        }
        return false;
    }
    public static bool AddIf(this List<TooltipLine> tooltips, bool condition, Func<string> nameDelegate, Func<string> textDelegate, Color? overrideColor = null)
    {
        if (condition)
        {
            TooltipLine line = new(ModInstance, nameDelegate?.Invoke(), textDelegate?.Invoke());
            if (overrideColor != null)
            {
                line.OverrideColor = overrideColor;
            }
            tooltips.Add(line);
            return true;
        }
        return false;
    }
    public static bool AddIf(this List<TooltipLine> tooltips, bool condition, string name, Func<string> textDelegate, Color? overrideColor = null)
    {
        if (condition)
        {
            TooltipLine line = new(ModInstance, name, textDelegate?.Invoke());
            if (overrideColor != null)
            {
                line.OverrideColor = overrideColor;
            }
            tooltips.Add(line);
            return true;
        }
        return false;
    }
    public static bool InsertIf(this List<TooltipLine> tooltips, bool condition, int index, string name, string text, Color? overrideColor = null)
    {
        if (condition)
        {
            TooltipLine line = new(ModInstance, name, text);
            if (overrideColor != null)
            {
                line.OverrideColor = overrideColor;
            }
            tooltips.Insert(index, line);
            return true;
        }
        return false;
    }
    public static bool InsertIf(this List<TooltipLine> tooltips, bool condition, int index, Func<string> nameDelegate, Func<string> textDelegate, Color? overrideColor = null)
    {
        if (condition)
        {
            TooltipLine line = new(ModInstance, nameDelegate?.Invoke(), textDelegate?.Invoke());
            if (overrideColor != null)
            {
                line.OverrideColor = overrideColor;
            }
            tooltips.Insert(index, line);
            return true;
        }
        return false;
    }
    public static bool InsertIf(this List<TooltipLine> tooltips, bool condition, int index, string name, Func<string> textDelegate, Color? overrideColor = null)
    {
        if (condition)
        {
            TooltipLine line = new(ModInstance, name, textDelegate?.Invoke());
            if (overrideColor != null)
            {
                line.OverrideColor = overrideColor;
            }
            tooltips.Insert(index, line);
            return true;
        }
        return false;
    }
    public static List<TooltipLine> GetTooltips(this Item item)
    {
        //摘自Main.MouseText_DrawItemTooltip
        float num = 1f;
        if (item.DamageType == DamageClass.Melee && Main.player[Main.myPlayer].kbGlove)
        {
            num += 1f;
        }
        if (Main.player[Main.myPlayer].kbBuff)
        {
            num += 0.5f;
        }
        if (num != 1f)
        {
            item.knockBack *= num;
        }
        if (item.DamageType == DamageClass.Ranged && Main.player[Main.myPlayer].shroomiteStealth)
        {
            item.knockBack *= 1f + ((1f - Main.player[Main.myPlayer].stealth) * 0.5f);
        }
        int num2 = 30;
        int oneDropLogo = -1;
        int researchLine = -1;
        float knockBack = item.knockBack;
        int numTooltips = 1;
        string[] texts = new string[num2];
        bool[] modifier = new bool[num2];
        bool[] badModifier = new bool[num2];
        for (int m = 0; m < num2; m++)
        {
            modifier[m] = false;
            badModifier[m] = false;
        }
        string[] names = new string[num2];
        Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref oneDropLogo, ref researchLine, knockBack, ref numTooltips, texts, modifier, badModifier, names, out int prefixlineIndex);
        if (Main.npcShop > 0 && item.value >= 0 && (item.type < ItemID.CopperCoin || item.type > ItemID.PlatinumCoin))
        {
            Main.LocalPlayer.GetItemExpectedPrice(item, out long calcForSelling, out long calcForBuying);
            long price = (item.isAShopItem || item.buyOnce) ? calcForBuying : calcForSelling;
            if (item.shopSpecialCurrency != -1)
            {
                names[numTooltips] = "SpecialPrice";
                CustomCurrencyManager.GetPriceText(item.shopSpecialCurrency, texts, ref numTooltips, price);
            }
            else if (price > 0L)
            {
                string text = "";
                long platinum = 0L;
                long gold = 0L;
                long silver = 0L;
                long copper = 0L;
                price *= item.stack;
                if (!item.buy)
                {
                    price /= 5L;
                    if (price < 1L)
                    {
                        price = 1L;
                    }
                    long singlePrice = price;
                    price *= item.stack;
                    int amount = Main.shopSellbackHelper.GetAmount(item);
                    if (amount > 0)
                    {
                        price += (-singlePrice + calcForBuying) * Math.Min(amount, item.stack);
                    }
                }
                if (price < 1L)
                {
                    price = 1L;
                }
                if (price >= 1000000L)
                {
                    platinum = price / 1000000L;
                    price -= platinum * 1000000L;
                }
                if (price >= 10000L)
                {
                    gold = price / 10000L;
                    price -= gold * 10000L;
                }
                if (price >= 100L)
                {
                    silver = price / 100L;
                    price -= silver * 100L;
                }
                if (price >= 1L)
                {
                    copper = price;
                }
                if (platinum > 0L)
                {
                    text = string.Concat(
                    [
                    text,
                        platinum.ToString(),
                        " ",
                        Lang.inter[15].Value,
                        " "
                    ]);
                }
                if (gold > 0L)
                {
                    text = string.Concat(
                    [
                    text,
                        gold.ToString(),
                        " ",
                        Lang.inter[16].Value,
                        " "
                    ]);
                }
                if (silver > 0L)
                {
                    text = string.Concat(
                    [
                    text,
                        silver.ToString(),
                        " ",
                        Lang.inter[17].Value,
                        " "
                    ]);
                }
                if (copper > 0L)
                {
                    text = string.Concat(
                    [
                    text,
                        copper.ToString(),
                        " ",
                        Lang.inter[18].Value,
                        " "
                    ]);
                }
                texts[numTooltips] = !item.buy ? Lang.tip[49].Value + " " + text : Lang.tip[50].Value + " " + text;
                names[numTooltips] = "Price";
                numTooltips++;
            }
            else if (item.type != ItemID.DefenderMedal)
            {
                texts[numTooltips] = Lang.tip[51].Value;
                names[numTooltips] = "Price";
                numTooltips++;
            }
        }

        //摘自ItemLoader.ModifyTooltips
        List<TooltipLine> tooltips = [];
        for (int i = 0; i < numTooltips; i++)
        {
            tooltips.Add(new TooltipLine(ModInstance, names[i], texts[i])
            {
                IsModifier = modifier[i],
                IsModifierBad = badModifier[i]
            });
        }
        if (item.prefix >= PrefixID.Count && prefixlineIndex != -1)
        {
            ModPrefix prefix = PrefixLoader.GetPrefix(item.prefix);
            IEnumerable<TooltipLine>? tooltipLines = prefix?.GetTooltipLines(item);
            if (tooltipLines != null)
            {
                foreach (TooltipLine line in tooltipLines)
                {
                    tooltips.Insert(prefixlineIndex, line);
                    prefixlineIndex++;
                }
            }
        }
        item.ModItem?.ModifyTooltips(tooltips);
        if (!item.IsAir)
        {
            foreach (GlobalItem globalItem in item.Globals)
            {
                globalItem.ModifyTooltips(item, tooltips);
            }
        }
        return tooltips;
    }
    #endregion
    #region Player
    public static bool ConsumeItem<T>(this Player player, bool reverseOrder = false, bool includeVoidBag = false) where T : ModItem
        => player.ConsumeItem(ModContent.ItemType<T>(), reverseOrder, includeVoidBag);
    #endregion
    #region Mod
    /// <summary>
    /// Retrieves the text value for a localization key belonging to this piece of content with the given suffix.<br/>
    /// The text returned will be for the currently selected language.
    /// </summary>
    public static string GetLocalizedValue(this Mod mod, string suffix, Func<string>? makeDefaultValue = null) => mod.GetLocalization(suffix, makeDefaultValue).Value;
    #endregion
    #region Item
    /// <summary>
    /// 以更安全的方式调用<see cref="Item.TryGetGlobalItem{T}(out T)"/>
    /// </summary>
    public static bool TryGetGlobalItemSafe<T>(this Item item, out T? result) where T : GlobalItem
        => TryGetGlobalSafe<GlobalItem, T>(item.type, item.EntityGlobals, out result);
    /// <summary>
    /// <br/>以更安全的方式调用<see cref="Item.TryGetGlobalItem{T}(T, out T)"/>
    /// <br/><paramref name="baseInstance"/>默认由<see cref="ModContent.GetInstance{T}"/>获得
    /// </summary>
    public static bool TryGetGlobalItemSafe<T>(this Item item, T baseInstance, out T? result) where T : GlobalItem
        => TryGetGlobalSafe<GlobalItem, T>(item.type, item.EntityGlobals, baseInstance, out result);
    public static bool IsNotAir(this Item item) => !item.IsAir;
    /// <summary>
    /// 在<paramref name="item"/>为空时也返回<see langword="true"/>
    /// </summary>
    public static bool IsAirS(this Item? item) => item == null || item.IsAir;
    /// <summary>
    /// 在<paramref name="item"/>为空时返回<see langword="false"/>
    /// </summary>
    public static bool IsNotAirS(this Item? item) => item != null && !item.IsAir;
    #endregion
    #region NPC
    public static bool TryGetGlobalNPCSafe<T>(this NPC npc, out T? result) where T : GlobalNPC
        => TryGetGlobalSafe<GlobalNPC, T>(npc.type, npc.EntityGlobals, out result);
    public static bool TryGetGlobalNPCSafe<T>(this NPC npc, T baseInstance, out T? result) where T : GlobalNPC
        => TryGetGlobalSafe<GlobalNPC, T>(npc.type, npc.EntityGlobals, baseInstance, out result);
    #endregion
    #region Projectile
    public static bool TryGetGlobalProjectileSafe<T>(this Projectile projectile, out T? result) where T : GlobalProjectile
        => TryGetGlobalSafe<GlobalProjectile, T>(projectile.type, projectile.EntityGlobals, out result);
    public static bool TryGetGlobalProjectileSafe<T>(this Projectile projectile, T baseInstance, out T? result) where T : GlobalProjectile
        => TryGetGlobalSafe<GlobalProjectile, T>(projectile.type, projectile.EntityGlobals, baseInstance, out result);

    #region FullyHostile和FullyFriendly
    public static bool IsFullyHostile(this Projectile projectile)
        => projectile.hostile && !projectile.friendly;
    public static bool IsFullyFriendly(this Projectile projectile)
        => projectile.friendly && !projectile.hostile;
    public static void SetFullyHostile(this Projectile projectile)
    {
        projectile.hostile = true;
        projectile.friendly = false;
    }
    public static void SetFullyFriendly(this Projectile projectile)
    {
        projectile.friendly = true;
        projectile.hostile = false;
    }
    #endregion
    #endregion
    #region Random
    public static Func<UnifiedRandom> DefaultUnifiedRandomGetter { get; set; } = () => Main.rand;
    /// <summary>
    /// <br/>需确保<paramref name="enumerable"/>不会变化长度
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IEnumerable{T}, UnifiedRandom)"/>
    /// </summary>
    public static T? Random<T>(this IEnumerable<T> enumerable, UnifiedRandom? rand = null)
    {
        rand ??= DefaultUnifiedRandomGetter();
        int length = enumerable.Count();
        return length == 0 ? default : enumerable.ElementAt(rand.Next(length));
    }
    /// <summary>
    /// 需确保<paramref name="enumerable"/>不会变化长度且长度非0
    /// </summary>
    public static T RandomF<T>(this IEnumerable<T> enumerable, UnifiedRandom? rand = null)
    {
        rand ??= DefaultUnifiedRandomGetter();
        int length = enumerable.Count();
        return enumerable.ElementAt(rand.Next(length));
    }
    /// <summary>
    /// <br/>需确保<paramref name="enumerable"/>不会变化长度且<paramref name="weight"/>在固定参数下的返回值不变
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IEnumerable{T}, Func{T, double}, UnifiedRandom?, bool)"/>
    /// </summary>
    public static T? Random<T>(this IEnumerable<T> enumerable, Func<T, double> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        double w = default;
        if (uncheckNegative)
        {
            double totalWeight = enumerable.Sum(t => weight(t));
            double randDouble = rand.NextDouble() * totalWeight;
            return enumerable.FirstOrDefault(t => GetRight(w = weight(t), w > randDouble || TigerUtils.Do(randDouble -= w)));
        }
        else
        {
            double totalWeight = enumerable.Sum(t => weight(t).WithMin(0));
            double randDouble = rand.NextDouble() * totalWeight;
            return totalWeight <= 0 ? default : enumerable.FirstOrDefault(t => GetRight(w = weight(t).WithMin(0), w > randDouble || TigerUtils.Do(randDouble -= w)));
        }

    }
    /// <summary>
    /// <br/>需确保<paramref name="enumerable"/>不会变化长度且<paramref name="weight"/>在固定参数下的返回值不变
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IEnumerable{T}, Func{T, float}, UnifiedRandom?, bool)"/>
    /// </summary>
    public static T? Random<T>(this IEnumerable<T> enumerable, Func<T, float> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        float w = default;
        if (uncheckNegative)
        {
            float totalWeight = enumerable.Sum(t => weight(t));
            float randFloat = rand.NextFloat() * totalWeight;
            return enumerable.FirstOrDefault(t => GetRight(w = weight(t), w > randFloat || TigerUtils.Do(randFloat -= w)));
        }
        else
        {
            float totalWeight = enumerable.Sum(t => weight(t).WithMin(0f));
            float randFloat = rand.NextFloat() * totalWeight;
            return totalWeight <= 0 ? default : enumerable.FirstOrDefault(t => GetRight(w = weight(t).WithMin(0f), w > randFloat || TigerUtils.Do(randFloat -= w)));
        }

    }
    /// <summary>
    /// <br/>需确保<paramref name="enumerable"/>不会变化长度且<paramref name="weight"/>在固定参数下的返回值不变
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IEnumerable{T}, Func{T, int}, UnifiedRandom?, bool)"/>
    /// </summary>
    public static T? Random<T>(this IEnumerable<T> enumerable, Func<T, int> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        int w = default;
        if (uncheckNegative)
        {
            int totalWeight = enumerable.Sum(t => weight(t));
            int randInt = rand.Next(totalWeight);
            return enumerable.FirstOrDefault(t => GetRight(w = weight(t), w > randInt || TigerUtils.Do(randInt -= w)));
        }
        else
        {
            int totalWeight = enumerable.Sum(t => weight(t).WithMin(0));
            int randInt = rand.Next(totalWeight);
            return totalWeight <= 0 ? default : enumerable.FirstOrDefault(t => GetRight(w = weight(t).WithMin(0), w > randInt || TigerUtils.Do(randInt -= w)));
        }
    }
    public static T? RandomS<T>(this IEnumerable<T> enumerable, UnifiedRandom? rand = null)
    {
        rand ??= DefaultUnifiedRandomGetter();
        T[] list = [.. enumerable];
        return list.Length == 0 ? default : list[rand.Next(list.Length)];
    }
    public static T? RandomS<T>(this IEnumerable<T> enumerable, Func<T, double> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        double w = default;
        double totalWeight = default;
        (double weight, T value)[] list = uncheckNegative ? [.. enumerable.Select(t => GetRight(totalWeight += w = weight(t), (w, t)))]
            : [.. enumerable.Select<T, (double weight, T value)>(t => (weight(t), t)).Where(p => p.weight > 0).WithAction(p => totalWeight += p.weight)];
        double randDouble = rand.NextDouble() * totalWeight;
        return list.FirstOrDefault(p => p.weight > randDouble || TigerUtils.Do(randDouble -= p.weight)).value;
    }
    public static T? RandomS<T>(this IEnumerable<T> enumerable, Func<T, float> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        float w = default;
        float totalWeight = default;
        (float weight, T value)[] list = uncheckNegative ? [.. enumerable.Select(t => GetRight(totalWeight += w = weight(t), (w, t)))]
            : [.. enumerable.Select<T, (float weight, T value)>(t => (weight(t), t)).Where(p => p.weight > 0).WithAction(p => totalWeight += p.weight)];
        float randFloat = rand.NextFloat() * totalWeight;
        return list.FirstOrDefault(p => p.weight > randFloat || TigerUtils.Do(randFloat -= p.weight)).value;
    }
    public static T? RandomS<T>(this IEnumerable<T> enumerable, Func<T, int> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        int w = default;
        int totalWeight = default;
        (int weight, T value)[] list = uncheckNegative ? [.. enumerable.Select(t => GetRight(totalWeight += w = weight(t), (w, t)))]
            : [.. enumerable.Select<T, (int weight, T value)>(t => (weight(t), t)).Where(p => p.weight > 0).WithAction(p => totalWeight += p.weight)];
        int randInt = rand.Next(totalWeight);
        return list.FirstOrDefault(p => p.weight > randInt || TigerUtils.Do(randInt -= p.weight)).value;
    }
    public static T? Random<T>(this IList<T> list, UnifiedRandom? rand = null)
    {
        int count = list.Count;
        if (count <= 0)
        {
            return default;
        }
        rand ??= DefaultUnifiedRandomGetter();
        return list.ElementAt(rand.Next(list.Count));
    }
    /// <summary>
    /// 需确保<paramref name="list"/>的长度非0
    /// </summary>
    public static T RandomF<T>(this IList<T> list, UnifiedRandom? rand = null)
    {
        rand ??= DefaultUnifiedRandomGetter();
        return list.ElementAt(rand.Next(list.Count));
    }
    /// <summary>
    /// <br/>需确保<paramref name="weight"/>在固定参数下的返回值不变
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IList{T}, Func{T, double}, UnifiedRandom?, bool)"/>
    /// </summary>
    public static T? Random<T>(this IList<T> list, Func<T, double> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        double w = default;
        if (uncheckNegative)
        {
            double totalWeight = list.Sum(t => weight(t));
            double randDouble = rand.NextDouble() * totalWeight;
            return list.FirstOrDefault(t => GetRight(w = weight(t), w > randDouble || TigerUtils.Do(randDouble -= w)));
        }
        else
        {
            double totalWeight = list.Sum(t => weight(t).WithMin(0));
            double randDouble = rand.NextDouble() * totalWeight;
            return totalWeight <= 0 ? default : list.FirstOrDefault(t => GetRight(w = weight(t).WithMin(0), w > randDouble || TigerUtils.Do(randDouble -= w)));
        }

    }
    /// <summary>
    /// <br/>需确保<paramref name="weight"/>在固定参数下的返回值不变
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IList{T}, Func{T, float}, UnifiedRandom?, bool)"/>
    /// </summary>
    public static T? Random<T>(this IList<T> list, Func<T, float> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        float w = default;
        if (uncheckNegative)
        {
            float totalWeight = list.Sum(t => weight(t));
            float randFloat = rand.NextFloat() * totalWeight;
            return list.FirstOrDefault(t => GetRight(w = weight(t), w > randFloat || TigerUtils.Do(randFloat -= w)));
        }
        else
        {
            float totalWeight = list.Sum(t => weight(t).WithMin(0f));
            float randFloat = rand.NextFloat() * totalWeight;
            return totalWeight <= 0 ? default : list.FirstOrDefault(t => GetRight(w = weight(t).WithMin(0f), w > randFloat || TigerUtils.Do(randFloat -= w)));
        }

    }
    /// <summary>
    /// <br/>需确保<paramref name="weight"/>在固定参数下的返回值不变
    /// <br/>若可能会变化, 请调用<see cref="RandomS{T}(IList{T}, Func{T, int}, UnifiedRandom?, bool)"/>
    /// </summary>
    public static T? Random<T>(this IList<T> list, Func<T, int> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        int w = default;
        if (uncheckNegative)
        {
            int totalWeight = list.Sum(t => weight(t));
            int randInt = rand.Next(totalWeight);
            return list.FirstOrDefault(t => GetRight(w = weight(t), w < randInt || TigerUtils.Do(randInt -= w)));
        }
        else
        {
            int totalWeight = list.Sum(t => weight(t).WithMin(0));
            int randInt = rand.Next(totalWeight);
            return totalWeight <= 0 ? default : list.FirstOrDefault(t => GetRight(w = weight(t).WithMin(0), w > randInt || TigerUtils.Do(randInt -= w)));
        }

    }
    public static T? RandomS<T>(this IList<T> list, Func<T, double> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        double w = default;
        double totalWeight = default;
        double[] weights = uncheckNegative ? [.. list.Select(t => GetRight(totalWeight += w = weight(t), w))]
        : [.. list.Select(t => GetRight(totalWeight += w = weight(t).WithMin(0), w))];
        double randDouble = rand.NextDouble() * totalWeight;
        int index = Range(list.Count).FirstOrDefault(i => weights[i] > randDouble || TigerUtils.Do(randDouble -= weights[i]), -1);
        return index == -1 ? default : list.ElementAt(index);
    }
    public static T? RandomS<T>(this IList<T> list, Func<T, float> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        float w = default;
        float totalWeight = default;
        float[] weights = uncheckNegative ? [.. list.Select(t => GetRight(totalWeight += w = weight(t), w))]
        : [.. list.Select(t => GetRight(totalWeight += w = weight(t).WithMin(0), w))];
        float randFloat = rand.NextFloat() * totalWeight;
        int index = Range(list.Count).FirstOrDefault(i => weights[i] > randFloat || TigerUtils.Do(randFloat -= weights[i]), -1);
        return index == -1 ? default : list.ElementAt(index);
    }
    public static T? RandomS<T>(this IList<T> list, Func<T, int> weight, UnifiedRandom? rand = null, bool uncheckNegative = false)
    {
        rand ??= DefaultUnifiedRandomGetter();
        int w = default;
        int totalWeight = default;
        int[] weights = uncheckNegative ? [.. list.Select(t => GetRight(totalWeight += w = weight(t), w))]
        : [.. list.Select(t => GetRight(totalWeight += w = weight(t).WithMin(0), w))];
        int randInt = rand.Next(totalWeight);
        int index = Range(list.Count).FirstOrDefault(i => weights[i] > randInt || TigerUtils.Do(randInt -= weights[i]), -1);
        return index == -1 ? default : list.ElementAt(index);
    }
    #endregion
    #region UnifiedRandom
    public static double NextDouble(this UnifiedRandom rand, double maxValue) => rand.NextDouble() * maxValue;
    public static double NextDouble(this UnifiedRandom rand, double minValue, double maxValue) => (rand.NextDouble() * (maxValue - minValue)) + minValue;
    /// <summary>
    /// 会有<paramref name="p"/>的概率得到<see langword="true"/>
    /// </summary>
    public static bool NextBool(this UnifiedRandom rand, float p) => rand.NextFloat() < p;
    /// <summary>
    /// 会有<paramref name="p"/>的概率得到<see langword="true"/>
    /// </summary>
    public static bool NextBool(this UnifiedRandom rand, double p) => rand.NextDouble() < p;
    /// <summary>
    /// 会有<paramref name="p"/>的概率得到<see langword="true"/>
    /// 当<paramref name="p"/>不在 (0, 1) 区间时不会取随机数
    /// </summary>
    public static bool NextBoolS(this UnifiedRandom rand, float p) => p > 0 && (p >= 1 || rand.NextFloat() < p);
    /// <summary>
    /// 会有<paramref name="p"/>的概率得到<see langword="true"/>
    /// 当<paramref name="p"/>不在 (0, 1) 区间时不会取随机数
    /// </summary>
    public static bool NextBoolS(this UnifiedRandom rand, double p) => p > 0 && (p >= 1 || rand.NextDouble() < p);
    #endregion
}
