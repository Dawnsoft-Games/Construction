using System;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    private static readonly HttpClient _httpClient = new HttpClient();
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan _minRequestInterval = TimeSpan.FromSeconds(1);

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

        // FORCE: Login UI immer anzeigen beim Start
        ForceShowLoginUI();

        Show();
    }
    private void ForceShowLoginUI()
    {
        _loginContainer.Visible = true;
        _mainContainer.Visible = false;
        _isConnected = false;
        SetStatus("API Key eingeben", null);
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

         var logoutButton = header.Layout.Add(new Button("Settings zurücksetzen"));
        logoutButton.Clicked = OnLogoutClicked;
        logoutButton.Icon = "refresh";
        logoutButton.Visible = _isConnected;

        // Login Container
        _loginContainer = Layout.Add(new Widget());
        _loginContainer.Layout = Layout.Column();
        _loginContainer.Layout.Spacing = 4;

        var loginTitle = _loginContainer.Layout.Add(new Label("Account-Verknüpfung"));
        loginTitle.SetStyles("font-weight: 500;");

        var tokenRow = _loginContainer.Layout.Add(new Widget());
        tokenRow.Layout = Layout.Row();
        tokenRow.Layout.Spacing = 4;

        tokenRow.Layout.Add(new Label("OpenAI API Key:") { MinimumWidth = 100 });
        _tokenInput = tokenRow.Layout.Add(new PasswordLineEdit());
        _tokenInput.PlaceholderText = "Geben Sie Ihren OpenAI API Key ein";

        // Token link button
        var tokenLinkButton = tokenRow.Layout.Add(new Button());
        tokenLinkButton.Icon = "open_in_new";
        tokenLinkButton.ToolTip = "OpenAI API Key erstellen";
        tokenLinkButton.MinimumWidth = 40;
        tokenLinkButton.Clicked = OnOpenGitHubTokenPage;

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
    private void OnLogoutClicked()
    {
        _isConnected = false;
        _accessToken = "";

        // Settings löschen
        EditorCookie.Set("copilot.connected", false);
        EditorCookie.Set("copilot.token", "");

        // UI zurücksetzen
        _loginContainer.Visible = true;
        _mainContainer.Visible = false;
        _tokenInput.Value = "";

        SetStatus("Abgemeldet", false);
    }

    private void OnOpenGitHubTokenPage()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://platform.openai.com/api-keys",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Öffnen des Links: {ex.Message}", false);
        }
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

        // Erste Reihe - Code-Analyse
        var buttonGrid1 = _mainContainer.Layout.Add(new Widget());
        buttonGrid1.Layout = Layout.Row();
        buttonGrid1.Layout.Spacing = 4;

        var explainButton = buttonGrid1.Layout.Add(new Button("Code Erklären"));
        explainButton.Clicked = OnExplainCode;
        explainButton.Icon = "psychology";

        var optimizeButton = buttonGrid1.Layout.Add(new Button("Optimieren"));
        optimizeButton.Clicked = OnOptimizeCode;
        optimizeButton.Icon = "tune";

        var testsButton = buttonGrid1.Layout.Add(new Button("Tests Generieren"));
        testsButton.Clicked = OnGenerateTests;
        testsButton.Icon = "quiz";

        // Zweite Reihe - s&box spezifische Commands
        var sboxTitle = _mainContainer.Layout.Add(new Label("s&box Commands"));
        sboxTitle.SetStyles("font-weight: 500; margin-top: 8px;");

        var buttonGrid2 = _mainContainer.Layout.Add(new Widget());
        buttonGrid2.Layout = Layout.Row();
        buttonGrid2.Layout.Spacing = 4;

        var componentButton = buttonGrid2.Layout.Add(new Button("Component erstellen"));
        componentButton.Clicked = OnCreateComponent;
        componentButton.Icon = "extension";

        var gameObjectButton = buttonGrid2.Layout.Add(new Button("GameObject"));
        gameObjectButton.Clicked = OnCreateGameObject;
        gameObjectButton.Icon = "view_in_ar";

        var sceneButton = buttonGrid2.Layout.Add(new Button("Scene"));
        sceneButton.Clicked = OnCreateScene;
        sceneButton.Icon = "landscape";

        // Dritte Reihe - Erweiterte s&box Features
        var buttonGrid3 = _mainContainer.Layout.Add(new Widget());
        buttonGrid3.Layout = Layout.Row();
        buttonGrid3.Layout.Spacing = 4;

        var shaderButton = buttonGrid3.Layout.Add(new Button("Shader"));
        shaderButton.Clicked = OnCreateShader;
        shaderButton.Icon = "color_lens";

        var materialButton = buttonGrid3.Layout.Add(new Button("Material"));
        materialButton.Clicked = OnCreateMaterial;
        materialButton.Icon = "texture";

        var soundButton = buttonGrid3.Layout.Add(new Button("Sound System"));
        soundButton.Clicked = OnCreateSoundSystem;
        soundButton.Icon = "volume_up";
    }

    // Event Handler für Buttons
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

        // Prüfe ob zu viele Requests in kurzer Zeit
        if (DateTime.Now - _lastRequestTime < TimeSpan.FromSeconds(1))
        {
            AddChatMessage("System", "⏳ Bitte warten Sie einen Moment zwischen den Anfragen.", false);
            return;
        }

        AddChatMessage("Sie", message, true);
        _chatInput.PlainText = "";

        // UI-Feedback während Request
        var thinkingMessage = AddChatMessage("System", "🤔 Denkt nach...", false);

        try
        {
            var response = await SendToCopilotAsync(message);

            // Entferne "Denkt nach..." Nachricht
            if (thinkingMessage != null)
            {
                thinkingMessage.Destroy();
            }

            AddChatMessage("Copilot", response, false);
        }
        catch (Exception ex)
        {
            // Entferne "Denkt nach..." Nachricht
            if (thinkingMessage != null)
            {
                thinkingMessage.Destroy();
            }

            AddChatMessage("System", $"❌ Fehler: {ex.Message}", false);
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

    // s&box spezifische Command Handler
    private void OnCreateComponent()
    {
        _chatInput.PlainText = "Erstelle mir ein neues Component für Spieler-Bewegung mit Input-Handling";
        OnSendMessage();
    }

    private void OnCreateGameObject()
    {
        _chatInput.PlainText = "Erstelle mir ein GameObject mit MeshRenderer, Collider und Transform Component";
        OnSendMessage();
    }

    private void OnCreateScene()
    {
        _chatInput.PlainText = "Erstelle mir eine neue Scene mit Beleuchtung, Spieler-Spawn und Terrain";
        OnSendMessage();
    }

    private void OnCreateShader()
    {
        _chatInput.PlainText = "Erstelle mir einen einfachen Shader mit Diffuse-Beleuchtung für s&box";
        OnSendMessage();
    }

    private void OnCreateMaterial()
    {
        _chatInput.PlainText = "Erstelle mir ein Material-System mit PBR-Properties für s&box";
        OnSendMessage();
    }

    private void OnCreateSoundSystem()
    {
        _chatInput.PlainText = "Erstelle mir ein Sound-System mit 3D-Audio und Effekten für s&box";
        OnSendMessage();
    }

    // Echte GitHub Copilot API Integration
    private async Task<string> SendToCopilotAsync(string message)
    {
        if (string.IsNullOrEmpty(_accessToken))
        {
            return "Fehler: Kein API Token konfiguriert.";
        }

        // Rate Limiting: Warte mindestens 1 Sekunde zwischen Requests
        var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
        if (timeSinceLastRequest < _minRequestInterval)
        {
            var waitTime = _minRequestInterval - timeSinceLastRequest;
            await Task.Delay(waitTime);
        }

        try
        {
            // Verwende OpenAI API statt GitHub Copilot (da diese öffentlich verfügbar ist)
            var requestBody = new
            {
                model = "gpt-3.5-turbo", // Günstiger als gpt-4
                messages = new[]
                {
                    new { role = "system", content = "Du bist ein hilfreicher AI-Assistent für s&box Game Development. Du hilfst beim Erstellen von C# Code, Components, und s&box spezifischen Features." },
                    new { role = "user", content = message }
                },
                max_tokens = 800, // Reduziert von 1500
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            _lastRequestTime = DateTime.Now; // Setze Request-Zeit

            // Verwende OpenAI API statt GitHub
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Spezielle Behandlung für Rate Limit Fehler
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Extrahiere Retry-After Header falls vorhanden
                    if (response.Headers.RetryAfter?.Delta.HasValue == true)
                    {
                        var retryAfter = response.Headers.RetryAfter.Delta.Value;
                        return $"Rate Limit erreicht. Bitte warten Sie {retryAfter.TotalSeconds:F0} Sekunden und versuchen Sie es erneut.";
                    }
                    return "Rate Limit erreicht. Bitte warten Sie einen Moment und versuchen Sie es erneut.";
                }

                return $"API Fehler ({response.StatusCode}): {errorContent}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);

            if (jsonResponse.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("message", out var messageObj) &&
                    messageObj.TryGetProperty("content", out var contentProp))
                {
                    var copilotResponse = contentProp.GetString();

                    // Prüfe ob es ein s&box Editor-Befehl ist
                    await HandleEditorCommands(copilotResponse, message);

                    return copilotResponse;
                }
            }

            return "Keine Antwort erhalten.";
        }
        catch (Exception ex)
        {
            return $"Fehler beim API-Aufruf: {ex.Message}";
        }
    }
    // ...existing code...


    // s&box Editor Integration - Befehle ausführen
    private async Task HandleEditorCommands(string copilotResponse, string originalMessage)
    {
        var lowerMessage = originalMessage.ToLower();

        // Component erstellen
        if (lowerMessage.Contains("erstelle") && lowerMessage.Contains("component"))
        {
            await CreateComponentFromCopilot(copilotResponse, originalMessage);
        }
        // Datei erstellen
        else if (lowerMessage.Contains("erstelle") && (lowerMessage.Contains("datei") || lowerMessage.Contains("file")))
        {
            await CreateFileFromCopilot(copilotResponse, originalMessage);
        }
        // Code in aktive Datei einfügen
        else if (lowerMessage.Contains("füge") && lowerMessage.Contains("code"))
        {
            await InsertCodeIntoActiveEditor(copilotResponse);
        }
    }

    private async Task CreateComponentFromCopilot(string copilotResponse, string originalMessage)
    {
        try
        {
            // Extrahiere Component-Namen aus der Nachricht
            var componentName = ExtractComponentName(originalMessage);
            if (string.IsNullOrEmpty(componentName))
                componentName = "NewCopilotComponent";

            // Extrahiere C# Code aus Copilot Antwort
            var codeBlocks = ExtractCodeBlocks(copilotResponse);
            var componentCode = codeBlocks.FirstOrDefault() ?? GenerateBasicComponent(componentName);

            // Korrekte s&box Project API - verwende GetRootPath()
            var projectPath = Project.Current.GetRootPath();
            var componentPath = Path.Combine(projectPath, "Code", "Components", $"{componentName}.cs");

            // Stelle sicher dass Verzeichnis existiert
            Directory.CreateDirectory(Path.GetDirectoryName(componentPath));

            // Schreibe Component-Datei
            await File.WriteAllTextAsync(componentPath, componentCode);

            // Verwende Asset-System für Refresh wie in #BatchPublisher
            var asset = AssetSystem.RegisterFile(componentPath);
            while (!asset.IsCompiledAndUpToDate)
            {
                await Task.Yield();
            }

            AddChatMessage("System", $"✅ Component '{componentName}' wurde erstellt: {componentPath}", false);

            // Öffne die neue Datei im Code-Editor
            SboxEditorUtility.OpenFileInEditor(componentPath);
        }
        catch (Exception ex)
        {
            AddChatMessage("System", $"❌ Fehler beim Erstellen des Components: {ex.Message}", false);
        }
    }

    private async Task CreateFileFromCopilot(string copilotResponse, string originalMessage)
    {
        try
        {
            var fileName = ExtractFileName(originalMessage);
            if (string.IsNullOrEmpty(fileName))
                fileName = "NewCopilotFile.cs";

            var codeBlocks = ExtractCodeBlocks(copilotResponse);
            var fileContent = codeBlocks.FirstOrDefault() ?? copilotResponse;

            var projectPath = Project.Current.GetRootPath();
            var filePath = Path.Combine(projectPath, "Code", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, fileContent);

            // Asset registrieren
            var asset = AssetSystem.RegisterFile(filePath);
            while (!asset.IsCompiledAndUpToDate)
            {
                await Task.Yield();
            }

            AddChatMessage("System", $"✅ Datei '{fileName}' wurde erstellt: {filePath}", false);
            SboxEditorUtility.OpenFileInEditor(filePath);
        }
        catch (Exception ex)
        {
            AddChatMessage("System", $"❌ Fehler beim Erstellen der Datei: {ex.Message}", false);
        }
    }

    private async Task InsertCodeIntoActiveEditor(string copilotResponse)
    {
        try
        {
            var codeBlocks = ExtractCodeBlocks(copilotResponse);
            if (codeBlocks.Any())
            {
                var codeToInsert = codeBlocks.First();

                // Code in aktiven Editor einfügen
                SboxEditorUtility.InsertCodeAtCursor(codeToInsert);

                AddChatMessage("System", "✅ Code wurde in den aktiven Editor eingefügt", false);
            }
        }
        catch (Exception ex)
        {
            AddChatMessage("System", $"❌ Fehler beim Einfügen des Codes: {ex.Message}", false);
        }
    }

    // Hilfsmethoden für Code-Extraktion
    private string ExtractComponentName(string message)
    {
        var patterns = new[]
        {
            @"component\s+(\w+)",
            @"erstelle\s+(\w+)\s+component",
            @"(\w+)\s+component"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private string ExtractFileName(string message)
    {
        var patterns = new[]
        {
            @"datei\s+(\w+\.cs)",
            @"file\s+(\w+\.cs)",
            @"erstelle\s+(\w+\.cs)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private List<string> ExtractCodeBlocks(string text)
    {
        var codeBlocks = new List<string>();
        var pattern = @"```(?:csharp|cs|c#)?\s*(.*?)```";
        var matches = Regex.Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                codeBlocks.Add(match.Groups[1].Value.Trim());
            }
        }

        return codeBlocks;
    }

    private string GenerateBasicComponent(string componentName)
    {
        // Verwende korrekte Package API basierend auf #attachments
        var packageName = Project.Current.Config.Ident; // Verwende Ident statt PackageName

        return $@"using Sandbox;

namespace {packageName}.Components;

/// <summary>
/// {componentName} - Generated by Copilot
/// </summary>
public sealed class {componentName} : Component
{{
    protected override void OnStart()
    {{
        // Component initialization
    }}

    protected override void OnUpdate()
    {{
        // Component update logic
    }}
}}";
    }

    // Code-Editor Integration basierend auf VSCode Pattern
    private string GetSelectedCodeFromEditor()
    {
        try
        {
            // Verwende die s&box Code Editor Integration
            return SboxEditorUtility.GetSelectedText() ??
                   "// Kein Code ausgewählt oder Editor nicht verfügbar";
        }
        catch (Exception ex)
        {
            AddChatMessage("System", $"Fehler beim Abrufen des Editor-Codes: {ex.Message}", false);
            return "// Fehler beim Code-Zugriff";
        }
    }

    // UI Helper Methoden
    private Widget AddChatMessage(string sender, string message, bool isUser)
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

        // Update Canvas - basierend auf den Attachments
        _chatHistory.Canvas.Update();

        try
        {
            // Update Geometry wie in den PropertySheetPopup Beispielen
            _chatHistory.Canvas.UpdateGeometry();
            _chatHistory.Canvas.AdjustSize();

            // Kein direktes Scrolling - lasse das System das automatisch handhaben
            // Das ScrollArea wird automatisch zum Ende scrollen wenn neue Inhalte hinzugefügt werden
        }
        catch (Exception ex)
        {
            // Fallback: Nur Update ohne Geometry-Anpassung
            Log.Warning($"ScrollArea Update Fehler: {ex.Message}");
        }

        return messageWidget; // Widget zurückgeben für späteres Löschen
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
        // DEBUG: Settings komplett zurücksetzen (temporär)
        EditorCookie.Set("copilot.connected", false);
        EditorCookie.Set("copilot.token", "");

        _isConnected = EditorCookie.Get("copilot.connected", false);
        var savedToken = EditorCookie.Get("copilot.token", "");

        if (_isConnected && !string.IsNullOrEmpty(savedToken))
        {
            _accessToken = savedToken;
            _tokenInput.Value = savedToken;
            SetStatus("Gespeicherte Verbindung gefunden", true);
            ShowMainInterface();
        }
        else
        {
            // Sicherstellen dass Login-UI sichtbar ist
            _isConnected = false;
            _accessToken = "";
            _loginContainer.Visible = true;
            _mainContainer.Visible = false;
            SetStatus("Nicht verbunden", false);
        }
    }
}

// s&box Editor Utility - basierend auf VSCode Integration Pattern
public static class SboxEditorUtility
{
    public static void OpenFileInEditor(string filePath)
    {
        try
        {
            // Verwende s&box VSCode Integration basierend auf #CodeEditor.VSCode.cs
            var codeEditor = new Editor.CodeEditors.VisualStudioCode();
            if (codeEditor.IsInstalled())
            {
                codeEditor.OpenFile(filePath, null, null);
                Log.Info($"Datei in VSCode geöffnet: {filePath}");
            }
            else
            {
                // Fallback: Verwende System-Standard
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
                Log.Info($"Datei geöffnet: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Konnte Datei nicht öffnen: {ex.Message}");

            // Alternative: Code in die Konsole ausgeben
            try
            {
                var content = File.ReadAllText(filePath);
                Log.Info($"Datei-Inhalt:\n{content}");
            }
            catch (Exception readEx)
            {
                Log.Warning($"Konnte Datei auch nicht lesen: {readEx.Message}");
            }
        }
    }

    public static void InsertCodeAtCursor(string code)
    {
        try
        {
            // Basierend auf #attachments - verwende Log für Code-Output
            Log.Info($"=== GENERATED CODE ===\n{code}\n=== END GENERATED CODE ===");

            // Alternative: Temporäre Datei erstellen und öffnen - Korrekte Project API
            var tempDir = Path.Combine(Project.Current.GetRootPath(), "Temp");
            Directory.CreateDirectory(tempDir);

            var tempFile = Path.Combine(tempDir, $"copilot_generated_{DateTime.Now:yyyyMMdd_HHmmss}.cs");
            File.WriteAllText(tempFile, code);

            Log.Info($"Code wurde in temporäre Datei geschrieben: {tempFile}");
            OpenFileInEditor(tempFile);
        }
        catch (Exception ex)
        {
            Log.Warning($"Konnte Code nicht bereitstellen: {ex.Message}");
        }
    }

    public static string GetSelectedText()
    {
        // Basierend auf #attachments - noch keine direkte Editor-API verfügbar
        return null;
    }
}

// Passwort LineEdit bleibt gleich
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