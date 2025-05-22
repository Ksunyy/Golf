using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace GolfGame
{
    public class Hole : IDisposable
    {
        private float Radius = 0.35f; // Было 0.25f
        private float Depth = 0.4f;   // Было 0.3f

        // Добавляем "зону притяжения" вокруг лунки
        private float AttractionRadius = 0.5f;
        private float AttractionForce = 0.3f;
        private float RollingAttractionForce = 0.5f;
        public Vector3 Position;
        private int VAO, VBO, EBO;
        private float[] vertices;
        private int[] indices;

        public Hole(Vector3 position)
        {
            Position = position;
            GenerateHoleGeometry();
            SetupBuffers();
        }

        private void GenerateHoleGeometry()
        {
            int segments = 32;
            float angleStep = MathF.PI * 2f / segments;

            List<float> _vertices = new List<float>();
            List<int> _indices = new List<int>();

            // Вершины для верхнего круга (край лунки)
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep;
                _vertices.Add(MathF.Cos(angle) * Radius); // X
                _vertices.Add(0.05f);                        // Y (верх)
                _vertices.Add(MathF.Sin(angle) * Radius); // Z
            }

            // Вершины для нижнего круга (дно лунки)
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep;
                _vertices.Add(MathF.Cos(angle) * Radius * 0.8f); // X (чуть уже)
                _vertices.Add(-Depth);                           // Y (низ)
                _vertices.Add(MathF.Sin(angle) * Radius * 0.8f); // Z (чуть уже)
            }

            // Индексы для боковых стенок
            for (int i = 0; i < segments; i++)
            {
                // Первый треугольник стенки
                _indices.Add(i);
                _indices.Add(i + 1);
                _indices.Add(segments + 1 + i);

                // Второй треугольник стенки
                _indices.Add(i + 1);
                _indices.Add(segments + 1 + i + 1);
                _indices.Add(segments + 1 + i);
            }

            // Индексы для дна (веер треугольников)
            int centerIndexBottom = _vertices.Count / 3;
            _vertices.Add(0f); _vertices.Add(-Depth); _vertices.Add(0f); // Центр дна

            for (int i = 0; i < segments; i++)
            {
                _indices.Add(segments + 1 + i);
                _indices.Add(segments + 1 + i + 1);
                _indices.Add(centerIndexBottom);
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
        public void ApplyAttraction(GolfBall ball)
        {
            Vector2 holePos2D = new Vector2(Position.X, Position.Z);
            Vector2 ballPos2D = new Vector2(ball.Position.X, ball.Position.Z);
            float distance = Vector2.Distance(holePos2D, ballPos2D);

            if (distance < AttractionRadius && distance > 0.1f)
            {
                Vector2 direction = holePos2D - ballPos2D;
                direction.Normalize();

                float force = ball.IsRolling ? RollingAttractionForce : AttractionForce;
                force *= (1 - distance / AttractionRadius)*0.2f;

                ball.Velocity += new Vector3(direction.X, 0, direction.Y) * force * 0.1f;
            }
        }

        public bool CheckBallInHole(Vector3 ballPosition, Vector3 ballVelocity)
        {
            // Проверяем горизонтальное расстояние
            Vector2 holePos2D = new Vector2(Position.X, Position.Z);
            Vector2 ballPos2D = new Vector2(ballPosition.X, ballPosition.Z);
            float distance = Vector2.Distance(holePos2D, ballPos2D);

            // Проверяем вертикальное положение (мяч должен быть близко к земле)
            //bool isBallLowEnough = ballPosition.Y <= Position.Y + 0.15f; // Было 0.1f

            // Мяч считается в лунке если:
            // 1. Он внутри радиуса лунки И достаточно низко
            // 2. ИЛИ он очень близко и движется медленно
            return (distance <= Radius) ||
                   (distance <= Radius * 1.2f && ballVelocity.Length < 0.5f);
        }

        public void Draw(Shader shader)
        {
            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);
            shader.SetColor4("objectColor", new Color4(0.1f, 0.1f, 0.1f, 1f)); // Темно-серый цвет

            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
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