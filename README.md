# PyLibC Documentation

*A C-style memory and pointer API for Python, powered by .NET.*

---

## Table of Contents

1. Overview
2. Installation
3. Quickstart
4. Core Concepts
5. API Reference: `Type`
6. API Reference: `Pointer`
7. API Reference: `Io`
8. Format Specifiers
9. Exceptions
10. Recipes
11. Safety Notes and Known Limitations
12. Package Layout

---

## 1. Overview

**PyLibC** brings the C memory model to Python. It lets you allocate raw, unmanaged memory, read and write typed values through numeric addresses, walk memory with C-style pointer arithmetic, follow multi-level pointers (`**p`, `***p`), and print values straight from memory with a `printf`-style function.

Under the hood, PyLibC is a thin Python layer over a compiled C# library (`PyLibC.dll`) that uses `NativeMemory` and `unsafe` pointers. Python talks to it through [pythonnet](https://github.com/pythonnet/pythonnet) on the CoreCLR runtime.

### Why PyLibC?

- **Real memory, real addresses.** Every pointer is a plain Python `int` holding an actual address.
- **Fixed-width types.** Rust-style type names (`i8` ... `u64`, `f32`, `f64`, `isize`, `usize`, `bool`, `char`) remove platform ambiguity.
- **C semantics.** Pointer arithmetic advances by `sizeof(type)`; allocations are zero-initialised; memory stays alive until *you* free it.
- **NumPy friendly.** Copy a NumPy array into native memory in one call.

```python
from PyLibC import Pointer, Io, Type

p = Pointer.ptr_new(42, Type.ptype["i32"])
Io.printf("value = %i32\n", p)
Pointer.ptr_free(p)
```

```
value = 42
```

> **Warning:** PyLibC gives you the power *and* the danger of C. There is no bounds checking, no garbage collection and no type tracking. See Section 11.

---

## 2. Installation

### Requirements

| Requirement | Notes |
| --- | --- |
| Python | 3.8 or newer recommended |
| .NET runtime | A CoreCLR runtime (.NET 6 or newer) must be installed |
| `pythonnet` | Bridge between Python and .NET |
| `numpy` | Required for `numpy_series_fork` (imported at package load) |
| `PyLibC.dll` | The compiled C# library, located in `PyLibC/bin/` |

```bash
pip install pythonnet numpy
```

Then install PyLibC itself (from source, or from your package index once published):

```bash
pip install .
```

### Verifying the install

```python
from PyLibC import Pointer, Type

p = Pointer.ptr_fork(Type.ptype["i32"])
print(Pointer.deref(p, 1, Type.ptype["i32"]))   # 0  (memory is zero-initialised)
Pointer.ptr_free(p)
```

If this prints `0`, the Python layer, the runtime and the DLL are all working.

---

## 3. Quickstart

### 3.1 A single value

```python
from PyLibC import Pointer, Type

p = Pointer.ptr_new(3.14, Type.ptype["f64"])     # allocate + initialise
print(Pointer.deref(p, 1, Type.ptype["f64"]))    # 3.14

Pointer.write(p, 2.71, Type.ptype["f64"])        # overwrite
print(Pointer.deref(p, 1, Type.ptype["f64"]))    # 2.71

Pointer.ptr_free(p)                              # always free
```

### 3.2 An array

```python
from PyLibC import Pointer, Type

arr = Pointer.list_series_fork([10, 20, 30, 40], Type.ptype["i32"])

p = arr
for _ in range(4):
    print(Pointer.deref(p, 1, Type.ptype["i32"]))
    p = Pointer.ptr_add(p, Type.ptype["i32"])     # p += sizeof(i32)

Pointer.ptr_free(arr)
```

```
10
20
30
40
```

### 3.3 A pointer to a pointer

```python
from PyLibC import Pointer, Type

x  = Pointer.ptr_new(99, Type.ptype["i32"])   # int x = 99;
pp = Pointer.ptr_new(x,  Type.ptype["u64"])   # stores the address of x

print(Pointer.deref(pp, 1, Type.ptype["u64"]) == x)   # True  (*pp  is the address of x)
print(Pointer.deref(pp, 2, Type.ptype["i32"]))        # 99    (**pp is the value)

Pointer.ptr_free(pp)
Pointer.ptr_free(x)
```

---

## 4. Core Concepts

### 4.1 Addresses are integers

Every function that returns a pointer returns a Python `int` containing the raw memory address. Every function that accepts a pointer takes such an `int`. The value `0` is the null pointer.

```python
p = Pointer.ptr_fork(Type.ptype["i32"])
print(hex(p))        # e.g. 0x1f4a2c0
```

### 4.2 Type strings

PyLibC does not track what type lives at an address. **You** supply a type string with each read, write or arithmetic call, exactly as you would cast in C. The supported types are:

Never type a type string by hand. Always take it from `Type.ptype`, for example `Type.ptype["i32"]`; every example in this document does so.

| Type | C equivalent | Size (bytes) | Python value |
| --- | --- | --- | --- |
| `i8` | `int8_t` | 1 | `int` |
| `i16` | `int16_t` | 2 | `int` |
| `i32` | `int32_t` | 4 | `int` |
| `i64` | `int64_t` | 8 | `int` |
| `u8` | `uint8_t` | 1 | `int` |
| `u16` | `uint16_t` | 2 | `int` |
| `u32` | `uint32_t` | 4 | `int` |
| `u64` | `uint64_t` | 8 | `int` |
| `isize` | `intptr_t` / `ssize_t` | pointer size (8 on 64-bit) | `int` |
| `usize` | `uintptr_t` / `size_t` | pointer size (8 on 64-bit) | `int` |
| `f32` | `float` | 4 | `float` |
| `f64` | `double` | 8 | `float` |
| `bool` | `bool` | 1 | `bool` |
| `char` | `char32_t` (code point) | 4 | `int` |

> **Note:** `char` is a 4-byte unsigned integer holding a Unicode code point. Reading it returns an `int`, not a one-character string. Use `chr(...)` in Python if you need the character.

### 4.3 Pointer arithmetic

Arithmetic follows C rules: adding to a pointer moves it by `sizeof(type)` bytes, not by one byte.

```python
p  = Pointer.series_fork(5, Type.ptype["i32"])
p1 = Pointer.ptr_add(p, Type.ptype["i32"])     # p + 4 bytes
p2 = Pointer.ptr_add(p1, Type.ptype["i32"])    # p + 8 bytes
```

### 4.4 Indirection levels

The `level` argument of `deref` and `deref_ass` mirrors the number of `*` in C:

| `level` | C expression | Behaviour |
| --- | --- | --- |
| `1` | `*p` | Read or write the value at `address` |
| `2` | `**p` | Read an address stored at `address`, then access the value there |
| `3` | `***p` | Follow two stored addresses, then access the value |

At every hop except the last, PyLibC reads a 64-bit address, so intermediate pointers must be stored as `u64` (or `usize`).

### 4.5 Ownership

Memory returned by `ptr_new`, `ptr_fork`, `series_fork`, `list_series_fork` and `numpy_series_fork` is **owned by you**. Python will never free it. Release it with `ptr_free` exactly once.

---

## 5. API Reference: `Type`

```python
from PyLibC import Type
```

A small lookup helper holding type metadata. It is not instantiated; use its class attributes directly.

### `Type.dtype`

`dict[str, int]` mapping each type string to its size in bytes.

```python
>>> Type.dtype[Type.ptype["i32"]]
4
>>> Type.dtype[Type.ptype["f64"]]
8
```

### `Type.ptype`

`dict[str, str]` mapping each valid type string to itself. Use it as a convenient namespace of valid type names (`Type.ptype["i32"]` gives `"i32"`) or to validate user input (`name in Type.ptype`).

```python
>>> Type.ptype["i32"]
'i32'
>>> "i32" in Type.ptype
True
>>> "int" in Type.ptype
False
```

> **Note:** `Type.dtype` uses a fixed 8 bytes for `isize` and `usize`. The native library uses the real pointer size of the current process. On 64-bit systems (the norm) these agree.

---

## 6. API Reference: `Pointer`

```python
from PyLibC import Pointer
```

All methods are static. `address` is always a Python `int`; `type` is always taken from Type.ptype (Section 4.2), e.g. Type.ptype\["i32"\].

### 6.1 Allocation and release

#### `Pointer.ptr_new(value, type)`

Allocate memory for one element of `type`, store `value` in it and return its address.

**Parameters**

- **value** *(int, float, bool)*: Initial value. It is converted to `type`.
- **type** *(str)*: Element type.

**Returns**

- **address** *(int)*: Address of the new element.

**Raises**

- `ArgumentException` if `type` is unsupported.
- `OverflowException` or `InvalidCastException` if `value` cannot be converted to `type`.

**Example**

```python
>>> p = Pointer.ptr_new(7, Type.ptype["u8"])
>>> Pointer.deref(p, 1, Type.ptype["u8"])
7
>>> Pointer.ptr_free(p)
```

---

#### `Pointer.ptr_fork(type)`

Allocate one zero-initialised element of `type` and return its address. Equivalent to `calloc(1, sizeof(type))`.

**Parameters**

- **type** *(str)*: Element type.

**Returns**

- **address** *(int)*

**Example**

```python
>>> p = Pointer.ptr_fork(Type.ptype["f32"])
>>> Pointer.deref(p, 1, Type.ptype["f32"])
0.0
>>> Pointer.ptr_free(p)
```

---

#### `Pointer.series_fork(size, type)`

Allocate a contiguous, zero-initialised block of `size` elements. Equivalent to `calloc(size, sizeof(type))`.

**Parameters**

- **size** *(int)*: Number of elements. Must be greater than 0.
- **type** *(str)*: Element type.

**Returns**

- **address** *(int)*: Address of the first element.

**Raises**

- `ArgumentOutOfRangeException` if `size == 0`.

**Example**

```python
>>> arr = Pointer.series_fork(1000, Type.ptype["u8"])
>>> Pointer.deref(arr, 1, Type.ptype["u8"])
0
>>> Pointer.ptr_free(arr)
```

---

#### `Pointer.list_series_fork(array, type)`

Allocate a block sized to `array` and copy each item into it, converted to `type`.

**Parameters**

- **array** *(list)*: Non-empty sequence of values.
- **type** *(str)*: Element type for every item.

**Returns**

- **address** *(int)*: Address of the first element.

**Raises**

- `ArgumentOutOfRangeException` if `array` is empty or `None`.
- Conversion errors if an item does not fit `type`. In that case the partially filled block is freed automatically, so nothing leaks.

**Example**

```python
>>> arr = Pointer.list_series_fork([1.5, 2.5, 3.5], Type.ptype["f64"])
>>> Pointer.deref(Pointer.ptr_add(arr, Type.ptype["f64"]), 1, Type.ptype["f64"])
2.5
>>> Pointer.ptr_free(arr)
```

---

#### `Pointer.numpy_series_fork(np_array, type)`

Flatten a NumPy array (row-major order) and copy it into native memory.

**Parameters**

- **np\_array** *(numpy.ndarray)*: Array of any shape. It is flattened with `ravel()`.
- **type** *(str)*: Element type for the native copy.

**Returns**

- **address** *(int)*: Address of the first element.

**Example**

```python
>>> import numpy as np
>>> a = np.array([[1, 2], [3, 4]], dtype=np.int32)
>>> p = Pointer.numpy_series_fork(a, Type.ptype["i32"])
>>> Pointer.deref(Pointer.ptr_add(p, Type.ptype["i32"]), 1, Type.ptype["i32"])
2
>>> Pointer.ptr_free(p)
```

**Notes**

The data is *copied*. Changing the native memory does not change the NumPy array, and vice versa.

---

#### `Pointer.ptr_free(address)`

Release memory previously allocated by PyLibC. Passing `0` does nothing, like `free(NULL)`.

**Parameters**

- **address** *(int)*: Address returned by an allocation function.

**Returns**

- `None`

**Notes**

Freeing the same address twice, or freeing an address that PyLibC did not allocate, is undefined behaviour and can crash the interpreter.

---

### 6.2 Reading and writing

#### `Pointer.deref(address, level, type)`

Read a value from memory.

**Parameters**

- **address** *(int)*: Starting address.
- **level** *(int)*: Indirection depth, `>= 1` (see Section 4.4).
- **type** *(str)*: Type of the *final* value being read.

**Returns**

- **value** *(int, float, bool)*: The value, converted to a native Python type.

**Raises**

- `ArgumentOutOfRangeException` if `level < 1`.
- `NullReferenceException` if a null address is met at any hop.
- `ArgumentException` if `type` is unsupported.

**Example**

```python
>>> p = Pointer.ptr_new(-5, Type.ptype["i16"])
>>> Pointer.deref(p, 1, Type.ptype["i16"])
-5
>>> Pointer.deref(p, 1, Type.ptype["u16"])      # same bits, read as unsigned
65531
>>> Pointer.ptr_free(p)
```

---

#### `Pointer.deref_ass(address, level, value, type)`

Write a value through a pointer, following `level - 1` stored addresses first. ("Deref-assign": `**p = value` in C.)

**Parameters**

- **address** *(int)*: Starting address.
- **level** *(int)*: Indirection depth, `>= 1`.
- **value**: New value, converted to `type`.
- **type** *(str)*: Type of the final location.

**Raises**

- Same as `deref`, plus conversion errors for `value`.

**Example**

```python
>>> x  = Pointer.ptr_new(1, Type.ptype["i32"])
>>> pp = Pointer.ptr_new(x, Type.ptype["u64"])
>>> Pointer.deref_ass(pp, 2, 500, Type.ptype["i32"])   # **pp = 500;
>>> Pointer.deref(x, 1, Type.ptype["i32"])
500
```

---

#### `Pointer.write(address, value, type)`

Write `value` at `address` (indirection level 1). Shorthand for `deref_ass(address, 1, value, type)`.

**Example**

```python
>>> p = Pointer.ptr_fork(Type.ptype["bool"])
>>> Pointer.write(p, True, Type.ptype["bool"])
>>> Pointer.deref(p, 1, Type.ptype["bool"])
True
```

> **Note:** the argument order differs from `deref_ass`: `write(address, value, type)` versus `deref_ass(address, level, value, type)`.

---

### 6.3 Pointer arithmetic

#### `Pointer.ptr_add(address, type)`

Return `address + sizeof(type)`: the address of the *next* element.

**Raises**

- `NullReferenceException` if `address == 0`.
- `OverflowException` on address overflow.

**Example**

```python
>>> arr = Pointer.series_fork(3, Type.ptype["i64"])
>>> Pointer.ptr_add(arr, Type.ptype["i64"]) - arr
8
```

---

#### `Pointer.ptr_sub(address, type)`

Return `address - sizeof(type)`: the address of the *previous* element.

**Raises**

- `NullReferenceException` if `address == 0`.
- `OverflowException` if the result would be below zero.

---

#### `Pointer.ptr_last_addr(address, size, type)`

Return the address of the last element in a block of `size` elements starting at `address`: `address + (size - 1) * sizeof(type)`.

**Parameters**

- **address** *(int)*: Address of the first element.
- **size** *(int)*: Number of elements, `> 0`.
- **type** *(str)*: Element type.

**Raises**

- `ArgumentNullException` if `address == 0`.
- `ArgumentOutOfRangeException` if `size == 0`.

**Example**

```python
>>> arr  = Pointer.list_series_fork([1, 2, 3], Type.ptype["i32"])
>>> last = Pointer.ptr_last_addr(arr, 3, Type.ptype["i32"])
>>> Pointer.deref(last, 1, Type.ptype["i32"])
3
>>> Pointer.ptr_free(arr)
```

---

## 7. API Reference: `Io`

```python
from PyLibC import Io
```

### `Io.printf(string, *args, end="")`

Print a formatted string, reading each value **directly from memory**.

Unlike Python's `print` or `%` formatting, the arguments are not values but **addresses**. Each format specifier in `string` names the type to read, and the matching address argument says where to read it from.

**Parameters**

- **string** *(str)*: Format string containing zero or more specifiers (Section 8).
- **\*args** *(int)*: One address per specifier, in order.
- **end** *(str, optional)*: Text appended after the output. Defaults to `""` (no newline; include `\n` in `string` or pass `end="\n"`).

**Returns**

- `None`. Output is written to standard output.

**Raises**

- `IndexOutOfRangeException` if there are fewer addresses than specifiers.
- `NullReferenceException` if an address is `0`.

**Examples**

```python
>>> a = Pointer.ptr_new(10, Type.ptype["i32"])
>>> b = Pointer.ptr_new(2.5, Type.ptype["f64"])
>>> Io.printf("a=%i32, b=%f64\n", a, b)
a=10, b=2.5
```

Printing an address with `%p`:

```python
>>> Io.printf("pointer is %p\n", a)
pointer is 140234567890
```

Printing a whole array:

```python
arr = Pointer.list_series_fork([1, 2, 3], Type.ptype["i32"])
p = arr
for _ in range(3):
    Io.printf("%i32 ", p)
    p = Pointer.ptr_add(p, Type.ptype["i32"])
Io.printf("\n")
```

```
1 2 3
```

**Notes**

- `%p` prints the *address itself* (as a decimal integer), not the value stored there.
- Values are rendered by .NET, so `bool` prints as `True`/`False`, `char` prints its numeric code point, and floating-point output follows the current culture's formatting.
- There is no escape for a literal specifier; text such as `%i32` in your string is always treated as a specifier.

---

## 8. Format Specifiers

| Specifier | Reads as | Output |
| --- | --- | --- |
| `%i8` `%i16` `%i32` `%i64` | signed integer | decimal integer |
| `%u8` `%u16` `%u32` `%u64` | unsigned integer | decimal integer |
| `%isize` `%usize` | pointer-sized integer | decimal integer |
| `%f32` `%f64` | floating point | decimal number |
| `%bool` | boolean | `True` or `False` |
| `%char` | 4-byte code point | decimal integer |
| `%p` | *(no read)* | the address itself, as an integer |

The specifier name is exactly the type string used by `Pointer`.

---

## 9. Exceptions

Errors raised inside the native library surface in Python as the matching .NET exception types (via pythonnet). Catch `System.Exception` subclasses imported from `System`, or catch Python's `Exception`.

| Exception | Raised when |
| --- | --- |
| `NullReferenceException` | A null address is dereferenced or used in arithmetic |
| `ArgumentException` | The type string is not supported |
| `ArgumentOutOfRangeException` | `level < 1`, or a count/list is empty |
| `ArgumentNullException` | `ptr_last_addr` receives address `0` |
| `OverflowException` | A value does not fit its type, or address arithmetic overflows or underflows |
| `IndexOutOfRangeException` | `Io.printf` has fewer addresses than specifiers |

```python
try:
    Pointer.deref(0, 1, Type.ptype["i32"])
except Exception as e:
    print("caught:", e)       # caught: Null pointer dereference
```

---

## 10. Recipes

### 10.1 Sum an array

```python
from PyLibC import Pointer, Type

n   = 5
arr = Pointer.list_series_fork([1, 2, 3, 4, 5], Type.ptype["i32"])

total, p = 0, arr
for _ in range(n):
    total += Pointer.deref(p, 1, Type.ptype["i32"])
    p = Pointer.ptr_add(p, Type.ptype["i32"])

print(total)    # 15
Pointer.ptr_free(arr)
```

### 10.2 Reverse an array in place

```python
from PyLibC import Pointer, Type

n   = 4
arr = Pointer.list_series_fork([1, 2, 3, 4], Type.ptype["i32"])

lo, hi = arr, Pointer.ptr_last_addr(arr, n, Type.ptype["i32"])
while lo < hi:
    a = Pointer.deref(lo, 1, Type.ptype["i32"])
    b = Pointer.deref(hi, 1, Type.ptype["i32"])
    Pointer.write(lo, b, Type.ptype["i32"])
    Pointer.write(hi, a, Type.ptype["i32"])
    lo = Pointer.ptr_add(lo, Type.ptype["i32"])
    hi = Pointer.ptr_sub(hi, Type.ptype["i32"])

# arr is now [4, 3, 2, 1]
Pointer.ptr_free(arr)
```

### 10.3 Swap through pointers

```python
from PyLibC import Pointer, Type

def swap(pa, pb, t):
    tmp = Pointer.deref(pa, 1, t)
    Pointer.write(pa, Pointer.deref(pb, 1, t), t)
    Pointer.write(pb, tmp, t)

x = Pointer.ptr_new(1, Type.ptype["i32"])
y = Pointer.ptr_new(2, Type.ptype["i32"])
swap(x, y, Type.ptype["i32"])
print(Pointer.deref(x, 1, Type.ptype["i32"]), Pointer.deref(y, 1, Type.ptype["i32"]))   # 2 1

Pointer.ptr_free(x); Pointer.ptr_free(y)
```

### 10.4 Safe cleanup with `try/finally`

```python
p = Pointer.series_fork(1024, Type.ptype["u8"])
try:
    ...  # use p
finally:
    Pointer.ptr_free(p)
```

### 10.5 Reinterpreting bytes (type punning)

```python
>>> p = Pointer.ptr_new(1.0, Type.ptype["f32"])
>>> hex(Pointer.deref(p, 1, Type.ptype["u32"]))
'0x3f800000'
>>> Pointer.ptr_free(p)
```

---

## 11. Safety Notes and Known Limitations

### Safety

1. **No bounds checking.** Reading or writing past the end of a block can corrupt memory or crash Python. Track your own element counts.
2. **No type tracking.** PyLibC trusts the `type` you pass. Reading an `i32` slot as `f64` returns garbage (and may read past the allocation).
3. **No automatic freeing.** Forgetting `ptr_free` leaks memory; freeing twice, or using an address after freeing it, is undefined behaviour.
4. **Only free what PyLibC allocated.** Never pass addresses from other libraries, or addresses produced by `ptr_add` (they point into a block, not to its start).
5. **Only the null address is checked.** Other invalid addresses are not detected and will crash the process on access.
6. **Not thread-safe.** Synchronise access yourself if multiple threads share memory.

### Known limitations

- Multi-level `deref`/`deref_ass` assume intermediate pointers are stored as 64-bit addresses.
- `Io.printf` takes addresses only (no literal values), has no width/precision flags and no `%%` escape.
- `ptr_add` / `ptr_sub` move by exactly one element; for larger jumps, call them repeatedly or compute `address + n * Type.dtype[type]` in Python.
- `Type.dtype` hard-codes 8 bytes for `isize`/`usize`.
- `numpy_series_fork` and `list_series_fork` copy through Python lists, so very large arrays are slow.
- `PyLibC.dll` must be present in `PyLibC/bin/` and a .NET (CoreCLR) runtime must be installed.

---

## 12. Package Layout

```
PyLibC/
├── __init__.py            # exports Pointer, Type, Io
├── pointers/
│   └── ptr.py             # Python wrapper: Type, Pointer
├── io/
│   └── printf.py          # Python wrapper: Io
└── bin/
    └── PyLibC.dll         # compiled C# core (ptr.Pointer, Io.IoPrint)
```

**Native core (C#)**

| Namespace | Class | Role |
| --- | --- | --- |
| `ptr` | `Pointer` | Allocation (`NativeMemory`), typed read/write, pointer arithmetic |
| `Io` | `IoPrint` | Format-string parsing and printing from memory |

**Python layer**

The wrappers load the CoreCLR runtime with `pythonnet.load("coreclr")`, reference `PyLibC.dll`, and expose the native methods as static Python methods. Public names exported by the package:

```python
__all__ = ["Pointer", "Type", "Io"]
```
