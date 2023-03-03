
namespace UnityExtensions
{
    [System.Flags]
    public enum PlatformMask
    {
        None = 0,

        WindowsEditor = 1 << 0,
        OSXEditor = 1 << 1,
        LinuxEditor = 1 << 2,

        WindowsPlayer = 1 << 4,
        OSXPlayer = 1 << 5,
        LinuxPlayer = 1 << 6,

        PS4 = 1 << 8,
        XboxOne = 1 << 9,
        Switch = 1 << 10,

        iPhone = 1 << 12,
        Android = 1 << 13,

        WSAPlayerX86 = 1 << 14,
        WSAPlayerX64 = 1 << 15,
        WSAPlayerARM = 1 << 16,

        WebGLPlayer = 1 << 18,

        tvOS = 1 << 20,
        Stadia = 1 << 21,
    }

}