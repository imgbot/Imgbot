Optionally, for more advanced scenarios, you can configure Imgbot using an `.imgbotconfig` json file in the root of your GitHub repository <!-- exact filename? can we remove the json? --> and set to your liking.

Here is an example `.imgbotconfig` file setup that shows some of the options:

```
{
    "schedule": "daily", // daily|weekly|monthly
    "ignoredFiles": [
        "*.jpg",                   // ignore by extension
        "image1.png",              // ignore by filename
        "public/special_images/*", // ignore by folderpath
    ],
    "aggressiveCompression": "true", // true|false
    "compressWiki": "true", // true|false
    "minKBReduced": 500, // delay new prs until size reduction meets this threshold (default to 10)
    "prTitle" : "Your own pr title",        
    "prBody" : " Text before optimization ratio {optimization_ratio} Text after optimization ratio 
                 Text before optimization details {optimization_details} Text after optimization details",
}
```

Outside of the `.imgbotconfig` file, you can configure additional options by logging into [imgbot.net](https://imgbot.net/app). This is the current list of settings supported in this UI:

 - Default branch override (Imgbot will look after a different branch instead of the repository default branch)

For any other option you would like to see supported, feel free to [open an issue](https://github.com/dabutvin/Imgbot/issues/new) or shoot an email over
to *help@imgbot.net*.
