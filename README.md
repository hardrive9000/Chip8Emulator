# CHIP-8 Emulator (.NET 9)

> "CHIP-8 is an interpreted programming language, developed by Joseph Weisbecker. It was initially used on the COSMAC VIP and Telmac 1800 8-bit microcomputers in the mid-1970s. CHIP-8 programs are run on a CHIP-8 virtual machine. It was made to allow video games to be more easily programmed for these computers." — Wikipedia

This is a (as for now work in progress) CHIP-8 emulator written in **.NET 9**, designed for clarity, maintainability, and modular architecture. The emulator uses [OpenTK](https://www.nuget.org/packages/OpenTK/) for rendering and keyboard input. Keyboard keys 0–9, A–F maps to the corresponding CHIP-8 keys.

While the initial version of the codebase was generated using an AI-assisted tool ([Anthropic Claude](https://claude.ai/)), significant effort was invested in **refactoring the code**, **improving the project structure**, and **enhancing overall readability**. The result is a cleaner, more modular emulator that aims to be educational as well as functional.

## Features

- Fully functional CHIP-8 emulator core
- Clean, organized project structure
- Emphasis on code clarity and maintainability
- Written in modern C# with .NET 9

## Goals

- Provide a readable and well-structured implementation of a CHIP-8 emulator
- Serve as a learning resource for anyone interested in emulation or .NET development
- Demonstrate how AI-generated code can be improved and brought to production-level quality through human insight and architectural decisions

## Compiling and Running

To run the emulator:

1. Clone the repository
2. Build the project using .NET 9 SDK
3. Run the executable and load a CHIP-8 ROM

```bash
dotnet build
dotnet run
```

**Keyboard mapping**:  
<kbd>D0</kbd> - <kbd>D9</kbd> = 0 - 9  
<kbd>A</kbd> = A  
<kbd>S</kbd> = B  
<kbd>D</kbd> = C  
<kbd>Z</kbd> = D  
<kbd>X</kbd> = E  
<kbd>C</kbd> = F  

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/)
- A valid CHIP-8 ROM file to test
