fn main() {
    let builder = csbindgen::Builder::new()
        .input_extern_file("src/lib.rs")
        .csharp_generate_const_filter(|_| true)
        .csharp_dll_name("libredb")
        .csharp_entry_point_prefix("")
        .csharp_namespace("Redb.Interop")
        .csharp_class_accessibility("public");

    builder
        .generate_csharp_file("../../src/Redb.Interop/NativeMethods.g.cs")
        .unwrap();

    builder
        .generate_csharp_file("../../src/Redb.Unity/Assets/Redb/Runtime/NativeMethods.g.cs")
        .unwrap();
}
