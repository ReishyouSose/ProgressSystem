using Microsoft.Xna.Framework.Graphics;
using ProgressSystem.UI.DeveloperMode.ExtraUI;
using RUIModule;
using System.Diagnostics;
using System.Text;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace ProgressSystem.UI.DeveloperMode.AchEditor
{
    public partial class AchEditor
    {
        private UIVnlPanel mainPanel = null!;
        private UIDropDownList<UIPageName> pageList = null!;
        private UIContainerPanel modView;
        private void RegisterEditPagePanel()
        {
            #region 主板
            mainPanel = new(1000, 800);
            mainPanel.SetCenter(0, 0, 0.55f, 0.5f);
            mainPanel.Info.SetMargin(10);
            mainPanel.canDrag = true;
            Register(mainPanel);
            #endregion

            #region ItemSlot
            UIItemSlot itemSlot = new();
            itemSlot.Events.OnLeftDown += evt =>
            {
                if (Main.mouseItem.type > ItemID.None)
                {
                    itemSlot.item = Main.mouseItem.Clone();
                }
                else
                {
                    itemSlot.item.SetDefaults();
                }
            };
            itemSlot.ReDraw = sb =>
            {
                itemSlot.DrawSelf(sb);
                Item item = itemSlot.item;
                StringBuilder text = new();
                text.Append("物品ID：" + item.type);
                text.AppendLine();
                text.Append("物块ID：" + item.createTile);
                text.AppendLine();
                text.Append("BuffID：" + item.buffType);
                ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text.ToString(),
                    itemSlot.HitBox().BottomLeft() + Vector2.UnitY * 5, Color.White, 0, Vector2.Zero, Vector2.One);
            };
            mainPanel.Register(itemSlot);
            #endregion

            #region Mod 列表
            UIVnlPanel groupFilter = new(0, 0);
            groupFilter.SetPos(0, 150);
            groupFilter.SetSize(120, -150, 0, 1);
            groupFilter.Info.SetMargin(10);
            mainPanel.Register(groupFilter);

            modView = new();
            modView.SetSize(0, 0, 1, 1);
            modView.autoPos[0] = 10;
            groupFilter.Register(modView);

            VerticalScrollbar gv = new(90, false, false);
            modView.SetVerticalScrollbar(gv);
            gv.Info.Left.Pixel += 10;
            groupFilter.Register(gv);
            #endregion

            #region 事件版
            int left = 130;
            UIVnlPanel eventPanel = new(0, 0);
            eventPanel.Info.SetMargin(10);
            eventPanel.SetPos(left, 40);
            eventPanel.SetSize(-left, -40, 1, 1, false);
            mainPanel.Register(eventPanel);
            #endregion

            #region 进度表列表
            pageList = new(mainPanel, eventPanel, x => new(x.text, x.key));

            pageList.showArea.SetPos(left, 0);
            pageList.showArea.SetSize(200, 30);
            pageList.showArea.SetMargin(10, 5);

            pageList.expandArea.SetPos(left, 30);
            pageList.expandArea.SetSize(200, 100);

            pageList.expandView.autoPos[0] = 5;
            left += pageList.showArea.Width + 10;
            #endregion

            #region 注册所有的 Mod
            CheckMods();
            #endregion

            #region 新建进度表
            UIText newProgress = new("新建进度表");
            newProgress.SetPos(left, 5);
            newProgress.SetSize(newProgress.TextSize);
            newProgress.Events.OnMouseOver += evt => newProgress.color = Color.Gold;
            newProgress.Events.OnMouseOut += evt => newProgress.color = Color.White;
            newProgress.Events.OnLeftDown += evt =>
            {
                pageInputer.ClearText();
                pagePanel.Info.IsVisible = true;
                mainPanel.LockInteract(false);
            };
            mainPanel.Register(newProgress);
            left += newProgress.Width + 10;
            #endregion

            #region 删除进度表
            UIText deleteProgress = new("删除进度表");
            deleteProgress.SetPos(left, 5);
            deleteProgress.SetSize(deleteProgress.TextSize);
            deleteProgress.Events.OnMouseOver += evt => deleteProgress.color = Color.Gold;
            deleteProgress.Events.OnMouseOut += evt => deleteProgress.color = Color.White;
            deleteProgress.Events.OnLeftDown += evt =>
            {
                if (EditingPage == null)
                {
                    return;
                }
                if (AchievementManager.RemovePage(EditingPage))
                {
                    CheckPages();
                }
            };
            mainPanel.Register(deleteProgress);
            left += deleteProgress.Width + 10;
            #endregion

            #region 位置检查
            UIText checkPos = new("位置检查");
            checkPos.SetPos(left, 5);
            checkPos.SetSize(checkPos.TextSize);
            checkPos.HoverToGold();
            checkPos.Events.OnLeftDown += evt =>
            {
                HashSet<Vector2> pos = [];
                foreach (UIAchSlot slot in slotByFullName.Values)
                {
                    if (!pos.Add(slot.pos))
                    {
                        Vector2 p = Vector2.Zero;
                        while (pos.Contains(p))
                        {
                            if (p.Y + 1 > p.X)
                            {
                                p.X++;
                            }
                            else
                                p.Y++;
                        }
                        slot.pos = p;
                        slot.SetPos(p * 80);
                    }
                }
                AchPos = pos;
            };
            mainPanel.Register(checkPos);
            left += checkPos.Width + 10;
            #endregion

            #region 保存提示
            saveTip = new("已保存", Color.Green);
            saveTip.SetSize(saveTip.TextSize);
            saveTip.SetPos(left, 5);
            saveTip.Events.OnLeftDown += evt =>
            {
                SaveProgress();
                ChangeSaveState(true);
            };
            mainPanel.Register(saveTip);
            left += saveTip.Width + 10;
            #endregion

            #region 保存路径
            UIVnlPanel savePathInputBg = new(0, 0);
            savePathInputBg.SetPos(left, 0);
            savePathInputBg.SetSize(-left, 30, 1);
            savePathInputBg.Info.RightMargin.Pixel = 10;
            mainPanel.Register(savePathInputBg);

            savePathInputer = new("输入保存路径");
            savePathInputer.SetSize(-40, 0, 1, 1);
            savePathInputBg.Register(savePathInputer);

            #region 打开资源管理器按钮
            UI3FrameImage selectSavePath = new(AssetLoader.VnlAdjust, x => false)
            {
                hoverText = "打开资源管理器选择复制路径"
            };
            selectSavePath.SetCenter(-40, 0, 1, 0.5f);
            selectSavePath.Events.OnLeftDown += evt =>
            {
                Main.QueueMainThreadAction(() =>
                {
                    var p = Process.Start("explorer.exe", "/select");
                });
            };
            savePathInputBg.Register(selectSavePath);
            #endregion

            #region 清除路径按钮
            UIClose clearSavePath = new();
            clearSavePath.SetCenter(-10, 0, 1, 0.5f);
            clearSavePath.Events.OnLeftDown += evt => savePathInputer.ClearText();
            savePathInputBg.Register(clearSavePath);
            #endregion

            #endregion

            #region 成就界面
            achView = new();
            achView.SetSize(-20, -20, 1, 1);
            achView.Events.OnLeftDown += evt =>
            {
                if (!LeftAlt && preSetting != null)
                {
                    preSetting.preSetting = false;
                    preSetting = null;
                }
            };
            achView.Events.OnLeftDoubleClick += evt =>
            {
                if (EditingPage == null)
                {
                    return;
                }
                Point mouse = (Main.MouseScreen - achView.ChildrenElements[0].HitBox(false).TopLeft()).ToPoint();
                Vector2 pos = new(mouse.X / 80, mouse.Y / 80);
                if (EditingAchSlot?.pos == pos)
                    return;
                string name = BaseName;
                int i = 1;
                while (EditingPage.Achievements.ContainsKey(editingMod.Name + "." + name + i))
                    i++;
                Achievement ach = new(EditingPage, editingMod, name + i);
                EditingPage.AddF(ach);
                ach.ShouldSaveStaticData = true;
                RegisterAchSlot(ach, pos);
            };
            achView.Events.OnRightDown += evt =>
            {
                collision = new();
                achView.AddElement(collision);
                if (!LeftCtrl)
                {
                    foreach (UIAchSlot ge in frameSelect)
                    {
                        ge.selected = false;
                    }
                    frameSelect.Clear();
                }
            };
            achView.Events.OnRightUp += evt =>
            {
                achView.RemoveElement(collision);
                foreach (UIAchSlot ge in tempSelect)
                {
                    frameSelect.Add(ge);
                }
                collision = null;
                tempSelect.Clear();
                interacted.Clear();
                if (!LeftAlt || preSetting == null || frameSelect.Count == 0)
                {
                    return;
                }
                foreach (UIAchSlot ge in frameSelect)
                {
                    if (preSetting == ge)
                    {
                        ge.selected = false;
                        continue;
                    }
                    Achievement orig = preSetting.ach;
                    Achievement pre = ge.ach;
                    if (pre.Predecessors.Contains(orig))
                    {
                        Main.NewText("不可互为前置");
                        continue;
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
                    ge.selected = false;
                }
                ChangeSaveState(false);
                frameSelect.Clear();
            };
            eventPanel.Register(achView);

            VerticalScrollbar ev = new(80);
            ev.Info.Left.Pixel += 10;
            achView.SetVerticalScrollbar(ev);
            eventPanel.Register(ev);

            HorizontalScrollbar eh = new(80) { useScrollWheel = false };
            eh.Info.Top.Pixel += 10;
            achView.SetHorizontalScrollbar(eh);
            eventPanel.Register(eh);

            Texture2D line = TextureAssets.MagicPixel.Value;
            Color color = Color.White;
            for (int i = 0; i < 50; i++)
            {
                UIImage hline = new(line, 5000, 2, color: color);
                hline.SetPos(0, i * 80);
                achView.AddElement(hline);

                UIImage vline = new(line, 2, 5000, color: color);
                vline.SetPos(80 * i, 0);
                achView.AddElement(vline);
            }
            #endregion
        }
        private void CheckMods()
        {
            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.Side != ModSide.Both || !mod.HasAsset("icon"))
                    continue;
                string modName = mod.Name;
                UIModSlot modSlot = new(RUIHelper.T2D(modName + "/icon"), modName) { hoverText = mod.DisplayName };
                modView.AddElement(modSlot);
                modSlot.ReDraw = sb =>
                {
                    modSlot.DrawSelf(sb);
                    if (editingMod.Name == modSlot.modName)
                    {
                        RUIHelper.DrawRec(sb, modSlot.HitBox(), 5f, Color.Red);
                    }
                };
                modSlot.Events.OnLeftDown += evt => editingMod = ModLoader.GetMod(modSlot.modName);
                modSlot.Events.OnLeftDown += evt => CheckPages();
            }
            if (modView.InnerUIE.Count > 0)
            {
                modView.InnerUIE[0].Events.LeftDown(modView.InnerUIE[0]);
            }
        }
        private void CheckPages()
        {
            pageList.ClearAllElements();
            ClearTemp();
            if (AchievementManager.PagesByMod.TryGetValue(editingMod, out var modPages))
            {
                foreach (var (name, page) in modPages)
                {
                    UIPageName pageName = new(page.DisplayName.Value ?? name, name);
                    pageName.SetSize(pageName.TextSize);
                    pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                    pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                    pageList.AddElement(pageName);
                    pageName.Events.OnLeftDown += evt => LoadPage();
                }
                pageList.ChangeShowElement(0);
                pageList.FirstUIE?.Events.LeftDown(null);
                pageList.Calculation();
            }
        }
        private void LoadPage()
        {
            EditingPageName = pageList.ShowUIE.key;
            if (EditingPage == null)
            {
                return;
            }
            ClearTemp();
            EditingPage.SetDefaultPositionForAchievements();
            foreach (Achievement ach in EditingPage.Achievements.Values)
            {
                RegisterAchSlot(ach, ach.Position!.Value, false);
            }
            foreach (UIAchSlot slot in slotByFullName.Values)
            {
                Achievement orig = slot.ach;
                foreach (Achievement pre in orig.Predecessors)
                {
                    RegisterRequireLine(orig, pre, false);
                }
            }
            ChangeEditingAch(null);
        }
        private void RegisterRequireLine(Achievement orig, Achievement pre, bool addPreToAch = true)
        {
            UIAchSlot start = slotByFullName[pre.FullName];
            UIAchSlot end = slotByFullName[orig.FullName];
            UIRequireLine line = new(start, end);
            achView.AddElement(line, 100);
            end.preLine.Add(line);
            if (addPreToAch)
            {
                orig.AddPredecessor(pre.FullName, true);
            }
        }
        private void RemoveRequireLine(Achievement orig, Achievement pre)
        {
            UIAchSlot me = slotByFullName[orig.FullName];
            UIRequireLine line = me.preLine.First(x => x.start.ach == pre);
            achView.RemoveElement(line);
            me.preLine.Remove(line);
            orig.RemovePredecessor(pre.FullName, true);
        }
    }
}
