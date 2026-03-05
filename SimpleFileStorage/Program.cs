using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleFileStorage.Model;
using SimpleFileStorage.ObjectStorage;
using SimpleFileStorage.Postgres;
using SimpleFileStorage.Stub;

var builder = WebApplication.CreateBuilder(args);

// ВАРИАНТ С ЗАГЛУШКАМИ
//builder.Services.AddTransient<IFileDataRepository, RepositoriesStub>();
//builder.Services.AddTransient<IFileMetadataRepository, RepositoriesStub>();
//builder.Services.AddTransient<FileService>();

// ВАРИАНТ Postgres ONLY
//builder.Services.AddDbContext<ApplicationDbContext>();
//builder.Services.AddTransient<IFileMetadataRepository, FileStorage>();
//builder.Services.AddTransient<IFileDataRepository, FileStorage>();
//builder.Services.AddTransient<FileService>();

// ВАРИАНТ Postgres + S3
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddTransient<IFileMetadataRepository, FileStorage>();
builder.Services.AddTransient<S3ServicesFactory>();
builder.Services.AddTransient<IAmazonS3>(opts => opts.GetRequiredService<S3ServicesFactory>().CreateClient());
builder.Services.AddTransient<IFileDataRepository>(opts => opts.GetRequiredService<S3ServicesFactory>().CreateStorage());
builder.Services.AddTransient<FileService>();

// ДОБАВЛЕНИЕ КОНТРОЛЛЕРОВ В КОНТЕЙНЕР ЗАВИСИМОСТЕЙ (IoC-контейнер)
builder.Services.AddControllers();

// СОЗДАНИЕ ПРИЛОЖЕНИЯ (С ЗАВИСИМОСТЯМИ И СЕРВИСАМИ)
var app = builder.Build();

// ВКЛЮЧЕНИЕ КОНТРОЛЛЕРОВ (МАППИНГ)
app.MapControllers();

// ВЫЗОВ АВТОМИГРАЦИЙ
await AutoApplyMigrationsWithBackoff();

// ВЫЗОВ АВТОСОЗДАНИЯ БАКЕТА В S3
await AutoEnsureS3BucketExistsWithBackoff();

// ЗАПУСК ПРИЛОЖЕНИЯ
app.Run();

// AutoApplyMigrationsWithBackoff - автомиграция для postgres с backoff-ми
// TODO: прочитать что такое backoff
async Task AutoApplyMigrationsWithBackoff()
{
    using var scope = app.Services.CreateScope();
    Console.WriteLine("Starting migrations processing...");

    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // примитивно заданные параметры backoff-а
    var maxRetryCount = 5;
    var initialDelayMs = 1000; // 1 секунда
    var maxDelayMs = 30000; // 30 секунд

    for (int i = 0; i < maxRetryCount; i++)
    {
        try
        {
            Console.WriteLine($"Attempt {i + 1} to connect to database...");
            // программный вызов применения миграций
            await db.Database.MigrateAsync();
            Console.WriteLine("Migrations were applied successfully if it exists");
            break;
        }
        catch (Exception ex) when (i < maxRetryCount - 1)
        {
            int delay = Math.Min(initialDelayMs * (int)Math.Pow(2, i), maxDelayMs);
            Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
            Console.WriteLine($"Waiting {delay}ms before next attempt...");
            // асинхронное ожидание
            await Task.Delay(delay);
        }
    }
}

// AutoEnsureS3BucketExistsWithBackoff - автоматическое обеспечение существования бакета с backoff-ми
async Task AutoEnsureS3BucketExistsWithBackoff()
{
    using var scope = app.Services.CreateScope();
    Console.WriteLine("Starting S3 bucket existence processing...");

    IAmazonS3 s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();

    string s3ConnectionProfile = Environment.GetEnvironmentVariable("S3_OPTIONS_PROFILE") ?? "default";
    IConfigurationSection s3Options = builder.Configuration.GetSection("S3Options").GetSection(s3ConnectionProfile);
    string bucketName = s3Options["BucketName"] ?? "default";

    var maxRetryCount = 5;
    var initialDelay = 1000; // 1 секунда
    var maxDelay = 30000; // 30 секунд

    for (int i = 0; i < maxRetryCount; i++)
    {
        try
        {
            Console.WriteLine($"Attempt {i + 1} to connect to s3 ...");
            // попытка проверить существование бакета или создать его
            await s3Client.EnsureBucketExistsAsync(bucketName);
            Console.WriteLine("S3 bucket existence processed");
            break;
        }
        catch (Exception ex) when (i < maxRetryCount - 1)
        {
            var delay = Math.Min(initialDelay * (int)Math.Pow(2, i), maxDelay);
            Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
            Console.WriteLine($"Waiting {delay}ms before next attempt...");
            // асинхронное ожидание
            await Task.Delay(delay);
        }
    }
}


// TODO: 
// 1. Автонастройка разных профилей appsetings.json для разных окружений (local, docker etc.) - РЕШЕНО ЧЕРЕЗ ИСПОЛЬЗОВАНИЕ ENV-ПЕРЕМЕННЫХ
// 2. Общий backoff-сервис
// 3. Вызов автомиграций (и подобных операций) с проверкой какого-либо условия
// 4. Отказаться от mcr.microsoft.com (Microsof Artifact Registry) в пользу Docker Hub
// + ПРОЧИЕ TODO по проекту (Ctrl + Shift + F -> поиск по "TODO")
