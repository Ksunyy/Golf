using GolfGame;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Reflection.Metadata;

namespace GolfGame
{
    public class Shader: IDisposable
    {
        private readonly int _handle;


        public Shader(string vertexPath, string fragmentPath)
        {



            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Комбинируем с относительным путем к шейдерам
            string fullVertexPath = Path.Combine(basePath, "Shaders", vertexPath);
            string fullFragmentPath = Path.Combine(basePath, "Shaders", fragmentPath);

            // Проверка существования файлов
            if (!File.Exists(fullVertexPath))
                throw new FileNotFoundException($"Vertex shader not found at: {fullVertexPath}");
            if (!File.Exists(fullFragmentPath))
                throw new FileNotFoundException($"Fragment shader not found at: {fullFragmentPath}");

            // Load and compile vertex shader
            string vertexShaderSource = File.ReadAllText(fullVertexPath);
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            CheckShaderCompileErrors(vertexShader);

            // Load and compile fragment shader
            string fragmentShaderSource = File.ReadAllText(fullFragmentPath);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            CheckShaderCompileErrors(fragmentShader);

            // Create shader program
            _handle = GL.CreateProgram();
            GL.AttachShader(_handle, vertexShader);
            GL.AttachShader(_handle, fragmentShader);
            GL.LinkProgram(_handle);

            CheckProgramLinkErrors(_handle);

            // Clean up shaders
            GL.DetachShader(_handle, vertexShader);
            GL.DetachShader(_handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public void Use()
        {
            GL.UseProgram(_handle);
        }
        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.Uniform1(location, value);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }

        public void SetVector3(string name, Vector3 vector)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.Uniform3(location, vector);
        }

        public void SetColor4(string name, Color4 color)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.Uniform4(location, color);
        }

        private void CheckShaderCompileErrors(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Shader compilation error: {infoLog}");
            }
        }

        private void CheckProgramLinkErrors(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Program linking error: {infoLog}");
            }
        }

        public void Dispose()
        {
            GL.DeleteProgram(_handle);
            GC.SuppressFinalize(this);
        }
    }


    public class gGame : GameWindow
    {
        private Camera camera;
        private GolfBall ball;
        private Ground ground;
        private Hole hole;
        private Shader shader;

        private int _score = 0;
        private float _messageDisplayTime = 0;
        private string _lastMessage = "";


        private bool shotInProgress = false;
        private float chargePower = 0f;
        private bool chargingShot = false;
        private Vector2 lastMousePos;

        private float maxChargeTime = 2.0f; // Максимальное время зарядки удара

        public gGame(int width, int height, string title)
            : base(GameWindowSettings.Default,
                  new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            camera = new Camera(new Vector3(0f, 1f, 3f), width / (float)height);
            ball = new GolfBall();
            ground = new Ground(); // Теперь без параметра шейдера
            hole = new Hole(new Vector3(2f, 0f, 0f));
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            shader = new Shader("vertexShader.vert", "fragmentShader.frag");
            CursorState = CursorState.Grabbed;
            lastMousePos = new Vector2(MouseState.X, MouseState.Y);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            camera.AspectRatio = e.Width / (float)e.Height;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (KeyboardState.IsKeyDown(Keys.Escape))
                Close();

            // Camera movement
            camera.ProcessKeyboard(KeyboardState, (float)e.Time);

            // Mouse look
            var mouse = MouseState;
            if (CursorState == CursorState.Grabbed)
            {
                var deltaX = mouse.X - lastMousePos.X;
                var deltaY = mouse.Y - lastMousePos.Y;
                camera.ProcessMouseMovement(deltaX, deltaY);
            }
            lastMousePos = new Vector2(mouse.X, mouse.Y);

            // Charging shot
            if (KeyboardState.IsKeyDown(Keys.Space) && !shotInProgress && !chargingShot)
            {
                chargingShot = true;
                chargePower = 0f;
            }

            if (chargingShot)
            {
                chargePower += (float)e.Time;
                if (chargePower > maxChargeTime)
                    chargePower = maxChargeTime;
            }

            // Выстрел при отпускании пробела
            if (KeyboardState.IsKeyReleased(Keys.Space) && chargingShot)
            {
                Vector3 direction = camera.Front;
                direction.Y += 0.2f;
                direction.Normalize();

                float power = MathHelper.Lerp(2.0f, 30.0f, chargePower / maxChargeTime);
                ball.Hit(direction, power);

                shotInProgress = true;
                chargingShot = false;
            }

            ball.Update((float)e.Time);

            // Применяем притяжение лунки к мячу (если вы добавили этот метод)
            hole.ApplyAttraction(ball);

            if (shotInProgress && ball.Velocity.Length < 0.01f)
            {
                bool wasInHole = hole.CheckBallInHole(ball.Position, ball.Velocity);

                if (wasInHole)
                {
                    _score++;
                    _lastMessage = $"ПОПАДАНИЕ №{_score}! Лунка перемещена.";
                    _messageDisplayTime = 3.0f; // Показывать 3 секунды
                    // Генерация новой позиции лунки
                    float x = (Random.Shared.NextSingle() * 8f) - 4f;
                    float z = (Random.Shared.NextSingle() * -8f);
                    hole.Position = new Vector3(x, 0f, z);

                    Console.WriteLine("ПОПАДАНИЕ! Лунка перемещена.");
                }
                if (_messageDisplayTime > 0)
                {
                    _messageDisplayTime -= (float)e.Time;
                }

                // Сброс мяча
                ball.Position = new Vector3(0f, 0.2f, 0f);
                ball.Velocity = Vector3.Zero;
                shotInProgress = false;
            }

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());

            // Draw objects
            ground.Draw(shader);
            hole.Draw(shader);
            ball.Draw(shader);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            shader.Dispose();
            ball.Dispose();
            ground.Dispose();
            hole.Dispose();
            base.OnUnload();
        }
    }

}
