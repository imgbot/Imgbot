
**Imgbot** uses compression algorithms to optimize images, and by default `LosslessCompress()` is used.
This means that while the file size is going down, the quality and dimensions remain intact.

When configuring your setup, you can choose between non-aggressive `LosslessCompress()` and aggressive `LossyCompression()` compression.

To provide the strong optimization, **Imgbot** utilizes multiple compression algorithms, and these are currently implemented:
 - [ImageMagick](http://www.imagemagick.org)
 - [Svgo](https://github.com/svg/svgo)
 - [MozJpeg](https://github.com/mozilla/mozjpeg)


The images are compressed in place after cloning the repo, and once installed into any repo, **Imgbot** will run the `LosslessCompress()` routine on an ongoing basis and open PRs to keep your images optimized.

The file size is measured before and after compression and the results are reported in the commit message body, to keep you updated.
