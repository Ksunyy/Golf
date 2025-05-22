using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.IO;
using StbImageSharp;

namespace GolfGame
{
    public class Ground : IDisposable
    {
        private readonly int VAO;
        private readonly int VBO;
        private readonly int textureID;
        private const float Size = 10f;
        private const float TextureRepeat = 5f;

        public Ground()
        {
            // ������� � ������������, ��������� � ����������� ������������
            float[] vertices = {
                // Position          // Normal       // Texture coords
                -Size, 0f, -Size,    0f, 1f, 0f,     0f, 0f,
                 Size, 0f, -Size,    0f, 1f, 0f,     TextureRepeat, 0f,
                 Size, 0f,  Size,    0f, 1f, 0f,     TextureRepeat, TextureRepeat,
                -Size, 0f,  Size,    0f, 1f, 0f,     0f, TextureRepeat
            };

            // �������� � ��������� VAO/VBO
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // �������� ��������� ������
            // ������� ������� (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // ������� ������� (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // ������� ���������� ��������� (location = 2)
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(0);

            // �������� ��������
            textureID = LoadTexture("Textures/grass.jpg");
        }

        private int LoadTexture(string path)
        {
            // ������ ���� � �����
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Texture file not found: {fullPath}");

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);

            // ��������� ���������� ��������
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // �������� ����������� � ������� StbImageSharp
            using (var stream = File.OpenRead(fullPath))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                GL.TexImage2D(TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    image.Data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texture;
        }

        public void Draw(Shader shader)
        {
            shader.Use();

            // ��������� ������
            Matrix4 model = Matrix4.Identity;
            shader.SetMatrix4("model", model);

            // �������� ��������
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            shader.SetInt("texture0", 0);

            // ���������
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            // �������
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteTexture(textureID);
            GC.SuppressFinalize(this);
        }
    }
}