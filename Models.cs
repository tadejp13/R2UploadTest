namespace R2Test
{
    public class Models
    {
    }

    public class PartETag
    {
        public int PartNumber { get; set; }
        public string ETag { get; set; }
        public PartETag(int partNumber, string etag)
        {
            PartNumber = partNumber;
            ETag = etag;
        }
    }

}
