using Sandbox;

namespace Construction
{
    public class MyComponent3 : Component
    {
        [Property] public float Speed { get; set; } = 3.0f;
        [Property] public bool IsActive { get; set; } = true;

        [Property] public Vector3 PositionOffset { get; set; } = Vector3.One;

        protected override void OnStart()
        {
            Log.Info("MyComponent3 started");
        }

        protected override void OnUpdate()
        {
            if (IsActive)
            {
                Transform.Position += Vector3.Left * Speed * Time.Delta;
            }
        }
    }
}