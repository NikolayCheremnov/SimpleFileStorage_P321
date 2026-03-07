namespace SimpleFileStorage.Model
{
    // IFileMetadataRepository - хранилище метаданных файлов
    public interface IFileMetadataRepository
    {
        Task Insert(FileMetadata metadata);
        Task<FileMetadata?> Get(Guid fileID);
        Task<List<FileMetadata>> GetAll();
    }
}
