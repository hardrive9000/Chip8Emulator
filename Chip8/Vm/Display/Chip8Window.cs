using Chip8.Vm.Cpu;
using Chip8.Vm.Display.Interfaces;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chip8.Vm.Display
{
    public class Chip8Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : GameWindow(gameWindowSettings, nativeWindowSettings), IChip8Window
    {
        public Chip8Cpu Chip8 { get; set; } = new();

        private int vertexBufferObject;
        private int vertexArrayObject;
        private int shaderProgram;
        private int textureId;

        private DateTime lastTimerUpdate = DateTime.Now;
        private DateTime lastCycleTime = DateTime.Now;
        private readonly int cyclesPerFrame = 10; // Ajustar según la velocidad deseada

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            // Crear shader
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec2 aPosition;
                layout (location = 1) in vec2 aTexCoord;
                
                out vec2 texCoord;
                
                void main()
                {
                    gl_Position = vec4(aPosition, 0.0, 1.0);
                    texCoord = aTexCoord;
                }
            ";

            string fragmentShaderSource = @"
                #version 330 core
                in vec2 texCoord;
                
                out vec4 FragColor;
                
                uniform sampler2D texture0;
                
                void main()
                {
                    float color = texture(texture0, texCoord).r;
                    FragColor = vec4(color, color, color, 1.0);
                }
            ";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Crear VAO y VBO
            float[] vertices = {
                // Posiciones    // Coordenadas de textura
                 1.0f,  1.0f,   1.0f, 0.0f, // Arriba derecha
                 1.0f, -1.0f,   1.0f, 1.0f, // Abajo derecha
                -1.0f, -1.0f,   0.0f, 1.0f, // Abajo izquierda
                -1.0f,  1.0f,   0.0f, 0.0f  // Arriba izquierda
            };

            uint[] indices = {
                0, 1, 3,
                1, 2, 3
            };

            vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);

            vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            int elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Configurar atributos de vértices
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Crear textura
            textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            // Configurar parámetros de textura
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Generar textura inicial vacía
            UpdateTexture();

            GL.UseProgram(shaderProgram);
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "texture0"), 0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Actualizar textura si es necesario
            if (Chip8.DrawFlag)
            {
                UpdateTexture();
                Chip8.DrawFlag = false;
            }

            // Dibujar cuadrado con textura
            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vertexArrayObject);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // Procesar entrada de teclado
            var keyboardState = KeyboardState;

            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            // Manejar todas las teclas del mapa
            foreach (var keyMapping in Chip8.keyMap)
            {
                bool isPressed = keyboardState.IsKeyDown(keyMapping.Key);
                Chip8.ProcessKeyInput(keyMapping.Key, isPressed);
            }

            // Ejecutar ciclos de CPU
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - lastCycleTime;
            if (elapsed.TotalMilliseconds > 1000.0 / 60.0) // Aproximadamente 60 fps
            {
                for (int i = 0; i < cyclesPerFrame; i++)
                {
                    Chip8.EmulateCycle();
                }
                lastCycleTime = now;
            }

            // Actualizar temporizadores a 60Hz
            if ((now - lastTimerUpdate).TotalMilliseconds >= 1000.0 / 60.0)
            {
                Chip8.UpdateTimers();
                lastTimerUpdate = now;
            }
        }

        private void UpdateTexture()
        {
            // Crear array de bytes para la textura (pixeles blanco/negro)
            byte[] textureData = new byte[64 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    textureData[y * 64 + x] = Chip8.display[x, y] ? (byte)255 : (byte)0;
                }
            }

            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, 64, 32, 0,
                         PixelFormat.Red, PixelType.UnsignedByte, textureData);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            GL.DeleteBuffer(vertexBufferObject);
            GL.DeleteProgram(shaderProgram);
            GL.DeleteTexture(textureId);
            GL.DeleteVertexArray(vertexArrayObject);

            base.OnUnload();
        }
    }
}
