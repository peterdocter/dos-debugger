/* mz.c - implementation for MZ Executable routines */

#include "mz.h"
#include "cpr/file_mapping.h"
#include <stdlib.h>
#include <memory.h>

struct mz_file_t
{
    mz_header_t header; /* header in the .EXE file */
    size_t size;        /* size of the used portion of the file, in bytes */
    size_t start;       /* offset of the start of the executable image */
    mmap_t *mm;         /* memory mapping object */
    unsigned char *pmem;    /* pointer to memory mapped file */
};

mz_file_t *mz_open(const char *filename)
{
    mz_file_t *file;
    size_t used_size;

#define REQUIRE(cond) \
    do { \
        if (!(cond)) { \
            mz_close(file); \
            return NULL; \
        } \
    } while (0)

    /* Allocate file structure and clear the fields. */
    file = (mz_file_t *)malloc(sizeof(mz_file_t));
    REQUIRE(file != NULL);
    memset(file, 0, sizeof(mz_file_t));

    /* Map the file into memory. */
    file->mm = mmap_open(filename, MMAP_READ|MMAP_READLOCK);
    REQUIRE(file->mm != NULL);

    /* Initialize members. */
    file->size = mmap_size(file->mm);
    file->pmem = mmap_address(file->mm);

    /* Read header. */
    REQUIRE(file->size >= sizeof(mz_header_t));
    memcpy(&file->header, file->pmem, sizeof(mz_header_t));

    /* Check signature. Both 'MZ' and 'ZM' are allowed. */
    REQUIRE(file->header.signature == 0x5A4D || file->header.signature == 0x4D5A);

    /* Compute the size of the executable, and update the _size member. */
    REQUIRE(file->header.page_count > 0);
    used_size = (size_t)file->header.page_count * 512 - 
            (file->header.last_page_size ? 512 - file->header.last_page_size : 0);
    REQUIRE(used_size <= file->size);
    file->size = used_size;

    /* Load relocation table. */

    /* Check header size. */
    file->start = (size_t)file->header.header_size * 16;
    REQUIRE(file->start <= file->size);

    /* Return loaded file. */
    return file;
#undef REQUIRE
}

void mz_close(mz_file_t *file)
{
    if (!file)
        return;

    if (file->mm)
        mmap_close(file->mm);

    memset(file, 0, sizeof(mz_file_t));
    free(file);
}

unsigned char *mz_image_address(mz_file_t *file)
{
    return (unsigned char *)mmap_address(file->mm) + file->start;
}

size_t mz_image_size(const mz_file_t *file)
{
    return file->size - file->start;
}
