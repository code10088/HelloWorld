using System;

namespace TTSDK
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class TTVersionAttribute : Attribute
    {
        public string MinTTContainerAndroidVersion { get; set; }

        public int MinAndroidOSVersion { get; set; }

        public string WebGLMethod { get; set; }
        
        public bool IsSupportWebGL { get; set; }
    }
}