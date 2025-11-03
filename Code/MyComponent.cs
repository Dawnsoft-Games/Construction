
public sealed class MyComponent : Component
{
	[Property] public string StringProperty { get; set; }
	[Property] public int IntProperty { get; set; }
	[Property] public float FloatProperty { get; set; }

	protected override void OnUpdate()
	{
	}
}
