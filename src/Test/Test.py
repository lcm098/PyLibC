# src/Test/Test

from src.pointers.ptr import Pointer
from src.pointers.ptr import Type


ptr1 = Pointer.ptr_fork(Type.ptype["i8"])
Pointer.write(ptr1, 55, Type.ptype["i8"],)

value = Pointer.deref(ptr1, 1, Type.ptype["i8"])
print(value)


#############################################

ptr2 = Pointer.ptr_fork(Type.ptype["usize"])
Pointer.write(ptr2, ptr1, Type.ptype["usize"])

Pointer.deref_ass(ptr2, 2, 101, Type.ptype["i8"])
value = Pointer.deref(ptr2, 2, Type.ptype["i8"])
print(value)

Pointer.ptr_free(ptr1)
Pointer.ptr_free(ptr2)

print(ptr1)
print(ptr2)