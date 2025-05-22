using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Drawing;
using System;


namespace GolfGame
{
    public class GolfBall : IDisposable
    {
        public Vector3 Position = new Vector3(0f, 0.2f, 0f);
        public Vector3 Velocity = Vector3.Zero;
        public bool IsRolling { get; private set; } = false;
        // Константы для физики

        private const float MinFlightForce = 8.0f; // Минимальная сила для полета
        private const float RollingFriction = 0.96f; // Трение при качении
        private bool _isInFlight = false;

        private const float Gravity = -9.8f; // Реалистичная гравитация (м/с?)
        private const float GroundBounce = 0.6f; // Коэффициент отскока от земли
        private const float GroundFriction = 0.98f; // Трение о землю
        private const float AirResistance = 0.999f; // Сопротивление воздуха
        private const float MinVelocity = 0.05f; // Минимальная скорость для остановки
        private const float RollingThreshold = 0.3f; // Порог для перехода в качение

        private int VAO, VBO, EBO;
        private float[] vertices;
        private int[] indices;

        public GolfBall(float radius = 0.2f, int sectors = 16, int stacks = 16)
        {
            GenerateSphereVertices(radius, sectors, stacks);
            SetupBuffers();
        }

        private void GenerateSphereVertices(float radius, int sectors, int stacks)
        {
            float sectorStep = 2 * MathF.PI / sectors;
            float stackStep = MathF.PI / stacks;

            List<float> _vertices = new List<float>();
            List<int> _indices = new List<int>();

            for (int i = 0; i <= stacks; ++i)
            {
                float stackAngle = MathF.PI / 2 - i * stackStep;
                float xy = radius * MathF.Cos(stackAngle);
                float z = radius * MathF.Sin(stackAngle);

                for (int j = 0; j <= sectors; ++j)
                {
                    float sectorAngle = j * sectorStep;
                    float x = xy * MathF.Cos(sectorAngle);
                    float y = xy * MathF.Sin(sectorAngle);

                    _vertices.Add(x);
                    _vertices.Add(y);
                    _vertices.Add(z);
                }
            }

            for (int i = 0; i < stacks; ++i)
            {
                int k1 = i * (sectors + 1);
                int k2 = k1 + sectors + 1;

                for (int j = 0; j < sectors; ++j, ++k1, ++k2)
                {
                    if (i != 0)
                    {
                        _indices.Add(k1);
                        _indices.Add(k2);
                        _indices.Add(k1 + 1);
                    }

                    if (i != (stacks - 1))
                    {
                        _indices.Add(k1 + 1);
                        _indices.Add(k2);
                        _indices.Add(k2 + 1);
                    }
                }
            }

            vertices = _vertices.ToArray();
            indices = _indices.ToArray();
        }

        private void SetupBuffers()
        {
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        public void CleanUp()
        {
            GL.DeleteVertexArray(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
        }

        public void Update(float deltaTime)
        {

            if (_isInFlight)
            {
                // Физика полета (как было)
                Velocity += new Vector3(0, Gravity, 0) * deltaTime;
                Velocity *= AirResistance;

                if (Position.Y <= 0.2f)
                {
                    Position.Y = 0.2f;
                    if (Math.Abs(Velocity.Y) > 0.1f)
                    {
                        Velocity.Y *= -GroundBounce;
                    }
                    else
                    {
                        _isInFlight = false; // Переход в качение
                    }
                }
            }
            else
            {
                // Физика качения
                Velocity.Xz *= RollingFriction;
                Velocity.Y = 0;

                // Остановка при малой скорости
                if (Velocity.Length < 0.01f)
                {
                    Velocity = Vector3.Zero;
                }
            }

            Position += Velocity * deltaTime;
        }


        public void Draw(Shader shader)
        {
            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);
            shader.SetColor4("objectColor", Color4.White);

            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Hit(Vector3 direction, float power)
        {
            // Нормализуем направление (на всякий случай)
            direction.Normalize();
            _isInFlight = power > MinFlightForce;
            if (_isInFlight)
            {
                // Полетный удар - добавляем вертикальную составляющую
                direction.Y += 0.2f;
                direction.Normalize();
                Velocity = direction * power;
            }
            else
            {
                // Удар качения - только горизонтальная составляющая
                direction.Y = 0;
                direction.Normalize();
                Velocity = direction * power * 0.6f; // Меньшая скорость для качения
                Position = new Vector3(Position.X, 0.2f, Position.Z); // Фиксируем на земле
            }
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GC.SuppressFinalize(this);
        }
    }

}


