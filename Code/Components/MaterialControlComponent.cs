using Sandbox;
using Construction.UI;

// Minimal, runtime-safe component. Editor-only features (Asset picker, Widget APIs) were removed
// to keep this component compatible with the runtime assembly used by this project.
public sealed class MaterialControlComponent : Component
{
    // Store the actual Material instance instead of a string path. Editor tools can
    // still write a path or asset, but at runtime we'll keep a Material reference here.
    [Property]
    public Material Material { get; set; }

    // UI objects (Panel and its subclasses) must not be serialized by System.Text.Json
    // because they contain non-serializable engine types. Prevent serialization here.
    [System.Text.Json.Serialization.JsonIgnore]
    public MaterialPanel MaterialPanel { get; private set; }

    

    [Property]
    public Color BackgroundColor { get; set; } = Color.White;

    // No editor-only lifecycle overrides here. Use OnUpdate for runtime behaviour if needed.
    protected override void OnUpdate()
    {
        // Intentionally empty. Implementation of runtime material application depends on
        // runtime APIs available in the project and was removed to avoid editor-only types.
    }
}
