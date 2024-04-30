namespace ProgressSystem.Core.Interfaces;

public interface IAchievementNode
{
    IEnumerable<IAchievementNode> NodeChildren => [];
    void Start() { }
    void Reset() { }
    void PostInitialize() { }
    public void StartTree()
    {
        Start();
        foreach (var child in NodeChildren)
        {
            child.StartTree();
        }
    }
    public void ResetTree()
    {
        Reset();
        foreach (var child in NodeChildren)
        {
            child.ResetTree();
        }
    }
    public void PostInitializeTree()
    {
        PostInitialize();
        foreach (var child in NodeChildren)
        {
            child.PostInitializeTree();
        }
    }
}
