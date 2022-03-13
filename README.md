# dnSpy.Extension.Wasm

A plugin to add WebAssembly support to [dnSpy](https://github.com/dnSpyEx/dnSpy).

## Features
As of v0.1.0, this plugin only serves as a WebAssembly disassembler. It aims to mimic dnSpy's look and feel as much as
possible.

![Disassembler screenshot](https://i.imgur.com/zpFrU6C.png)

More features are planned for the near future. A non-exhaustive list of some ideas:
- A decompiler
- "Find References" to search for usages of a method/global
- Renaming symbols and saving the edited .wasm file
- Tooltips for opcodes in the disassembler

## Compatibility
This extension targets [dnSpyEx v6.1.9](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.1.9) and is not tested with
any other version or distribution. 

## Debugging
Run `build.ps1 netframework` in the dnSpy submodule using the Visual Studio
developer powershell, then use the provided run command for JetBrains Rider.

If you don't use Rider, you can start dnSpy with the `--extension-directory`
parameter to quickly load the extension.

## Installation and more info
For more info regarding dnSpy extensions and their installation, see the README of
[my other extension](https://github.com/HoLLy-HaCKeR/dnSpy.Extension.HoLLy/blob/master/README.md), which is more
up-to-date and complete.
