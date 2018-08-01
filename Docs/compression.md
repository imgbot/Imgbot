ImgBot uses [ImageMagick](http://www.imagemagick.org) to optimize images through the `LosslessCompress()` routine from the The .NET wrapper [Magick.NET](https://github.com/dlemstra/Magick.NET).
This means that while the file size is going down, the quality and dimensions remain intact.

The file size is measured before and after compression and the results are reported in the commit message body.

The images are compressed in place after cloning the repo.

Once installed into any repo, ImgBot will run the `LosslessCompress()` routine on an ongoing basis and open PRs to keep your images optimized.

