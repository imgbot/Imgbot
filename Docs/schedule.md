You can set a schedule for your ImgBot PRs through the `.imgbotconfig` file.

 - This configuration is optional and is only required if you want less frequent pull requests
 - Accepts the following syntax: daily|weekly|monthly
 - Limits the PRs from ImgBot to once a day, once a week, or once a month respectively
 - The default behavior is to receive ImgBot PRs as images require optimization

This will effectively set the maximum frequency of pull requests submitted from ImgBot to your repo. This is ideal for projects that receive many image updates and don't want to see optimizations until they can all be batched up together. Most projects will benefit the most form the default setting of receiving image optimizations from ImgBot as new images are added to the project.

If you do want to set a schedule for ImgBot pull requests the current supported values are 'daily', 'weekly', and 'monthly'.

`.imgbotconfig`

```
{
    "schedule": "weekly"
}
```