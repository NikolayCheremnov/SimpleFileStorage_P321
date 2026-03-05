namespace SimpleFileStorage.Model
{
    // IFileDataRepository - хранилище самих файлов
    public interface IFileDataRepository
    {
        Task Insert(FileData file);
        Task<FileData?> Get(Guid fileID);
    }
}
