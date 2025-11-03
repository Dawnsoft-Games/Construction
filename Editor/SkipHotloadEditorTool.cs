using Sandbox;
using Editor;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Construction.Editor
{
    [EditorApp("SkipHotload Tool", "code", "Fügt [SkipHotload] zu Component-Klassen hinzu")]
    public class SkipHotloadEditorTool : BaseWindow
    {
        private string selectedDirectory = "Code";
        private List<string> csFiles = new();
        private HashSet<string> changedFiles = new();
        private ScrollArea scrollArea;
        private Layout fileListLayout;
        private FileSystemWatcher watcher;
        private CancellationTokenSource buildCancellation;

        public SkipHotloadEditorTool()
        {
            WindowTitle = "SkipHotload Tool";
            SetWindowIcon("code");
            Size = new Vector2(600, 500);

            Layout = Layout.Column();

            var infoLabel = new Label("Datei-Watcher überwacht Änderungen. 'Auto [SkipHotload] anwenden' fügt [SkipHotload] zu unveränderten Dateien hinzu und entfernt es von geänderten.\n[SkipHotload] verhindert Hotload für die Klasse.");
            infoLabel.WordWrap = true;
            Layout.Add(infoLabel);

            // Directory info
            Layout.Add(new Label($"Verzeichnis: {selectedDirectory} (änderbar im Code)"));

            // Scrollable file list
            scrollArea = new ScrollArea(this);
            fileListLayout = Layout.Column();
            scrollArea.Layout = fileListLayout;
            Layout.Add(scrollArea, 1);

            // Buttons
            var buttonRow = Layout.Row();
            var addButton = new Button("[SkipHotload] hinzufügen");
            addButton.Clicked += () => ProcessAllFiles(true);
            buttonRow.Add(addButton);

            var removeButton = new Button("[SkipHotload] entfernen");
            removeButton.Clicked += () => ProcessAllFiles(false);
            buttonRow.Add(removeButton);

            var autoButton = new Button("Auto [SkipHotload] anwenden");
            autoButton.Clicked += ApplyAutoSkipHotload;
            buttonRow.Add(autoButton);

            Layout.Add(buttonRow);

            // Start file watcher
            StartFileWatcher();

            RefreshFileList();
        }

        private void StartFileWatcher()
        {
            string fullPath = Path.Combine(Project.Current.GetRootPath(), selectedDirectory);
            if (!Directory.Exists(fullPath)) return;

            watcher = new FileSystemWatcher(fullPath, "*.cs")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string relativePath = Path.GetRelativePath(Path.Combine(Project.Current.GetRootPath(), selectedDirectory), e.FullPath);
            changedFiles.Add(relativePath);
            Log.Info($"Datei geändert: {relativePath}");

            // Cancel previous build task
            buildCancellation?.Cancel();
            buildCancellation = new CancellationTokenSource();

            // Start auto-build after delay
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000, buildCancellation.Token); // 2 second delay
                    await BuildProjectAsync(buildCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    // Build was cancelled, do nothing
                }
            });
        }

        private void RefreshFileList()
        {
            csFiles.Clear();
            fileListLayout.Clear(true);

            string fullPath = Path.Combine(Project.Current.GetRootPath(), selectedDirectory);
            if (!Directory.Exists(fullPath))
            {
                fileListLayout.Add(new Label($"Verzeichnis nicht gefunden: {fullPath}"));
                return;
            }

            csFiles = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories).ToList();

            foreach (string file in csFiles)
            {
                string relativePath = Path.GetRelativePath(fullPath, file);
                var label = new Label(relativePath);
                fileListLayout.Add(label);
            }
        }

        private void ProcessAllFiles(bool add)
        {
            string fullPath = Path.Combine(Project.Current.GetRootPath(), selectedDirectory);
            foreach (string file in csFiles)
            {
                ProcessFile(file, add);
            }
            Log.Info($"Fertig! [SkipHotload] wurde {(add ? "hinzugefügt" : "entfernt")} zu allen Dateien.");
        }

        private void ApplyAutoSkipHotload()
        {
            string fullPath = Path.Combine(Project.Current.GetRootPath(), selectedDirectory);
            foreach (string file in csFiles)
            {
                string relativePath = Path.GetRelativePath(fullPath, file);
                bool isChanged = changedFiles.Contains(relativePath);
                ProcessFile(file, !isChanged); // Add if not changed, remove if changed
            }
            Log.Info("Auto [SkipHotload] angewendet!");
        }

        private void ProcessFile(string filePath, bool add)
        {
            string content = File.ReadAllText(filePath);
            string originalContent = content;

            Regex classRegex = new Regex(@"class\s+(\w+)\s*:\s*Component", RegexOptions.Multiline);
            var matches = classRegex.Matches(content);

            foreach (Match match in matches)
            {
                string className = match.Groups[1].Value;
                int classIndex = match.Index;
                int lineStart = content.LastIndexOf('\n', classIndex) + 1;
                string beforeClass = content.Substring(0, lineStart);

                if (add)
                {
                    if (!Regex.IsMatch(beforeClass, @"\[SkipHotload\]", RegexOptions.Multiline))
                    {
                        string insert = "[SkipHotload]\n";
                        content = content.Insert(lineStart, insert);
                        Log.Info($"[SkipHotload] zu Klasse {className} in {filePath} hinzugefügt.");
                    }
                }
                else
                {
                    // Remove [SkipHotload] if present
                    Regex skipRegex = new Regex(@"\[SkipHotload\]\s*\n", RegexOptions.Multiline);
                    content = skipRegex.Replace(content, "");
                    Log.Info($"[SkipHotload] von Klasse {className} in {filePath} entfernt.");
                }
            }

            if (content != originalContent)
            {
                File.WriteAllText(filePath, content);
            }
        }

        private async Task BuildProjectAsync(CancellationToken token)
        {
            try
            {
                Log.Info("Starte automatischen Build...");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "build",
                        WorkingDirectory = Project.Current.GetRootPath(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(token);

                if (process.ExitCode == 0)
                {
                    Log.Info("Automatischer Build erfolgreich abgeschlossen.");
                }
                else
                {
                    Log.Error($"Build fehlgeschlagen: {error}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Fehler beim automatischen Build: {ex.Message}");
            }
        }
    }
}