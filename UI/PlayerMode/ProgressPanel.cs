using ProgressSystem.UI.PlayerMode.ExtraUI;
using RUIModule;
using UIModSlot = ProgressSystem.UI.DeveloperMode.ExtraUI.UIModSlot;

namespace ProgressSystem.UI.PlayerMode
{
    public class ProgressPanel : ContainerElement
    {
        internal static ProgressPanel Ins = null!;
        public ProgressPanel() => Ins = this;
        private Mod seletedMod = null!;
        private UIContainerPanel achView = null!;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            Info.IsVisible = true;
            RemoveAll();
            RegisterEditPagePanel();
        }
        private void RegisterEditPagePanel()
        {
            UIVnlPanel bg = new(1000, 800);
            bg.SetCenter(0, 0, 0.55f, 0.5f);
            bg.Info.SetMargin(10);
            bg.canDrag = true;
            Register(bg);

            UIVnlPanel groupFilter = new(0, 0);
            groupFilter.SetSize(100, 0, 0, 1);
            groupFilter.Info.SetMargin(10);
            bg.Register(groupFilter);

            UIContainerPanel groupView = new();
            groupView.SetSize(0, 0, 1, 1);
            groupView.autoPos[0] = true;
            groupView.spaceY = 10;
            groupFilter.Register(groupView);

            VerticalScrollbar gv = new(100, canDrag: false);
            groupView.SetVerticalScrollbar(gv);
            groupFilter.Register(gv);

            UIVnlPanel eventPanel = new(0, 0);
            eventPanel.Info.SetMargin(10);
            eventPanel.SetPos(110, 40);
            eventPanel.SetSize(-110, -40, 1, 1, false);
            bg.Register(eventPanel);
            int left = 110;

            UIDropDownList<UIText> pageList = new(bg, eventPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            pageList.showArea.SetPos(110, 0);
            pageList.showArea.SetSize(200, 30);

            pageList.expandArea.SetPos(110, 30);
            pageList.expandArea.SetSize(200, 100);

            pageList.expandView.autoPos[0] = true;
            left += pageList.showArea.Width + 10;

            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.Side != ModSide.Both || mod.Assets == null || !mod.HasAsset("icon"))
                    continue;
                string modName = mod.Name;
                UIModSlot modSlot = new(RUIHelper.T2D(modName + "/icon"), modName) { hoverText = mod.DisplayName };
                modSlot.ReDraw = sb =>
                {
                    modSlot.DrawSelf(sb);
                    if (seletedMod.Name == modSlot.modName)
                    {
                        RUIHelper.DrawRec(sb, modSlot.HitBox(), 5f, Color.Red);
                    }
                };
                modSlot.Events.OnLeftDown += evt =>
                {
                    pageList.ClearAllElements();
                    ClearTemp();
                    seletedMod = ModLoader.GetMod(modSlot.modName);
                    if (AchievementManager.PagesByMod.TryGetValue(seletedMod, out var modPages))
                    {
                        bool have = false;
                        foreach (var (page, ges) in modPages)
                        {
                            UIText pageName = new(page);
                            pageName.SetSize(pageName.TextSize);
                            pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                            pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                            pageName.Events.OnLeftDown += evt => LoadPage(pageName.text);
                            pageList.AddElement(pageName);
                            have = true;
                        }
                        if (have)
                        {
                            pageList.ChangeShowElement(0);
                            pageList.expandView.InnerUIE[0].Events.LeftDown(null);
                        }
                    }
                };
                groupView.AddElement(modSlot);
            }
            groupView.InnerUIE[0].Events.LeftDown(null);

            achView = new();
            achView.SetSize(-20, -20, 1, 1);
            eventPanel.Register(achView);

            VerticalScrollbar ev = new(80);
            ev.Info.Left.Pixel += 10;
            achView.SetVerticalScrollbar(ev);
            eventPanel.Register(ev);

            HorizontalScrollbar eh = new(80) { useScrollWheel = false };
            eh.Info.Top.Pixel += 10;
            achView.SetHorizontalScrollbar(eh);
            eventPanel.Register(eh);
        }
        private void ClearTemp()
        {
            achView?.InnerUIE.RemoveAll(MatchTempGE);
            achView?.Vscroll.ForceSetPixel(0);
            achView?.Hscroll.ForceSetPixel(0);
        }
        private static bool MatchTempGE(BaseUIElement uie) => uie is UIAchSlot or UIRequireLine;
        private void LoadPage(string pageName)
        {
            ClearTemp();
            Dictionary<string, UIAchSlot> slotByFullName = [];
            var page = AchievementManager.PagesByMod[seletedMod][pageName];
            page.SetDefaultPositionForAchievements();
            foreach (Achievement ach in page.Achievements.Values)
            {
                UIAchSlot slot = new(ach);
                achView.AddElement(slot);
                slotByFullName.Add(ach.FullName, slot);
            }
            foreach (UIAchSlot slot in slotByFullName.Values)
            {
                Achievement orig = slot.ach;
                foreach (Achievement pre in orig.Predecessors)
                {
                    RegisterRequireLine(slotByFullName, orig, pre);
                }
            }
        }
        private void RegisterRequireLine(Dictionary<string, UIAchSlot> slotByFullName, Achievement orig, Achievement pre)
        {
            UIAchSlot start = slotByFullName[pre.FullName];
            UIAchSlot end = slotByFullName[orig.FullName];
            UIRequireLine line = new(start, end);
            achView.AddElement(line, 0);
            end.preLine.Add(line);
        }
    }
}
