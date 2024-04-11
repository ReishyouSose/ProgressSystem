namespace ProgressSystem.UIEditor.ExtraUI;

public class UIGECollision : BaseUIElement
{
    private Vector2 startPos;
    public UIGECollision(Vector2 startPos)
    {
        this.startPos = startPos;
    }
    public override void Update(GameTime gt)
    {
        base.Update(gt);

    }
}
