namespace ProgressSystem.Core.Requirements.MiscRequirements;

/// <summary>
/// 有一个房子
/// </summary>
public class HouseRequirement : Requirement
{
    #region 钩子
    static HouseRequirement()
    {
        MonoModHooks.Add(typeof(WorldGen).GetMethod("TrySpawningTownNPC", TMLReflection.bfns), HookTrySpawningTownNPC);
    }
    static void HookTrySpawningTownNPC(Action<int, int> orig, int x, int y)
    {
        if (OnFindHouse != null)
        {
            CheckHouse(x, y);
        }

        orig(x, y);
    }
    /// <summary>
    /// 根据<see cref="WorldGen.SpawnTownNPC(int, int)"/>编写
    /// </summary>
    static void CheckHouse(int x, int y)
    {
		bool flag = Main.tileSolid[379];
		Main.tileSolid[379] = true;
        CheckHouseInner(x, y);
		Main.tileSolid[379] = flag;
    }
    static void CheckHouseInner(int x, int y)
    {
        if (!Main.wallHouse[Main.tile[x, y].WallType])
        {
            return;
        }
        if (!WorldGen.StartRoomCheck(x, y))
        {
            return;
        }
        if (!WorldGen.RoomNeeds(-1))
        {
            return;
        }
        // if (WorldGen.roomHasStinkbug || WorldGen.roomHasEchoStinkbug)
        // {
        //     return;
        // }
        WorldGen.ScoreRoom();
        if (WorldGen.hiScore <= 0)
        {
            // if (!WorldGen.roomOccupied)
            if (WorldGen.hiScore != -1)
            {
                return;
            }
        }
        OnFindHouse?.Invoke();
    }
    public static Action? OnFindHouse;
    #endregion

    public HouseRequirement() : base() { }
    protected override void BeginListen()
    {
        base.BeginListen();
        OnFindHouse += ListenHasHouse;
    }
    protected override void EndListen()
    {
        base.EndListen();
        OnFindHouse -= ListenHasHouse;
    }
    private void ListenHasHouse()
    {
        CompleteSafe();
    }
}
