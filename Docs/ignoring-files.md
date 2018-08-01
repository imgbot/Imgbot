Some users may want to keep some images from being optimized. There can be a variety of reasons to want to keep an image in it's original state that differ from project to project.

If this is something you need in your project, ImgBot offers an ignore option.

 - This configuration is optional and is only required if there are specific images or folders of images you do not want touched
 - Accepts the syntax for searchPattern on [Directory.EnumerateFiles()](https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles)
 - Limits the images optimized by ImgBot by essentially ignoring them
 - When ignoring by filename no path is necessary
 - When ignoring by folder name full path from root is necessary

`.imgbotconfig`

Ignoring by extension

```
"ignoredFiles": [
    "*.jpeg"
]
```

Ignoring all images in a specific folder

```
"ignoredFiles": [
    "public/special_images/*"
]
```

Ignoring individual image files

```
"ignoredFiles": [
    "special-image1.png",
    "other-image1.png"
]
```

