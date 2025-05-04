using Chip8.Vm.Cpu;

namespace Chip8.Vm.Display.Interfaces
{
    public interface IChip8Window
    {
        Chip8Cpu Chip8 { get; set; }
    }
}