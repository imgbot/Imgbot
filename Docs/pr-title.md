You can set a pull request title for your Imgbot PRs through the `.imgbotconfig` file.

 - This configuration is optional and is only required if you want a custom title for your pr's
 - Available only for paid plans
 - Accepts any string written using github [markdown](https://docs.github.com/en/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax)
 - The default title to display is: "[ImgBot] Optimize images"

`.imgbotconfig`

```
{
    "prTitle": "My title"
}
```
