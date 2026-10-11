using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.VideoStorage;

namespace SurveillanceCameras.Infrastructure.Repository.VideoStorage;

/// <summary>Lưu video vào Cloudflare R2 qua giao thức S3.</summary>
public sealed class R2VideoStorageRepository : IVideoStorageRepository
{
    private readonly R2VideoStorageOptions _options;

    public R2VideoStorageRepository(IOptions<R2VideoStorageOptions> options)
    {
        _options = options.Value;
    }

    public Task UploadFileAsync(VideoStorageTarget target, string key, string filePath, string contentType,
        CancellationToken cancellationToken = default)
        => RunAsync(target, "tải file lên", client => client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = target.Bucket,
            Key = key,
            FilePath = filePath,
            ContentType = contentType,
            // R2 không hỗ trợ ký nội dung kiểu streaming (chunked) của SDK.
            DisablePayloadSigning = true
        }, cancellationToken), cancellationToken);

    public Task DeleteAsync(VideoStorageTarget target, string key, CancellationToken cancellationToken = default)
        => RunAsync(target, "xoá file", client => client.DeleteObjectAsync(target.Bucket, key, cancellationToken),
            cancellationToken);

    public Task<VideoStorageUsage> GetUsageAsync(VideoStorageTarget target, CancellationToken cancellationToken = default)
        => RunAsync(target, "đọc bucket", async client =>
        {
            long bytes = 0;
            var objects = 0;
            string? continuation = null;
            do
            {
                var page = await client.ListObjectsV2Async(
                    new ListObjectsV2Request { BucketName = target.Bucket, ContinuationToken = continuation }, cancellationToken);
                foreach (var item in page.S3Objects ?? [])
                {
                    bytes += item.Size ?? 0;
                    objects++;
                }

                continuation = page.IsTruncated == true ? page.NextContinuationToken : null;
            } while (continuation is not null);

            return new VideoStorageUsage(bytes, objects);
        }, cancellationToken);

    private async Task<T> RunAsync<T>(VideoStorageTarget target, string action, Func<AmazonS3Client, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        using var client = new AmazonS3Client(
            new BasicAWSCredentials(target.AccessKeyId, target.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = string.Format(_options.EndpointTemplate, target.CloudflareAccountId),
                AuthenticationRegion = "auto",
                ForcePathStyle = true,
                Timeout = TimeSpan.FromSeconds(_options.UploadTimeoutSeconds),
                // SDK mặc định gắn checksum CRC vào mọi request; R2 không nhận.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });

        try
        {
            return await operation(client);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            throw new VideoStorageException(
                $"R2 từ chối khi {action} ({ex.ErrorCode}): sai Access Key/Secret Key, hoặc khoá không có quyền đọc-ghi bucket \"{target.Bucket}\".", ex);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new VideoStorageException($"Không tìm thấy bucket \"{target.Bucket}\" ở tài khoản Cloudflare này.", ex);
        }
        catch (AmazonServiceException ex)
        {
            throw new VideoStorageException($"R2 báo lỗi khi {action}: {ex.ErrorCode ?? ex.StatusCode.ToString()} — {ex.Message}", ex);
        }
        // Lỗi mạng/hết giờ/Account ID sai (không phân giải được tên miền).
        catch (Exception ex) when (ex is AmazonClientException or HttpRequestException or IOException
                                       || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new VideoStorageException($"Không kết nối được tới R2 khi {action}: {ex.Message}", ex);
        }
    }
}
