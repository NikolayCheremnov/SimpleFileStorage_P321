namespace SimpleFileStorage.Model
{
    // FileService - сервис для работы с файлами
    public class FileService
    {
        private readonly IFileMetadataRepository _metadatas;
        private readonly IFileDataRepository _files;

        public FileService(IFileMetadataRepository metadatas, IFileDataRepository files)
        {
            _metadatas = metadatas;
            _files = files;
        }

        // Upload - загрузить файл в систему
        // вход: параметр загрузки файла (имя, тип, данные самого файла)
        // выход: внутрисистемный идентификатор сохраненного файла
        public async Task<Guid> Upload(UploadFileParam param)
        {
            // генерируем внутрисистемный идентификатор для нового файла
            Guid fileID = Guid.NewGuid();
            // формируем объекты для добавления в хранилище файлов: метаданные и сам файл
            FileMetadata metadata = new FileMetadata()
            {
                FileID = fileID,
                FileName = param.FileName,
                ContentType = param.ContentType,
                SizeBytes = param.Data.Length,
                UploadedAt = DateTime.UtcNow,
            };
            FileData data = new FileData()
            {
                FileID = fileID,
                Data = param.Data,
            };
            // сохраняем метаданные и файла
            // TODO: отсутствует транзакционность - при ошибке вставки файла сохранятся ненужные лишние метаданные
            await _metadatas.Insert(metadata);
            await _files.Insert(data);
            // возвращаем идентификатор файла для возможности получения метаданных и скачивания файла в дальнейшем
            return fileID;
        }

        // GetFileMetadata - получить метаданные файла
        // вход: внутрисистемный идентификатор файла
        // выход: объект метаданных файла
        // исключения: FileNotFoundException если файл с таким id не найден
        public async Task<FileMetadata> GetFileMetadata(Guid fileID)
        {
            FileMetadata? metadata = await _metadatas.Get(fileID);
            if (metadata == null)
            {
                throw new FileNotFoundException();
            }
            return metadata;
        }

        // GetFileData - получить файл
        // вход: внутрисистемный идентификатор файла
        // выход: объект данных файла (id + файла)
        // исключения: FileNotFoundException если файл с таким id не найден
        public async Task<FileData> GetFileData(Guid fileID)
        {
            FileData? data = await _files.Get(fileID);
            if (data == null)
            {
                throw new FileNotFoundException();
            }
            return data;
        }

        // GetAllFilesMetadata - получить метаданные всех файлов
        public async Task<List<FileMetadata>> GetAllFilesMetadata()
        {
            return await _metadatas.GetAll();
        }
    }
}
