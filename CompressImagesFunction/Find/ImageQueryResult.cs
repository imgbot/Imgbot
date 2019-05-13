namespace CompressImagesFunction.Find
{
    public class ImageQueryResult
    {
        public string[] ImagePaths { get; set; }

        // This is to signal that there are images on subsequent pages
        public bool HasMoreImages { get; set; }
    }
}
