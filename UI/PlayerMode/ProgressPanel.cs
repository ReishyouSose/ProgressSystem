using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.UI.PlayerMode.ExtraUI;
using RUIModule;
using UIModSlot = ProgressSystem.UI.DeveloperMode.ExtraUI.UIModSlot;

namespace ProgressSystem.UI.PlayerMode
{
    public class ProgressPanel : ContainerElement
    {
        internal static ProgressPanel Ins = null!;
        public ProgressPanel() => Ins = this;
        private Mod selectedMod;
        private Achievement? focusAch;
        private UIContainerPanel achView;
        private UIContainerPanel descriptionView;
        private UIContainerPanel requireView;
        private UIContainerPanel rewardView;
        private UIText submit;
        private UIText recieve;
        private UIVnlPanel achPanel;
        private UIVnlPanel detailsPanel;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            Info.IsVisible = true;
            RemoveAll();

            achPanel = new(1000, 800);
            achPanel.SetCenter(0, 0, 0.55f, 0.5f);
            achPanel.Info.SetMargin(10);
            achPanel.canDrag = true;
            Register(achPanel);

            RegisterMainPanel(achPanel);
            RegisterFocus(achPanel);
        }
        private void RegisterMainPanel(UIVnlPanel bg)
        {
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
                    if (selectedMod.Name == modSlot.modName)
                    {
                        RUIHelper.DrawRec(sb, modSlot.HitBox(), 5f, Color.Red);
                    }
                };
                modSlot.Events.OnLeftDown += evt =>
                {
                    pageList.ClearAllElements();
                    ClearTemp();
                    selectedMod = ModLoader.GetMod(modSlot.modName);
                    if (AchievementManager.PagesByMod.TryGetValue(selectedMod, out var modPages))
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
        private void RegisterFocus(UIVnlPanel bg)
        {
            detailsPanel = new(800, 600) { canDrag = true };
            detailsPanel.SetCenter(0, 0, 0.5f, 0.5f);
            detailsPanel.Info.SetMargin(10);
            detailsPanel.Info.IsVisible = false;
            detailsPanel.Events.OnMouseOver += evt => bg.LockInteract(false);
            detailsPanel.Events.OnMouseOut += evt => bg.LockInteract(true);
            Register(detailsPanel);

            UIVnlPanel descriptionBg = new(0, 0);
            descriptionBg.Info.SetMargin(10);
            descriptionBg.SetSize(-5, -40, 0.33f, 1);
            detailsPanel.Register(descriptionBg);

            UIText desc = new("详情");
            desc.SetSize(desc.TextSize);
            desc.SetCenter(0, 15, 0.5f);
            descriptionBg.Register(desc);

            descriptionView = new();
            descriptionView.SetPos(0, 30);
            descriptionView.SetSize(-10, -30, 1, 1);
            descriptionView.autoPos[0] = true;
            descriptionBg.Register(descriptionView);

            VerticalScrollbar dV = new(30);
            dV.Info.Left.Pixel += 10;
            descriptionView.SetVerticalScrollbar(dV);
            descriptionBg.Register(dV);

            UIVnlPanel submitBg = new(0, 0);
            submitBg.SetPos(0, -30, 0, 1);
            submitBg.SetSize(-5, 30, 0.33f);
            detailsPanel.Register(submitBg);

            submit = new("提交");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 5, 0.5f, 0.5f);
            submit.HoverToGold();
            submit.Events.OnLeftDown += evt => focusAch.Submit();
            submitBg.Register(submit);

            UIVnlPanel requireBg = new(0, 0);
            requireBg.SetPos(5, 0, 0.33f, 0);
            requireBg.SetSize(-2, -40, 0.33f, 1);
            requireBg.Info.SetMargin(10);
            detailsPanel.Register(requireBg);

            UIText require = new("需求");
            require.SetSize(require.TextSize);
            require.SetCenter(0, 15, 0.5f);
            requireBg.Register(require);

            requireView = new();
            requireView.SetPos(0, 30);
            requireView.SetSize(-10, -40, 1, 1);
            requireView.autoPos[0] = true;
            requireBg.Register(requireView);

            VerticalScrollbar requireV = new(30);
            requireV.Info.Left.Pixel += 10;
            requireView.SetVerticalScrollbar(requireV);
            requireBg.Register(requireV);

            HorizontalScrollbar requireH = new() { useScrollWheel = false };
            requireH.Info.Top.Pixel += 10;
            requireView.SetHorizontalScrollbar(requireH);
            requireBg.Register(requireH);

            UIVnlPanel recieveBg = new(0, 0);
            recieveBg.SetPos(5, -30, 0.33f, 1);
            recieveBg.SetSize(-2, 30, 0.33f);
            detailsPanel.Register(recieveBg);

            recieve = new("领取");
            recieve.SetSize(recieve.TextSize);
            recieve.SetCenter(0, 5, 0.5f, 0.5f);
            recieve.HoverToGold();
            recieve.Events.OnLeftDown += evt => focusAch.TryReceiveAllReward();
            recieveBg.Register(recieve);

            UIVnlPanel rewardBg = new(0, 0);
            rewardBg.SetPos(5, 0, 0.67f, 0);
            rewardBg.SetSize(-5, -40, 0.33f, 1);
            rewardBg.Info.SetMargin(10);
            detailsPanel.Register(rewardBg);

            UIText reward = new("奖励");
            reward.SetSize(reward.TextSize);
            reward.SetCenter(0, 15, 0.5f);
            rewardBg.Register(reward);

            rewardView = new();
            rewardView.SetPos(0, 30);
            rewardView.SetSize(-5, -40, 1, 1);
            rewardView.autoPos[0] = true;
            rewardBg.Register(rewardView);

            VerticalScrollbar rewardV = new(30);
            rewardV.Info.Left.Pixel += 10;
            rewardView.SetVerticalScrollbar(rewardV);
            rewardBg.Register(rewardV);

            HorizontalScrollbar rewardH = new() { useScrollWheel = false };
            rewardH.Info.Top.Pixel += 10;
            rewardView.SetHorizontalScrollbar(rewardH);
            rewardBg.Register(rewardH);

            UIVnlPanel closeBg = new(0, 0);
            closeBg.SetPos(5, -30, 0.67f, 1);
            closeBg.SetSize(-5, 30, 0.33f);
            detailsPanel.Register(closeBg);

            UIText close = new("关闭");
            close.SetSize(close.TextSize);
            close.SetCenter(0, 5, 0.5f, 0.5f);
            close.HoverToGold();
            close.Events.OnLeftDown += evt =>
            {
                detailsPanel.Info.IsVisible = false;
                bg.LockInteract(true);
            };
            closeBg.Register(close);
        }
        private void ClearTemp()
        {
            achView?.InnerUIE.RemoveAll(MatchTempGE);
            achView?.Vscroll.ForceSetPixel(0);
            achView?.Hscroll.ForceSetPixel(0);
            descriptionView?.ClearAllElements();
            requireView?.ClearAllElements();
            rewardView?.ClearAllElements();
        }
        private static bool MatchTempGE(BaseUIElement uie) => uie is UIAchSlot or UIRequireLine;
        private void LoadPage(string pageName)
        {
            ClearTemp();
            Dictionary<string, UIAchSlot> slotByFullName = [];
            var page = AchievementManager.PagesByMod[selectedMod][pageName];
            page.SetDefaultPositionForAchievements();
            foreach (Achievement ach in page.Achievements.Values)
            {
                UIAchSlot slot = new(ach);
                slot.Events.OnLeftDown += evt => ChangeFocus(slot.ach);
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
        private void ChangeFocus(Achievement? ach)
        {
            descriptionView.ClearAllElements();
            requireView.ClearAllElements();
            rewardView.ClearAllElements();
            focusAch = ach;
            if (ach != null)
            {
                UIText name = new(ach.DisplayName.Value ?? ach.Name);
                name.SetSize(name.TextSize);
                descriptionView.AddElement(name);

                UIText desc = new(ach.Description.Value ?? "没有描述");
                desc.SetSize(desc.TextSize);
                descriptionView.AddElement(desc);

                CheckRequirements(ach.Requirements, 0);
                CheckRewards(ach.Rewards, 0);
                detailsPanel.Info.IsVisible = true;
                achPanel.LockInteract(false);
            }
        }
        private void CheckRequirements(RequirementList requires, int index)
        {
            if (requires.Any())
            {
                foreach (Requirement require in requires)
                {
                    UIRequireText text = new(require, requires);
                    text.SetPos(index * 30, 0);
                    requireView.AddElement(text);
                    if (require is CombineRequirement combine)
                    {
                        CheckRequirements(combine.Requirements, index + 1);
                    }
                }
            }
            else
            {
                UIText none = new("空需求");
                none.SetPos(index * 30, 0);
                none.SetSize(none.TextSize);
                requireView.AddElement(none);
            }
            if (index == 0)
                requireView.Calculation();
        }
        private void CheckRewards(RewardList rewards, int index, CombineReward? combineR = null)
        {
            if (rewards.Any())
            {
                foreach (Reward reward in rewards)
                {
                    UIRewardText text = new(reward, rewards);
                    text.SetPos(index * 30, 0);
                    rewardView.AddElement(text);
                    if (combineR != null)
                    {
                        text.selected.color = combineR.Contains(index) ? Color.Green : Color.Red;
                        text.text.HoverToGold();
                        text.text.Events.OnLeftDown += evt =>
                        {
                            bool? result = combineR.TrySelect(text.reward);
                            if (result == null)
                                return;
                            text.selected.color = result.Value ? Color.Green : Color.Red;
                        };
                    }
                    if (reward is CombineReward combine)
                    {
                        CheckRewards(combine.Rewards, index + 1, combine);
                    }
                }
            }
            else
            {
                UIText none = new("空奖励");
                none.SetPos(index * 30, 0);
                none.SetSize(none.TextSize);
                rewardView.AddElement(none);
            }
            if (index == 0)
                rewardView.Calculation();
        }
    }
}
