using Microsoft.Xna.Framework.Graphics;
using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.UI.DeveloperMode.ExtraUI;
using RUIModule;

namespace ProgressSystem.UI.DeveloperMode.AchEditor
{
    public partial class AchEditor
    {
        private void RegisterEditAchPanel()
        {
            UIVnlPanel editBg = new(430, 300) { canDrag = true };
            editBg.Info.SetMargin(10);
            editBg.SetCenter(editBg.Width / 2f, 0, 0, 0.5f);
            Register(editBg);

            UIVnlPanel[] editPanels = new UIVnlPanel[3];
            for (int i = 0; i < 3; i++)
            {
                ref UIVnlPanel panel = ref editPanels[i];
                panel = new(0, 0);
                panel.Info.SetMargin(10);
                panel.SetSize(-30, 0, 1, 1);
                editBg.Register(panel);
            }
            editPanels[1].Info.IsVisible = false;
            editPanels[2].Info.IsVisible = false;
            string path = "ProgressSystem/Assets/Textures/UI/";

            void SwitchPanel(int id)
            {
                editingPanel = id;
                for (int i = 0; i < 3; i++)
                    editPanels[i].Info.IsVisible = i == id;
            }

            void DrawFocusFrame(SpriteBatch sb, BaseUIElement uie, int id)
            {
                uie.DrawSelf(sb);
                if (editingPanel == id)
                {
                    RUIHelper.DrawRec(sb, uie.HitBox(), 2f, Color.Gold);
                }
            }

            UIImage baseInfo = new(RUIHelper.T2D(path + "BaseInfo")) { hoverText = "基本信息" };
            baseInfo.SetPos(-22, 0, 1);
            baseInfo.Events.OnLeftDown += evt => SwitchPanel(0);
            baseInfo.ReDraw = sb => DrawFocusFrame(sb, baseInfo, 0);
            editBg.Register(baseInfo);

            UIImage require = new(RUIHelper.T2D(path + "Require")) { hoverText = "需求设置" };
            require.SetPos(-22, 32, 1);
            require.Events.OnLeftDown += evt => SwitchPanel(1);
            require.ReDraw = sb => DrawFocusFrame(sb, require, 1);
            editBg.Register(require);

            UIImage reward = new(RUIHelper.T2D(path + "Reward")) { hoverText = "奖励设置" };
            reward.SetPos(-22, 64, 1);
            reward.Events.OnLeftDown += evt => SwitchPanel(2);
            reward.ReDraw = sb => DrawFocusFrame(sb, reward, 2);
            editBg.Register(reward);

            UIAdjust adjust = new();
            editBg.Register(adjust);

            RegisterBaseInfoPanel(editPanels[0]);
            RegisterRequirePanel(editPanels[1]);
            RegisterRewardPanle(editPanels[2]);
        }
        private void RegisterBaseInfoPanel(BaseUIElement panel)
        {
            int y = 0;
            UIVnlPanel achNameInputBg = new(0, 0);
            achNameInputBg.SetSize(0, 30, 1);
            achNameInputBg.Info.RightMargin.Pixel = 10;
            panel.Register(achNameInputBg);
            y += achNameInputBg.Height + 10;

            UIText nameChecker = new("未选中成就", Color.Red);
            nameChecker.SetPos(0, y);
            nameChecker.SetSize(nameChecker.TextSize);
            panel.Register(nameChecker);

            achNameInputer = new("输入成就名");
            achNameInputer.SetSize(-70, 0, 1, 1);
            achNameInputer.OnInputText += text =>
            {
                if (EditingPage == null || EditingAch == null)
                {
                    return;
                }
                Achievement current = EditingAch;
                var achs = EditingPage.Achievements;
                if (text.Any())
                {
                    if (text == current.Name)
                    {
                        nameChecker.ChangeText("名称可用", false);
                        nameChecker.color = Color.Green;
                        return;
                    }
                    if (achs.TryGetValue(current.Mod.Name + "." + text, out Achievement? ach))
                    {
                        nameChecker.ChangeText($"名称已被{ach.Position}占用", false);
                        nameChecker.color = Color.Red;
                    }
                    else
                    {
                        nameChecker.ChangeText("名称可用", false);
                        nameChecker.color = Color.Green;
                    }
                }
                else
                {
                    nameChecker.ChangeText("名称不可为空", false);
                    nameChecker.color = Color.Red;
                }
            };
            achNameInputBg.Register(achNameInputer);

            UIClose clearName = new();
            clearName.SetCenter(-10, 0, 1, 0.5f);
            clearName.Events.OnLeftDown += evt => achNameInputer.ClearText();
            achNameInputBg.Register(clearName);

            UIText changeName = new("修改名称");
            changeName.SetSize(changeName.TextSize);
            changeName.SetPos(-changeName.Width, y, 1);
            changeName.HoverToGold();
            changeName.Events.OnLeftDown += evt =>
            {
                UIAchSlot? currentSlot = EditingAchSlot;
                if (nameChecker.color == Color.Green && currentSlot != null)
                {
                    Achievement ach = EditingAch!;
                    string oldName = EditingAchFullName;
                    slotByFullName.Remove(oldName);
                    EditingPage!.Remove(oldName);

                    ach.Name = achNameInputer.Text;
                    EditingPage!.Add(ach);
                    slotByFullName.Add(ach.FullName, currentSlot);
                    ChangeSaveState(false);
                }
            };
            achNameInputBg.Register(changeName);
            y += nameChecker.Height;

            UIVnlPanel submitBg = new(0, 0);
            submitBg.SetPos(0, y);
            submitBg.SetSize(0, 30, 1);
            panel.Register(submitBg);
            y += submitBg.Height + 10;

            submit = new("需要手动提交    否");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 5, 0.5f, 0.5f);
            submit.HoverToGold();
            submit.Events.OnLeftDown += evt =>
            {
                if (EditingAch != null)
                {
                    ref bool needSubmit = ref EditingAch.NeedSubmit;
                    needSubmit = !needSubmit;
                    submit.ChangeText($"需要手动提交    {(needSubmit ? "是" : "否")}");
                    ChangeSaveState(false);
                }
            };
            submitBg.Register(submit);

            UIVnlPanel preNeedCountBg = new(0, 0);
            preNeedCountBg.SetPos(0, y);
            preNeedCountBg.SetSize(0, 30, 1);
            panel.Register(preNeedCountBg);
            y += preNeedCountBg.Height + 10;

            UIText preNeedCount = new("前置进度需求量");
            preNeedCount.SetPos(10, 5);
            preNeedCount.SetSize(preNeedCount.TextSize);
            preNeedCountBg.Register(preNeedCount);

            int left = 0;
            UIImage preIncrease = new(AssetLoader.Increase);
            preIncrease.Info.Left.Set(-30, 1);
            preIncrease.Info.Top.Pixel = 5;
            preIncrease.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                if (need == null)
                    return;
                need++;
                preCount.ChangeText(need.Value.ToString(), false);
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preIncrease);
            left += 40;

            preCount = new("未选", drawStyle: 1);
            preCount.SetSize(preCount.TextSize);
            preCount.SetPos(-preCount.TextSize.X - left, 5, 1);
            preCount.HoverToGold();
            preCount.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                string text;
                if (need.HasValue)
                {
                    need = null;
                    text = "null";
                }
                else
                {
                    need = 0;
                    text = "0";
                }
                preCount.ChangeText(text, false);
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preCount);
            left += preCount.Width + 10;

            UIImage preDecrease = new(AssetLoader.Decrease);
            preDecrease.Info.Left.Set(-left - 20, 1);
            preDecrease.Info.Top.Pixel = 5;
            preDecrease.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                if (need == null)
                    return;
                need--;
                preCount.ChangeText(need.Value.ToString(), false);
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preDecrease);
        }
        private void RegisterRequirePanel(BaseUIElement panel)
        {
            float leftWidth = 0.4f;
            UIVnlPanel constructBg = new(0, 0);
            constructBg.SetSize(0, -80, leftWidth, 1);
            constructBg.SetPos(0, 80);
            panel.Register(constructBg);

            UIContainerPanel constructView = new();
            constructView.autoPos[0] = 10;
            constructView.SetPos(10, 10);
            constructView.SetSize(-40, -20, 1, 1);
            constructBg.Register(constructView);

            VerticalScrollbar dataV = new(28, false, false);
            dataV.Info.Top.Pixel += 5;
            dataV.Info.Height.Pixel -= 10;
            constructView.SetVerticalScrollbar(dataV);
            constructBg.Register(dataV);

            UIVnlPanel cdsNeedCountBg = new(0, 0);
            cdsNeedCountBg.SetSize(0, 30, 1);
            panel.Register(cdsNeedCountBg);

            UIText needCount = new("需求达成数");
            needCount.SetPos(10, 5);
            needCount.SetSize(needCount.TextSize);
            cdsNeedCountBg.Register(needCount);

            int left = 0;
            UIImage increase = new(AssetLoader.Increase);
            increase.Info.Left.Set(-30, 1);
            increase.Info.Top.Pixel = 5;
            increase.Events.OnLeftDown += evt =>
            {
                CombineRequirement? combine = EditingCombineRequire;
                if (combine != null)
                {
                    combine.Count += 1;
                    UpdateRqsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else if (EditingAch != null)
                {
                    EditingAch.RequirementCountNeeded += 1;
                    UpdateRqsCountText();
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个需求层级/成就栏位");
                }
            };
            cdsNeedCountBg.Register(increase);
            left += 40;

            rqsCountText = new("未选", drawStyle: 1);
            rqsCountText.SetSize(rqsCountText.TextSize);
            rqsCountText.SetPos(-rqsCountText.TextSize.X - left, 5, 1);
            cdsNeedCountBg.Register(rqsCountText);
            left += rqsCountText.Width + 10;

            UIImage decrease = new(AssetLoader.Decrease);
            decrease.Info.Left.Set(-left - 20, 1);
            decrease.Info.Top.Pixel = 5;
            decrease.Events.OnLeftDown += evt =>
            {
                CombineRequirement? combine = EditingCombineRequire;
                if (combine != null)
                {
                    combine.Count -= 1;
                    UpdateRqsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else if (EditingAch != null)
                {
                    EditingAch.RequirementCountNeeded -= 1;
                    UpdateRqsCountText();
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个需求层级/成就栏位");
                }
            };
            cdsNeedCountBg.Register(decrease);

            UIVnlPanel addCombineBg = new(0, 0);
            addCombineBg.SetPos(10, 40, leftWidth);
            addCombineBg.SetSize(-10, 30, 1 - leftWidth);
            panel.Register(addCombineBg);

            UIText addCombine = new("添加组合需求") { hoverText = "默认需求一个" };
            addCombine.SetSize(addCombine.TextSize);
            addCombine.SetCenter(0, 5, 0.5f, 0.5f);
            addCombine.HoverToGold();
            addCombine.Events.OnLeftDown += evt =>
            {
                var editingRequires = EditingRequires;
                if (editingRequires == null)
                {
                    Main.NewText("请先选择一个需求层级/成就栏位");
                    return;
                }
                CombineRequirement require = new(1)
                {
                    ShouldSaveStaticData = true
                };
                editingRequires.Add(require);
                EditingCombineRequire = require;
                UpdateRqsCountText();
                CheckRequirements();
                ChangeSaveState(false);
            };
            addCombineBg.Register(addCombine);

            UIVnlPanel requirePanel = new(0, 0);
            requirePanel.SetPos(10, 80, leftWidth);
            requirePanel.SetSize(-10, -80, 1 - leftWidth, 1);
            requirePanel.Info.SetMargin(10);
            panel.Register(requirePanel);

            requireView = new();
            requireView.SetSize(-20, -10, 1, 1);
            requireView.autoPos[0] = 10;
            requireView.Events.OnRightDown += evt => EditingCombineRequire = null;
            requirePanel.Register(requireView);

            VerticalScrollbar rqsV = new();
            rqsV.Info.Left.Pixel += 10;
            requireView.SetVerticalScrollbar(rqsV);
            requirePanel.Register(rqsV);

            HorizontalScrollbar rqsH = new() { useScrollWheel = false };
            rqsH.Info.Top.Pixel += 10;
            requireView.SetHorizontalScrollbar(rqsH);
            requirePanel.Register(rqsH);

            UIDropDownList<UIText> constructList = new(panel, constructBg, x => new(x.text)) { buttonXoffset = 10 };

            constructList.showArea.SetPos(0, 40);
            constructList.showArea.SetSize(0, 30, leftWidth);
            constructList.showArea.SetMargin(10, 5);

            constructList.expandArea.SetPos(0, 80);
            constructList.expandArea.SetSize(0, -80, leftWidth, 1);

            constructList.expandView.autoPos[0] = 5;
            constructList.expandView.Vscroll.canDrag = false;

            string end = "Requirement";
            int len = end.Length;

            foreach (var require in ModContent.GetContent<Requirement>())
            {
                if (require is CombineRequirement)
                    continue;
                var tables = require.GetConstructInfoTables();
                var requireName = require.GetType().Name;
                if (requireName.EndsWith(end))
                {
                    requireName = requireName[0..^len];
                }
                UIText requireType = new(requireName);
                requireType.SetSize(requireType.TextSize);
                requireType.HoverToGold();
                requireType.Events.OnLeftDown += evt =>
                {
                    constructView.ClearAllElements();
                    foreach (var constructInfo in tables)
                    {
                        RegisterRequireDataPanel(constructInfo, constructView);
                    }
                };
                constructList.AddElement(requireType);
            }
            constructList.ChangeShowElement(0);
        }
        private void RegisterRewardPanle(BaseUIElement panel)
        {
            float leftWidth = 0.4f;
            UIVnlPanel constructBg = new(0, 0);
            constructBg.SetPos(0, 80);
            constructBg.SetSize(0, -80, leftWidth, 1);
            panel.Register(constructBg);

            UIContainerPanel constructView = new();
            constructView.autoPos[0] = 10;
            constructView.SetPos(10, 10);
            constructView.SetSize(-40, -20, 1, 1);
            constructBg.Register(constructView);

            VerticalScrollbar dataV = new(28, false, false);
            dataV.Info.Top.Pixel += 5;
            dataV.Info.Height.Pixel -= 10;
            constructView.SetVerticalScrollbar(dataV);
            constructBg.Register(dataV);

            UIDropDownList<UIText> constructList = new(panel, constructBg, x => new(x.text)) { buttonXoffset = 10 };

            constructList.showArea.SetPos(0, 40);
            constructList.showArea.SetSize(0, 30, leftWidth);
            constructList.showArea.SetMargin(10, 5);

            constructList.expandArea.SetPos(0, 80);
            constructList.expandArea.SetSize(0, -80, leftWidth, 1);

            constructList.expandView.autoPos[0] = 5;
            constructList.expandView.Vscroll.canDrag = false;

            string end = "Reward";
            int len = end.Length;

            foreach (Reward reward in ModContent.GetContent<Reward>())
            {
                if (reward is CombineReward)
                    continue;
                var tables = reward.GetConstructInfoTables();
                var rewardName = reward.GetType().Name;
                if (rewardName.EndsWith(end))
                {
                    rewardName = rewardName[0..^len];
                }
                UIText rewardType = new(rewardName);
                rewardType.SetSize(rewardType.TextSize);
                rewardType.HoverToGold();
                rewardType.Events.OnLeftDown += evt =>
                {
                    constructView.ClearAllElements();
                    foreach (var constructInfo in tables)
                    {
                        RegisterRewardDataPanel(constructInfo, constructView);
                    }
                };
                constructList.AddElement(rewardType);
                constructList.ChangeShowElement(0);
            }
            UIVnlPanel rwsSelectCountBg = new(0, 0);
            rwsSelectCountBg.SetSize(0, 30, 1);
            panel.Register(rwsSelectCountBg);

            UIText selectCount = new("奖励可选数");
            selectCount.SetPos(10, 5);
            selectCount.SetSize(selectCount.TextSize);
            rwsSelectCountBg.Register(selectCount);

            int left = 0;
            UIImage increase = new(AssetLoader.Increase);
            increase.Info.Left.Set(-30, 1);
            increase.Info.Top.Pixel = 5;
            increase.Events.OnLeftDown += evt =>
            {
                CombineReward? combine = EditingCombineReward;
                if (combine != null)
                {
                    combine.Count += 1;
                    UpdateRwsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRewards();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个奖励层级");
                }
            };
            rwsSelectCountBg.Register(increase);
            left += 40;

            rwsCountText = new("未选", drawStyle: 1);
            rwsCountText.SetSize(rwsCountText.TextSize);
            rwsCountText.SetPos(-rwsCountText.TextSize.X - left, 5, 1);
            rwsSelectCountBg.Register(rwsCountText);
            left += rwsCountText.Width + 10;

            UIImage decrease = new(AssetLoader.Decrease);
            decrease.Info.Left.Set(-left - 20, 1);
            decrease.Info.Top.Pixel = 5;
            decrease.Events.OnLeftDown += evt =>
            {
                CombineReward? combine = EditingCombineReward;
                if (combine != null)
                {
                    combine.Count -= 1;
                    UpdateRwsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRewards();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个奖励层级");
                }
            };
            rwsSelectCountBg.Register(decrease);

            UIVnlPanel addCombineBg = new(0, 0);
            addCombineBg.SetPos(10, 40, leftWidth);
            addCombineBg.SetSize(-10, 30, 1 - leftWidth);
            panel.Register(addCombineBg);

            UIText addCombine = new("添加选择奖励") { hoverText = "默认可选一个" };
            addCombine.SetSize(addCombine.TextSize);
            addCombine.SetCenter(0, 5, 0.5f, 0.5f);
            addCombine.HoverToGold();
            addCombine.Events.OnLeftDown += evt =>
            {
                var editingRewards = EditingRewards;
                if (editingRewards == null)
                {
                    Main.NewText("请先选择一个奖励层级");
                    return;
                }
                CombineReward combine = new()
                {
                    ShouldSaveStaticData = true
                };
                editingRewards.Add(combine);
                EditingCombineReward = combine;
                CheckRewards();
                ChangeSaveState(false);
            };
            addCombineBg.Register(addCombine);

            UIVnlPanel rewardPanel = new(0, 0);
            rewardPanel.SetPos(10, 80, leftWidth);
            rewardPanel.SetSize(-10, -80, 1 - leftWidth, 1);
            rewardPanel.Info.SetMargin(10);
            panel.Register(rewardPanel);

            rewardView = new();
            rewardView.SetSize(-20, -10, 1, 1);
            rewardView.autoPos[0] = 5;
            rewardView.Events.OnRightDown += evt => EditingCombineReward = null;
            rewardPanel.Register(rewardView);

            VerticalScrollbar rwsV = new();
            rwsV.Info.Left.Pixel += 10;
            rewardView.SetVerticalScrollbar(rwsV);
            rewardPanel.Register(rwsV);

            HorizontalScrollbar rwsH = new() { useScrollWheel = false };
            rwsH.Info.Top.Pixel += 10;
            rewardView.SetHorizontalScrollbar(rwsH);
            rewardPanel.Register(rwsH);
        }
        private void RegisterRequireDataPanel(ConstructInfoTable<Requirement> data, UIContainerPanel constructView)
        {
            UIVnlPanel constructPanel = new(0, 0);
            constructPanel.Info.SetMargin(10);
            constructView.AddElement(constructPanel);
            int innerY = 0;
            foreach (var info in data)
            {
                UIText name = new(info.DisplayName.Value ?? "Anonymous");
                name.SetPos(0, innerY);
                name.SetSize(name.TextSize);
                constructPanel.Register(name);
                innerY += 28;

                UIText legal = new(info.Important ? "可以为空" : "不可为空");
                legal.SetPos(0, innerY);
                constructPanel.Register(legal);
                innerY += 28;

                UIVnlPanel valueInputBg = new(0, 28);
                valueInputBg.Info.Width.Percent = 1;
                valueInputBg.SetPos(0, innerY);
                constructPanel.Register(valueInputBg);

                UIInputBox valueInputer = new(info.Type.Name);
                valueInputer.SetSize(-40, 0, 1, 1);
                var bind = info;
                valueInputer.OnInputText += text =>
                {
                    if (text.Any())
                    {
                        bind.SetValue(text);
                        legal.ChangeText(bind.IsMet ? "合法值：" + bind.GetValue() : "不合法");
                    }
                    else
                        legal.ChangeText(bind.Important ? "可以为空" : "不可为空");
                };
                valueInputBg.Register(valueInputer);

                UIClose clear = new();
                clear.SetCenter(-10, 0, 1, 0.5f);
                clear.Events.OnLeftDown += evt => valueInputer.ClearText();
                valueInputBg.Register(clear);
                innerY += 48;
            }
            UIText create = new("添加需求");
            create.SetSize(create.TextSize);
            create.SetPos(0, innerY);
            create.Events.OnMouseOver += evt => create.color = Color.Gold;
            create.Events.OnMouseOut += evt => create.color = Color.White;
            create.Events.OnLeftDown += evt =>
            {
                var editingRequires = EditingRequires;
                if (editingRequires == null)
                {
                    Main.NewText("请先选择一个成就栏位/需求层级");
                    return;
                }
                if (data.TryConstruct(out Requirement? require))
                {
                    require!.ShouldSaveStaticData = true;
                    editingRequires.Add(require);
                    CheckRequirements();
                    ChangeSaveState(false);
                }
            };
            constructPanel.Register(create);
            constructPanel.SetSize(0, innerY + 48, 1);
            constructView.Calculation();
        }
        private void RegisterRewardDataPanel(ConstructInfoTable<Reward> data, UIContainerPanel constructView)
        {
            UIVnlPanel constructPanel = new(0, 0);
            constructPanel.Info.SetMargin(10);
            constructView.AddElement(constructPanel);
            int innerY = 0;
            foreach (var info in data)
            {
                UIText name = new(info.DisplayName.Value ?? "Anonymous");
                name.SetPos(0, innerY);
                name.SetSize(name.TextSize);
                constructPanel.Register(name);
                innerY += 28;

                UIText legal = new(info.Important ? "可以为空" : "不可为空");
                legal.SetPos(0, innerY);
                constructPanel.Register(legal);
                innerY += 28;

                UIVnlPanel valueInputBg = new(0, 28);
                valueInputBg.Info.Width.Percent = 1;
                valueInputBg.SetPos(0, innerY);
                constructPanel.Register(valueInputBg);

                UIInputBox valueInputer = new(info.Type.Name);
                valueInputer.SetSize(-40, 0, 1, 1);
                var bind = info;
                valueInputer.OnInputText += text =>
                {
                    if (text.Any())
                    {
                        bind.SetValue(text);
                        legal.ChangeText(bind.IsMet ? "合法值：" + bind.GetValue() : "不合法");
                    }
                    else
                        legal.ChangeText(bind.Important ? "可以为空" : "不可为空");
                };
                valueInputBg.Register(valueInputer);

                UIClose clear = new();
                clear.SetCenter(-10, 0, 1, 0.5f);
                clear.Events.OnLeftDown += evt => valueInputer.ClearText();
                valueInputBg.Register(clear);
                innerY += 48;
            }
            UIText create = new("添加奖励");
            create.SetSize(create.TextSize);
            create.SetPos(0, innerY);
            create.Events.OnMouseOver += evt => create.color = Color.Gold;
            create.Events.OnMouseOut += evt => create.color = Color.White;
            create.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                if (data.TryConstruct(out Reward? reward))
                {
                    reward!.ShouldSaveStaticData = true;
                    EditingRewards.Add(reward);
                    CheckRewards();
                    ChangeSaveState(false);
                }
            };
            constructPanel.Register(create);
            constructPanel.SetSize(0, innerY + 48, 1);
            constructView.Calculation();
        }
        private void CheckRequirements(RequirementList? requires = null, int index = 0)
        {
            if (index == 0)
                requireView.ClearAllElements();
            requires ??= EditingAch!.Requirements;
            if (requires.Count != 0)
            {
                foreach (Requirement require in requires)
                {
                    UIRequireText text = new(require, requires);
                    text.SetPos(index * 30, 0);
                    text.delete.Events.OnLeftDown += evt =>
                    {
                        if (EditingAch == null)
                        {
                            return;
                        }
                        text.requirements.Remove(text.requirement);
                        ChangeSaveState(false);
                        if (EditingCombineRequire == text.requirement)
                        {
                            EditingCombineRequire = null;
                        }
                        CheckRequirements();
                    };
                    requireView.AddElement(text);
                    if (require is CombineRequirement combine)
                    {
                        text.text.HoverToGold();
                        text.text.Events.OnLeftDown += evt =>
                        {
                            EditingCombineRequire = combine;
                            UpdateRqsCountText();
                        };
                        text.text.Events.OnUpdate += evt =>
                        {
                            text.text.overrideColor = EditingCombineRequire == combine ? Color.Red : null;
                        };
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
        private void CheckRewards(RewardList? rewards = null, int index = 0)
        {
            if (index == 0)
                rewardView.ClearAllElements();
            rewards ??= EditingAch!.Rewards;
            if (rewards.Count != 0)
            {
                foreach (Reward reward in rewards)
                {
                    UIRewardText text = new(reward, rewards);
                    text.SetPos(index * 30, 0);
                    text.delete.Events.OnLeftDown += evt =>
                    {
                        if (EditingAch == null)
                        {
                            return;
                        }
                        text.rewards.Remove(text.reward);
                        ChangeSaveState(false);
                        if (EditingCombineReward == text.reward)
                        {
                            EditingCombineReward = null;
                        }
                        CheckRewards();
                    };
                    rewardView.AddElement(text);
                    if (reward is CombineReward combine)
                    {
                        text.text.HoverToGold();
                        var cb = combine;
                        text.text.Events.OnLeftDown += evt =>
                        {
                            EditingCombineReward = cb;
                        };
                        text.text.Events.OnUpdate += evt =>
                        {
                            text.text.overrideColor = EditingCombineReward == cb ? Color.Red : null;
                        };
                        CheckRewards(cb.Rewards, index + 1);
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
        private void UpdateRqsCountText() => rqsCountText?.ChangeText(EditingCombineRequire?.Count.ToString() ??
            EditingAch?.RequirementCountNeeded.ToString() ?? "未选", false);
        private void UpdateRwsCountText() => rwsCountText?.ChangeText(EditingCombineReward?.Count.ToString() ?? "未选", false);
    }
}
