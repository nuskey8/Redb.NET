fn main() {
    let builder = csbindgen::Builder::new()
        .input_extern_file("src/lib.rs")
        .csharp_generate_const_filter(|_| true)
        .csharp_dll_name("redb")
        .csharp_entry_point_prefix("")
        .csharp_namespace("Redb.Interop")
        .csharp_class_accessibility("public");

    // .NET
    builder
        .generate_csharp_file("../../src/Redb.Interop/NativeMethods.g.cs")
        .unwrap();

    // Unity
    builder
        .csharp_dll_name_if("UNITY_IOS && !UNITY_EDITOR", "__Internal")
        .generate_csharp_file("../../src/Redb.Unity/Assets/Redb/Interop/NativeMethods.g.cs")
        .unwrap();
}
