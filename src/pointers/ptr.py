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
        "u32": 4, "i32": 4, "u64": 8, "i64" : 8,
        "f32": 4, "f64": 8, "isize": 8, "usize": 8,
        "char":4, "bool": 1
    }

    ptype = {
        "i8": "i8", "u8": "u8", "i16" : "i16", "u16": "u16",
        "u32": "u32", "i32": "i32", "u64": "u64", "i64" : "i64",
        "f32": "f32", "f64": "f64", "isize": "isize", "char":"char",
        "bool": "bool", "usize":"usize"
    }

class Pointer:

    @staticmethod
    def ptr_fork(type:str):
        return _CPointer.ptr_fork(type)

    @staticmethod
    def write(address, value, type):
        _CPointer.ptr_write(address, type, value)

    @staticmethod
    def deref(address, level, type):
        return _CPointer.Deref(address, level, type)

    @staticmethod
    def deref_ass(address, level, value, type):
        return _CPointer.DerefAss(address, level, value, type)

    @staticmethod
    def ptr_free(address):
        _CPointer.ptr_free(address)