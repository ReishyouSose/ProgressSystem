namespace ProgressSystem.Core.StaticData;

public static class StaticDataHelper
{
    public static void SaveStaticDataTemplate<T, TChild>(this T self, IEnumerable<TChild> children,
        Func<TChild, string> getChildFullName,
        string childrenName, TagCompound tag,
        Action<T, TagCompound>? extraSaveStaticData = null)
        where T : IWithStaticData where TChild  : IWithStaticData
    {
        if (self.ShouldSaveStaticData)
        {
            tag["SaveStatic"] = true;
            tag["Type"] = self.GetType().FullName;
            extraSaveStaticData?.Invoke(self, tag);
        }
        TagCompound data = [..children.SelectWhere(c =>
        {
            TagCompound tag = new TagCompound().WithAction(c.SaveStaticData);
            return tag.Count == 0 ? null : NewHolder(NewPair(getChildFullName(c), (object)tag));
        })];
        if (data.Count > 0)
        {
            tag[childrenName] = data;
        }
    }
    public static void LoadStaticDataTemplate<T, TChild>(this T self,
        Func<string, TChild?> getChildByFullName,
        Action<TChild, Mod, string> setChildModAndName,
        Action<string, TChild> addChildWithFullName,
        string childrenName, TagCompound tag,
        Action<T, TagCompound>? extraLoadStaticData = null)
        where T : IWithStaticData where TChild : IWithStaticData
    {
        self.ShouldSaveStaticData = tag.GetWithDefault<bool>("SaveStatic");
        if (self.ShouldSaveStaticData)
        {
            extraLoadStaticData?.Invoke(self, tag);
        }
        TagCompound data = tag.GetWithDefault<TagCompound>(childrenName, []);
        foreach (var (fullName, value) in data)
        {
            if (value is not TagCompound childData)
            {
                continue;
            }
            var child = getChildByFullName(fullName);
            if (child != null)
            {
                child.LoadStaticData(childData);
                continue;
            }
            var tri = GetObjectWithStaticData<TChild>(fullName, childData);
            if (tri == null)
            {
                continue;
            }
            (child, Mod mod, string name) = tri.Value;
            setChildModAndName(child, mod, name);
            addChildWithFullName(fullName, child);
            child.LoadStaticData(childData);
        }
    }
    public static void SaveStaticDataListTemplate<T, TChild>(this T self, IEnumerable<TChild> children,
        string childrenName, TagCompound tag,
        Action<T, TagCompound>? extraSaveStaticData = null)
        where T : IWithStaticData where TChild : IWithStaticData
    {
        int i = 0;
        SaveStaticDataTemplate(self, children, c => i++.ToString(), childrenName, tag, extraSaveStaticData);
    }
    public static void LoadStaticDataListTemplate<T, TChild>(this T self,
        Func<int, TChild?> getChildByIndex,
        Action<int, TChild> addChildWithIndex,
        string childrenName, TagCompound tag,
        Action<T, TagCompound>? extraLoadStaticData = null)
        where T : IWithStaticData where TChild : IWithStaticData
    {
        LoadStaticDataTemplate(self, fullName => int.TryParse(fullName, out int index) ? getChildByIndex(index) : default,
            (r, m, n) => { }, (fullName, child) =>
            {
                if (!int.TryParse(fullName, out int index))
                {
                    index = -1;
                }
                addChildWithIndex(index, child);
            }, childrenName, tag, extraLoadStaticData);
    }
    public static (T Value, Mod Mod, string Name)? GetObjectWithStaticData<T>(string fullName, TagCompound data) where T : IWithStaticData
    {
        
        var tokens = fullName.Split('.', 2);
        if (tokens.Length < 2)
        {
            return null;
        }
        if (!ModLoader.TryGetMod(tokens[0], out Mod mod))
        {
            return null;
        }
        string name = tokens[1];
        if (!data.TryGet("Type", out string typeName))
        {
            return null;
        }
        tokens = typeName.Split('.', 2);
        if (!ModLoader.TryGetMod(tokens[0], out Mod modOfType))
        {
            return null;
        }
        Type? type = modOfType.GetType().Assembly.GetType(typeName);
        if (type == null || !type.IsAssignableTo(typeof(T)))
        {
            return null;
        }
        T? result = (T?)Activator.CreateInstance(type);
        if (result == null)
        {
            return null;
        }
        return (result, mod, name);
    }
}
