# src/Test/Test

from src.pointers.ptr import Pointer
from src.pointers.ptr import Type


# ptr1 = Pointer.ptr_fork(Type.ptype["i8"])
# Pointer.write(ptr1, 55, Type.ptype["i8"],)

# value = Pointer.deref(ptr1, 1, Type.ptype["i8"])
# print(value)


# #############################################

# ptr2 = Pointer.ptr_fork(Type.ptype["usize"])
# Pointer.write(ptr2, ptr1, Type.ptype["usize"])

# Pointer.deref_ass(ptr2, 2, 101, Type.ptype["i8"])
# value = Pointer.deref(ptr2, 2, Type.ptype["i8"])
# print(value)

# Pointer.ptr_free(ptr1)
# Pointer.ptr_free(ptr2)

# #----------------------------------------------

# print(ptr1)
# print(ptr2)


####################################################

# Test File
# SIZE = 10
# base = Pointer.series_fork(SIZE, Type.ptype["i32"])
# end = Pointer.ptr_last_addr(base, SIZE, Type.ptype["i32"])

# ptr = base
# count = 0

# while ptr <= end:
#     Pointer.write(ptr, (10*count), Type.ptype["i32"])
#     ptr = Pointer.ptr_add(ptr, Type.ptype["i32"])
#     count+=1


# ptr = base

# while ptr <= end:
#     print(Pointer.deref(ptr, 1, Type.ptype["i32"]))
#     ptr = Pointer.ptr_add(ptr, Type.ptype["i32"])


# Pointer.ptr_free(base)



SIZE = 26
base = Pointer.series_fork(SIZE, Type.ptype["char"])
end = Pointer.ptr_last_addr(base, SIZE, Type.ptype["char"])

ptr = base
i = 97

while ptr <= end:
    Pointer.write(ptr, i, Type.ptype["char"])
    ptr = Pointer.ptr_add(ptr, Type.ptype["char"])
    i += 1

ptr = base

while ptr <= end:
    print(chr(Pointer.deref(ptr, 1, Type.ptype["char"])))
    ptr = Pointer.ptr_add(ptr, Type.ptype["char"])

Pointer.ptr_free(base)