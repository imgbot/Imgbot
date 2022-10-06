Some users may want to keep some images from being optimized that depends on the project. Imgbot offers an optional ignore option for specific images or folders.

The ignore option accepts regex patterns and simple globbing. Ignoring by filename does not require a path, see examples below.

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

Ignoring nested folders

```
"ignoredFiles": [
    "**/test_images/**"
]
```

Ignoring paths that start with a pattern

```
"ignoredFiles": [
    "path/to/prefix*"
]
```
