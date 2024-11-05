namespace TestApp
{
    using Hexa.NET.SDL2;
    using HexaGen.Runtime;

    public unsafe class SDLNativeContext : INativeContext
    {
        public nint GetProcAddress(string procName)
        {
            return (nint)SDL.GLGetProcAddress(procName);
        }

        public bool IsExtensionSupported(string extensionName)
        {
            return SDL.GLExtensionSupported(extensionName) == SDLBool.True;
        }

        public bool TryGetProcAddress(string procName, out nint procAddress)
        {
            procAddress = (nint)SDL.GLGetProcAddress(procName);
            return procAddress != 0;
        }

        public void Dispose()
        {
        }
    }
}