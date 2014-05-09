FastLogAsync
============

Fast, convinient and simple c# class for file/console logging in asynchronous way. 

Grab single Log.cs and put it in your project or install a [package from NuGet].

` Install-Package FastLogAsync `

Configuration
-------------
**Enable/disable logging at compile time**

Define `FAST_LOG` compilation symbol in progect properties to allow logging in assembly you defined it in.

In addition, defne `FAST_TRACE` to also allow trace/debug logging.

These symbols help you to control performance impact for a build. If you don't define `FAST_LOG` - compiler will sipmply remove all log calls from resulting binary. 

In general case you need to have FAST_LOG defined for all assemblies that use logging, and `FAST_TRACE` for debug configurations only.

**Control logging at runtime**

You can use following static properties to configure logging.
```c#
// echo everything to console
Log.ConsoleOutputEnabled = false;
// save log lines to file
Log.FileOutputEnabled = true;
// enable/disable specific methods
// you still need to define FAST_LOG first though
Log.InfoLogEnabled = true;
Log.ErrorLogEnabled = true;
// This will only have effect if FAST_LOG and FAST_TRACE are defined
Log.TraceLogEnabled = true;
// Timestamp format for log messages
Log.TimeStampFormat = "HH:mm:ss.fff"; // Valid DateTime format string 
```

You can also define settings in application configuration file using the same names
```xml
<appSettings>
    <add key="ConsoleOutputEnabled" value="true"/>
</appSettings>
```
Usage
-----
You can use following static methods to log messages:
```c#
Log.Info("Hello, Log");
Log.Info("Hello, {0}", Log);
Log.Error("Something strange happened.");
Log.Error(exception);
Log.Trace("Hello, Log"); // this will aslo append caller method name, assembly name and line numbler  

```

Output
------
Logger will create daily files in `yyMMdd.log` format.
