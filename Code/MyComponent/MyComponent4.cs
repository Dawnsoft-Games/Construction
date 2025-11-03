using Sandbox;

namespace Construction
{
    public class MyComponent4 : Component
    {
        [Property] public float Speed { get; set; } = 4.0f;
        [Property] public bool IsActive { get; set; } = false;

        protected override void OnStart()
        {
            Log.Info("MyComponent4 started");
        }

        protected override void OnUpdate()
        {
            if (IsActive)
            {
                Transform.Position += Vector3.Right * Speed * Time.Delta;
            }
        }
    }
}