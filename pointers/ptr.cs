
// PyLibC/pointers/ptr.cs

using System;
using System.Runtime.InteropServices;

namespace ptr {
    public static unsafe class Pointer {
        private static void Check(nuint address) {
            if (address == 0)
                throw new NullReferenceException("Null pointer dereference");
        }

        public static dynamic Deref(nuint address, int level, string type)
        {
            if (level < 1) {
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be >= 1");
            }


            for (int i=1; i<level; i++)
            {
                Check(address);
                address = *(nuint *) address;
            }

            Check(address);
            void* pointer = (void*)address;

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

                "isize" => *(nint*)pointer,
                "usize" => *(nuint*)pointer,

                "f32"   => *(float*)pointer,
                "f64"   => *(double*)pointer,

                "bool"  => *(byte*)pointer != 0,
                "char"  => *(uint*)pointer,

                _ => throw new ArgumentException("Unsupported type", nameof(type))
            };
        }

        private static void* Resolve(nuint address, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be >= 1");

            for (int i = 1; i < level; i++)
            {
                Check(address);
                address = *(nuint*)address;
            }

            Check(address);
            return (void*)address;
        }

        public static void DerefAss(nuint address, int level, object value, string type)
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

        public static void ptr_free(nuint address)
        {
            if (address == 0)
                return;

            NativeMemory.Free((void*)address);
        }

        public static nuint ptr_fork(string type)
        {
            nuint bytes = type switch
            {
                "i8"    => (nuint)sizeof(sbyte),
                "i16"   => (nuint)sizeof(short),
                "i32"   => (nuint)sizeof(int),
                "i64"   => (nuint)sizeof(long),
                "i128"  => 16,

                "u8"    => (nuint)sizeof(byte),
                "u16"   => (nuint)sizeof(ushort),
                "u32"   => (nuint)sizeof(uint),
                "u64"   => (nuint)sizeof(ulong),
                "u128"  => 16,

                "isize" => (nuint)IntPtr.Size,
                "usize" => (nuint)IntPtr.Size,

                "f32"   => (nuint)sizeof(float),
                "f64"   => (nuint)sizeof(double),

                "bool"  => 1,
                "char"  => 4,

                _ => throw new ArgumentException("Unsupported type", nameof(type))
            };

            void* natPointer = NativeMemory.Alloc(bytes);
            if (natPointer == null) {
                throw new OutOfMemoryException();
            }
            return (nuint)natPointer;
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

        public static void ptr_write(nuint address, string type, object value)
        {
            if (address == 0)
                throw new NullReferenceException("Null pointer");

            void* pointer = (void*)address;

            switch (type)
            {
                case "i8":    *(sbyte*)pointer = Convert.ToSByte(value); break;
                case "i16":   *(short*)pointer = Convert.ToInt16(value); break;
                case "i32":   *(int*)pointer = Convert.ToInt32(value); break;
                case "i64":   *(long*)pointer = Convert.ToInt64(value); break;

                case "i128":
                    throw new NotSupportedException("i128 requires a custom 128-bit implementation.");

                case "u8":    *(byte*)pointer = Convert.ToByte(value); break;
                case "u16":   *(ushort*)pointer = Convert.ToUInt16(value); break;
                case "u32":   *(uint*)pointer = Convert.ToUInt32(value); break;
                case "u64":   *(ulong*)pointer = Convert.ToUInt64(value); break;

                case "u128":
                    throw new NotSupportedException("u128 requires a custom 128-bit implementation.");

                case "isize": *(nint*)pointer = ToNInt(value); break;
                case "usize": *(nuint*)pointer = ToNUInt(value); break;

                case "f32":   *(float*)pointer = Convert.ToSingle(value); break;
                case "f64":   *(double*)pointer = Convert.ToDouble(value); break;

                case "bool":  *(byte*)pointer = Convert.ToBoolean(value) ? (byte)1 : (byte)0; break;

                case "char":
                    *(uint*)pointer = Convert.ToUInt32(value);
                    break;

                default:
                    throw new ArgumentException("Unsupported type", nameof(type));
            }
        }
    }
}
