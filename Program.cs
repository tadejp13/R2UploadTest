// See https://aka.ms/new-console-template for more information
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;

Console.WriteLine("R2 test");

// r2
var tmpBucket = "yourBucket";
var s3Key = "yourKey";
var s3Secret = "yourSecret";
var s3Url = "your s3 url";


AWSConfigsS3.UseSignatureVersion4 = true;
var s3Config =
    new AmazonS3Config
    {
        ServiceURL = s3Url,
        ForcePathStyle = true,
    };

using IAmazonS3 client = new AmazonS3Client(s3Key, s3Secret, s3Config);

var objectKey = $"snow-{DateTime.UtcNow.ToString("HH-mm-ss")}.mp4";

var initReq = new InitiateMultipartUploadRequest()
{
    BucketName = tmpBucket,
    Key = objectKey,
};

var init = await client.InitiateMultipartUploadAsync(initReq);
var uploadId = init.UploadId;

var req = new GetPreSignedUrlRequest()
{
    Verb = HttpVerb.PUT,
    BucketName = tmpBucket,
    Key = objectKey,
    Expires = DateTime.Now.AddDays(1),
    UploadId = uploadId,
    PartNumber = 1,
};

var url = client.GetPreSignedURL(req);

List<PartETag> uploadEtags = new();
HttpClient httpClient = new HttpClient();
var response = await httpClient.PutAsync(url, new ByteArrayContent(File.ReadAllBytes("snowflake.mp4")));
Console.WriteLine(response);

response.EnsureSuccessStatusCode();

response.Headers.TryGetValues("ETag", out var etags);
Console.WriteLine(etags);
uploadEtags.Add(new(1, etags.FirstOrDefault().Replace("\"", "")));

Console.WriteLine(JsonConvert.SerializeObject(uploadEtags));

var partETags = new List<Amazon.S3.Model.PartETag>();
uploadEtags.ForEach(
    tag =>
    {
        partETags.Add(new Amazon.S3.Model.PartETag(tag.PartNumber, @$"""{tag.ETag}"""));
    }
);

var completeMPURequest = new CompleteMultipartUploadRequest()
{
    BucketName = tmpBucket,
    UploadId = uploadId,
    Key = objectKey,
    PartETags = partETags
};

var completeMPUResponse = await client.CompleteMultipartUploadAsync(completeMPURequest);


