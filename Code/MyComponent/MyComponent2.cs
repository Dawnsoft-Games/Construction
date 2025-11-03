using Sandbox;

namespace Construction
{
    public class MyComponent2 : Component
    {
        [Property] public float Speed { get; set; } = 2.0f;
        [Property] public bool IsActive { get; set; } = false;

        protected override void OnStart()
        {
            Log.Info("MyComponent2 started");
        }

        protected override void OnUpdate()
        {
            if (IsActive)
            {
                Transform.Position += Vector3.Down * Speed * Time.Delta;
            }
        }
    }
}