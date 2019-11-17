If your repository has a wiki with images, you can opt in to optimize these images as well as your code repo.

All of the images in the wiki will be updated directly on the default branch. This is due to the lack of branch management and no pull requests available within GitHub wikis.

By adding `compressWiki` in the `.imgbotconfig` file as displayed below, you will enable this feature.

```
{
    "schedule": "daily", // daily|weekly|monthly
    "ignoredFiles": [
        "*.jpg",                   // ignore by extension
        "image1.png",              // ignore by filename
        "public/special_images/*", // ignore by folderpath
    ],
    "aggressiveCompression": "true" // true|false
    "compressWiki": "true" // true|false
}
```
