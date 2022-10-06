To change the default threshold, you can set an optional space saved threshold using the `.imgbotconfig` file. 

- This option limits the Imgbot pull request frequency over time.
- Accepts only numbers as input (see examples below)
- The default value is 10 kilobytes.

`.imgbotconfig`

Setting 500 KB threshold:

```
{
    "minKBReduced": 500
}
```

To disable the threshold and open a pull request no matter how much size is reduced, unset the default:

```
{
    "minKBReduced": null
}
```
