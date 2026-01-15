use redb::{
    ReadableDatabase, ReadableTable, ReadableTableMetadata, TableDefinition, backends::FileBackend,
};
use std::{
    ffi::{c_char, c_void},
    fs::OpenOptions,
};

pub const REDB_OK: i32 = 0;

// 1- DatabaseError
pub const REDB_ERROR_FILE_ERROR: i32 = 1;
pub const REDB_ERROR_DATABASE_ALREADY_OPEN: i32 = 2;
pub const REDB_ERROR_REPAIR_ABORTED: i32 = 3;
pub const REDB_ERROR_UPGRADE_REQUIRED: i32 = 4;
pub const REDB_ERROR_STORAGE_ERROR: i32 = 5;

// 11- CompactionError
pub const REDB_ERROR_COMPACTION: i32 = 11;

// 21- TableError
pub const REDB_ERROR_TABLE_TYPE_MISMATCH: i32 = 21;
pub const REDB_ERROR_TABLE_IS_MULTIMAP: i32 = 22;
pub const REDB_ERROR_TABLE_IS_NOT_MULTIMAP: i32 = 23;
pub const REDB_ERROR_TYPE_DEFINITION_CHANGED: i32 = 24;
pub const REDB_ERROR_TABLE_DOES_NOT_EXIST: i32 = 25;
pub const REDB_ERROR_TABLE_EXISTS: i32 = 26;
pub const REDB_ERROR_TABLE_ALREADY_OPEN: i32 = 27;

// 41- TransactionError
pub const REDB_ERROR_READ_TRANSACTION_STILL_IN_USE: i32 = 41;

// 51- SavepointError
pub const REDB_ERROR_INVALID_SAVEPOINT: i32 = 51;

// 100- Custom errors
pub const REDB_ERROR_KEY_NOT_FOUND: i32 = 100;

pub const fn database_error_code(err: &redb::DatabaseError) -> i32 {
    match err {
        redb::DatabaseError::DatabaseAlreadyOpen => REDB_ERROR_DATABASE_ALREADY_OPEN,
        redb::DatabaseError::RepairAborted => REDB_ERROR_REPAIR_ABORTED,
        redb::DatabaseError::UpgradeRequired(_) => REDB_ERROR_UPGRADE_REQUIRED,
        redb::DatabaseError::Storage(_) => REDB_ERROR_STORAGE_ERROR,
        _ => REDB_ERROR_FILE_ERROR,
    }
}

fn table_error_code(err: &redb::TableError) -> i32 {
    match err {
        redb::TableError::TableTypeMismatch { .. } => REDB_ERROR_TABLE_TYPE_MISMATCH,
        redb::TableError::TableIsMultimap(_) => REDB_ERROR_TABLE_IS_MULTIMAP,
        redb::TableError::TableIsNotMultimap(_) => REDB_ERROR_TABLE_IS_NOT_MULTIMAP,
        redb::TableError::TypeDefinitionChanged { .. } => REDB_ERROR_TYPE_DEFINITION_CHANGED,
        redb::TableError::TableDoesNotExist(_) => REDB_ERROR_TABLE_DOES_NOT_EXIST,
        redb::TableError::TableExists(_) => REDB_ERROR_TABLE_EXISTS,
        redb::TableError::TableAlreadyOpen(_, _) => REDB_ERROR_TABLE_ALREADY_OPEN,
        redb::TableError::Storage(_) => REDB_ERROR_STORAGE_ERROR,
        _ => todo!(),
    }
}

fn transaction_error_code(err: &redb::TransactionError) -> i32 {
    match err {
        redb::TransactionError::ReadTransactionStillInUse(_) => {
            REDB_ERROR_READ_TRANSACTION_STILL_IN_USE
        }
        redb::TransactionError::Storage(_) => REDB_ERROR_STORAGE_ERROR,
        _ => todo!(),
    }
}

fn savepoint_error_code(err: &redb::SavepointError) -> i32 {
    match err {
        redb::SavepointError::InvalidSavepoint => REDB_ERROR_INVALID_SAVEPOINT,
        redb::SavepointError::Storage(_) => REDB_ERROR_STORAGE_ERROR,
        _ => todo!(),
    }
}

#[repr(C)]
pub struct redb_database_options {
    pub cache_size: usize,
    pub backend: redb_backend,
}

#[repr(C)]
pub enum redb_backend {
    File,
    InMemory,
}

#[repr(C)]
pub enum redb_durability {
    None,
    Immediate,
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_create_database(
    path: *const c_char,
    options: *const redb_database_options,
    out: *mut *mut c_void,
) -> i32 {
    let c_str = unsafe {
        assert!(!path.is_null());
        std::ffi::CStr::from_ptr(path)
    };
    let str_slice = c_str.to_str().unwrap();

    let db = if !options.is_null() {
        let opts = unsafe { &*options };
        let mut builder = redb::Database::builder();
        builder.set_cache_size(opts.cache_size);
        match opts.backend {
            redb_backend::File => {
                let Ok(file) = OpenOptions::new()
                    .read(true)
                    .write(true)
                    .create(true)
                    .truncate(false)
                    .open(str_slice)
                else {
                    return REDB_ERROR_FILE_ERROR;
                };

                let Ok(backend) = FileBackend::new(file) else {
                    return REDB_ERROR_FILE_ERROR;
                };

                match builder.create_with_backend(backend) {
                    Ok(db) => db,
                    Err(err) => return database_error_code(&err),
                }
            }
            redb_backend::InMemory => {
                match builder.create_with_backend(redb::backends::InMemoryBackend::new()) {
                    Ok(db) => db,
                    Err(err) => return database_error_code(&err),
                }
            }
        }
    } else {
        match redb::Database::create(str_slice) {
            Ok(db) => db,
            Err(err) => return database_error_code(&err),
        }
    };

    unsafe {
        *out = Box::into_raw(Box::new(db)) as *mut c_void;
    };

    REDB_OK
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_open_database(path: *const c_char, out: *mut *mut c_void) -> i32 {
    let c_str = unsafe {
        assert!(!path.is_null());
        std::ffi::CStr::from_ptr(path)
    };
    let str_slice = c_str.to_str().unwrap();

    let db = match redb::Database::open(str_slice) {
        Ok(db) => db,
        Err(err) => return database_error_code(&err),
    };

    unsafe {
        *out = Box::into_raw(Box::new(db)) as *mut c_void;
    };

    REDB_OK
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_compact_database(db: *mut c_void) -> i32 {
    let db = unsafe {
        assert!(!db.is_null());
        &mut *(db as *mut redb::Database)
    };

    match db.compact() {
        Ok(_) => REDB_OK,
        Err(_) => REDB_ERROR_COMPACTION,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_database(db: *mut c_void) {
    if db.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(db as *mut redb::Database));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_table(table: *mut c_void) {
    if table.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(table as *mut redb::Table<&str, i32>));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_readonly_table(table: *mut c_void) {
    if table.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(table as *mut redb::ReadOnlyTable<&str, i32>));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_write_transaction(tx: *mut c_void) {
    if tx.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(tx as *mut redb::WriteTransaction));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_read_transaction(tx: *mut c_void) {
    if tx.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(tx as *mut redb::ReadTransaction));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_savepoint(savepoint: *mut c_void) {
    if savepoint.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(savepoint as *mut redb::Savepoint));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_string(s: *mut c_char) {
    if s.is_null() {
        return;
    }
    unsafe {
        drop(std::ffi::CString::from_raw(s));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free(ptr: *mut c_void) {
    if ptr.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(ptr));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_blob(blob: *mut u8) {
    if blob.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(blob as *mut u8));
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_begin_write(db: *mut c_void, out: *mut *mut c_void) -> i32 {
    let db = unsafe {
        assert!(!db.is_null());
        &mut *(db as *mut redb::Database)
    };

    match db.begin_write() {
        Ok(tx) => {
            unsafe {
                *out = Box::into_raw(Box::new(tx)) as *mut c_void;
            };
            REDB_OK
        }
        Err(err) => transaction_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_set_durability(
    tx: *mut c_void,
    durability: redb_durability,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    let redb_durability = match durability {
        redb_durability::None => redb::Durability::None,
        redb_durability::Immediate => redb::Durability::Immediate,
    };

    match tx.set_durability(redb_durability) {
        Ok(_) => REDB_OK,
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_set_quick_repair(tx: *mut c_void, quick_repair: bool) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    tx.set_quick_repair(quick_repair);
    REDB_OK
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_set_two_phase_commit(
    tx: *mut c_void,
    two_phase_commit: bool,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    tx.set_two_phase_commit(two_phase_commit);
    REDB_OK
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_open_table(
    tx: *mut c_void,
    name: *const c_char,
    out: *mut *mut c_void,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    let c_str = unsafe {
        assert!(!name.is_null());
        std::ffi::CStr::from_ptr(name)
    };
    let str_slice = c_str.to_str().unwrap();

    match tx.open_table(TableDefinition::<&[u8], &[u8]>::new(str_slice)) {
        Ok(table) => {
            unsafe {
                *out = Box::into_raw(Box::new(table)) as *mut c_void;
            };
            REDB_OK
        }
        Err(err) => table_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_delete_table(tx: *mut c_void, name: *const c_char) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    let c_str = unsafe {
        assert!(!name.is_null());
        std::ffi::CStr::from_ptr(name)
    };
    let str_slice = c_str.to_str().unwrap();

    match tx.delete_table(TableDefinition::<&[u8], &[u8]>::new(str_slice)) {
        Ok(_) => REDB_OK,
        Err(err) => table_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_rename_table(
    tx: *mut c_void,
    old_name: *const c_char,
    new_name: *const c_char,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    let old_c_str = unsafe {
        assert!(!old_name.is_null());
        std::ffi::CStr::from_ptr(old_name)
    };
    let old_str_slice = old_c_str.to_str().unwrap();

    let new_c_str = unsafe {
        assert!(!new_name.is_null());
        std::ffi::CStr::from_ptr(new_name)
    };
    let new_str_slice = new_c_str.to_str().unwrap();

    match tx.rename_table(
        TableDefinition::<&[u8], &[u8]>::new(old_str_slice),
        TableDefinition::<&[u8], &[u8]>::new(new_str_slice),
    ) {
        Ok(_) => REDB_OK,
        Err(err) => table_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_abort(tx: *mut c_void) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        Box::from_raw(tx as *mut redb::WriteTransaction)
    };

    match tx.abort() {
        Ok(_) => REDB_OK,
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_commit(tx: *mut c_void) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        Box::from_raw(tx as *mut redb::WriteTransaction)
    };

    match tx.commit() {
        Ok(_) => REDB_OK,
        Err(err) => match err {
            redb::CommitError::Storage(_) => REDB_ERROR_STORAGE_ERROR,
            _ => todo!(),
        },
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_ephemeral_savepoint(tx: *mut c_void, out: *mut *mut c_void) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    match tx.ephemeral_savepoint() {
        Ok(savepoint) => {
            unsafe {
                *out = Box::into_raw(Box::new(savepoint)) as *mut c_void;
            };
            REDB_OK
        }
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_restore_savepoint(tx: *mut c_void, savepoint: *mut c_void) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    let savepoint = unsafe {
        assert!(!savepoint.is_null());
        &*(savepoint as *mut redb::Savepoint)
    };

    match tx.restore_savepoint(savepoint) {
        Ok(_) => REDB_OK,
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_persistent_savepoint(tx: *mut c_void, out: *mut u64) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    match tx.persistent_savepoint() {
        Ok(id) => {
            unsafe {
                *out = id;
            };
            REDB_OK
        }
        Err(err) => match err {
            redb::SavepointError::InvalidSavepoint => REDB_ERROR_INVALID_SAVEPOINT,
            redb::SavepointError::Storage(_) => REDB_ERROR_STORAGE_ERROR,
            _ => todo!(),
        },
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_get_persistent_savepoint(
    tx: *mut c_void,
    id: u64,
    out: *mut *mut c_void,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    match tx.get_persistent_savepoint(id) {
        Ok(savepoint) => {
            unsafe {
                *out = Box::into_raw(Box::new(savepoint)) as *mut c_void;
            };
            REDB_OK
        }
        Err(err) => savepoint_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_delete_persistent_savepoint(
    tx: *mut c_void,
    id: u64,
    out: *mut bool,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    match tx.delete_persistent_savepoint(id) {
        Ok(ret) => {
            unsafe {
                *out = ret;
            };
            REDB_OK
        }
        Err(err) => savepoint_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_write_tx_lists_persistent_savepoint(
    tx: *mut c_void,
    out: *mut *mut u64,
    count: *mut usize,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *(tx as *mut redb::WriteTransaction)
    };

    let iter = match tx.list_persistent_savepoints() {
        Ok(iter) => iter,
        Err(_) => return REDB_ERROR_STORAGE_ERROR,
    };

    let result = iter.collect::<Vec<_>>();
    unsafe {
        *count = result.len();
        *out = Box::into_raw(result.into_boxed_slice()) as *mut u64;
    }

    REDB_OK
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_insert(
    table: *mut c_void,
    key: *const u8,
    key_len: usize,
    value: *const u8,
    value_len: usize,
) -> i32 {
    let table = unsafe {
        assert!(!table.is_null());
        &mut *(table as *mut redb::Table<&[u8], &[u8]>)
    };

    let key_slice = unsafe {
        assert!(!key.is_null());
        std::slice::from_raw_parts(key, key_len)
    };

    let value_slice = unsafe {
        assert!(!value.is_null());
        std::slice::from_raw_parts(value, value_len)
    };

    match table.insert(key_slice, value_slice) {
        Ok(_) => REDB_OK,
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_begin_read(db: *mut c_void, out: *mut *mut c_void) -> i32 {
    let db = unsafe {
        assert!(!db.is_null());
        &*db.cast::<redb::Database>()
    };

    match db.begin_read() {
        Ok(tx) => {
            unsafe {
                *out = Box::into_raw(Box::new(tx)) as *mut c_void;
            };
            REDB_OK
        }
        Err(err) => transaction_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_read_tx_open_table(
    tx: *mut c_void,
    name: *const c_char,
    out: *mut *mut c_void,
) -> i32 {
    let tx = unsafe {
        assert!(!tx.is_null());
        &mut *tx.cast::<redb::ReadTransaction>()
    };

    let c_str = unsafe {
        assert!(!name.is_null());
        std::ffi::CStr::from_ptr(name)
    };
    let str_slice = c_str.to_str().unwrap();

    match tx.open_table(TableDefinition::<&[u8], &[u8]>::new(str_slice)) {
        Ok(table) => {
            unsafe {
                *out = Box::into_raw(Box::new(table)) as *mut c_void;
            };
            REDB_OK
        }
        Err(err) => table_error_code(&err),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_get(
    table: *mut c_void,
    key: *const u8,
    key_len: usize,
    blob: *mut *mut u8,
    count: *mut usize,
) -> i32 {
    let table = unsafe {
        assert!(!table.is_null());
        &*table.cast::<redb::ReadOnlyTable<&[u8], &[u8]>>()
    };

    let key_slice = unsafe {
        assert!(!key.is_null());
        std::slice::from_raw_parts(key as *const u8, key_len)
    };

    match table.get(&key_slice) {
        Ok(Some(value)) => {
            let value_slice = value.value();
            let value_len = value_slice.len();
            // write to buffer
            unsafe {
                *blob = Box::into_raw(value_slice.to_vec().into_boxed_slice()) as *mut u8;
                *count = value_len;
            }
            REDB_OK
        }
        Ok(None) => REDB_ERROR_KEY_NOT_FOUND,
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_table_len(table: *mut c_void, out: *mut u64) -> i32 {
    let table = unsafe {
        assert!(!table.is_null());
        &*table.cast::<redb::ReadOnlyTable<&[u8], &[u8]>>()
    };

    match table.len() {
        Ok(len) => {
            unsafe {
                *out = len;
            }
            REDB_OK
        }
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

type RangeIter<'a> = Box<
    dyn Iterator<
            Item = Result<
                (
                    redb::AccessGuard<'a, &'static [u8]>,
                    redb::AccessGuard<'a, &'static [u8]>,
                ),
                redb::StorageError,
            >,
        > + 'a,
>;

pub struct RedbIterator {
    inner: *mut c_void,
    next_fn: unsafe extern "C" fn(
        *mut c_void,
        *mut *mut u8,
        *mut usize,
        *mut *mut u8,
        *mut usize,
    ) -> i32,
}

unsafe impl Send for RedbIterator {}

unsafe extern "C" fn redb_iter_next_impl(
    iter_ptr: *mut c_void,
    key_blob: *mut *mut u8,
    key_len: *mut usize,
    value_blob: *mut *mut u8,
    value_len: *mut usize,
) -> i32 {
    type IterType<'a> = Box<
        dyn Iterator<
                Item = Result<
                    (
                        redb::AccessGuard<'a, &'static [u8]>,
                        redb::AccessGuard<'a, &'static [u8]>,
                    ),
                    redb::StorageError,
                >,
            > + 'a,
    >;

    unsafe {
        let iter = &mut *(iter_ptr as *mut IterType);

        match iter.next() {
            Some(Ok((key, value))) => {
                let key_slice = key.value();
                let value_slice = value.value();

                *key_blob = Box::into_raw(key_slice.to_vec().into_boxed_slice()) as *mut u8;
                *key_len = key_slice.len();
                *value_blob = Box::into_raw(value_slice.to_vec().into_boxed_slice()) as *mut u8;
                *value_len = value_slice.len();

                REDB_OK
            }
            Some(Err(_)) => REDB_ERROR_STORAGE_ERROR,
            None => REDB_ERROR_KEY_NOT_FOUND, // End of iterator
        }
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_iter(table: *mut c_void, out: *mut *mut c_void) -> i32 {
    let table = unsafe {
        assert!(!table.is_null());
        &*table.cast::<redb::ReadOnlyTable<&'static [u8], &'static [u8]>>()
    };

    match table.iter() {
        Ok(iter) => {
            let boxed_iter: RangeIter = Box::new(iter);
            let iter_handle = RedbIterator {
                inner: Box::into_raw(Box::new(boxed_iter)) as *mut c_void,
                next_fn: redb_iter_next_impl,
            };

            unsafe {
                *out = Box::into_raw(Box::new(iter_handle)) as *mut c_void;
            }
            REDB_OK
        }
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_range(
    table: *mut c_void,
    start_key: *const u8,
    start_key_len: usize,
    end_key: *const u8,
    end_key_len: usize,
    out: *mut *mut c_void,
) -> i32 {
    let table = unsafe {
        assert!(!table.is_null());
        &*table.cast::<redb::ReadOnlyTable<&'static [u8], &'static [u8]>>()
    };

    let start = if !start_key.is_null() && start_key_len > 0 {
        let slice = unsafe { std::slice::from_raw_parts(start_key, start_key_len) };
        Some(slice.to_vec())
    } else {
        None
    };

    let end = if !end_key.is_null() && end_key_len > 0 {
        let slice = unsafe { std::slice::from_raw_parts(end_key, end_key_len) };
        Some(slice.to_vec())
    } else {
        None
    };

    let range_iter = match (&start, &end) {
        (Some(s), Some(e)) => {
            let s_slice: &[u8] = s.as_slice();
            let e_slice: &[u8] = e.as_slice();
            table.range::<&[u8]>(s_slice..e_slice)
        }
        (Some(s), None) => {
            let s_slice: &[u8] = s.as_slice();
            table.range::<&[u8]>(s_slice..)
        }
        (None, Some(e)) => {
            let e_slice: &[u8] = e.as_slice();
            table.range::<&[u8]>(..e_slice)
        }
        (None, None) => table.range::<&[u8]>(..),
    };

    match range_iter {
        Ok(iter) => {
            let boxed_iter: RangeIter = Box::new(iter);
            let iter_handle = RedbIterator {
                inner: Box::into_raw(Box::new(boxed_iter)) as *mut c_void,
                next_fn: redb_iter_next_impl,
            };

            unsafe {
                *out = Box::into_raw(Box::new(iter_handle)) as *mut c_void;
            }
            REDB_OK
        }
        Err(_) => REDB_ERROR_STORAGE_ERROR,
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_iter_next(
    iter: *mut c_void,
    key_blob: *mut *mut u8,
    key_len: *mut usize,
    value_blob: *mut *mut u8,
    value_len: *mut usize,
) -> i32 {
    let iter_handle = unsafe {
        assert!(!iter.is_null());
        &mut *iter.cast::<RedbIterator>()
    };

    unsafe { (iter_handle.next_fn)(iter_handle.inner, key_blob, key_len, value_blob, value_len) }
}

#[unsafe(no_mangle)]
pub extern "C" fn redb_free_iter(iter: *mut c_void) {
    if iter.is_null() {
        return;
    }

    unsafe {
        let iter = iter as *mut RedbIterator;
        if !(*iter).inner.is_null() {
            drop(Box::from_raw((*iter).inner as *mut RangeIter));
        }
        drop(Box::from_raw(iter));
    }
}
