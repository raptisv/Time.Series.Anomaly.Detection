# Time Series Anomaly Detection - From Graylog to Grafana
It is usually not enough to just monitor a time series graph 24/7, you need to identify spikes and also get alerted.

![alt text](https://github.com/raptisv/Time.Series.Anomaly.Detection/blob/main/Graylog2Grafana.Web/wwwroot/img/Graylog2Grafana_3.png "")

# TL;DR
This project
1. Periodically fetches histogram data from Graylog, based on custom queries
2. Persists that information using SQLite to prevent spamming on Graylog api
3. Identifies upwards or downwards spikes realtime (optionally sends an alert)
4. Serves all the above information in Grafana using [SimpleJson datasource](https://grafana.com/grafana/plugins/grafana-simple-json-datasource/) 

# The problem

Graylog dashboards can display time series graphs, based on custom queries, but the experience is not the best, while Grafana is great for the job. Also, you may need to process the information before displaying it to the graph. By far, the most common data processing upon time series information is anomaly detection. 

Additionaly, it is often not easy to predict in advance, all the metrics you need to be displayed in Grafana (usually through Prometheus). Since all the information is usually on Graylog, you may need to monitor some custom Graylog queries for some time, before turning it to metrics.

# Run with Docker
1. Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop)
2. From VS right click on `..\Graylog2Grafana.Web\DockerFile` and select `Build Docker Image`
3.  On directory `..\Time.Series.Anomaly.Detection\docker` execute `docker-compose -p graylog2grafana up -d`. This will create a docker compose including Graylog, Grafana & the current Graylog2Grafana solution
4. When compose is up, you can navigate to [Graylog](http://localhost:9000/), [Grafana](http://localhost:3000/) and [Graylog2Grafana](http://localhost:5002/) 
5. At this point Graylog2Grafana has already started loading histogram data from Graylog. You can [add, edit or delete custom Graylog queries here](http://localhost:5002/). Only thing left to do is to setup a Grafana dashboard in order to display that data

# Setup Grafana
* Navigate to [Grafana](http://localhost:3000/)
* Install plugin [SimpleJson](http://localhost:3000/plugins/grafana-simple-json-datasource?page=overview)
* Navigate to [datasources](http://localhost:3000/datasources) and add SimpleJson as a datasource
  - Set the name to `Graylog2Grafana`
  - Set the url to `http://localhost:5002`. 
  - Set the access to `Browser`
  - Click on `save & test`
 
![alt text](https://github.com/raptisv/Time.Series.Anomaly.Detection/blob/main/Graylog2Grafana.Web/wwwroot/img/Graylog2Grafana_0.png "")

* Create a new dashboard and inside it a new `empty panel`
* When editing the panel 
  - Set the datasource to `Graylog2Grafana`
  - Select `timeserie`
  - Select `all_logs` <- This is just a demo predefined query. **All your new custom queries will be available here to select**

![alt text](https://github.com/raptisv/Time.Series.Anomaly.Detection/blob/main/Graylog2Grafana.Web/wwwroot/img/Graylog2Grafana_2.png "")

# Anomaly detection 
Anomaly/spike detection is executed in the background, everytime the queries refresh their data. We usually care about realtime data, that is why **it will produce an alert only if an anomaly was detected in the last minute**. 

> The library used for anomaly detection is ML.NET. You will find and excellent guide of how to start with ML.NET time series in this [documentation](https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/phone-calls-anomaly-detection).

In order to see the spikes detected in Grafana, we have to setup **Dashboard annotations**. 
Go to the `Dashboard settings` and add a new `Annotation query` with the following settings
1. Set Name to `Downwards spikes` or something similar
2. Set Data source to `Graylog2Grafana`
3. Set the query to `Downwards#all_logs` <- This is a convention explained below

The query field on Step 3. is a convention we have to make. The format here is `{MonitorType}#{query_name_a}#{query_name_b}` (notice the `#` separator). Available values for `MonitorType` are `Downwards` and `Upwards` depending on the type of spikes we wish to see in the dashboard. After the type we include the Graylog custom query names we wish to see in the dashboard. For example the `Downwards#query_a#query_b#query_c` means that we want to see all downwards spikes for custom Graylog queries *query_a*, *query_b* and *query_c*. If you wish to monitor all your queries you may set a `*` like `Downwards#*`.

![alt text](https://github.com/raptisv/Time.Series.Anomaly.Detection/blob/main/Graylog2Grafana.Web/wwwroot/img/Graylog2Grafana_1.png "")

# Alerts
Detected spikes are going to be displayed in the Grafana dashboard as explained above. Optionaly, you may setup a Slack channel id and auth token in the configuration, in order to receive the relevant notification in Slack also. 

TODO: More ways to notify will be added and an easy way to configure will be exposed.

Enjoy monitoring you custom Graylog queries 😊
