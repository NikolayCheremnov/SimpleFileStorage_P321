using Microsoft.EntityFrameworkCore;
using SimpleFileStorage.Model;

namespace SimpleFileStorage.Postgres
{
    // FileStorage - postgres-хранилище файлов
    public class FileStorage : IFileDataRepository, IFileMetadataRepository
    {
        private readonly ApplicationDbContext _db;

        public FileStorage(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<FileMetadata?> Get(Guid fileID)
        {
            return await _db.Metadatas.FirstOrDefaultAsync(md => md.FileID == fileID);
        }

        public async Task Insert(FileMetadata metadata)
        {
            await _db.Metadatas.AddAsync(metadata);
            await _db.SaveChangesAsync();   
        }

        [Obsolete("use ObjectStorage.FileS3Storage instead")]
        public async Task Insert(FileData file)
        {
            await _db.Files.AddAsync(file);
            await _db.SaveChangesAsync();
        }

        [Obsolete("use ObjectStorage.FileS3Storage instead")]
        async Task<FileData?> IFileDataRepository.Get(Guid fileID)
        {
            return await _db.Files.FirstOrDefaultAsync(file => file.FileID == fileID);
        }
    }
}
