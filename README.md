# SortResourceKeys
This short C# code is used to create an application that internally sorts the keys within .resx resource files in the current directory.

Windows comes with a built-in C# compiler. To generate the application, use the command below, invoking the C# compiler (csc.exe) and passing the source code file, Program.cs, as a parameter.

- `%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe /out:SortResourceKeys.exe /target:exe Program.cs`
