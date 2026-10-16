# src/Test/Test

from PyLibC import Pointer, Type, Io
import numpy as np

# ptr1 = Pointer.ptr_fork(Type.ptype["i8"])
# Pointer.write(ptr1, 55, Type.ptype["i8"])

# value = Pointer.deref(ptr1, 1, Type.ptype["i8"])
# print(value)


# #############################################

# ptr2 = Pointer.ptr_fork(Type.ptype["u64"])
# Pointer.write(ptr2, ptr1, Type.ptype["u64"])

# Pointer.deref_ass(ptr2, 2, 101, Type.ptype["i8"])
# value = Pointer.deref(ptr2, 2, Type.ptype["i8"])
# print(value)

# Pointer.ptr_free(ptr1)
# Pointer.ptr_free(ptr2)

# ----------------------------------------------

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



###################################################################


# SIZE = 26
# base = Pointer.series_fork(SIZE, Type.ptype["char"])
# end = Pointer.ptr_last_addr(base, SIZE, Type.ptype["char"])

# ptr = base
# i = 97

# while ptr <= end:
#     Pointer.write(ptr, i, Type.ptype["char"])
#     ptr = Pointer.ptr_add(ptr, Type.ptype["char"])
#     i += 1

# ptr = base

# while ptr <= end:
#     print(chr(Pointer.deref(ptr, 1, Type.ptype["char"])))
#     ptr = Pointer.ptr_add(ptr, Type.ptype["char"])

# Pointer.ptr_free(base)


##############################################################


# np_arr = np.array([1, 2, 3, 4, 5])
# lists = [11, 22, 33, 44, 55]

# ptr_series1 = Pointer.numpy_series_fork(np_arr, Type.ptype["u32"])
# ptr_series2 = Pointer.list_series_fork(lists, Type.ptype["u32"])

# print(Pointer.deref(ptr_series1, 1, Type.ptype["u32"]))
# print(Pointer.deref(ptr_series2, 1, Type.ptype["u32"]))

# Pointer.ptr_free(ptr_series1)
# Pointer.ptr_free(ptr_series1)


################################################################

# x = np.zeros((5, 5), dtype=np.uint32)

# x_ptr = x.ctypes.data
#     
# # x ki apni memory ka address (Python int)
# # yahan ham yah kah rahe hai ki hello PyLibC tum apna memory allocate nahi karo
# # balki numpy as starting memory address le lo aur ending memory calculate karo
# # aur esme change karo 

# # agar ham yah karte Pointer.numpy_series_fork(np_arr, Type.ptype["u32"])
# # to ham same elements ke sath alag memory allocate kar rahe hai same elements
# # ke liye, jo numpy memory se alag hai

# x_end = Pointer.ptr_last_addr(x_ptr, x.size, Type.ptype["u32"])

# count = 0
# ptr = x_ptr
# while ptr <= x_end:
#     Pointer.write(ptr, count * 2, Type.ptype["u32"])
#     ptr = Pointer.ptr_add(ptr, Type.ptype["u32"])
#     count += 1

# print(x)    # ab x badla hua dikhega: 0, 2, 4 ... 48


ptr1 = Pointer.ptr_new(55, Type.ptype["i8"])
Io.printf("%i8 | %p", ptr1, ptr1, end="\n")

Pointer.ptr_free(ptr1)