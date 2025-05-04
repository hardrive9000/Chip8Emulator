using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chip8.Vm.Cpu.Interfaces
{
    public interface IChip8Cpu
    {
        bool DrawFlag { get; set; }
        bool WaitingForKeyPress { get; set; }

        void ClearDisplay();
        void EmulateCycle();
        void LoadROM(string filename);
        void ProcessKeyInput(Keys key, bool pressed);
        void ProcessOpcode(ushort opcode);
        void UpdateTimers();
    }
}