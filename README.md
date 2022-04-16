# ntplibcs

## Description

This library is an attempt to implement the module [ntplib](https://github.com/cf-natali/ntplib) for Python in C#.

## Checked
- .Net 6.0
- .NET Framework 4.8

## Example

```C#
using NtpLibCs;


var c = new NtpClient();
var response = c.Request("time.windows.com", version: 3);

Console.WriteLine("offset         : " + response.Offset + " [s]");
var txTimeTicks = (long) (response.TxTime * 10000000);
var txTimeDatetime = new DateTimeOffset(ticks: (txTimeTicks + Ntp.SystemEpoch.Ticks), offset: TimeSpan.Zero);
Console.WriteLine("txTime         : " + txTimeDatetime);
Console.WriteLine("Leap           : " + NtpUtils.LeapToText(response.Leap));
Console.WriteLine("root delay     : " + response.RootDelay + " [s]");
Console.WriteLine("Ref ID         : " + NtpUtils.RefIdToText(response.RefId));

/*
 * offset         : -1.3222367763519287 [s]
 * txTime         : 2022/04/08 4:34:48 +00:00
 * Leap           : no warning
 * root delay     : 0.0020751953125 [s]
 * Ref ID         : 25.66.230.1
 */
```

## LICENSE

This software is released under the MIT License, see LICENSE.

