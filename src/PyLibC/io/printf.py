# PyLibC/src/PyLibC/pointers/ptr.py

import re
import os
from pathlib import Path
from pythonnet import load
import numpy as np

load("coreclr")

import clr

dll_path = Path(__file__).resolve().parents[1] / "bin" / "PyLibC.dll"
clr.AddReference(str(dll_path))


from Io import IoPrint 
import System



class Io:

    @staticmethod
    def printf(string, *args, end=""):
        
        tokens = re.findall(r'%(i8|u8|i16|u16|u32|i32|u64|i64|f32|f64|isize|usize|char|bool|p)', string)

        IoPrint.printf(string, tokens, end, list(args))
        