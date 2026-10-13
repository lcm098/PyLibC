# PyLibC/src/pointers/ptr.py

import os
from pythonnet import load

load("coreclr")

import clr

dll_path = os.path.abspath("bin/Debug/net10.0/PyLibC.dll")
clr.AddReference(dll_path)

from ptr import Pointer as _CPointer   # C# class, renamed
import System



class Type:
    dtype = {
        "i8": 1, "u8": 1, "i16" : 2, "u16": 2,
        "u32": 4, "i32": 4, "u64": 8, "i64" : 8
    }

class Pointer:

    """
        INT
    """
    @staticmethod
    def ptr_aint(size=4):
        return _CPointer.AllocInt(System.UIntPtr(size))

    @staticmethod
    def ptr_wint(address, value):
        _CPointer.WriteInt(address, value)

    @staticmethod
    def ptr_rint(address):
        return _CPointer.ReadInt(address)

    @staticmethod
    def ptr_fint(address):
        _CPointer.FreeInt(address)

    """
        LONG
    """

    @staticmethod
    def ptr_along(size=8):
        return _CPointer.AllocLong(System.UIntPtr(size))

    @staticmethod
    def ptr_wlong(address, value):
        _CPointer.WriteLong(address, value)

    @staticmethod
    def ptr_rlong(address):
        return _CPointer.ReadLong(address)

    @staticmethod
    def ptr_flong(address):
        _CPointer.FreeLong(address)

    """
        DEREFERENCE
    """
    @staticmethod
    def ptr_idef(address, level):
        return _CPointer.IntDeref(address, level)

    @staticmethod
    def ptr_idef_ass(address, level, value):
        _CPointer.IntDefAss(address, level, value)


    """
        INT ARRAY
    """

    @staticmethod
    def ptr_aint_array(size=1):
        return _CPointer.IntArray(System.UIntPtr(size))

    @staticmethod
    def ptr_fint_array(address):
        _CPointer.IntFreeArray(address)

    @staticmethod
    def ptr_get_last(address, size):
        return _CPointer.IntLastAddr(address, System.UIntPtr(size))

    """
        POINTER ARITHMETIC
    """

    @staticmethod
    def ptr_add_byte(address, offset):
        return _CPointer.AddByte(address, System.IntPtr(offset))


    @staticmethod
    def ptr_sub_byte(address, offset):
        return _CPointer.SubByte(address, System.IntPtr(offset))