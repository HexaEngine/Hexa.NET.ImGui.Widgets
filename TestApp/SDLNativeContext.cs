namespace TestApp
{
    using Hexa.NET.SDL2;
    using HexaGen.Runtime;

    public unsafe class SDLNativeContext : IGLContext
    {
        private readonly SDLWindow* window;
        private readonly SDLGLContext context;

        public SDLNativeContext(SDLWindow* window)
        {
            this.window = window;
            context = SDL.GLCreateContext(window);
        }

        public nint Handle => context.Handle;

        public bool IsCurrent => SDL.GLGetCurrentContext() == context;

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
            SDL.GLDeleteContext(context);
        }

        public void MakeCurrent()
        {
            SDL.GLMakeCurrent(window, context);
        }

        public void SwapBuffers()
        {
            SDL.GLSwapWindow(window);
        }

        public void SwapInterval(int interval)
        {
            SDL.GLSetSwapInterval(interval);
        }
    }
}