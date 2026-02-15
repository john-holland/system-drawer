Barracuda ONNX requires Google.Protobuf to import .onnx models.

To fix "The type or namespace name 'Google' could not be found" errors:

1. Download the Google.Protobuf NuGet package:
   https://www.nuget.org/packages/Google.Protobuf/

2. Get the DLL:
   - Click "Download package" and rename the .nupkg to .zip, then unzip.
   - Or use: dotnet add package Google.Protobuf (then copy from obj or bin).
   - Copy the DLL from lib/netstandard2.0/Google.Protobuf.dll (or netstandard1.1).

3. Place Google.Protobuf.dll in this folder:
   Assets/Plugins/Google.Protobuf/

4. Reopen Unity (or trigger recompile). The BarracudaProtobufPatcher will add
   the reference to Barracuda's ONNX assembly if needed.

If you use NuGetForUnity: Add package Google.Protobuf and ensure the DLL
ends up in a location Unity loads (e.g. this folder).
