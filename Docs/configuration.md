ImgBot supports optional configuration through a `.imgbotconfig` json file.
This is not a required step to using ImgBot and is only for more advanced scenarios.

This file should be placed in the root of the repository and set to your liking.
See [this past issue](https://github.com/dabutvin/ImgBot/issues/49) for details about the location for this file.

Here is an example .imgbotconfig setup that shows some of the options.

```
{
    "schedule": "daily", // daily|weekly|monthly
    "ignoredFiles": [
        "*.jpg",                   // ignore by extension
        "image1.png",              // ignore by filename
        "public/special_images/*", // ignore by folderpath
    ],
    "aggressiveCompression": "true" // true|false
}
```

If there are any configuration settings you would like to see supported,
please feel free to [open an issue](https://github.com/dabutvin/ImgBot/issues/new) in the repo or shoot an email over
to help@imgbot.net
