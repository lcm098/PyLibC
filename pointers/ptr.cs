// PyLibC/pointers/ptr.cs

using System;
using System.Runtime.InteropServices;

namespace ptr {
    public static unsafe class Pointer {

        // ---------------------------------------------------------
        // Private helpers (nuint yahin tak limited hai)
        // ---------------------------------------------------------

        private static void Check(ulong address) {
            if (address == 0)
                throw new NullReferenceException("Null pointer dereference");
        }

        private static nuint SizeOf(string type) => type switch
        {
            "i8"    => (nuint)sizeof(sbyte),
            "i16"   => (nuint)sizeof(short),
            "i32"   => (nuint)sizeof(int),
            "i64"   => (nuint)sizeof(long),

            "u8"    => (nuint)sizeof(byte),
            "u16"   => (nuint)sizeof(ushort),
            "u32"   => (nuint)sizeof(uint),
            "u64"   => (nuint)sizeof(ulong),

            "isize" => (nuint)IntPtr.Size,
            "usize" => (nuint)IntPtr.Size,

            "f32"   => (nuint)sizeof(float),
            "f64"   => (nuint)sizeof(double),

            "bool"  => 1,
            "char"  => 4,

            _ => throw new ArgumentException("Unsupported type", nameof(type))
        };

        private static void* Resolve(ulong address, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be >= 1");

            for (int i = 1; i < level; i++)
            {
                Check(address);
                address = *(ulong*)address;
            }

            Check(address);
            return (void*)address;
        }

        private static nint ToNInt(object value) => value switch
        {
            nint n   => n,
            nuint u  => (nint)u,
            _        => (nint)Convert.ToInt64(value)
        };

        private static nuint ToNUInt(object value) => value switch
        {
            nuint u  => u,
            nint n   => (nuint)n,
            _        => (nuint)Convert.ToUInt64(value)
        };

        // ---------------------------------------------------------
        // Allocation / free
        // ---------------------------------------------------------

        public static ulong ptr_new(object value, string type)
        {
            ulong ptr = (ulong) NativeMemory.AllocZeroed(1, SizeOf(type));
            DerefAss(ptr, 1, value, type);
            return ptr;
        }

        public static ulong ptr_fork(string type)
        {
            return (ulong)NativeMemory.AllocZeroed(1, SizeOf(type));
        }

        public static ulong series_fork(ulong count, string type)
        {
            if (count == 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be > 0");

            return (ulong)NativeMemory.AllocZeroed((nuint)count, SizeOf(type));
        }

        public static void ptr_free(ulong address)
        {
            if (address == 0)
                return;

            NativeMemory.Free((void*)address);
        }

        // ---------------------------------------------------------
        // Read / write
        // ---------------------------------------------------------

        public static dynamic Deref(ulong address, int level, string type)
        {
            void* pointer = Resolve(address, level);

            return type switch
            {
                "i8"    => *(sbyte*)pointer,
                "i16"   => *(short*)pointer,
                "i32"   => *(int*)pointer,
                "i64"   => *(long*)pointer,

                "u8"    => *(byte*)pointer,
                "u16"   => *(ushort*)pointer,
                "u32"   => *(uint*)pointer,
                "u64"   => *(ulong*)pointer,

                // Python ko UIntPtr ki jagah normal int mile
                "isize" => (long)*(nint*)pointer,
                "usize" => (ulong)*(nuint*)pointer,

                "f32"   => *(float*)pointer,
                "f64"   => *(double*)pointer,

                "bool"  => *(byte*)pointer != 0,
                "char"  => *(uint*)pointer,

                _ => throw new ArgumentException("Unsupported type", nameof(type))
            };
        }

        public static void DerefAss(ulong address, int level, object value, string type)
        {
            void* pointer = Resolve(address, level);

            switch (type)
            {
                case "i8":    *(sbyte*)pointer  = Convert.ToSByte(value);   break;
                case "i16":   *(short*)pointer  = Convert.ToInt16(value);   break;
                case "i32":   *(int*)pointer    = Convert.ToInt32(value);   break;
                case "i64":   *(long*)pointer   = Convert.ToInt64(value);   break;

                case "u8":    *(byte*)pointer   = Convert.ToByte(value);    break;
                case "u16":   *(ushort*)pointer = Convert.ToUInt16(value);  break;
                case "u32":   *(uint*)pointer   = Convert.ToUInt32(value);  break;
                case "u64":   *(ulong*)pointer  = Convert.ToUInt64(value);  break;

                case "isize": *(nint*)pointer   = ToNInt(value);            break;
                case "usize": *(nuint*)pointer  = ToNUInt(value);           break;

                case "f32":   *(float*)pointer  = Convert.ToSingle(value);  break;
                case "f64":   *(double*)pointer = Convert.ToDouble(value);  break;

                case "bool":  *(byte*)pointer   = Convert.ToBoolean(value) ? (byte)1 : (byte)0; break;
                case "char":  *(uint*)pointer   = Convert.ToUInt32(value);  break;

                default:
                    throw new ArgumentException("Unsupported type", nameof(type));
            }
        }

        public static void ptr_write(ulong address, string type, object value)
        {
            DerefAss(address, 1, value, type);
        }

        // ---------------------------------------------------------
        // Pointer arithmetic (C-style: count * sizeof(type))
        // ---------------------------------------------------------

        public static ulong ptr_add(ulong address, string type)
        {
            Check((nuint)address);
            return checked(address + (ulong)SizeOf(type));
        }

        public static ulong ptr_sub(ulong address, string type)
        {
            Check((nuint)address);
            ulong offset = checked((ulong)SizeOf(type));
            if (offset > address)
                throw new OverflowException("Pointer subtraction underflow");
            return address - offset;
        }

        public static ulong ptr_last_addr(ulong address, ulong count, string type)
        {
            if (address == 0)
                throw new ArgumentNullException(nameof(address));

            if (count == 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be > 0");

            ulong size = (ulong)SizeOf(type);
            ulong offset = checked((count - 1) * size);

            return checked(address + offset);
        }

        public static ulong list_series_fork(object[] values, string type)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(values), "List must not be empty");

            ulong size = (ulong)SizeOf(type);
            ulong baseAddr = (ulong)NativeMemory.AllocZeroed((nuint)values.Length, (nuint)size);

            try
            {
                for (int i = 0; i < values.Length; i++)
                    DerefAss(baseAddr + (ulong)i * size, 1, values[i], type);
            }
            catch
            {
                // value galat ho (jaise "abc" i32 mein) toh memory leak na ho
                NativeMemory.Free((void*)baseAddr);
                throw;
            }

            return baseAddr;
        }
    }
}