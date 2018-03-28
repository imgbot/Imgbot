# ImgBot

ImgBot crawls all your image files in GitHub and submits pull requests after applying a lossless compression.
This will make the file size go down, but leave the dimensions and quality just as good.

![screenshot](https://imgbot.net/images/screen.png)

## Configuration

ImgBot supports optional configuration through a `.imgbotconfig` json file.
This is not a required step to using ImgBot and is only for more advanced scenarios.
This file should be placed in the root of the repository and set to your liking.

```
{
    "schedule": "daily" // daily|weekly|monthly
    "ignoredFiles": [
    	"*.jpg",                   // by extension
    	"image1.png",              // by filename
    	"public/special_images/*", // by folderpath
    ]
}
```

The following are the currently supported parameters.
If there are any configuration settings you would like to see supported,
please feel free to open an issue here in the repo or shoot an email over
to ImgBotHelp@gmail.com

 - schedule
    - optional
    - Accepts: daily|weekly|monthly
    - Limits the PRs from ImgBot to once a day, once a week, or once a month respectively
    - The default behavior is to receive ImgBot PRs as images require optimization
 - ignoredFiles
 	- optional
 	- Accepts the syntax for searchPattern on [Directory.EnumerateFiles()](https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles)
 	- Limits the images optimized by ImgBot by esentially ignoring them
 	- When ignoring by filename no path is neccesary, when ignoring by foldername full path from root is necessary


Find out more: https://imgbot.net/docs

## Contributing

All the code for ImgBot is available on GitHub. We will gladly accept contributions for the service, the website, and the documentation. This is where you can find out how to get set up to run locally as well as detailed information on exactly how ImgBot works.

https://imgbot.net/docs#contributing
