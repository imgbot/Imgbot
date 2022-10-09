
Imgbot uses compression algorithms to optimize images. While the file size is going down, the quality and dimensions remain intact.

To provide the strong optimization, Imgbot uses multiple compression algorithms. 

Imgbot currently implements the following algorithms:
 - [ImageMagick](http://www.imagemagick.org)
 - [Svgo](https://github.com/svg/svgo)
 - [MozJpeg](https://github.com/mozilla/mozjpeg)


By default, Imgbot uses the `LosslessCompress()` method. When setting up Imgbot, you can also switch to the aggressive `LossyCompression()` compression. Learn more about [the aggressive compression](#aggressive-compression).

After cloning the repo, the compression happens in-place. Once installed into any repo, Imgbot will run the selected compression routine on an ongoing basis and open pull requests to keep your images optimized.

To keep you updated, compression results are reported in the commit message body.
