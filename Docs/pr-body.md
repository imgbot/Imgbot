To set an optional custom body for your pull requests, you can set a pull request body for your Imgbot pull requests using the `.imgbotconfig` file.

- Accepts any string written using [GitHub markdown](https://docs.github.com/en/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax).
- The default pull request body is one from [here](https://imgbot.net/images/screen.png?cache=2),
- For paid plans only.

`.imgbotconfig`

```
{
    "prBody" : " Text before optimization ratio {optimization_ratio} Text after optimization ratio 
                 Text before optimization details {optimization_details} Text after optimization details",
}
```
