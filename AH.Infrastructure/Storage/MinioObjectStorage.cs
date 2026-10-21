using AH.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace AH.Infrastructure.Storage;

// One bucket for the application; the bucket is created on first use, so a fresh MinIO container needs no setup.
internal sealed class MinioObjectStorage(IMinioClient minio, IOptions<ObjectStorageOptions> options, ILogger<MinioObjectStorage> logger)
    : IObjectStorage
{
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    private string Bucket => options.Value.Bucket;

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        await minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(Bucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
            cancellationToken);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        using var buffer = new MemoryStream();
        try
        {
            await minio.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(Bucket)
                    .WithObject(key)
                    .WithCallbackStream((stream, ct) => stream.CopyToAsync(buffer, ct)),
                cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }

        return buffer.ToArray();
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        await minio.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(Bucket).WithObject(key), cancellationToken);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (!_bucketReady)
            {
                if (!await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(Bucket), cancellationToken))
                {
                    await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket), cancellationToken);
                    logger.LogInformation("Created object storage bucket {Bucket}", Bucket);
                }

                _bucketReady = true;
            }
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string Bucket { get; set; } = "astrahive";
}
