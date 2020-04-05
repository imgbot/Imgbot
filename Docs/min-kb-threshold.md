You can set a space saved threshold using the `.imgbotconfig` file.

 - This configuration is optional and is only required if you want to change the default threshold
 - Default setting is 10KB
 - Accepts only numbers as input (e.g. `"minKBReduced": 500` for a 500 KB threshold)
 - Can be used to limit the frequency of PRs Imgbot will open over time

`.imgbotconfig`

Setting 500 KB threshold

```
{
    "minKBReduced": 500
}
```

To disable this threshold and always open a PR no matter how much size is reduced unset the default
```
{
    "minKBReduced": null
}
```
