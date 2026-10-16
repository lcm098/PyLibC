
using System;
using ptr;

namespace Io
{
    public static unsafe class IoPrint
    {
        public static void printf(string format, string[] types, string end, ulong[] address)
        {
            int last = 0;
            int index = 0;

            foreach (System.Text.RegularExpressions.Match match in
                System.Text.RegularExpressions.Regex.Matches(
                    format,
                    @"%(i8|u8|i16|u16|u32|i32|u64|i64|f32|f64|isize|usize|char|bool|p)"
                ))
            {
                Console.Write(format.Substring(last, match.Index - last));

                string type = types[index];
                ulong addr = address[index];

                object value = type == "p"
                    ? (object)addr
                    : ptr.Pointer.Deref(addr, 1, type);

                Console.Write(value);

                last = match.Index + match.Length;
                index++;
            }

            Console.Write(format.Substring(last));
            Console.Write(end);
        }
    }
}