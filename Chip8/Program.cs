using Chip8.Vm.Display;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

byte i = 1;
string? romPath;
string[] files = [];

if (Directory.Exists("roms"))
{
    files = Directory.GetFiles("roms");
}

Console.WriteLine("CHIP-8 Emulator");
Console.WriteLine("Available ROMs in /rom directory:");
Console.WriteLine("0. Enter path manually");

foreach (string file in files)
{
    Console.WriteLine(i + ". " + Path.GetFileName(file));
    i++;
}

while (byte.TryParse(Console.ReadLine(), out byte option) && !(option > files.Length))
{
    if (option == 0)
    {
        Console.WriteLine("Enter ROM path:");
        romPath = Console.ReadLine();
        if (!File.Exists(romPath))
        {
            Console.WriteLine("ROM file not found.");
            return;
        }
    }
    else
    {
        romPath = files[option - 1];
    }

    using Chip8Window window = new(GameWindowSettings.Default, NativeWindowSettings.Default);
    window.Title = "CHIP-8 Emulator - " + Path.GetFileName(romPath);
    window.Size = new Vector2i(640, 320); // 10x scale of original 64x32 display

    window.Chip8 = new();
    window.Chip8.LoadROM(romPath);

    window.Run();
}
