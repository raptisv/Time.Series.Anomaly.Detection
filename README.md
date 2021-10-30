# Time Series Anomaly Detection - From Graylog to Grafana
It is usually not enough to just monitor a time series graph 24/7, you need to identify spikes and also get alerted.

# TL;DR
This project
1. Periodically fetches histogram data from Graylog, based on custom queries
2. Persists that information using SQLite
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
 
![alt text](https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png "") 

* Create a new dashboard and inside it a new `empty panel`
* When editing the panel 
  - Set the datasource to `Graylog2Grafana`
  - Select `timeserie`
  - Select `all_logs` <- This is just a demo predefined query. **All your new custom queries will be available here to select**

![alt text](https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png "") 

* Save the panel and you can start monitoring you custom Graylog queries 😊
