# Carbonator Service #

A simple Windows Service that collects [Performance Counters](https://msdn.microsoft.com/en-us/library/windows/desktop/aa373083%28v=vs.85%29.aspx) and 
reports metrics to a [Graphite](http://graphite.readthedocs.org/en/latest/overview.html) server.

See [releases](https://github.com/CryptonZylog/carbonator/releases) for change log/version history.

## Compiling ##

#### Via command-line (CMD or PowerShell) ####

[Skip 1-3 if you already have nuget installed]

1. Carbonator uses nuget package manager for log4net and any future external packages
2. If you don't have nuget, see https://docs.nuget.org/consume/installing-nuget (command line utility)
3. Install nuget above and add path to nuget.exe to your PATH environment variable
4. Start your command prompt/powershell in directory where Carbonator.sln is located
5. Run ``nuget restore Carbonator.sln`` to restore packages
6. Run ``build.cmd`` to compile Carbonator
7. The binaries should be present in ``Carbonator\bin\Release`` directory

#### Via Visual Studio (2013/2015/CE) ####

1. Open Carbonator.sln
2. Build Solution (F6)
3. The binaries should be present in ``Carbonator\bin\Release`` directory

## Installation ##

Copy binaries to wherever directory you want application to run from. (e.g. ``C:\Program Files\Carbonator``)

### Console Mode ###

This mode is useful if you want to try out configuration first and see metrics reported.

Run ``Crypton.Carbonator.exe --console --verbose`` from Windows command line (or PowerShell).

### Windows Service Mode ###

1. Run ``install-service.cmd`` to install Carbonator Windows Service. (Run as administrator from the directory that contains Carbonator binaries).
2. Run ``net start carbonator`` to start Carbonator service, or use Services console (services.msc)

Carbonator uses NETWORK SERVICE Windows NT account.

## Configuration ##

Carbonator configuration is contained within the ``Crypton.Carbonator.exe.config`` XML file.

You will need to adjust ``/configuration/carbonator/graphite`` XML element settings to wherever your Graphite server is and port (if different).

The ``/configuration/carbonator/counters`` list can be used to select which performance counters are monitored and reported. You can use Windows Performance Monitor ``perfmon.msc``
to determine which counters you may be interested (and which are available).

The configuration for each performance counter element is self explanatory:

- `path` = the Graphite/Carbon metric path that will be reported. 
  You can use ``%HOST%`` special string in the `path` setting which will be replaced with the name of the current computer when metric is reported. Use ``%host%`` to force the hostname in lowercase.
- `category` = Performance Counter category
- `counter` = Performance Counter name
- `instance` = Performance Counter instance, needed by certain counters such as Processor (_Total to represent overall CPU usage or by specific CPU instead).
  For counters that do not have an instance, leave this attribute empty or remove it entirely.

Some sample performance counters are provided in default configuration file.

You may need to adjust `defaultCulture` attribute if your Windows installation is localized differently since performance counter names will be translated.

**It may be necessary to add ``NETWORK SERVICE`` (or another user account Carbonator is running as) to [Performance Log Users](https://technet.microsoft.com/en-us/library/cc785098%28v=ws.10%29.aspx) group or certain counters may not load or report zero for all values.**

## Miscellaneous ##

Carbonator will report errors in Windows Event Log (Application) under `carbonator` event source.

Default counter sampling is every 1000ms, with reports going out every 5000ms. Pickle protocol is not used, metrics are reported individually as-is.

Carbonator is licensed under MIT License.
