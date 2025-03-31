
namespace UnityExtensions
{
    /// <summary>
    /// Extensions for bitwise operations.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Returns a Boolean value indicating whether all non-zero bits from a specified target occur within this value.
        /// </summary>
        public static bool ContainBits(this int value, int target) { return (value & target) == target; }


        /// <summary>
        /// Returns a Boolean value indicating whether any non-zero bit from a specified target occurs within this value.
        /// </summary>
        public static bool IntersectBits(this int value1, int value2) { return (value1 & value2) != 0; }


        /// <summary>
        /// Remove all non-zero bits from a specified target in this value.
        /// </summary>
        public static void RemoveBits(this ref int value, int target) { value = value & ~target; }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~7
        /// </summary>
        public static void SetBit0(this ref sbyte value, int bit) { value = (sbyte)(value & (~(1 << bit))); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~7
        /// </summary>
        public static void SetBit0(this ref byte value, int bit) { value = (byte)(value & (~(1u << bit))); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~15
        /// </summary>
        public static void SetBit0(this ref short value, int bit) { value = (short)(value & (~(1 << bit))); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~15
        /// </summary>
        public static void SetBit0(this ref ushort value, int bit) { value = (ushort)(value & (~(1u << bit))); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~31
        /// </summary>
        public static void SetBit0(this ref int value, int bit) { value = value & (~(1 << bit)); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~31
        /// </summary>
        public static void SetBit0(this ref uint value, int bit) { value = value & (~(1u << bit)); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~63
        /// </summary>
        public static void SetBit0(this ref long value, int bit) { value = value & (~(1L << bit)); }


        /// <summary>
        /// Set value of specified binary bit to zero, bit = 0~63
        /// </summary>
        public static void SetBit0(this ref ulong value, int bit) { value = value & (~(1UL << bit)); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~7
        /// </summary>
        public static void SetBit1(this ref sbyte value, int bit) { value = (sbyte)((byte)value | (1u << bit)); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~7
        /// </summary>
        public static void SetBit1(this ref byte value, int bit) { value = (byte)(value | (1u << bit)); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~15
        /// </summary>
        public static void SetBit1(this ref short value, int bit) { value = (short)((ushort)value | (1u << bit)); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~15
        /// </summary>
        public static void SetBit1(this ref ushort value, int bit) { value = (ushort)(value | (1u << bit)); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~31
        /// </summary>
        public static void SetBit1(this ref int value, int bit) { value = value | (1 << bit); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~31
        /// </summary>
        public static void SetBit1(this ref uint value, int bit) { value = value | (1u << bit); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~63
        /// </summary>
        public static void SetBit1(this ref long value, int bit) { value = value | (1L << bit); }


        /// <summary>
        /// Set value of specified binary bit to one, bit = 0~63
        /// </summary>
        public static void SetBit1(this ref ulong value, int bit) { value = value | (1UL << bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~7
        /// </summary>
        public static void SetBit(this ref sbyte value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~7
        /// </summary>
        public static void SetBit(this ref byte value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~15
        /// </summary>
        public static void SetBit(this ref short value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~15
        /// </summary>
        public static void SetBit(this ref ushort value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~31
        /// </summary>
        public static void SetBit(this ref int value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~31
        /// </summary>
        public static void SetBit(this ref uint value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~63
        /// </summary>
        public static void SetBit(this ref long value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Set value of specified binary bit to one or zero, bit = 0~63
        /// </summary>
        public static void SetBit(this ref ulong value, int bit, bool is1) { if (is1) value.SetBit1(bit); else value.SetBit0(bit); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~7
        /// </summary>
        public static void ReverseBit(this ref sbyte value, int bit) { value = (sbyte)(value ^ (1 << bit)); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~7
        /// </summary>
        public static void ReverseBit(this ref byte value, int bit) { value = (byte)(value ^ (1u << bit)); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~15
        /// </summary>
        public static void ReverseBit(this ref short value, int bit) { value = (short)(value ^ (1 << bit)); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~15
        /// </summary>
        public static void ReverseBit(this ref ushort value, int bit) { value = (ushort)(value ^ (1u << bit)); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~31
        /// </summary>
        public static void ReverseBit(this ref int value, int bit) { value = value ^ (1 << bit); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~31
        /// </summary>
        public static void ReverseBit(this ref uint value, int bit) { value = value ^ (1u << bit); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~63
        /// </summary>
        public static void ReverseBit(this ref long value, int bit) { value = value ^ (1L << bit); }


        /// <summary>
        /// Reverse the value of specified binary bit, bit = 0~63
        /// </summary>
        public static void ReverseBit(this ref ulong value, int bit) { value = value ^ (1UL << bit); }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~7
        /// </summary>
        public static bool GetBit(this sbyte value, int bit) { return (value & (1 << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~7
        /// </summary>
        public static bool GetBit(this byte value, int bit) { return (value & (1u << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~15
        /// </summary>
        public static bool GetBit(this short value, int bit) { return (value & (1 << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~15
        /// </summary>
        public static bool GetBit(this ushort value, int bit) { return (value & (1u << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~31
        /// </summary>
        public static bool GetBit(this int value, int bit) { return (value & (1 << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~31
        /// </summary>
        public static bool GetBit(this uint value, int bit) { return (value & (1u << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~63
        /// </summary>
        public static bool GetBit(this long value, int bit) { return (value & (1L << bit)) != 0; }


        /// <summary>
        /// Get the value of specified binary bit, bit = 0~63
        /// </summary>
        public static bool GetBit(this ulong value, int bit) { return (value & (1UL << bit)) != 0; }

    } // class Extensions

} // namespace UnityExtensions