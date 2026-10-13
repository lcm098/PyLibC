<div align="center">

# 🧭 PyLibC · Pointer API

### C-style raw memory for Python, with every call mapped to the C you already know

`Python` ⇄ `pythonnet (coreclr)` ⇄ `C# unsafe` ⇄ `NativeMemory (calloc / free)`

![python](https://img.shields.io/badge/python-3.10%2B-3776AB?logo=python&logoColor=white)
![dotnet](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![memory](https://img.shields.io/badge/memory-unmanaged-critical)
![parity](https://img.shields.io/badge/C%20parity-documented-success)

</div>

---

## 📑 Table of Contents

1. [Overview](#1--overview)
2. [Installation and Setup](#2--installation-and-setup)
3. [Mental Model: Python `int` = C pointer](#3--mental-model-python-int--c-pointer)
4. [Type and Size Mapping](#4--type-and-size-mapping)
5. [Master Mapping Table: API ↔ C](#5--master-mapping-table-api--c)
6. [API Reference](#6--api-reference)
   - [6.1 Int](#61-int)
   - [6.2 Long](#62-long)
   - [6.3 Multi-level Dereference](#63-multi-level-dereference)
   - [6.4 Int Arrays](#64-int-arrays)
   - [6.5 Pointer Arithmetic](#65-pointer-arithmetic)
7. [Pointer Arithmetic Deep Dive: C scaling vs byte offsets](#7--pointer-arithmetic-deep-dive-c-scaling-vs-byte-offsets)
8. [Behavior Parity Matrix: where we match C, and where we don't](#8--behavior-parity-matrix)
9. [Worked Programs: C and Python side by side](#9--worked-programs-c-and-python-side-by-side)
10. [Proving the Match: a parity test you can run](#10--proving-the-match-a-parity-test-you-can-run)
11. [Recipes: building `arr[i]` and `sizeof`](#11--recipes-building-arri-and-sizeof)
12. [Common Bugs: in C and in this API](#12--common-bugs-in-c-and-in-this-api)
13. [Cheat Sheet](#13--cheat-sheet)
14. [Error Reference](#14--error-reference)
15. [FAQ](#15--faq)
16. [Glossary](#16--glossary)

---

## 1. ✨ Overview

`PyLibC.Pointer` lets Python programs do what C programs do with raw memory:

| Capability | C idiom it mirrors |
|---|---|
| 🧱 Allocate and free zeroed blocks | `calloc` / `free` |
| ✍️ Read and write typed values at an address | `*p = v;` and `v = *p;` |
| 🪜 Follow several pointer hops in one call | `*p`, `**p`, `***p` |
| 📦 Contiguous `int` arrays | `calloc(n, sizeof(int))`, `&arr[n-1]` |
| ➕ Move an address by a byte offset | `(char *)p + offset` |

### Design principles

1. **Same vocabulary as C.** If you can write it in C, you can write it here, and this document shows the C line next to every Python line.
2. **Addresses are plain integers.** A pointer is a Python `int`; `0` is `NULL`.
3. **Explicit ownership.** You allocate, you free. Nothing is garbage collected.
4. **Safer than C in one place only:** null addresses raise an exception instead of being undefined behavior. Everything else is as unforgiving as C. See [section 8](#8--behavior-parity-matrix).

> [!WARNING]
> This is **unmanaged memory**. There is no garbage collector, no bounds checking, no double-free detection, and no use-after-free detection. A wrong non-null address crashes the whole process, exactly like a segmentation fault in C.

---

## 2. 🛠 Installation and Setup

### 2.1 Enable unsafe code in `PyLibC.csproj`

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

### 2.2 Build the DLL

```bash
dotnet build
```

Produces `bin/Debug/net10.0/PyLibC.dll`.

### 2.3 Install pythonnet

```bash
pip install pythonnet
```

### 2.4 Import

```python
from src.pointers.ptr import Pointer
```

> [!NOTE]
> `dll_path` is resolved relative to the **current working directory**. Run scripts from the project root (`python -m src.Test.Test`) or anchor the path to the file:
> ```python
> dll_path = os.path.abspath(
>     os.path.join(os.path.dirname(__file__), "../../bin/Debug/net10.0/PyLibC.dll")
> )
> ```

> [!TIP]
> After **every** C# change, run `dotnet build` again. Python keeps loading the old DLL otherwise.

### 2.5 Why the C# class is imported as `_CPointer`

```python
from ptr import Pointer as _CPointer   # the C# class
class Pointer:                         # the Python wrapper
    ...
```

Both are called `Pointer`. If you import the C# class without an alias, the Python class overwrites it and you get `AttributeError: type object 'Pointer' has no attribute 'AllocInt'`.

---

## 3. 🧠 Mental Model: Python `int` = C pointer

In C, a pointer is a variable holding a memory address. In this API, the **same thing is a Python `int`** holding that address.

```c
int *score_ptr = calloc(1, sizeof(int));   // score_ptr holds e.g. 0x1000
```

```python
score_ptr = Pointer.ptr_aint()             # score_ptr holds e.g. 0x1000 (a Python int)
```

### 3.1 The layers

```text
┌────────────────────────────────────────────────────────────┐
│ Your Python code        Pointer.ptr_wint(score_ptr, 5)     │
├────────────────────────────────────────────────────────────┤
│ ptr.py wrapper          _CPointer.WriteInt(address, value) │
├────────────────────────────────────────────────────────────┤
│ pythonnet (coreclr)     Python int  ⇄  C# long / int       │
├────────────────────────────────────────────────────────────┤
│ ptr.cs  (unsafe C#)     *(int*)address = value;            │
├────────────────────────────────────────────────────────────┤
│ NativeMemory            calloc / free  (the C heap)        │
└────────────────────────────────────────────────────────────┘
```

The last line of `WriteInt` is literally the C statement `*(int *)address = value;`.

### 3.2 What a pointer chain looks like

The naming convention used throughout this document: `int_ptr` is an `int *`, `int_pptr` is an `int **`, `int_ppptr` is an `int ***`. The number of `p`s equals the number of `*`s you need to reach the int.

```text
 int_ppptr = 0x3000        int_pptr = 0x2000         int_ptr = 0x1000
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│ address 0x3000  │       │ address 0x2000  │       │ address 0x1000  │
│ holds  0x2000   │ ────▶ │ holds  0x1000   │ ────▶ │ holds  102      │
│ (8 bytes, long) │       │ (8 bytes, long) │       │ (4 bytes, int)  │
└─────────────────┘       └─────────────────┘       └─────────────────┘

 *int_ppptr   == int_pptr  (0x2000)
 **int_ppptr  == int_ptr   (0x1000)
 ***int_ppptr == 102
```

### 3.3 Memory is bytes, little-endian

On x86-64 and ARM64 (the platforms .NET 10 targets for desktop and server), integers are stored least-significant byte first. This matters if you ever inspect memory byte by byte.

| Value | Type | Bytes in memory (low address → high) |
|---|---|---|
| `102` | `int` | `66 00 00 00` |
| `-111` | `int` | `91 FF FF FF` |
| `0x2000` | `long` | `00 20 00 00 00 00 00 00` |

---

## 4. 📐 Type and Size Mapping

| C type | Size | This API | C# type used inside |
|---|---|---|---|
| `int` | 4 bytes (all mainstream platforms) | `ptr_*int*` family | `int` |
| `long long` / `int64_t` | 8 bytes | `ptr_*long*` family | `long` |
| `long` | **8 on Linux/macOS, 4 on Windows** | `ptr_*long*` family (always 8) | `long` |
| `int *`, `int **`, any pointer | 8 bytes on 64-bit | stored in a `long` slot | `long` |
| `size_t` (allocation size) | 8 bytes | Python `int` → `nuint` | `nuint` |
| `ptrdiff_t` (byte offset) | 8 bytes | Python `int` → `nint` | `nint` |
| `NULL` | `0` | Python `0` | `0` |

> [!IMPORTANT]
> The API's `long` is **always 64-bit**, like C's `long long`. On Linux and macOS that also equals C's `long`. On Windows, C's `long` is 32-bit, so use `long long` in your Windows C code when comparing.

### 4.1 `size` means different things in different functions

| Function | `size` is measured in | C analogue |
|---|---|---|
| `ptr_aint(size)` | **bytes** | `calloc(1, size)` |
| `ptr_along(size)` | **bytes** | `calloc(1, size)` |
| `ptr_aint_array(size)` | **`int` elements** | `calloc(count, sizeof(int))` |
| `ptr_get_last(address, size)` | **`int` elements** | `&arr[count - 1]` |
| `ptr_add_byte(address, offset)` | **bytes** | `(char *)p + offset` |

---

## 5. 🗺 Master Mapping Table: API ↔ C

Variable names here are the same ones used in the C and Python examples later in this document.

| Python call | Equivalent C | Reads as |
|---|---|---|
| `score_ptr = Pointer.ptr_aint()` | `int *score_ptr = calloc(1, sizeof(int));` | allocate one zeroed int |
| `Pointer.ptr_wint(score_ptr, 90)` | `*score_ptr = 90;` | store an int |
| `Pointer.ptr_rint(score_ptr)` | `*score_ptr` | load an int |
| `Pointer.ptr_fint(score_ptr)` | `free(score_ptr);` | release the block |
| `slot_ptr = Pointer.ptr_along()` | `long long *slot_ptr = calloc(1, sizeof(long long));` | allocate one zeroed 64-bit slot |
| `Pointer.ptr_wlong(slot_ptr, v)` | `*slot_ptr = v;` | store a 64-bit value / address |
| `Pointer.ptr_rlong(slot_ptr)` | `*slot_ptr` | load a 64-bit value / address |
| `Pointer.ptr_flong(slot_ptr)` | `free(slot_ptr);` | release the block |
| `Pointer.ptr_idef(int_ppptr, 3)` | `***int_ppptr` | read through 3 levels |
| `Pointer.ptr_idef_ass(int_ppptr, 3, v)` | `***int_ppptr = v;` | write through 3 levels |
| `scores_base = Pointer.ptr_aint_array(n)` | `int *scores_base = calloc(n, sizeof(int));` | allocate zeroed `int[n]` |
| `Pointer.ptr_get_last(scores_base, n)` | `scores_base + n - 1` (same as `&scores_base[n-1]`) | address of last element |
| `Pointer.ptr_fint_array(scores_base)` | `free(scores_base);` | release the array |
| `Pointer.ptr_add_byte(p, k)` | `(int *)((char *)p + k)` | move forward `k` bytes |
| `Pointer.ptr_sub_byte(p, k)` | `(int *)((char *)p - k)` | move backward `k` bytes |

---

## 6. 📚 API Reference

Every function is a `@staticmethod` on the `Pointer` class. Each entry follows the same template: **signature → C equivalent → parameters → returns → behavior versus C → example in both languages**.

---

### 6.1 Int

#### `ptr_aint(size=4) → int`

> Allocates a zero-initialized block of `size` **bytes** and returns its address.

**C equivalent**

```c
int *score_ptr = calloc(1, size);      // default size == sizeof(int) == 4
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `size` | `int` | `4` | Block size in **bytes** |

**Returns:** address of the block, or `0` if the allocator failed.

**Behavior versus C**

| Aspect | C `calloc` | `ptr_aint` | Match |
|---|---|---|---|
| Memory is zeroed | yes | yes (`AllocZeroed`) | ✅ |
| Returns `NULL` on failure | yes | returns `0` | ✅ |
| Contents when read before write | `0` | `0` | ✅ |
| Compare with `malloc` | `malloc` leaves garbage | not applicable, always zeroed | ⚠️ closer to `calloc` |

**Example**

```c
// C
int *score_ptr = calloc(1, sizeof(int));
printf("%d\n", *score_ptr);
free(score_ptr);
```

```python
# Python
score_ptr = Pointer.ptr_aint()
print(Pointer.ptr_rint(score_ptr))
Pointer.ptr_fint(score_ptr)
```

Both print `0`.

---

#### `ptr_wint(address, value) → None`

> Writes a 32-bit signed integer at `address`.

**C equivalent**

```c
*(int *)address = value;               // or simply:  *score_ptr = value;
```

| Parameter | Type | Description |
|---|---|---|
| `address` | `int` | Destination address |
| `value` | `int` | Value to store (must fit in a signed 32-bit int) |

**Raises:** `NullReferenceException` if `address == 0`.

**Behavior versus C**

| Aspect | C | `ptr_wint` | Match |
|---|---|---|---|
| Stores 4 bytes at the address | yes | yes | ✅ |
| Writing through `NULL` | undefined behavior (usually a crash) | throws `NullReferenceException` | 🛡 safer |
| Writing through a dangling or out-of-range address | undefined behavior | undefined behavior, usually a fatal `AccessViolationException` | ✅ same |
| Value too large for `int` | silently converted by C | rejected by pythonnet at the call boundary, no silent wrap | ⚠️ differs |

**Example**

```c
// C
int *score_ptr = calloc(1, sizeof(int));
*score_ptr = 90;
```

```python
# Python
score_ptr = Pointer.ptr_aint()
Pointer.ptr_wint(score_ptr, 90)
```

---

#### `ptr_rint(address) → int`

> Reads a 32-bit signed integer from `address`.

**C equivalent**

```c
int current_score = *(int *)address;   // or simply:  *score_ptr
```

**Raises:** `NullReferenceException` if `address == 0`.

**Behavior versus C:** identical load; the only difference is the defined exception on a null address.

```c
// C
int current_score = *score_ptr;
```

```python
# Python
current_score = Pointer.ptr_rint(score_ptr)
```

---

#### `ptr_fint(address) → None`

> Frees a block allocated by `ptr_aint`.

**C equivalent**

```c
free(address);
```

**Behavior versus C**

| Aspect | C `free` | `ptr_fint` | Match |
|---|---|---|---|
| `free(NULL)` | no-op | no-op (no null check here) | ✅ |
| Double free | undefined behavior | undefined behavior | ✅ same |
| Free a pointer not from the allocator, or one moved into the middle of a block | undefined behavior | undefined behavior | ✅ same |
| Use after free | undefined behavior | undefined behavior | ✅ same |

```c
// C
free(score_ptr);
score_ptr = NULL;        // good habit
```

```python
# Python
Pointer.ptr_fint(score_ptr)
score_ptr = 0            # good habit: Python's NULL
```

---

### 6.2 Long

The long family mirrors the int family but is 8 bytes wide. Its main job is to hold **addresses**, which makes pointer-to-pointer chains possible.

#### `ptr_along(size=8) → int`

**C equivalent:** `long long *slot_ptr = calloc(1, size);` (default `size == sizeof(long long) == 8`). Zeroed, returns `0` on failure.

#### `ptr_wlong(address, value) → None`

**C equivalent:** `*(long long *)address = value;`
**Raises:** `NullReferenceException` if `address == 0`.

#### `ptr_rlong(address) → int`

**C equivalent:** `*(long long *)address`
**Raises:** `NullReferenceException` if `address == 0`.

#### `ptr_flong(address) → None`

**C equivalent:** `free(address);`

**Example: storing an address inside a slot**

```c
// C
int  player_score = 7;
int *int_ptr      = &player_score;      // int_ptr holds the address of player_score
int **int_pptr    = &int_ptr;           // int_pptr holds the address of int_ptr

printf("%d\n", **int_pptr);             // 7
```

```python
# Python
player_score_addr = Pointer.ptr_aint()                  # the int itself
Pointer.ptr_wint(player_score_addr, 7)

int_ptr  = player_score_addr                            # int *   (address of the int)

int_pptr = Pointer.ptr_along()                          # int **  (address of a slot ...)
Pointer.ptr_wlong(int_pptr, int_ptr)                    # ... that holds int_ptr

print(Pointer.ptr_idef(int_pptr, 2))                    # 7

Pointer.ptr_flong(int_pptr)
Pointer.ptr_fint(player_score_addr)
```

> [!NOTE]
> In C, `&int_ptr` hands you the address of a variable that already exists on the stack. Python has no addressable variables, so here you **allocate a slot** with `ptr_along` and write the inner address into it. The memory layout that results is the same.

---

### 6.3 Multi-level Dereference

#### `ptr_idef(address, level) → int`

> Follows `level - 1` pointer hops through memory, then reads a 32-bit `int`.

**C equivalent**

| `level` | C expression | Meaning |
|---|---|---|
| `1` | `*int_ptr` | the address points straight at the int |
| `2` | `**int_pptr` | pointer → pointer → int |
| `3` | `***int_ppptr` | pointer → pointer → pointer → int |
| `n` | `*…*p` with `n` stars | `n - 1` hops of 8 bytes each, then a 4-byte read |

| Parameter | Type | Description |
|---|---|---|
| `address` | `int` | The outermost pointer value (type `int` followed by `level` stars) |
| `level` | `int` | Number of dereferences, must be `>= 1` |

**Raises:** `ArgumentOutOfRangeException` if `level < 1`; `NullReferenceException` if the starting address or **any hop** is `0`.

**Algorithm (C#, simplified)**

```csharp
long current_addr = address;
for (int hop = 1; hop < level; hop++) {
    Check(current_addr);                  // null check
    current_addr = *(long *)current_addr; // one dereference of an int* / int** / ...
}
Check(current_addr);
return *(int *)current_addr;              // final dereference reads the int
```

**Behavior versus C**

| Aspect | C | `ptr_idef` | Match |
|---|---|---|---|
| Result of `***p` on a valid chain | the int | the int | ✅ |
| Intermediate pointer is `NULL` | undefined behavior | `NullReferenceException` | 🛡 safer |
| Intermediate pointer is garbage | undefined behavior | fatal `AccessViolationException` | ✅ same |
| Type safety (is the last hop really an `int`?) | compiler checks types | **no check**, you must keep levels right | ⚠️ differs |

**Example**

```c
// C
int   player_score = 102;
int  *int_ptr      = &player_score;
int **int_pptr     = &int_ptr;
int ***int_ppptr   = &int_pptr;

printf("%d\n", ***int_ppptr);   // 102
```

```python
# Python
player_score_addr = Pointer.ptr_aint()
Pointer.ptr_wint(player_score_addr, 102)
int_ptr = player_score_addr                      # int *

int_pptr = Pointer.ptr_along()                   # int **
Pointer.ptr_wlong(int_pptr, int_ptr)

int_ppptr = Pointer.ptr_along()                  # int ***
Pointer.ptr_wlong(int_ppptr, int_pptr)

print(Pointer.ptr_idef(int_ppptr, 3))            # 102
```

**Dry run of `ptr_idef(0x3000, 3)`** (using the addresses from [section 3.2](#32-what-a-pointer-chain-looks-like)):

| Step | `current_addr` before | Operation | `current_addr` after |
|---|---|---|---|
| start | `0x3000` | none | `0x3000` |
| hop 1 | `0x3000` | `current_addr = *(long *)0x3000` | `0x2000` |
| hop 2 | `0x2000` | `current_addr = *(long *)0x2000` | `0x1000` |
| read | `0x1000` | `return *(int *)0x1000` | **`102`** |

---

#### `ptr_idef_ass(address, level, value) → None`

> Walks exactly like `ptr_idef`, then **writes** a 32-bit `int` at the final address.

**C equivalent**

```c
***int_ppptr = value;      // for level == 3
```

Both functions share one internal walk, so a given `level` always lands on the same address in both.

```c
// C
***int_ppptr = -111;
printf("%d\n", ***int_ppptr);   // -111
```

```python
# Python
Pointer.ptr_idef_ass(int_ppptr, 3, -111)
print(Pointer.ptr_idef(int_ppptr, 3))   # -111
```

> [!CAUTION]
> If `level` is one too small, the write lands on an **intermediate pointer slot** and overwrites its low 4 bytes with your value. The next read follows a corrupted address and crashes. This is the same class of bug as writing through the wrong number of stars in C.

---

### 6.4 Int Arrays

#### `ptr_aint_array(size=1) → int`

> Allocates a zero-initialized array of `size` **`int` elements** (`size × 4` bytes).

**C equivalent**

```c
int *scores_base = calloc(size, sizeof(int));
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `size` | `int` | `1` | Number of `int` elements |

**Returns:** the base address, or `0` on failure. **Always check for `0`.**

**Behavior versus C**

| Aspect | C `calloc(n, sizeof(int))` | `ptr_aint_array(n)` | Match |
|---|---|---|---|
| Zeroed | yes | yes | ✅ |
| Overflow of `n * 4` detected | yes (returns `NULL`) | yes (two-argument `AllocZeroed`) | ✅ |
| Failure result | `NULL` | `0` | ✅ |
| `n == 0` | implementation-defined (may be `NULL` or a unique pointer) | same (delegates to the C allocator) | ✅ |

```c
// C
int *scores_base = calloc(5, sizeof(int));
if (scores_base == NULL) { /* handle failure */ }
```

```python
# Python
scores_base = Pointer.ptr_aint_array(5)
if scores_base == 0:
    raise MemoryError("could not allocate scores array")
```

---

#### `ptr_get_last(address, size) → int`

> Returns the address of the **last element** of an `int` array. It is the last element itself, not one past it.

**C equivalent**

```c
int *last_score_ptr = scores_base + size - 1;     // identical to &scores_base[size - 1]
```

Formula used inside: `address + size × 4 − 4` bytes.

| Parameter | Type | Description |
|---|---|---|
| `address` | `int` | Base address of the array |
| `size` | `int` | Number of elements |

> [!CAUTION]
> Do not call this with `size == 0`. The formula yields `address − 4`, an address **before** the array, and C has no valid equivalent for it either.

> [!NOTE]
> Because the result is the last element itself, a forward walk must use `cursor <= last_score_ptr`. In C, the same trap appears when people write `p < last` instead of `p <= last`, or compare against one-past-the-end (`scores_base + n`) with `<`. Both styles are valid as long as the comparison matches the end pointer you chose.

```c
// C
int *last_score_ptr = scores_base + 5 - 1;
```

```python
# Python
last_score_ptr = Pointer.ptr_get_last(scores_base, 5)
```

---

#### `ptr_fint_array(address) → None`

> Frees an array allocated by `ptr_aint_array`. Pass the **original base address**.

**C equivalent:** `free(scores_base);`

Same rules as `free`: free once, free the original pointer, never touch the block afterwards.

---

### 6.5 Pointer Arithmetic

#### `ptr_add_byte(address, offset) → int`

> Returns `address + offset`, with `offset` measured in **bytes**.

**C equivalent**

```c
(int *)((char *)address + offset)
```

| Parameter | Type | Description |
|---|---|---|
| `address` | `int` | Starting address |
| `offset` | `int` | Bytes to add (may be negative) |

---

#### `ptr_sub_byte(address, offset) → int`

> Returns `address − offset`, with `offset` measured in **bytes**.

**C equivalent**

```c
(int *)((char *)address - offset)
```

> [!NOTE]
> `ptr_sub_byte` is documented from the Python wrapper (`_CPointer.SubByte`). Make sure the matching `SubByte` method exists in `ptr.cs`.

**Key difference from C pointer arithmetic**

C scales by the pointee type. This API never scales. `ptr_add_byte` is the equivalent of casting to `char *` first. See the full explanation in [section 7](#7--pointer-arithmetic-deep-dive-c-scaling-vs-byte-offsets).

```c
// C   (typed pointer, automatically scaled by sizeof(int))
int *third_score_ptr = scores_base + 2;
```

```python
# Python   (bytes: element_index * INT_SIZE)
INT_SIZE = 4
third_score_ptr = Pointer.ptr_add_byte(scores_base, 2 * INT_SIZE)
```

---

## 7. ➕ Pointer Arithmetic Deep Dive: C scaling vs byte offsets

### 7.1 The rule in C

When you add `k` to a typed pointer, C moves it by `k × sizeof(*pointer)` bytes.

```c
int  *scores_base = ...;           // 0x2000
scores_base + 1;                   // 0x2004   (+1 element = +4 bytes)
scores_base + 3;                   // 0x200C   (+3 elements = +12 bytes)

char *raw_base = (char *)scores_base;
raw_base + 1;                      // 0x2001   (+1 byte)
raw_base + 12;                     // 0x200C
```

### 7.2 The rule here

`ptr_add_byte` and `ptr_sub_byte` always behave like the **`char *`** case.

| Goal | C | This API |
|---|---|---|
| Address of element `i` | `scores_base + i` | `ptr_add_byte(scores_base, i * INT_SIZE)` |
| Next element | `cursor++` | `cursor = ptr_add_byte(cursor, INT_SIZE)` |
| Previous element | `cursor--` | `cursor = ptr_sub_byte(cursor, INT_SIZE)` |
| Skip 3 elements | `cursor += 3` | `cursor = ptr_add_byte(cursor, 3 * INT_SIZE)` |
| Move 1 raw byte | `(char *)cursor + 1` | `ptr_add_byte(cursor, 1)` |

### 7.3 Address table for a 5-element int array at `0x2000`

| Index | C expression | Address | `ptr_add_byte(scores_base, ...)` |
|---|---|---|---|
| 0 | `&scores_base[0]` | `0x2000` | `0` |
| 1 | `&scores_base[1]` | `0x2004` | `4` |
| 2 | `&scores_base[2]` | `0x2008` | `8` |
| 3 | `&scores_base[3]` | `0x200C` | `12` |
| 4 | `&scores_base[4]` | `0x2010` | `16` |
| (end) | `scores_base + 5` | `0x2014` | `20` (one-past-the-end, never dereference) |

### 7.4 Pointer comparison

Addresses are Python integers, so `<`, `<=`, `==`, `!=`, `>`, `>=` compare them directly, the same as comparing pointers in C.

```c
while (cursor <= last_score_ptr) { ... }
```

```python
while cursor <= last_score_ptr:
    ...
```

> [!NOTE]
> C only defines pointer comparison within one array (or one past its end). Python integers compare any two addresses without complaint. Treat that freedom as a trap, not a feature.

### 7.5 Pointer difference

C's `end - start` gives an **element count**. With addresses as integers, you divide the byte difference yourself:

```c
ptrdiff_t element_count = last_score_ptr - scores_base;       // elements
```

```python
element_count = (last_score_ptr - scores_base) // INT_SIZE    # elements
```

---

## 8. ⚖️ Behavior Parity Matrix

Legend: ✅ identical to C · 🛡 safer than C (defined error instead of undefined behavior) · ⚠️ differs from C

| # | Behavior | C | PyLibC | |
|---|---|---|---|---|
| 1 | Allocation is zeroed | `calloc` yes, `malloc` no | always zeroed | ✅ (matches `calloc`) |
| 2 | Allocation failure signal | `NULL` | `0` | ✅ |
| 3 | Read or write through null | UB (usually segfault) | `NullReferenceException` | 🛡 |
| 4 | Null hop in a multi-level deref | UB | `NullReferenceException` | 🛡 |
| 5 | `free(NULL)` | no-op | no-op | ✅ |
| 6 | Double free | UB | UB (may abort the process) | ✅ |
| 7 | Use after free | UB | UB | ✅ |
| 8 | Out-of-bounds read/write | UB | UB (no bounds check) | ✅ |
| 9 | Wild non-null address | segfault | fatal `AccessViolationException`, **not catchable** in Python | ✅ |
| 10 | Pointer arithmetic scaling | scaled by pointee type | **never scaled**, bytes only | ⚠️ |
| 11 | Pointer type system | compiler-checked | none, every address is an `int` | ⚠️ |
| 12 | Values that overflow `int` | silently converted | rejected at the call boundary | ⚠️ |
| 13 | `long` width | platform-dependent | always 64-bit | ⚠️ (equals `long long`) |
| 14 | Pointer comparison across unrelated blocks | UB | works, returns a bool | ⚠️ |
| 15 | Pointer values are printable numbers | `%p` | native Python `int` / `hex()` | ✅ |
| 16 | Dereference level checking | types enforce it | runtime `level >= 1` check only | ⚠️ |
| 17 | Stack variables and `&x` | available | not available, you allocate slots instead | ⚠️ |

### 8.1 What "UB" costs you here

In C, undefined behavior might appear to work. Here it generally follows the same pattern: a wrong but mapped address silently corrupts data, an unmapped one kills the process with a fatal `AccessViolationException`. You will not get a Python traceback for that one.

### 8.2 Printing addresses like `%p`

```c
printf("%p\n", (void *)scores_base);
```

```python
print(hex(scores_base))
```

---

## 9. 🧪 Worked Programs: C and Python side by side

Each program is shown in C, then in Python, followed by the output (identical for both) and a dry run.

Shared constants for every Python example:

```python
from src.pointers.ptr import Pointer

INT_SIZE = 4
```

---

### 9.1 Program 1: Counter

**C**

```c
#include <stdio.h>
#include <stdlib.h>

int main(void) {
    int *visit_counter = calloc(1, sizeof(int));
    *visit_counter = 102;
    printf("%d\n", *visit_counter);
    free(visit_counter);
    return 0;
}
```

**Python**

```python
visit_counter = Pointer.ptr_aint()
Pointer.ptr_wint(visit_counter, 102)
print(Pointer.ptr_rint(visit_counter))
Pointer.ptr_fint(visit_counter)
```

**Output (both)**

```text
102
```

**Dry run**

| Step | C | Python | Memory at `0x1000` |
|---|---|---|---|
| 1 | `calloc(1, sizeof(int))` | `ptr_aint()` | `00 00 00 00` |
| 2 | `*visit_counter = 102` | `ptr_wint(visit_counter, 102)` | `66 00 00 00` |
| 3 | `*visit_counter` | `ptr_rint(visit_counter)` | unchanged, yields `102` |
| 4 | `free(visit_counter)` | `ptr_fint(visit_counter)` | released |

---

### 9.2 Program 2: Three-level pointer chain

**C**

```c
#include <stdio.h>

int main(void) {
    int player_score = 102;

    int   *int_ptr   = &player_score;
    int  **int_pptr  = &int_ptr;
    int ***int_ppptr = &int_pptr;

    printf("%d\n", ***int_ppptr);
    ***int_ppptr = -111;
    printf("%d\n", ***int_ppptr);
    return 0;
}
```

**Python**

```python
player_score_addr = Pointer.ptr_aint()
Pointer.ptr_wint(player_score_addr, 102)

int_ptr = player_score_addr                 # int *

int_pptr = Pointer.ptr_along()              # int **
Pointer.ptr_wlong(int_pptr, int_ptr)

int_ppptr = Pointer.ptr_along()             # int ***
Pointer.ptr_wlong(int_ppptr, int_pptr)

try:
    print(Pointer.ptr_idef(int_ppptr, 3))
    Pointer.ptr_idef_ass(int_ppptr, 3, -111)
    print(Pointer.ptr_idef(int_ppptr, 3))
finally:
    Pointer.ptr_flong(int_ppptr)
    Pointer.ptr_flong(int_pptr)
    Pointer.ptr_fint(player_score_addr)
```

**Output (both)**

```text
102
-111
```

**Dry run**

| Statement | C meaning | Python meaning | State |
|---|---|---|---|
| allocate score | `int player_score` | `player_score_addr = ptr_aint()` | `0x1000: 0` |
| write 102 | `= 102` | `ptr_wint(0x1000, 102)` | `0x1000: 102` |
| `int_ptr` | `&player_score` | `int_ptr = 0x1000` | none |
| `int_pptr` slot | `&int_ptr` | `ptr_along()` → `0x2000`, write `0x1000` | `0x2000: 0x1000` |
| `int_ppptr` slot | `&int_pptr` | `ptr_along()` → `0x3000`, write `0x2000` | `0x3000: 0x2000` |
| read level 3 | `***int_ppptr` | `ptr_idef(0x3000, 3)` | `102` |
| write level 3 | `***int_ppptr = -111` | `ptr_idef_ass(0x3000, 3, -111)` | `0x1000: -111` |
| read level 3 | `***int_ppptr` | `ptr_idef(0x3000, 3)` | `-111` |

---

### 9.3 Program 3: Fill and print an array

**C**

```c
#include <stdio.h>
#include <stdlib.h>

#define SCORE_COUNT 5

int main(void) {
    int *scores_base = calloc(SCORE_COUNT, sizeof(int));
    if (scores_base == NULL) return 1;

    for (int index = 0; index < SCORE_COUNT; index++) {
        scores_base[index] = (index + 1) * 10;
    }
    for (int index = 0; index < SCORE_COUNT; index++) {
        printf("%d\n", scores_base[index]);
    }

    free(scores_base);
    return 0;
}
```

**Python**

```python
SCORE_COUNT = 5

scores_base = Pointer.ptr_aint_array(SCORE_COUNT)
if scores_base == 0:
    raise MemoryError("could not allocate scores array")

try:
    for index in range(SCORE_COUNT):
        element_addr = Pointer.ptr_add_byte(scores_base, index * INT_SIZE)
        Pointer.ptr_wint(element_addr, (index + 1) * 10)

    for index in range(SCORE_COUNT):
        element_addr = Pointer.ptr_add_byte(scores_base, index * INT_SIZE)
        print(Pointer.ptr_rint(element_addr))
finally:
    Pointer.ptr_fint_array(scores_base)
```

**Output (both)**

```text
10
20
30
40
50
```

**Dry run** (`scores_base = 0x2000`)

| `index` | C: `scores_base[index]` | Python: `element_addr` | Value written |
|---|---|---|---|
| 0 | `*(0x2000)` | `0x2000` | `10` |
| 1 | `*(0x2004)` | `0x2004` | `20` |
| 2 | `*(0x2008)` | `0x2008` | `30` |
| 3 | `*(0x200C)` | `0x200C` | `40` |
| 4 | `*(0x2010)` | `0x2010` | `50` |

---

### 9.4 Program 4: Pointer-walking loop and sum

**C**

```c
#include <stdio.h>
#include <stdlib.h>

#define SCORE_COUNT 5

int main(void) {
    int *scores_base     = calloc(SCORE_COUNT, sizeof(int));
    int *last_score_ptr  = scores_base + SCORE_COUNT - 1;
    int *cursor;
    int  next_value = 10;
    int  total      = 0;

    for (cursor = scores_base; cursor <= last_score_ptr; cursor++) {
        *cursor = next_value;
        next_value += 10;
    }
    for (cursor = scores_base; cursor <= last_score_ptr; cursor++) {
        total += *cursor;
    }

    printf("total=%d\n", total);
    free(scores_base);
    return 0;
}
```

**Python**

```python
SCORE_COUNT = 5

scores_base    = Pointer.ptr_aint_array(SCORE_COUNT)
last_score_ptr = Pointer.ptr_get_last(scores_base, SCORE_COUNT)
next_value     = 10
total          = 0

if scores_base != 0:
    try:
        cursor = scores_base
        while cursor <= last_score_ptr:
            Pointer.ptr_wint(cursor, next_value)
            next_value += 10
            cursor = Pointer.ptr_add_byte(cursor, INT_SIZE)     # cursor++

        cursor = scores_base
        while cursor <= last_score_ptr:
            total += Pointer.ptr_rint(cursor)
            cursor = Pointer.ptr_add_byte(cursor, INT_SIZE)     # cursor++

        print(f"total={total}")
    finally:
        Pointer.ptr_fint_array(scores_base)                     # the base, not cursor
```

**Output (both)**

```text
total=150
```

**Dry run of the write loop** (`scores_base = 0x2000`, `last_score_ptr = 0x2010`)

| Check | `cursor` | `cursor <= last_score_ptr` | Action | `next_value` after |
|---|---|---|---|---|
| 1 | `0x2000` | ✅ | write `10` | 20 |
| 2 | `0x2004` | ✅ | write `20` | 30 |
| 3 | `0x2008` | ✅ | write `30` | 40 |
| 4 | `0x200C` | ✅ | write `40` | 50 |
| 5 | `0x2010` | ✅ | write `50` | 60 |
| 6 | `0x2014` | ❌ | loop ends | none |

> [!CAUTION]
> `cursor < last_score_ptr` would skip the final element, in C and in Python alike.

---

### 9.5 Program 5: Reverse an array in place (two cursors)

**C**

```c
#include <stdio.h>
#include <stdlib.h>

#define NUMBER_COUNT 5

int main(void) {
    int *numbers_base = calloc(NUMBER_COUNT, sizeof(int));
    for (int index = 0; index < NUMBER_COUNT; index++)
        numbers_base[index] = index + 1;            // 1 2 3 4 5

    int *left_cursor  = numbers_base;
    int *right_cursor = numbers_base + NUMBER_COUNT - 1;

    while (left_cursor < right_cursor) {
        int saved_left_value = *left_cursor;
        *left_cursor  = *right_cursor;
        *right_cursor = saved_left_value;
        left_cursor++;
        right_cursor--;
    }

    for (int index = 0; index < NUMBER_COUNT; index++)
        printf("%d ", numbers_base[index]);
    printf("\n");

    free(numbers_base);
    return 0;
}
```

**Python**

```python
NUMBER_COUNT = 5

numbers_base = Pointer.ptr_aint_array(NUMBER_COUNT)
if numbers_base == 0:
    raise MemoryError("could not allocate numbers array")

try:
    for index in range(NUMBER_COUNT):
        element_addr = Pointer.ptr_add_byte(numbers_base, index * INT_SIZE)
        Pointer.ptr_wint(element_addr, index + 1)           # 1 2 3 4 5

    left_cursor  = numbers_base
    right_cursor = Pointer.ptr_get_last(numbers_base, NUMBER_COUNT)

    while left_cursor < right_cursor:
        saved_left_value  = Pointer.ptr_rint(left_cursor)
        saved_right_value = Pointer.ptr_rint(right_cursor)
        Pointer.ptr_wint(left_cursor,  saved_right_value)
        Pointer.ptr_wint(right_cursor, saved_left_value)
        left_cursor  = Pointer.ptr_add_byte(left_cursor,  INT_SIZE)   # left_cursor++
        right_cursor = Pointer.ptr_sub_byte(right_cursor, INT_SIZE)   # right_cursor--

    reversed_values = [
        Pointer.ptr_rint(Pointer.ptr_add_byte(numbers_base, index * INT_SIZE))
        for index in range(NUMBER_COUNT)
    ]
    print(" ".join(str(value) for value in reversed_values))
finally:
    Pointer.ptr_fint_array(numbers_base)
```

**Output (both)**

```text
5 4 3 2 1
```

**Dry run** (`numbers_base = 0x2000`)

| Round | `left_cursor` | `right_cursor` | `left < right` | Action | Array after |
|---|---|---|---|---|---|
| 1 | `0x2000` | `0x2010` | ✅ | swap `1 ↔ 5` | `[5, 2, 3, 4, 1]` |
| 2 | `0x2004` | `0x200C` | ✅ | swap `2 ↔ 4` | `[5, 4, 3, 2, 1]` |
| 3 | `0x2008` | `0x2008` | ❌ | stop (cursors met) | `[5, 4, 3, 2, 1]` |

---

### 9.6 Program 6: Swap two ints through pointers

**C**

```c
#include <stdio.h>
#include <stdlib.h>

void swap_ints(int *first_ptr, int *second_ptr) {
    int saved_first_value = *first_ptr;
    *first_ptr  = *second_ptr;
    *second_ptr = saved_first_value;
}

int main(void) {
    int *width_ptr  = calloc(1, sizeof(int));
    int *height_ptr = calloc(1, sizeof(int));
    *width_ptr  = 3;
    *height_ptr = 7;

    printf("before: %d %d\n", *width_ptr, *height_ptr);
    swap_ints(width_ptr, height_ptr);
    printf("after:  %d %d\n", *width_ptr, *height_ptr);

    free(width_ptr);
    free(height_ptr);
    return 0;
}
```

**Python**

```python
def swap_ints(first_addr, second_addr):
    saved_first_value = Pointer.ptr_rint(first_addr)
    Pointer.ptr_wint(first_addr, Pointer.ptr_rint(second_addr))
    Pointer.ptr_wint(second_addr, saved_first_value)

width_addr  = Pointer.ptr_aint()
height_addr = Pointer.ptr_aint()
Pointer.ptr_wint(width_addr, 3)
Pointer.ptr_wint(height_addr, 7)

try:
    print(f"before: {Pointer.ptr_rint(width_addr)} {Pointer.ptr_rint(height_addr)}")
    swap_ints(width_addr, height_addr)
    print(f"after:  {Pointer.ptr_rint(width_addr)} {Pointer.ptr_rint(height_addr)}")
finally:
    Pointer.ptr_fint(width_addr)
    Pointer.ptr_fint(height_addr)
```

**Output (both)**

```text
before: 3 7
after:  7 3
```

This is the textbook demonstration of "pass by pointer": the function receives **addresses**, so it modifies the caller's memory.

---

### 9.7 Program 7: Null handling (where Python is safer)

**C**

```c
int *missing_ptr = NULL;
int  oops = *missing_ptr;      // undefined behavior: typically "Segmentation fault"
```

**Python**

```python
missing_addr = 0
try:
    Pointer.ptr_rint(missing_addr)
except Exception as error:     # System.NullReferenceException surfaced by pythonnet
    print("caught:", error)
```

```text
caught: Null pointer dereference
```

Only the **null** case is turned into an exception. A non-null bad address still kills the process.

---

## 10. ✅ Proving the Match: a parity test you can run

The surest way to trust the mapping is to run the same scenario in C and in Python and diff the output.

### 10.1 `parity.c`

```c
#include <stdio.h>
#include <stdlib.h>

#define SCORE_COUNT 5

int main(void) {
    /* chain */
    int player_score = 102;
    int   *int_ptr   = &player_score;
    int  **int_pptr  = &int_ptr;
    int ***int_ppptr = &int_pptr;
    printf("chain_before=%d\n", ***int_ppptr);
    ***int_ppptr = -111;
    printf("chain_after=%d\n", ***int_ppptr);

    /* array fill, sum, reverse */
    int *scores_base    = calloc(SCORE_COUNT, sizeof(int));
    int *last_score_ptr = scores_base + SCORE_COUNT - 1;
    int *cursor;
    int  next_value = 10;
    int  total      = 0;

    for (cursor = scores_base; cursor <= last_score_ptr; cursor++) {
        *cursor = next_value;
        next_value += 10;
    }
    printf("array=");
    for (cursor = scores_base; cursor <= last_score_ptr; cursor++) {
        printf("%d%s", *cursor, cursor == last_score_ptr ? "\n" : " ");
        total += *cursor;
    }
    printf("sum=%d\n", total);

    int *left_cursor = scores_base, *right_cursor = last_score_ptr;
    while (left_cursor < right_cursor) {
        int saved_left_value = *left_cursor;
        *left_cursor  = *right_cursor;
        *right_cursor = saved_left_value;
        left_cursor++;
        right_cursor--;
    }
    printf("reversed=");
    for (cursor = scores_base; cursor <= last_score_ptr; cursor++)
        printf("%d%s", *cursor, cursor == last_score_ptr ? "\n" : " ");

    free(scores_base);
    return 0;
}
```

### 10.2 `parity.py`

```python
from src.pointers.ptr import Pointer

INT_SIZE    = 4
SCORE_COUNT = 5

def format_array(base_addr, last_addr):
    values, cursor = [], base_addr
    while cursor <= last_addr:
        values.append(str(Pointer.ptr_rint(cursor)))
        cursor = Pointer.ptr_add_byte(cursor, INT_SIZE)
    return " ".join(values)

# chain
player_score_addr = Pointer.ptr_aint()
Pointer.ptr_wint(player_score_addr, 102)
int_ptr   = player_score_addr
int_pptr  = Pointer.ptr_along();  Pointer.ptr_wlong(int_pptr,  int_ptr)
int_ppptr = Pointer.ptr_along();  Pointer.ptr_wlong(int_ppptr, int_pptr)

print(f"chain_before={Pointer.ptr_idef(int_ppptr, 3)}")
Pointer.ptr_idef_ass(int_ppptr, 3, -111)
print(f"chain_after={Pointer.ptr_idef(int_ppptr, 3)}")

# array
scores_base    = Pointer.ptr_aint_array(SCORE_COUNT)
last_score_ptr = Pointer.ptr_get_last(scores_base, SCORE_COUNT)

try:
    next_value, total, cursor = 10, 0, scores_base
    while cursor <= last_score_ptr:
        Pointer.ptr_wint(cursor, next_value)
        next_value += 10
        cursor = Pointer.ptr_add_byte(cursor, INT_SIZE)

    print(f"array={format_array(scores_base, last_score_ptr)}")

    cursor = scores_base
    while cursor <= last_score_ptr:
        total += Pointer.ptr_rint(cursor)
        cursor = Pointer.ptr_add_byte(cursor, INT_SIZE)
    print(f"sum={total}")

    left_cursor, right_cursor = scores_base, last_score_ptr
    while left_cursor < right_cursor:
        saved_left_value = Pointer.ptr_rint(left_cursor)
        Pointer.ptr_wint(left_cursor,  Pointer.ptr_rint(right_cursor))
        Pointer.ptr_wint(right_cursor, saved_left_value)
        left_cursor  = Pointer.ptr_add_byte(left_cursor,  INT_SIZE)
        right_cursor = Pointer.ptr_sub_byte(right_cursor, INT_SIZE)

    print(f"reversed={format_array(scores_base, last_score_ptr)}")
finally:
    Pointer.ptr_fint_array(scores_base)
    Pointer.ptr_flong(int_ppptr)
    Pointer.ptr_flong(int_pptr)
    Pointer.ptr_fint(player_score_addr)
```

### 10.3 Run and compare

```bash
gcc -Wall -Wextra -o parity parity.c
./parity > c_output.txt
python -m parity > python_output.txt      # or wherever you saved the script
diff c_output.txt python_output.txt && echo "IDENTICAL"
```

### 10.4 Expected output (both)

```text
chain_before=102
chain_after=-111
array=10 20 30 40 50
sum=150
reversed=50 40 30 20 10
```

---

## 11. 🧰 Recipes: building `arr[i]` and `sizeof`

These are **not part of the API**. They are small helpers you can paste in to make Python code read like C.

### 11.1 Named size constants (replace magic numbers)

```python
INT_SIZE     = 4      # sizeof(int)
LONG_SIZE    = 8      # sizeof(long long)
POINTER_SIZE = 8      # sizeof(void *) on 64-bit
```

### 11.2 `&arr[index]`

```python
def element_addr(base_addr, index, element_size=INT_SIZE):
    return Pointer.ptr_add_byte(base_addr, index * element_size)
```

```c
&scores_base[index]
```

### 11.3 `arr[index]` read and `arr[index] = v` write

```python
def int_array_get(base_addr, index):
    return Pointer.ptr_rint(element_addr(base_addr, index))

def int_array_set(base_addr, index, value):
    Pointer.ptr_wint(element_addr(base_addr, index), value)
```

```c
int  current = scores_base[index];
scores_base[index] = value;
```

### 11.4 Bounds-checked variant (what C does not give you)

```python
def checked_element_addr(base_addr, index, count):
    if not 0 <= index < count:
        raise IndexError(f"index {index} out of range for array of {count} ints")
    return element_addr(base_addr, index)
```

### 11.5 Context manager for automatic `free`

```python
from contextlib import contextmanager

@contextmanager
def int_array(count):
    base_addr = Pointer.ptr_aint_array(count)
    if base_addr == 0:
        raise MemoryError("allocation failed")
    try:
        yield base_addr
    finally:
        Pointer.ptr_fint_array(base_addr)

with int_array(5) as scores_base:
    int_array_set(scores_base, 0, 10)
    print(int_array_get(scores_base, 0))      # 10
# freed automatically here
```

---

## 12. 🐛 Common Bugs: in C and in this API

| # | Bug | In C | In this API | Fix |
|---|---|---|---|---|
| 1 | Loop never advances | `while (p != end) { ... }` hangs | `while cursor != last_score_ptr:` hangs | advance the cursor **and** the counter every round |
| 2 | Off-by-one at the end | `p < last` skips the last element | `cursor < last_score_ptr` skips it too | use `<=` with a pointer to the last element, or `<` with a one-past-the-end pointer |
| 3 | Freeing a moved pointer | `free(p + 3)` is UB | `ptr_fint_array(cursor)` after moving is UB | keep `scores_base` untouched and free that |
| 4 | Forgetting to scale | not possible with typed pointers | `ptr_add_byte(base, index)` lands in the middle of an int | multiply by `INT_SIZE` |
| 5 | Wrong deref level | compile error or wrong type | silently wrong, can corrupt a pointer slot | count the stars; level equals the number of `*` |
| 6 | Storing an address in an `int` slot | truncation warnings | `ptr_wint` rejects values over 32 bits (or truncates if you do it elsewhere) | use the `long` family for addresses |
| 7 | Double free | UB, often `free(): double free detected` | UB | set the variable to `0` after freeing, then guard |
| 8 | Memory leak | forgetting `free` | forgetting `ptr_f*` | use `try / finally` or the context manager in [11.5](#115-context-manager-for-automatic-free) |
| 9 | Use after free | UB | UB | never touch the address after freeing |
| 10 | `size` unit confusion | `calloc(n, sizeof(int))` vs bytes | `ptr_aint(size)` takes bytes, `ptr_aint_array(size)` takes elements | check the table in [4.1](#41-size-means-different-things-in-different-functions) |
| 11 | Stale DLL | n/a | old behavior persists after editing C# | `dotnet build` |
| 12 | Name shadowing | n/a | `AttributeError: ... no attribute 'AllocInt'` | import the C# class as `_CPointer` |

### 12.1 The infinite loop, annotated

```python
# BROKEN
count = 0
while scores_base != last_score_ptr:                  # scores_base never changes
    element_addr = Pointer.ptr_add_byte(scores_base, count * INT_SIZE)
    Pointer.ptr_wint(element_addr, (count + 1) * 10)  # count never changes either
```

The C twin hangs for exactly the same reason:

```c
int count = 0;
while (scores_base != last_score_ptr) {               // scores_base never changes
    scores_base[count] = (count + 1) * 10;            // count never changes
}
```

---

## 13. 🧾 Cheat Sheet

| I want to… | C | Python |
|---|---|---|
| Allocate one int | `int *p = calloc(1, sizeof(int));` | `p = Pointer.ptr_aint()` |
| Store an int | `*p = 5;` | `Pointer.ptr_wint(p, 5)` |
| Load an int | `*p` | `Pointer.ptr_rint(p)` |
| Free an int | `free(p);` | `Pointer.ptr_fint(p)` |
| Allocate a pointer slot | `long long *s = calloc(1, 8);` | `s = Pointer.ptr_along()` |
| Store an address in the slot | `*s = (long long)p;` | `Pointer.ptr_wlong(s, p)` |
| Read `**pp` | `**pp` | `Pointer.ptr_idef(pp, 2)` |
| Write `**pp` | `**pp = 9;` | `Pointer.ptr_idef_ass(pp, 2, 9)` |
| Allocate `int[n]` | `int *a = calloc(n, sizeof(int));` | `a = Pointer.ptr_aint_array(n)` |
| `&a[i]` | `a + i` | `Pointer.ptr_add_byte(a, i * INT_SIZE)` |
| `&a[n-1]` | `a + n - 1` | `Pointer.ptr_get_last(a, n)` |
| `p++` (int pointer) | `p++;` | `p = Pointer.ptr_add_byte(p, INT_SIZE)` |
| `p--` (int pointer) | `p--;` | `p = Pointer.ptr_sub_byte(p, INT_SIZE)` |
| Compare pointers | `p <= q` | `p <= q` |
| Print an address | `printf("%p", (void *)p)` | `print(hex(p))` |
| Check allocation | `if (p == NULL)` | `if p == 0:` |
| Free an array | `free(a);` | `Pointer.ptr_fint_array(a)` |

---

## 14. 🚨 Error Reference

| Error | Raised by | Cause | Fix |
|---|---|---|---|
| `NullReferenceException: Null pointer dereference` | `ptr_wint`, `ptr_rint`, `ptr_wlong`, `ptr_rlong`, `ptr_idef`, `ptr_idef_ass` | address, or an intermediate hop, is `0` | check allocation results; confirm each pointer slot was written |
| `ArgumentOutOfRangeException: level must be >= 1` | `ptr_idef`, `ptr_idef_ass` | `level < 1` | pass `level >= 1` |
| `OverflowException` / conversion error from pythonnet | any call with a numeric argument | value does not fit the C# parameter type (for example an int above 2³¹−1) | keep values inside the type's range |
| `AccessViolationException` (fatal, process aborts) | any read or write | non-null invalid address: freed, out of bounds, corrupted | review frees, loop bounds, deref levels |
| `AttributeError: ... has no attribute 'AllocInt'` | import time | the wrapper class shadows the C# class | alias the import as `_CPointer` |
| `TypeError` from pythonnet on size arguments | allocation calls | argument type cannot convert to `nuint` or `nint` | pass a non-negative Python `int` |
| Program hangs | your loop | loop variable never changes | advance the variable you test |
| Same crash after fixing C# | n/a | DLL not rebuilt | `dotnet build` |

> [!IMPORTANT]
> `AccessViolationException` terminates the whole process. Python `try/except` cannot catch it. Treat it exactly like a C segmentation fault: debug the logic, do not try to recover.

---

## 15. ❓ FAQ

**Is this faster than normal Python lists?**
Not the point. The goal is C-accurate memory semantics, for teaching, porting C algorithms, or interop experiments. Every call crosses the Python ⇄ .NET boundary, which costs more than a list index.

**Why are addresses plain `int`s instead of a `Pointer` object?**
It keeps the model identical to C, where a pointer is just a number. It also lets you compare, print, and do math on addresses without wrapper methods. The cost is no type safety (row 11 of the [parity matrix](#8--behavior-parity-matrix)).

**Why does `ptr_add_byte` not scale by element size like C?**
Because Python integers carry no pointee type. The function equals C's `(char *)p + offset`. Multiply by `INT_SIZE` yourself, or use the [`element_addr` recipe](#112-arrindex).

**Can I do `address + 4` directly in Python?**
Yes, that gives the same number. `ptr_add_byte` exists so the code reads like pointer arithmetic and routes through the same native-width conversion as the rest of the API.

**Which free function should I call?**
All three (`ptr_fint`, `ptr_flong`, `ptr_fint_array`) end in the same native `free`, so any would work. Use the one matching the allocator for readability and so future type-specific cleanup will not break your code.

**What about `double`, `char`, structs?**
Not covered by the current API. The same pattern extends naturally (`AllocDouble`, `WriteByte`, struct offsets via `ptr_add_byte`).

**Is it thread-safe?**
No more than C is. Nothing synchronizes access to the memory you allocate.

---

## 16. 📖 Glossary

| Term | Meaning |
|---|---|
| **Address** | An integer naming a byte in memory. In this API, a Python `int`. |
| **Pointer** | A value that holds an address. Here: any Python `int` you treat as an address. |
| **NULL** | The address `0`. Never valid to read or write. |
| **Dereference** | Reading or writing the memory at an address (`*p`). |
| **Level** | How many dereferences to perform; equals the number of `*` in the C expression. |
| **Base address** | The address of an array's first element. Keep it for `free`. |
| **Cursor** | A moving pointer used to walk an array. Never free it. |
| **Element** | One item of an array (4 bytes for `int`). |
| **One past the end** | The address just after the last element (`base + n`). Valid to compare, never to dereference. |
| **UB** | Undefined behavior: the program may crash, corrupt data, or appear to work. |
| **Dangling pointer** | A pointer to memory that was already freed. |
| **Little-endian** | Byte order with the least significant byte at the lowest address. |
| **`calloc`** | C allocator that returns zeroed memory; the model for every `ptr_a*` function. |

---

<div align="center">

**Made with 🧠 and a healthy respect for segmentation faults.**

</div>