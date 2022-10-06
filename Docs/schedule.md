The default behavior is to receive Imgbot pull requests as soon as images require optimization. To see less frequent pull requests, you can set an optional schedule for your Imgbot pull requests through the `.imgbotconfig` file. 

Set the maximum pull request frequency submitted from Imgbot to your repo if your project receives many image updates and you don't want to see optimizations until they can all be batched up together. Most projects will benefit the most from the default setting of receiving image optimizations from Imgbot as soon as new images are added to the project.

If you do want to set a schedule for Imgbot pull requests, the current supported values are 'daily', 'weekly', and 'monthly'.

`.imgbotconfig` example for weekly pull requests:

```
{
    "schedule": "weekly"
}
```
