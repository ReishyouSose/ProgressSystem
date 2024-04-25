namespace ProgressSystem.Core.Interfaces;

public interface IAchievementNode
{
    IEnumerable<IAchievementNode> NodeChildren => [];
    void Start() { }
    void Reset() { }
    public void StartTree() => IAchievementNodeHelper.StartTree(this);
    public void ResetTree() => IAchievementNodeHelper.ResetTree(this);
}

public static class IAchievementNodeHelper
{
    public static void StartTree(IAchievementNode node)
    {
        node.Start();
        foreach (var child in node.NodeChildren)
        {
            child.StartTree();
        }
    }
    public static void ResetTree(IAchievementNode node)
    {
        node.Reset();
        foreach (var child in node.NodeChildren)
        {
            child.ResetTree();
        }
    }
}
