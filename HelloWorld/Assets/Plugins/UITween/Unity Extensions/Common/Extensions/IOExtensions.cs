using System;
using System.IO;
using System.Collections.Generic;

namespace UnityExtensions
{
    public static class IOExtensions
    {
        public delegate bool TryParseFunc<T>(string text, out T value);

        struct BinaryIO<T>
        {
            public static Func<BinaryReader, T> read;
            public static Action<BinaryWriter, T> write;
        }

        struct TextIO<T>
        {
            public static Action<TextWriter, T> write;
            public static TryParseFunc<T> tryParse;
        }

        static IOExtensions()
        {
            BinaryIO<int>.read = r => r.ReadInt32();
            BinaryIO<int>.write = (w, v) => w.Write(v);
            TextIO<int>.write = (w, v) => w.Write(v);
            TextIO<int>.tryParse = int.TryParse;

            BinaryIO<float>.read = r => r.ReadSingle();
            BinaryIO<float>.write = (w, v) => w.Write(v);
            TextIO<float>.write = (w, v) => w.Write(v);
            TextIO<float>.tryParse = float.TryParse;

            BinaryIO<ulong>.read = r => r.ReadUInt64();
            BinaryIO<ulong>.write = (w, v) => w.Write(v);
            TextIO<ulong>.write = (w, v) => w.Write(v);
            TextIO<ulong>.tryParse = ulong.TryParse;

            BinaryIO<uint>.read = r => r.ReadUInt32();
            BinaryIO<uint>.write = (w, v) => w.Write(v);
            TextIO<uint>.write = (w, v) => w.Write(v);
            TextIO<uint>.tryParse = uint.TryParse;

            BinaryIO<ushort>.read = r => r.ReadUInt16();
            BinaryIO<ushort>.write = (w, v) => w.Write(v);
            TextIO<ushort>.write = (w, v) => w.Write(v);
            TextIO<ushort>.tryParse = ushort.TryParse;

            BinaryIO<string>.read = r => r.ReadString();
            BinaryIO<string>.write = (w, v) => w.Write(v);
            TextIO<string>.write = (w, v) => w.Write(v);
            TextIO<string>.tryParse = (string t, out string v) => { v = t; return t != null; };

            BinaryIO<sbyte>.read = r => r.ReadSByte();
            BinaryIO<sbyte>.write = (w, v) => w.Write(v);
            TextIO<sbyte>.write = (w, v) => w.Write(v);
            TextIO<sbyte>.tryParse = sbyte.TryParse;

            BinaryIO<long>.read = r => r.ReadInt64();
            BinaryIO<long>.write = (w, v) => w.Write(v);
            TextIO<long>.write = (w, v) => w.Write(v);
            TextIO<long>.tryParse = long.TryParse;

            BinaryIO<DateTime>.read = r => DateTime.FromBinary(r.ReadInt64());
            BinaryIO<DateTime>.write = (w, v) => w.Write(v.ToBinary());
            TextIO<DateTime>.write = (w, v) => w.Write(v.ToLocalTime().ToString());
            TextIO<DateTime>.tryParse = DateTime.TryParse;

            BinaryIO<short>.read = r => r.ReadInt16();
            BinaryIO<short>.write = (w, v) => w.Write(v);
            TextIO<short>.write = (w, v) => w.Write(v);
            TextIO<short>.tryParse = short.TryParse;

            BinaryIO<decimal>.read = r => r.ReadDecimal();
            BinaryIO<decimal>.write = (w, v) => w.Write(v);
            TextIO<decimal>.write = (w, v) => w.Write(v);
            TextIO<decimal>.tryParse = decimal.TryParse;

            BinaryIO<byte>.read = r => r.ReadByte();
            BinaryIO<byte>.write = (w, v) => w.Write(v);
            TextIO<byte>.write = (w, v) => w.Write(v);
            TextIO<byte>.tryParse = byte.TryParse;

            BinaryIO<bool>.read = r => r.ReadBoolean();
            BinaryIO<bool>.write = (w, v) => w.Write(v);
            TextIO<bool>.write = (w, v) => w.Write(v);
            TextIO<bool>.tryParse = bool.TryParse;

            BinaryIO<double>.read = r => r.ReadDouble();
            BinaryIO<double>.write = (w, v) => w.Write(v);
            TextIO<double>.write = (w, v) => w.Write(v);
            TextIO<double>.tryParse = double.TryParse;

            BinaryIO<char>.read = r => r.ReadChar();
            BinaryIO<char>.write = (w, v) => w.Write(v);
            TextIO<char>.write = (w, v) => w.Write(v);
            TextIO<char>.tryParse = char.TryParse;
        }

        /// <summary>
        /// Register a custom type reading function to BinaryReader.
        /// </summary>
        public static void Register<T>(Func<BinaryReader, T> read)
        {
            BinaryIO<T>.read = read;
        }

        /// <summary>
        /// Register a custom type writing function to BinaryWriter.
        /// </summary>
        public static void Register<T>(Action<BinaryWriter, T> write)
        {
            BinaryIO<T>.write = write;
        }

        /// <summary>
        /// Register a custom type writing function to TextWriter.
        /// </summary>
        public static void Register<T>(Action<TextWriter, T> write)
        {
            TextIO<T>.write = write;
        }

        /// <summary>
        /// Register a custom type parsing function.
        /// </summary>
        public static void Register<T>(TryParseFunc<T> tryParse)
        {
            TextIO<T>.tryParse = tryParse;
        }

        /// <summary>
        /// Read a specific type data from the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static T Read<T>(this BinaryReader reader)
        {
            return BinaryIO<T>.read(reader);
        }

        /// <summary>
        /// Write a specific type data to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Write<T>(this BinaryWriter writer, T value)
        {
            BinaryIO<T>.write(writer, value);
        }

        /// <summary>
        /// Write a specific type data to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// Note: DateTime is always converted to Local time when writing.
        /// </summary>
        public static void Write<T>(this TextWriter writer, T value)
        {
            TextIO<T>.write(writer, value);
        }

        /// <summary>
        /// Write a specific type data to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// Note: DateTime is always converted to Local time when writing.
        /// </summary>
        public static void WriteLine<T>(this TextWriter writer, T value)
        {
            TextIO<T>.write(writer, value);
            writer.WriteLine();
        }

        /// <summary>
        /// Try parse a specific type data.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static bool TryParse<T>(this string text, out T value)
        {
            return TextIO<T>.tryParse(text, out value);
        }

        /// <summary>
        /// Read a specific type data list from the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Read<T>(this BinaryReader reader, IList<T> buffer, int index, int count)
        {
            while (count-- > 0)
            {
                buffer[index++] = BinaryIO<T>.read(reader);
            }
        }

        /// <summary>
        /// Write a specific type data list to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Write<T>(this BinaryWriter writer, IList<T> buffer, int index, int count)
        {
            while (count-- > 0)
            {
                BinaryIO<T>.write(writer, buffer[index++]);
            }
        }

        /// <summary>
        /// Read a specific type data array from the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Read<T>(this BinaryReader reader, ref T[] array)
        {
            int count = reader.ReadInt32();
            if (array == null || array.Length != count) array = new T[count];
            reader.Read(array, 0, count);
        }

        /// <summary>
        /// Read a specific type data list from the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Read<T>(this BinaryReader reader, IList<T> list)
        {
            int count = reader.ReadInt32();
            while (count-- > 0)
            {
                list.Add(BinaryIO<T>.read(reader));
            }
        }

        /// <summary>
        /// Write a specific type data list to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Write<T>(this BinaryWriter writer, IList<T> buffer)
        {
            writer.Write(buffer.Count);
            writer.Write(buffer, 0, buffer.Count);
        }

        /// <summary>
        /// Read a specific type data dictionary from the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Read<TKey, TValue>(this BinaryReader reader, IDictionary<TKey, TValue> dictionary)
        {
            int count = reader.ReadInt32();
            while (count-- > 0)
            {
                var key = reader.Read<TKey>();
                var value = reader.Read<TValue>();
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Write a specific type data dictionary to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// </summary>
        public static void Write<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> dictionary)
        {
            writer.Write(dictionary.Count);
            foreach (var pair in dictionary)
            {
                writer.Write<TKey>(pair.Key);
                writer.Write<TValue>(pair.Value);
            }
        }

        /// <summary>
        /// Write a specific type data list to the stream.
        /// Default support numeric types, DateTime and string, you can use Register to register custom types.
        /// Note: DateTime is always converted to Local time when writing.
        /// </summary>
        public static void Write<T>(this TextWriter writer, IList<T> buffer, string separator, int index, int count)
        {
            while (count-- > 0)
            {
                TextIO<T>.write(writer, buffer[index++]);
                if (count > 0) writer.Write(separator);
            }
        }

        /// <summary>
        /// Write an Int32 to the stream.
        /// </summary>
        public static void WriteBinary(this Stream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }

        /// <summary>
        /// Read an Int32 from the stream.
        /// </summary>
        public static int ReadBinaryInt32(this Stream stream)
        {
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();
            return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
        }

    } // class IOExtensions

} // namespace UnityExtensions