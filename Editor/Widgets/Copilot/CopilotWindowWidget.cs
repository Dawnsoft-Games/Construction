using System;
using System.Threading.Tasks;
using System.Linq;

namespace Editor.Copilot;

[EditorApp("GitHub Copilot", "auto_awesome", "AI-powered code assistance")]
public class CopilotWindow : BaseWindow
{
    private Widget _loginContainer;
    private Widget _mainContainer;
    private PasswordLineEdit _tokenInput;
    private Button _connectButton;
    private Label _statusLabel;
    private TextEdit _chatInput;
    private ScrollArea _chatHistory;
    private Widget _chatContainer;

    private bool _isConnected = false;
    private string _accessToken = "";

    public CopilotWindow()
    {
        WindowTitle = "GitHub Copilot";
        SetWindowIcon("auto_awesome");
        Size = new Vector2(800, 600);
        MinimumSize = new Vector2(600, 400);

        Layout = Layout.Column();
        Layout.Margin = 8;
        Layout.Spacing = 8;

        CreateUI();
        LoadSettings();
        Show();
    }

    [Menu("Editor", "Window/Copilot", "auto_awesome")]
    public static void OpenCopilotWindow()
    {
        _ = new CopilotWindow();
    }

    private void CreateUI()
    {
        // Header
        var header = Layout.Add(new Widget());
        header.Layout = Layout.Row();

        var headerLabel = header.Layout.Add(new Label("GitHub Copilot Integration"));
        headerLabel.SetStyles("font-size: 16px; font-weight: 600;");
        header.Layout.AddStretchCell();

        // Login Container
        _loginContainer = Layout.Add(new Widget());
        _loginContainer.Layout = Layout.Column();
        _loginContainer.Layout.Spacing = 4;

        var loginTitle = _loginContainer.Layout.Add(new Label("Account-Verknüpfung"));
        loginTitle.SetStyles("font-weight: 500;");

        var tokenRow = _loginContainer.Layout.Add(new Widget());
        tokenRow.Layout = Layout.Row();
        tokenRow.Layout.Spacing = 4;

        tokenRow.Layout.Add(new Label("GitHub Token:") { MinimumWidth = 100 });
        _tokenInput = tokenRow.Layout.Add(new PasswordLineEdit());
        _tokenInput.PlaceholderText = "Geben Sie Ihren GitHub Personal Access Token ein";

        var buttonRow = _loginContainer.Layout.Add(new Widget());
        buttonRow.Layout = Layout.Row();
        buttonRow.Layout.AddStretchCell();

        _connectButton = buttonRow.Layout.Add(new Button("Verbinden"));
        _connectButton.Clicked = OnConnectClicked;
        _connectButton.Icon = "link";

        var helpButton = buttonRow.Layout.Add(new Button("Hilfe"));
        helpButton.Clicked = OnHelpClicked;
        helpButton.Icon = "help";

        _statusLabel = _loginContainer.Layout.Add(new Label("Nicht verbunden"));
        _statusLabel.SetStyles("color: #ff6b6b;");

        // Main Container (initially hidden)
        _mainContainer = Layout.Add(new Widget());
        _mainContainer.Layout = Layout.Column();
        _mainContainer.Layout.Spacing = 4;
        _mainContainer.Visible = false;

        CreateChatInterface();
        CreateCodeAssistance();
    }

    private void CreateChatInterface()
    {
        var chatTitle = _mainContainer.Layout.Add(new Label("Copilot Chat"));
        chatTitle.SetStyles("font-weight: 500;");

        // Chat history
        _chatHistory = _mainContainer.Layout.Add(new ScrollArea(this));
        _chatHistory.MinimumHeight = 200;
        _chatHistory.Canvas = new Widget();
        _chatHistory.Canvas.Layout = Layout.Column();

        _chatContainer = _chatHistory.Canvas;

        // Chat input
        var inputRow = _mainContainer.Layout.Add(new Widget());
        inputRow.Layout = Layout.Row();
        inputRow.Layout.Spacing = 4;

        _chatInput = inputRow.Layout.Add(new TextEdit());
        _chatInput.PlaceholderText = "Fragen Sie Copilot nach Code-Hilfe...";
        _chatInput.MinimumHeight = 60;

        var sendButton = inputRow.Layout.Add(new Button("Senden"));
        sendButton.Clicked = OnSendMessage;
        sendButton.Icon = "send";
        sendButton.MinimumWidth = 80;
    }

    private void CreateCodeAssistance()
    {
        _mainContainer.Layout.Add(new Separator(4.0f));

        var assistTitle = _mainContainer.Layout.Add(new Label("Code-Assistenz"));
        assistTitle.SetStyles("font-weight: 500;");

        var buttonGrid = _mainContainer.Layout.Add(new Widget());
        buttonGrid.Layout = Layout.Row();
        buttonGrid.Layout.Spacing = 4;

        var explainButton = buttonGrid.Layout.Add(new Button("Code Erklären"));
        explainButton.Clicked = OnExplainCode;
        explainButton.Icon = "psychology";

        var optimizeButton = buttonGrid.Layout.Add(new Button("Optimieren"));
        optimizeButton.Clicked = OnOptimizeCode;
        optimizeButton.Icon = "tune";

        var testsButton = buttonGrid.Layout.Add(new Button("Tests Generieren"));
        testsButton.Clicked = OnGenerateTests;
        testsButton.Icon = "quiz";
    }

    // Rest der Methoden bleiben gleich...
    private async void OnConnectClicked()
    {
        var token = _tokenInput.Value.Trim();
        if (string.IsNullOrEmpty(token))
        {
            SetStatus("Bitte geben Sie einen gültigen Token ein", false);
            return;
        }

        _connectButton.Enabled = false;
        _connectButton.Text = "Verbinde...";
        SetStatus("Verbindung wird hergestellt...", null);

        try
        {
            var success = await ValidateTokenAsync(token);
            if (success)
            {
                _accessToken = token;
                _isConnected = true;
                SetStatus("Erfolgreich verbunden", true);
                SaveSettings();
                ShowMainInterface();
            }
            else
            {
                SetStatus("Verbindung fehlgeschlagen. Überprüfen Sie Ihren Token.", false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", false);
        }
        finally
        {
            _connectButton.Enabled = true;
            _connectButton.Text = "Verbinden";
        }
    }

    private void OnHelpClicked()
    {
        var helpWindow = new CopilotHelpWindow();
        helpWindow.Show();
    }

    private async void OnSendMessage()
    {
        var message = _chatInput.PlainText.Trim();
        if (string.IsNullOrEmpty(message) || !_isConnected)
            return;

        AddChatMessage("Sie", message, true);
        _chatInput.PlainText = "";

        try
        {
            var response = await SendToCopilotAsync(message);
            AddChatMessage("Copilot", response, false);
        }
        catch (Exception ex)
        {
            AddChatMessage("System", $"Fehler: {ex.Message}", false);
        }
    }

    private void OnExplainCode()
    {
        var selectedCode = GetSelectedCodeFromEditor();
        if (!string.IsNullOrEmpty(selectedCode))
        {
            _chatInput.PlainText = $"Erkläre mir diesen Code:\n\n```csharp\n{selectedCode}\n```";
            OnSendMessage();
        }
        else
        {
            AddChatMessage("System", "Kein Code ausgewählt. Bitte wählen Sie Code im Editor aus.", false);
        }
    }

    private void OnOptimizeCode()
    {
        var selectedCode = GetSelectedCodeFromEditor();
        if (!string.IsNullOrEmpty(selectedCode))
        {
            _chatInput.PlainText = $"Optimiere diesen Code für bessere Performance und Lesbarkeit:\n\n```csharp\n{selectedCode}\n```";
            OnSendMessage();
        }
        else
        {
            AddChatMessage("System", "Kein Code ausgewählt. Bitte wählen Sie Code im Editor aus.", false);
        }
    }

    private void OnGenerateTests()
    {
        var selectedCode = GetSelectedCodeFromEditor();
        if (!string.IsNullOrEmpty(selectedCode))
        {
            _chatInput.PlainText = $"Generiere Unit Tests für diesen Code:\n\n```csharp\n{selectedCode}\n```";
            OnSendMessage();
        }
        else
        {
            AddChatMessage("System", "Kein Code ausgewählt. Bitte wählen Sie Code im Editor aus.", false);
        }
    }

    private void AddChatMessage(string sender, string message, bool isUser)
    {
        var messageWidget = new Widget();
        messageWidget.Layout = Layout.Column();
        messageWidget.Layout.Margin = 4;

        var bgColor = isUser ? "#2d3748" : "#1a202c";
        messageWidget.SetStyles($"background-color: {bgColor}; border-radius: 8px; padding: 8px;");

        var header = messageWidget.Layout.Add(new Label($"{sender}:"));
        header.SetStyles("font-weight: 600; font-size: 12px;");

        var content = messageWidget.Layout.Add(new Label(message));
        content.WordWrap = true;
        content.SetStyles("font-size: 11px;");

        _chatContainer.Layout.Add(messageWidget);
        _chatHistory.Canvas.Update();
    }

    private void SetStatus(string message, bool? success)
    {
        _statusLabel.Text = message;

        var color = success switch
        {
            true => "#48bb78",    // grün
            false => "#f56565",   // rot
            null => "#ed8936"     // orange
        };

        _statusLabel.SetStyles($"color: {color};");
    }

    private void ShowMainInterface()
    {
        _loginContainer.Visible = false;
        _mainContainer.Visible = true;
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        await Task.Delay(1000);
        return !string.IsNullOrEmpty(token) && token.Length > 10;
    }

    private async Task<string> SendToCopilotAsync(string message)
    {
        await Task.Delay(1500);
        return $"Copilot Antwort auf: {message}\n\nDies ist eine Beispielantwort. In der echten Implementierung würde hier die GitHub Copilot API aufgerufen werden.";
    }

    private string GetSelectedCodeFromEditor()
    {
        return "// Beispiel Code\npublic class Example\n{\n    public void Method() { }\n}";
    }

    private void SaveSettings()
    {
        EditorCookie.Set("copilot.connected", _isConnected);
        if (_isConnected)
        {
            EditorCookie.Set("copilot.token", _accessToken);
        }
    }

    private void LoadSettings()
    {
        _isConnected = EditorCookie.Get("copilot.connected", false);
        if (_isConnected)
        {
            _accessToken = EditorCookie.Get("copilot.token", "");
            if (!string.IsNullOrEmpty(_accessToken))
            {
                SetStatus("Gespeicherte Verbindung gefunden", true);
                ShowMainInterface();
            }
        }
    }
}

// ...existing code...
/// <summary>
/// Custom password input widget that masks characters
/// </summary>
public class PasswordLineEdit : LineEdit
{
    private string _actualText = "";
    private char _maskChar = '●';
    private bool _updating = false;

    public new string Value
    {
        get => _actualText;
        set
        {
            _actualText = value ?? "";
            UpdateMaskedDisplay();
        }
    }

    public PasswordLineEdit(Widget parent = null) : base(parent)
    {
        TextChanged += HandleTextChanged;
    }

    private void HandleTextChanged(string newText)
    {
        if (_updating || newText.All(c => c == _maskChar))
            return;

        _actualText = newText;
        UpdateMaskedDisplay();
    }

    private void UpdateMaskedDisplay()
    {
        if (_updating) return;

        _updating = true;
        var masked = new string(_maskChar, _actualText.Length);
        base.Value = masked;
        _updating = false;
    }
}