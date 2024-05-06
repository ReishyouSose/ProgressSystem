using ProgressSystem.UI.DeveloperMode.ExtraUI;

namespace ProgressSystem.UI.DeveloperMode.AchEditor
{
    public partial class AchEditor
    {
        private void RegisterNewPagePanel()
        {
            pagePanel = new(300, 200, opacity: 1);
            pagePanel.SetCenter(300, 0, 0.5f, 0.5f);
            pagePanel.Info.IsVisible = false;
            pagePanel.Info.SetMargin(10);
            Register(pagePanel);

            UIVnlPanel inputBg = new(200, 30);
            inputBg.SetCenter(0, 0, 0.5f, 0.25f);
            pagePanel.Register(inputBg);

            UIText report = new("不可为空", Color.Red);
            report.SetSize(report.TextSize);
            report.SetCenter(0, 0, 0.5f, 0.5f);
            pagePanel.Register(report);

            pageInputer = new("输入进度组名称");
            pageInputer.SetSize(-40, 0, 1, 1);
            pageInputer.OnInputText += text => MatchPageName(text, report);
            inputBg.Register(pageInputer);

            UIClose clearText = new();
            clearText.Events.OnLeftDown += evt => pageInputer.ClearText();
            clearText.SetCenter(-20, 0, 1, 0.5f);
            inputBg.Register(clearText);

            UIText submit = new("创建");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 0, 0.3f, 0.75f);
            submit.Events.OnMouseOver += evt => submit.color = Color.Gold;
            submit.Events.OnMouseOut += evt => submit.color = Color.White;
            submit.Events.OnLeftDown += evt =>
            {
                if (report.color == Color.Green)
                {
                    string name = pageInputer.Text;
                    mainPanel.LockInteract(true);
                    pagePanel.Info.IsVisible = false;
                    UIPageName pageName = new(name);
                    pageName.SetSize(pageName.TextSize);
                    pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                    pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                    pageList.AddElement(pageName);
                    pageName.Events.OnLeftDown += evt => LoadPage();
                    EditingPage = AchievementPage.Create(editingMod, name);
                    EditingPage.ShouldSaveStaticData = true;
                    ClearTemp();
                    SaveProgress();
                }
            };
            pagePanel.Register(submit);

            UIText cancel = new("取消");
            cancel.SetSize(cancel.TextSize);
            cancel.SetCenter(0, 0, 0.7f, 0.75f);
            cancel.Events.OnMouseOver += evt => cancel.color = Color.Gold;
            cancel.Events.OnMouseOut += evt => cancel.color = Color.White;
            cancel.Events.OnLeftDown += evt =>
            {
                mainPanel.LockInteract(true);
                pagePanel.Info.IsVisible = false;
            };
            pagePanel.Register(cancel);
        }
    }
}
