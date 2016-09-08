# Carbonator Service v2 #

A simple Windows Service that collects [Performance Counters](https://msdn.microsoft.com/en-us/library/windows/desktop/aa373083%28v=vs.85%29.aspx) and 
reports metrics to a [Graphite](http://graphite.readthedocs.org/en/latest/overview.html) or InfluxDB server.

See [releases](https://github.com/CryptonZylog/carbonator/releases) for change log/version history.


## v2 Breaking Changes ##

v2 is currently experimental and includes noticeable breaking changes requiring you to adjust your configuration (.config file).

- ``<graphite>
    `` element removed
    - Introduced ``<output>
        `` element which allows switching between graphite and influxdb via ``name`` attribute
        - Due to complexity with how paths are handled in graphite vs. how metrics are submitted in influxdb, only one template format would be supported
        - Changed ``path`` attribute in ``<counters/add>`` to ``template`` (semantics)
        - The template differs between influxdb and graphite. Graphite templates work just like in v1, however, v2 templates work as a prefix in their [line protocol](https://docs.influxdata.com/influxdb/v0.13/write_protocols/line/).
        - (e.g. ``<add template="disk-space,host=%HOST%,env=prod,disk=%COUNTER_INSTANCE%" ...>
            `` results in ``disk-space,host=MY-PC,env =prod,disk =C: C:=1132578 1473354688679524608`` written to influxdb)


## :new: InfluxDB output support ##

v2 introduces support for sending metrics to influxdb via HTTP POST. It is configured via ``<output>
`` entry:
``<add name="myinfluxdb" type="influxdb" postingUrl="http://localhost:8086/write?db=mydb" />``

As described above, the ``template=`` attribute becomes a prefix format for influxdb metric line.
Carbonator will then use counter's instance name (e.g. "C:" in case of a Logical Disk), or "value" if instance name is empty and supply that value.

Example data transmitted for performance counters:

- Category=``LogicalDisk`` Counter=``% Free Space`` Instance=``.+`` (regex)
  - Configuration: ``<add template="disk-space,host=%HOST%,env=prod,disk=%COUNTER_INSTANCE%" category="LogicalDisk" counter="% Free Space" instance=".+" />``
  - On-the-wire ``disk-space,host=MY-PC,env=prod,disk=C: C:=222123123 1473354688679524608``

- Category=``Memory`` Counter=``Available Bytes`` Instance=[empty]
  - Configuration: ``<add template="memory_avail,host=%HOST%,env=prod" category="Memory" counter="Available Bytes" instance="" />``
  - On-the-wire: ``memory_avail,host=MY-PC,env=prod value=1123123123 1473354688679524608``


Metrics are submitted in a batch (single POST) every 5 seconds (or as configured in the output setting).