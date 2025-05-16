using Chip8.Vm.Cpu.Interfaces;
using Chip8.Vm.Keyboard;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chip8.Vm.Cpu
{
    public class Chip8Cpu : IChip8Cpu
    {
        // CHIP-8 tiene 4K de memoria
        private byte[] memory = new byte[4096];

        // CPU registers (V0-VF)
        private byte[] V = new byte[16];

        // Index register
        private ushort I;

        // Program counter
        private ushort pc;

        // Stack con 16 niveles
        private ushort[] stack = new ushort[16];
        private byte sp;

        // Temporizadores
        private byte delayTimer;
        private byte soundTimer;

        // Display de 64x32 píxeles (monocromático)
        public bool[,] display = new bool[64, 32];

        // Estado de las teclas (0x0 a 0xF)
        public bool[] keys = new bool[16];

        // Fuente hexadecimal (0-F)
        private readonly byte[] fontset =
        [
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        ];

        private readonly Random random = new();
        public bool DrawFlag { get; set; } = false;
        public bool WaitingForKeyPress { get; set; } = false;
        private byte KeyRegister { get; set; }

        // Constructor
        public Chip8Cpu()
        {
            // Inicialización
            pc = 0x200; // Los programas comienzan en 0x200
            I = 0;
            sp = 0;

            // Limpiar display, registros y memoria
            ClearDisplay();
            Array.Clear(V, 0, V.Length);
            Array.Clear(memory, 0, memory.Length);
            Array.Clear(stack, 0, stack.Length);
            Array.Clear(keys, 0, keys.Length);

            // Cargar fontset en memoria (convencionalmente en 0x000-0x1FF)
            for (int i = 0; i < fontset.Length; i++)
            {
                memory[i] = fontset[i];
            }

            // Inicializar temporizadores
            delayTimer = 0;
            soundTimer = 0;
        }

        // Cargar ROM desde archivo
        public void LoadROM(string filename)
        {
            byte[] rom = File.ReadAllBytes(filename);
            if (rom.Length > 4096 - 0x200)
            {
                throw new Exception("ROM too big for memory");
            }

            for (int i = 0; i < rom.Length; i++)
            {
                memory[i + 0x200] = rom[i];
            }

            Console.WriteLine($"ROM Loaded: {filename} - ({rom.Length} bytes)");
        }

        // Limpiar pantalla
        public void ClearDisplay()
        {
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    display[x, y] = false;
                }
            }
            DrawFlag = true;
        }

        // Un ciclo del emulador
        public void EmulateCycle()
        {
            // Obtener opcode (2 bytes)
            ushort opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);

            // Proceso de opcode
            ProcessOpcode(opcode);

            // Si estamos esperando una tecla, no incrementamos PC
            if (!WaitingForKeyPress)
            {
                // Incrementar PC para el siguiente opcode
                pc += 2;
            }
        }

        public void ProcessOpcode(ushort opcode)
        {
            // Variables de utilidad para interpretar opcodes
            ushort nnn = (ushort)(opcode & 0x0FFF);        // Dirección (12-bit)
            byte nn = (byte)(opcode & 0x00FF);             // Valor de 8-bit
            byte n = (byte)(opcode & 0x000F);              // Valor de 4-bit
            byte x = (byte)((opcode & 0x0F00) >> 8);       // Índice de registro Vx
            byte y = (byte)((opcode & 0x00F0) >> 4);       // Índice de registro Vy

            // Decodificación de opcode
            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode)
                    {
                        case 0x00E0: // CLS - Limpiar pantalla
                            ClearDisplay();
                            break;

                        case 0x00EE: // RET - Retornar de subrutina
                            sp--;
                            pc = stack[sp];
                            break;

                        default:
                            throw new Exception($"Opcode desconocido: {opcode:X4}");
                    }
                    break;

                case 0x1000: // JP addr - Saltar a dirección nnn
                    pc = (ushort)(nnn - 2); // -2 porque se añaden 2 después
                    break;

                case 0x2000: // CALL addr - Llamar subrutina en nnn
                    stack[sp] = pc;
                    sp++;
                    pc = (ushort)(nnn - 2); // -2 porque se añaden 2 después
                    break;

                case 0x3000: // SE Vx, byte - Salta próxima instrucción si Vx = nn
                    if (V[x] == nn)
                        pc += 2;
                    break;

                case 0x4000: // SNE Vx, byte - Salta próxima instrucción si Vx != nn
                    if (V[x] != nn)
                        pc += 2;
                    break;

                case 0x5000: // SE Vx, Vy - Salta próxima instrucción si Vx = Vy
                    if (V[x] == V[y])
                        pc += 2;
                    break;

                case 0x6000: // LD Vx, byte - Carga valor nn en Vx
                    V[x] = nn;
                    break;

                case 0x7000: // ADD Vx, byte - Suma nn a Vx
                    V[x] += nn;
                    break;

                case 0x8000:
                    switch (opcode & 0x000F)
                    {
                        case 0x0000: // LD Vx, Vy - Carga valor de Vy en Vx
                            V[x] = V[y];
                            break;

                        case 0x0001: // OR Vx, Vy - OR lógico entre Vx y Vy
                            V[x] |= V[y];
                            break;

                        case 0x0002: // AND Vx, Vy - AND lógico entre Vx y Vy
                            V[x] &= V[y];
                            break;

                        case 0x0003: // XOR Vx, Vy - XOR lógico entre Vx y Vy
                            V[x] ^= V[y];
                            break;

                        case 0x0004: // ADD Vx, Vy - Suma Vy a Vx con carry
                            int sum = V[x] + V[y];
                            V[0xF] = (byte)(sum > 0xFF ? 1 : 0); // Carry flag
                            V[x] = (byte)(sum & 0xFF);
                            break;

                        case 0x0005: // SUB Vx, Vy - Resta Vy de Vx con borrow
                            V[0xF] = (byte)(V[x] > V[y] ? 1 : 0); // No borrow flag
                            V[x] = (byte)(V[x] - V[y] & 0xFF);
                            break;

                        case 0x0006: // SHR Vx - Shift right Vx
                            V[0xF] = (byte)(V[x] & 0x1); // LSB
                            V[x] >>= 1;
                            break;

                        case 0x0007: // SUBN Vx, Vy - Resta Vx de Vy
                            V[0xF] = (byte)(V[y] > V[x] ? 1 : 0); // No borrow flag
                            V[x] = (byte)(V[y] - V[x] & 0xFF);
                            break;

                        case 0x000E: // SHL Vx - Shift left Vx
                            V[0xF] = (byte)((V[x] & 0x80) >> 7); // MSB
                            V[x] <<= 1;
                            break;

                        default:
                            throw new Exception($"Opcode desconocido: {opcode:X4}");
                    }
                    break;

                case 0x9000: // SNE Vx, Vy - Salta próxima instrucción si Vx != Vy
                    if (V[x] != V[y])
                        pc += 2;
                    break;

                case 0xA000: // LD I, addr - Carga dirección nnn en I
                    I = nnn;
                    break;

                case 0xB000: // JP V0, addr - Salta a dirección nnn + V0
                    pc = (ushort)(nnn + V[0] - 2); // -2 porque se añaden 2 después
                    break;

                case 0xC000: // RND Vx, byte - Genera número aleatorio con máscara
                    V[x] = (byte)(random.Next(0, 256) & nn);
                    break;

                case 0xD000: // DRW Vx, Vy, nibble - Dibuja sprite
                    int xPos = V[x] % 64;
                    int yPos = V[y] % 32;
                    V[0xF] = 0; // Reset collision flag

                    for (int row = 0; row < n; row++)
                    {
                        byte spriteByte = memory[I + row];

                        for (int col = 0; col < 8; col++)
                        {
                            // Si el bit está activo en el sprite
                            if ((spriteByte & 0x80 >> col) != 0)
                            {
                                int pixelX = (xPos + col) % 64;
                                int pixelY = (yPos + row) % 32;

                                // Si el pixel ya está activo, hay colisión
                                if (display[pixelX, pixelY])
                                {
                                    V[0xF] = 1;
                                }

                                // XOR el pixel
                                display[pixelX, pixelY] ^= true;
                            }
                        }
                    }

                    DrawFlag = true;
                    break;

                case 0xE000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x009E: // SKP Vx - Salta próxima instrucción si tecla Vx está presionada
                            if (keys[V[x]])
                                pc += 2;
                            break;

                        case 0x00A1: // SKNP Vx - Salta próxima instrucción si tecla Vx no está presionada
                            if (!keys[V[x]])
                                pc += 2;
                            break;

                        default:
                            throw new Exception($"Opcode desconocido: {opcode:X4}");
                    }
                    break;

                case 0xF000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x0007: // LD Vx, DT - Carga valor de delay timer en Vx
                            V[x] = delayTimer;
                            break;

                        case 0x000A: // LD Vx, K - Espera por key press y guarda en Vx
                            WaitingForKeyPress = true;
                            KeyRegister = x;
                            break;

                        case 0x0015: // LD DT, Vx - Carga Vx en delay timer
                            delayTimer = V[x];
                            break;

                        case 0x0018: // LD ST, Vx - Carga Vx en sound timer
                            soundTimer = V[x];
                            break;

                        case 0x001E: // ADD I, Vx - Suma Vx a I
                            I += V[x];
                            break;

                        case 0x0029: // LD F, Vx - Carga ubicación de sprite de dígito Vx en I
                            I = (ushort)(V[x] * 5); // Cada sprite de dígito ocupa 5 bytes
                            break;

                        case 0x0033: // LD B, Vx - Almacena representación BCD de Vx
                            memory[I] = (byte)(V[x] / 100);           // Centenas
                            memory[I + 1] = (byte)(V[x] / 10 % 10); // Decenas
                            memory[I + 2] = (byte)(V[x] % 10);        // Unidades
                            break;

                        case 0x0055: // LD [I], Vx - Almacena V0 a Vx en memoria desde I
                            for (int i = 0; i <= x; i++)
                            {
                                memory[I + i] = V[i];
                            }
                            break;

                        case 0x0065: // LD Vx, [I] - Carga V0 a Vx desde memoria desde I
                            for (int i = 0; i <= x; i++)
                            {
                                V[i] = memory[I + i];
                            }
                            break;

                        default:
                            throw new Exception($"Opcode desconocido: {opcode:X4}");
                    }
                    break;

                default:
                    throw new Exception($"Opcode desconocido: {opcode:X4}");
            }
        }

        // Actualizar temporizadores (60Hz)
        public void UpdateTimers()
        {
            if (delayTimer > 0)
                delayTimer--;

            if (soundTimer > 0)
            {
                if (soundTimer == 1)
                    Console.Beep(); // Sonido simple cuando el timer llega a 0
                soundTimer--;
            }
        }

        // Procesar entrada de teclado
        public void ProcessKeyInput(Keys key, bool pressed)
        {
            if (KeyboardMapping.keyMap.TryGetValue(key, out int keyIndex))
            {
                keys[keyIndex] = pressed;

                // Si estamos esperando una tecla y se presiona alguna
                if (WaitingForKeyPress && pressed)
                {
                    V[KeyRegister] = (byte)keyIndex;
                    WaitingForKeyPress = false;
                }
            }
        }
    }
}
