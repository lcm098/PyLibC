
from src.pointers.ptr import Pointer
from src.pointers.ptr import Type


# ptr1 = Pointer.ptr_aint()
# Pointer.ptr_wint(ptr1, 102)

# ptr2 = Pointer.ptr_along()
# Pointer.ptr_wlong(ptr2, ptr1)

# ptr3 = Pointer.ptr_along()
# Pointer.ptr_wlong(ptr3, ptr2)


# print(Pointer.ptr_idef(ptr3, 3))
# Pointer.ptr_idef_ass(ptr3, 3, -111)
# print(Pointer.ptr_idef(ptr3, 3))

# Pointer.ptr_fint(ptr1)
# Pointer.ptr_flong(ptr2)
# Pointer.ptr_flong(ptr3)



# ptr1 = Pointer.ptr_aint_array(10)
# size = 5

# if ptr1 != 0:

#     try:
#         for i in range(size):
#             element_addr = Pointer.ptr_add_byte(ptr1, i*4)
#             Pointer.ptr_wint(element_addr, (i+1)*10)

#         for i in range(size):
#             element_addr = Pointer.ptr_add_byte(ptr1, i * 4)
#             print(Pointer.ptr_rint(element_addr))

#     finally:
#         Pointer.ptr_fint_array(ptr1)



# SIZE = 10
# base = Pointer.ptr_aint_array(SIZE)
# last_addr = Pointer.ptr_get_last(base, SIZE)

# if base != 0:

#     try:
#         count = 0
#         ptr1 = base

#         while ptr1 <= last_addr:
#             Pointer.ptr_wint(ptr1, count * 10)
#             ptr1 = Pointer.ptr_add_byte(ptr1, 4)
#             count += 1

#         ptr1 = base
#         while ptr1 <= last_addr:
#             print(Pointer.ptr_rint(ptr1))
#             ptr1 = Pointer.ptr_add_byte(ptr1, 4)

#     finally:
#         Pointer.ptr_fint_array(base)






score_track = Pointer.ptr_aint()
Pointer.ptr_wint(score_track, 555)
print(Pointer.ptr_rint(score_track))
Pointer.ptr_fint(score_track)
