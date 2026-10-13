
# PyLibC
****
### **pointers**

## Data Type
```
Type.type["i8"] Type.type["u8"]
Type.type["i16"] Type.type["u16"] 
Type.type["i32"] Type.type["u32"] 
Type.type["i64"] Type.type["u64"]

```

## Methods

> **Pointer.ptr_aint()**
ptr_aint means "pointer allocate integer"; it does not take any parameters,
because it, by default, allocates (4 bytes) of memory for an int,
in the background, and it returns an address as a (signed long) address.

> **Pointer.ptr_fint(address)**
ptr_fint free reserved memory of pointer on heap.
If you forget to free it, it will cause a memory leak


```python
ptr1 = Pointer.ptr_aint()
Pointer.ptr_fint(ptr1)
```

> **Pointer.ptr_wint(address, value)**
ptr_wint takes two parameters: firstly, an address as a long,
and secondly, a value of int. ptr_wint is responsible for
writing an int value in heap memory; it automatically dereferences
the pointer and writes the value; it does not return anything.
ptr_wint is a method which gives you the superpower to
make pointer chaining. Without ptr_wint, you can do pointer chaining.

```python
ptr1 = Pointer.ptr_aint()
Pointer.ptr_wint(ptr1, 55)
Pointer.ptr_fint(ptr1)
```

