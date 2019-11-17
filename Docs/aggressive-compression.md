You can maximize Imgbot's compression through the `.imgbotconfig` file.

 - This configuration is optional and is only required if you require a more aggressive compression of images
 - Accepts the syntax: true|false
 - Uses a more aggressive compression, rather than the lossless compression that is used by default

`.imgbotconfig`

Using full compression capabilities

```
{
    "aggressiveCompression": true
}
```

Using lossless compression (default)
```
{
    "aggressiveCompression": false
}
```
