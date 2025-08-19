using System;

namespace Editor.Copilot;

/// <summary>
/// Help window for Copilot setup
/// </summary>
internal class CopilotHelpWindow : BaseWindow
{
    public CopilotHelpWindow()
    {
        SetModal(true, true);
        Size = new Vector2(600, 500);
        MinimumSize = Size;

        WindowTitle = "Copilot Setup Hilfe";
        SetWindowIcon("help");

        Layout = Layout.Column();
        Layout.Margin = 16;
        Layout.Spacing = 8;

        CreateContent();
    }

    private void CreateContent()
    {
        var title = Layout.Add(new Label("GitHub Copilot Setup"));
        title.SetStyles("font-size: 18px; font-weight: 700;");

        Layout.Add(new Separator(2.0f));

        var scrollArea = Layout.Add(new ScrollArea(this));
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Column();
        scrollArea.Canvas.Layout.Spacing = 12;
        var content = scrollArea.Canvas;

        // Schritt 1
        AddStep(content, "1", "GitHub Account vorbereiten",
            "• Stellen Sie sicher, dass Sie ein GitHub Account haben\n" +
            "• Aktivieren Sie GitHub Copilot in Ihrem Account\n" +
            "• Gehen Sie zu GitHub.com → Settings → Developer settings");

        // Schritt 2  
        AddStep(content, "2", "Personal Access Token erstellen",
            "• Klicken Sie auf 'Personal access tokens' → 'Tokens (classic)'\n" +
            "• Klicken Sie 'Generate new token' → 'Generate new token (classic)'\n" +
            "• Geben Sie eine Beschreibung ein (z.B. 's&box Copilot')\n" +
            "• Wählen Sie die benötigten Scopes aus:\n" +
            "  - repo (für Code-Zugriff)\n" +
            "  - copilot (für Copilot-Funktionen)");

        // Schritt 3
        AddStep(content, "3", "Token in s&box verwenden",
            "• Kopieren Sie den generierten Token\n" +
            "• Fügen Sie ihn in das Token-Feld ein\n" +
            "• Klicken Sie 'Verbinden'\n" +
            "• Der Token wird sicher gespeichert");

        content.Layout.Add(new Separator(2.0f));

        var warningWidget = content.Layout.Add(new Widget());
        warningWidget.Layout = Layout.Row();
        warningWidget.Layout.Spacing = 8;

        var warningIcon = warningWidget.Layout.Add(new Label("⚠️"));
        warningIcon.SetStyles("font-size: 16px;");

        var warningText = warningWidget.Layout.Add(new Label(
            "Sicherheitshinweis: Teilen Sie Ihren Token niemals mit anderen. " +
            "Der Token wird lokal verschlüsselt gespeichert."));
        warningText.WordWrap = true;
        warningText.SetStyles("font-size: 12px; color: #ed8936;");

        // Buttons
        var buttonRow = Layout.Add(new Widget());
        buttonRow.Layout = Layout.Row();
        buttonRow.Layout.AddStretchCell();

        var githubButton = buttonRow.Layout.Add(new Button("GitHub öffnen"));
        githubButton.Icon = "open_in_new";
        githubButton.Clicked = () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/settings/tokens",
            UseShellExecute = true
        });

        var closeButton = buttonRow.Layout.Add(new Button("Schließen"));
        closeButton.Clicked = Close;
    }

    private void AddStep(Widget parent, string number, string title, string description)
    {
        var stepWidget = parent.Layout.Add(new Widget());
        stepWidget.Layout = Layout.Column();
        stepWidget.Layout.Spacing = 4;
        stepWidget.SetStyles("background-color: #2d3748; border-radius: 8px; padding: 12px;");

        var headerWidget = stepWidget.Layout.Add(new Widget());
        headerWidget.Layout = Layout.Row();
        headerWidget.Layout.Spacing = 8;

        var numberLabel = headerWidget.Layout.Add(new Label(number));
        numberLabel.SetStyles("font-size: 14px; font-weight: 700; color: #4fd1c7; min-width: 24px;");

        var titleLabel = headerWidget.Layout.Add(new Label(title));
        titleLabel.SetStyles("font-size: 14px; font-weight: 600;");

        var descLabel = stepWidget.Layout.Add(new Label(description));
        descLabel.WordWrap = true;
        descLabel.SetStyles("font-size: 12px; margin-left: 32px;");
    }
}