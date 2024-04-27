use std::{ffi::CStr, slice, str};

#[no_mangle]
pub unsafe extern "C" fn print(ptr: *const u8, len: usize) {
    let slice = slice::from_raw_parts(ptr, len);
    let str = str::from_utf8_unchecked(slice);

    println!("{}", str);
}

#[no_mangle]
pub unsafe extern "C" fn print_null_terminated(ptr: *const u8) {
    // Unsafely construct while finding null terminator
    let cstr = CStr::from_ptr(ptr.cast());
    let str = str::from_utf8_unchecked(cstr.to_bytes());

    println!("{}", str);
}

#[no_mangle]
pub unsafe extern "C" fn count_runes(ptr: *const u8, len: usize) -> usize {
    let slice = slice::from_raw_parts(ptr, len);
    let str = str::from_utf8_unchecked(slice);

    str.chars().count()
}

#[no_mangle]
pub extern "C" fn get_str() -> *const u8 {
    "Hello from Rust!\0".as_ptr()
}
