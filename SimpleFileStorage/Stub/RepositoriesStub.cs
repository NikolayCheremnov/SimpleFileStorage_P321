using SimpleFileStorage.Model;

namespace SimpleFileStorage.Stub
{
    // RepositoriesStub - in-memory заглушка для IFileMetadataRepository и IFileDataRepository
    public class RepositoriesStub : IFileMetadataRepository, IFileDataRepository
    {
        private readonly static Dictionary<Guid, FileMetadata> _metadatas = new Dictionary<Guid, FileMetadata>();
        private readonly static Dictionary<Guid, FileData> _files = new Dictionary<Guid, FileData>();

        public async Task<FileData?> Get(Guid fileID)
        {
            if (!_files.ContainsKey(fileID))
            {
                return null;
            }
            return _files[fileID];
        }

        public async Task Insert(FileData file)
        {
            _files[file.FileID] = file;
        }

        public async Task Insert(FileMetadata metadata)
        {
            _metadatas[metadata.FileID] = metadata;
        }

        async Task<FileMetadata?> IFileMetadataRepository.Get(Guid fileID)
        {
            if (!_metadatas.ContainsKey(fileID))
            {
                return null;
            }
            return _metadatas[fileID];
        }
    }
}
