using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Camera
{
    public Vector3 Position;
    public Vector3 Front = -Vector3.UnitZ;
    public Vector3 Up = Vector3.UnitY;
    public Vector3 Right = Vector3.UnitX;

    private float _pitch;
    private float _yaw = -MathHelper.PiOver2; // Without this, you'd be started rotated 90 degrees right
    private float _fov = MathHelper.PiOver2;

    public float AspectRatio { get; set; }

    public float Pitch
    {
        get => MathHelper.RadiansToDegrees(_pitch);
        set
        {
            var angle = MathHelper.Clamp(value, -89f, 89f);
            _pitch = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    public float Yaw
    {
        get => MathHelper.RadiansToDegrees(_yaw);
        set
        {
            _yaw = MathHelper.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    public float Fov
    {
        get => MathHelper.RadiansToDegrees(_fov);
        set => _fov = MathHelper.DegreesToRadians(MathHelper.Clamp(value, 1f, 90f));
    }

    public Camera(Vector3 position, float aspectRatio)
    {
        Position = position;
        AspectRatio = aspectRatio;
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Front, Up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);
    }

    private void UpdateVectors()
    {
        Front.X = MathF.Cos(_pitch) * MathF.Cos(_yaw);
        Front.Y = MathF.Sin(_pitch);
        Front.Z = MathF.Cos(_pitch) * MathF.Sin(_yaw);

        Front = Vector3.Normalize(Front);
        Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));
    }

    public void ProcessKeyboard(KeyboardState input, float deltaTime)
    {
        const float cameraSpeed = 2.5f;

        if (input.IsKeyDown(Keys.W))
            Position += Front * cameraSpeed * deltaTime;
        if (input.IsKeyDown(Keys.S))
            Position -= Front * cameraSpeed * deltaTime;
        if (input.IsKeyDown(Keys.A))
            Position -= Right * cameraSpeed * deltaTime;
        if (input.IsKeyDown(Keys.D))
            Position += Right * cameraSpeed * deltaTime;
    }

    public void ProcessMouseMovement(float deltaX, float deltaY)
    {
        const float sensitivity = 0.2f;

        Yaw += deltaX * sensitivity;
        Pitch -= deltaY * sensitivity; // Reversed since y-coordinates range from bottom to top
    }
}