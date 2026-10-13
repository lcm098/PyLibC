
// PyLibC/pointers/ptr.cs

using System;
using System.Runtime.InteropServices;

namespace ptr {
    public static unsafe class Pointer {
        private static void Check(long address) {
            if (address == 0)
                throw new NullReferenceException("Null pointer dereference");
        }

        /*
            FOR INT
         */

        public static long AllocInt() {
            void *__ptr__ = NativeMemory.AllocZeroed((nuint) 4);
            return (long)__ptr__;
        }

        public static void WriteInt(long address, int value) {
            Check(address);
            *(int*)address = value;
        }
        public static void FreeInt(long address) {
            NativeMemory.Free((void*)address);
        }

        public static int ReadInt(long address) {
            Check(address);
            return *(int *)address;
        }


        /*
            FOR LONG
         */
        public static long AllocLong() {
            void *__ptr__ = NativeMemory.AllocZeroed((nuint) 8);
            return (long)__ptr__;
        }

        public static void WriteLong(long address, long value) {
            Check(address);
            *(long*)address = value;
        }
        public static void FreeLong(long address) {
            NativeMemory.Free((void*)address);
        }

        public static long ReadLong(long address) {
            Check(address);
            return *(long *)address;
        }


        /*
            Array
        */
        public static long IntArray(nuint size)
        {
            int* array = (int *)NativeMemory.AllocZeroed(size, (nuint)sizeof(int));
            if (array != null)
            {
                return (long)array;   
            } else
            {
                return 0;
            }
        }

        public static void IntFreeArray(long address)
        {
            NativeMemory.Free((void*) address);
        }

        public static long IntLastAddr(long address, nuint size)
        {
            byte* ptr = (byte*)address;
            ptr += (nint) (size * (nuint)sizeof(int) - sizeof(int));
            return (long)ptr;
        }

        /*
            Pointer Arthematic
        */

        public static long AddByte(long address, nint offset)
        {
            byte* ptr = (byte*)address;
            ptr+=offset;
            return (long)ptr;
        }

        public static long SubByte(long address, nint offset)
        {
            byte* ptr = (byte*)address;
            ptr-=offset;
            return (long)ptr;
        }

        /*
                DEREFERENCE
        */

        private static long Walk(long address, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), "level must be >= 1");

            long addr = address;
            for (int i = 1; i < level; i++)
            {
                Check(addr);
                addr = *(long*)addr;
            }
            Check(addr);
            return addr;
        }

        public static int IntDeref(long address, int level)
            => *(int*)Walk(address, level);

        public static void IntDefAss(long address, int level, int value)
            => *(int*)Walk(address, level) = value;

    }
}
