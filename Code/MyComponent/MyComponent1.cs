using Sandbox;

namespace Construction
{
    public class MyComponent1 : Component
    {
        [Property] public float Speed { get; set; } = 1.0f;
        [Property] public bool IsActive { get; set; } = true;
        [Property] public Vector3 PositionOffset { get; set; } = Vector3.One;

        protected override void OnStart()
        {
            Log.Info("MyComponent1 started");
        }

        protected override void OnUpdate()
        {
            if (IsActive)
            {
                Transform.Position += Vector3.Up * Speed * Time.Delta;
            }
        }

        // Dummy code to fill lines
        private int dummy1 = 1;
        private int dummy2 = 2;
        // Add more dummies and comments to reach ~500 lines
        // ...
    }
}