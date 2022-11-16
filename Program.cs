// See https://aka.ms/new-console-template for more information
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System.Diagnostics;

Console.WriteLine("R2 test");

// r2
var bucketName = "yourBucket";
var s3Key = "yourKey";
var s3Secret = "yourSecret";
var s3Url = "your s3 url";

AWSConfigsS3.UseSignatureVersion4 = true;
var s3Config =
    new AmazonS3Config
    {
        ServiceURL = s3Url,
        ForcePathStyle = true,
        SignatureVersion = "v4",
    };

using IAmazonS3 s3Client = new AmazonS3Client(s3Key, s3Secret, s3Config);

try
{
    var objectKey = $"snow-{DateTime.UtcNow.ToString("HH-mm-ss")}.mp4";

    var initReq = new InitiateMultipartUploadRequest()
    {
        BucketName = bucketName,
        Key = objectKey,
    
    };

    var init = await s3Client.InitiateMultipartUploadAsync(initReq);
    var uploadId = init.UploadId;

    var req = new GetPreSignedUrlRequest()
    {
        Verb = HttpVerb.PUT,
        BucketName = bucketName,
        Key = objectKey,
        Expires = DateTime.UtcNow.AddDays(1),
        UploadId = uploadId,
        PartNumber = 1,
    };

    var url = s3Client.GetPreSignedURL(req);
    url = url.Replace("+", "%2B");
    Console.WriteLine($"presigned url = {url}");
    List<PartETag> uploadEtags = new();
    HttpClient httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    var response = await httpClient.PutAsync(url, new ByteArrayContent(File.ReadAllBytes("snowflake.mp4")));
    response.EnsureSuccessStatusCode();
    Console.WriteLine(response);

    response.Headers.TryGetValues("ETag", out var etags);
    Console.WriteLine(etags);
    uploadEtags.Add(new(1, etags.FirstOrDefault().Replace("\"", "")));

    Console.WriteLine(JsonConvert.SerializeObject(uploadEtags));

    var partETags = new List<Amazon.S3.Model.PartETag>();
    uploadEtags.ForEach(
        tag =>
        {
            //partETags.Add(new Amazon.S3.Model.PartETag(tag.PartNumber, @$"""{tag.ETag.Replace("+", "%2B")}"""));
            partETags.Add(new Amazon.S3.Model.PartETag(tag.PartNumber, @$"""{tag.ETag}"""));
        }
    );

    var completeMPURequest = new CompleteMultipartUploadRequest()
    {
        BucketName = bucketName,
        UploadId = uploadId,
        Key = objectKey,
        PartETags = partETags
    };

    Console.WriteLine("");
    Console.WriteLine("");
    Console.WriteLine("");
    Console.WriteLine("");
    Console.WriteLine(JsonConvert.SerializeObject(completeMPURequest, Formatting.Indented));
 
    var completeMPUResponse = await s3Client.CompleteMultipartUploadAsync(completeMPURequest);
}
catch (Exception ex)
{
    Debug.WriteLine(ex);
    throw;
}