using Microsoft.Xna.Framework.Input;
using ProgressSystem.UI.DeveloperMode.ExtraUI;
using RUIModule;

namespace ProgressSystem.UI.DeveloperMode.AchEditor
{
    public partial class AchEditor
    {
        private void CheckKeyState()
        {
            KeyboardState state = Keyboard.GetState();
            LeftShift = state.IsKeyDown(Keys.LeftShift);
            LeftCtrl = state.IsKeyDown(Keys.LeftControl);
            LeftAlt = state.IsKeyDown(Keys.LeftAlt);
            bool pressS = state.IsKeyDown(Keys.S);
            bool delete = state.IsKeyDown(Keys.Delete);
            bool pressR = state.IsKeyDown(Keys.R);
            if (!pressS)
                trySave = false;
            if (!delete)
                tryDelete = false;
            if (!trySave && LeftCtrl && pressS)
            {
                SaveProgress();
                trySave = true;
                ChangeSaveState(true);
            }
            if (!tryDelete && delete)
            {
                foreach (UIAchSlot slot in frameSelect)
                {
                    RemoveAchSlot(slot, true);
                }
                frameSelect.Clear();
                tryDelete = true;
            }
        }
        private void CheckDragging()
        {
            if (dragging || collision != null)
            {
                Point target = Main.MouseScreen.ToPoint();
                var eh = achView.Hscroll;
                var ev = achView.Vscroll;
                if (target.X > achView.Right && eh.Real < 1)
                {
                    eh.MoveView(target.X - achView.Right, 15);
                }
                else if (target.X < achView.Left && eh.Real > 0)
                {
                    eh.MoveView(target.X - achView.Left, 40);
                }
                if (target.Y > achView.Bottom && ev.Real < 1)
                {
                    ev.MoveView(target.Y - achView.Bottom, 15);
                }
                else if (target.Y < achView.Top && ev.Real > 0)
                {
                    ev.MoveView(target.Y - achView.Top, 40);
                }
            }
            if (draggingSelected)
            {
                Vector2 mouse = Main.MouseScreen;
                Vector2 origin = achView.ChildrenElements[0].HitBox(false).TopLeft();
                int x = (int)(mouse.X - origin.X) / 80;
                int y = (int)(mouse.Y - origin.Y) / 80;
                x = Math.Max(x, 0);
                y = Math.Max(y, 0);
                Vector2 p = new(x, y);
                Vector2 offset = p - selectedStart;
                if (offset != Vector2.Zero)
                {
                    bool allMet = true;
                    foreach (UIAchSlot ge in frameSelect)
                    {
                        AchPos.Remove(ge.pos);
                    }
                    foreach (UIAchSlot ge in frameSelect)
                    {
                        Vector2 newPos = ge.pos + offset;
                        if (newPos.X < 0)
                        {
                            allMet = false;
                            break;
                        }
                        if (newPos.Y < 0)
                        {
                            allMet = false;
                            break;
                        }
                        Vector2 pos = ge.pos + offset;
                        if (AchPos.Contains(pos))
                        {
                            allMet = false;
                            break;
                        }
                    }
                    if (allMet)
                    {
                        foreach (UIAchSlot ge in frameSelect)
                        {
                            ge.pos += offset;
                            AchPos.Add(ge.pos);
                            ge.SetPos(ge.pos * 80);
                        }
                        selectedStart = p;
                        ChangeSaveState(false);
                    }
                }
            }
        }
        private void AchSlotLeftCheck(BaseUIElement uie)
        {
            UIAchSlot ge = (UIAchSlot)uie;
            Achievement ach = ge.ach;
            ChangeEditingAch(ach);
            if (ach.ShouldSaveStaticData)
                return;
            if (LeftAlt)
            {
                frameSelect.Clear();
                tempSelect.Clear();
                if (preSetting == null)
                {
                    preSetting = ge;
                    ge.preSetting = true;
                }
                else
                {
                    if (preSetting != ge)
                    {
                        Achievement orig = preSetting.ach;
                        Achievement pre = ach;
                        if (pre.Predecessors.Contains(orig))
                        {
                            Main.NewText("不可互为前置");
                            return;
                        }
                        if (preSetting.PreAch.Contains(ge))
                        {
                            RemoveRequireLine(orig, pre);
                        }
                        else
                        {
                            RegisterRequireLine(orig, pre);
                        }
                        ChangeSaveState(false);
                    }
                }
                return;
            }
            if (LeftCtrl)
            {
                if (frameSelect.Contains(ge))
                {
                    frameSelect.Remove(ge);
                    ge.selected = false;
                }
                else
                    ge.selected = frameSelect.Add(ge);
            }
            else if (frameSelect.Count != 0)
            {
                draggingSelected = true;
                Point mouse = (Main.MouseScreen - achView.ChildrenElements[0].HitBox(false).TopLeft()).ToPoint();
                selectedStart = new(mouse.X / 80, mouse.Y / 80);
            }
            AchPos.Remove(ge.pos);
            dragging = true;
        }
        private void GESlotUpdate(BaseUIElement uie)
        {
            UIAchSlot ge = (UIAchSlot)uie;
            if (ge.ach.ShouldSaveStaticData)
                return;
            if (collision != null)
            {
                bool intersects = ge.HitBox().Intersects(collision.selector);
                if (LeftCtrl)
                {
                    if (!interacted.Contains(ge) && intersects)
                    {
                        if (frameSelect.Contains(ge))
                        {
                            frameSelect.Remove(ge);
                            ge.selected = false;
                        }
                        else
                            ge.selected = frameSelect.Add(ge);
                        interacted.Add(ge);
                    }
                }
                else
                {
                    if (intersects)
                    {
                        tempSelect.Add(ge);
                        ge.selected = true;
                    }
                    else
                    {
                        tempSelect.Remove(ge);
                        ge.selected = false;
                    }
                }
            }
        }
        private void RegisterEventToAchSlot(UIAchSlot ge)
        {
            var ev = achView.Vscroll;
            var eh = achView.Hscroll;
            ge.Events.OnMouseOver += evt =>
            {
                ev.canDrag = false;
                eh.canDrag = false;
            };
            ge.Events.OnMouseOut += evt =>
            {
                ev.canDrag = true;
                eh.canDrag = true;
            };
            ge.Events.OnLeftDown += AchSlotLeftCheck;
            ge.Events.OnLeftUp += evt =>
            {
                dragging = false;
                draggingSelected = false;
                if (ge.ach.Position != ge.pos)
                {
                    ge.ach.Position = ge.pos;
                    ChangeSaveState(false);
                }
                AchPos.Add(ge.pos);
            };
            ge.Events.OnUpdate += GESlotUpdate;
            ge.Events.OnRightDoubleClick += evt => RemoveAchSlot(ge);
            ge.ReDraw = sb =>
            {
                ge.DrawSelf(sb);
                if (ge.ach.FullName == EditingAchFullName)
                {
                    RUIHelper.DrawRec(sb, ge.HitBox().Modified(4, 4, -8, -8), 2f, Color.SkyBlue);
                }
            };
        }
    }
}
