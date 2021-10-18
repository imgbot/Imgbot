You can set a pull request body for your Imgbot PRs through the `.imgbotconfig` file.

- This configuration is optional and is only required if you want a custom body for your pr's
- Available only for paid plans
- Accepts any string written using github [markdown](https://docs.github.com/en/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax)
- The default pr body to display is the one from [here](https://imgbot.net/images/screen.png?cache=2)

`.imgbotconfig`

```
{
    "prBody" : " Text before optimization ratio {optimization_ratio} Text after optimization ratio 
                 Text before optimization details {optimization_details} Text after optimization details",
}
```
