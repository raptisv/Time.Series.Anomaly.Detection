
$(document).ready(function () {

    const verticalLinePlugin = {
        getLinePosition: function (chart, pointIndex) {
            const meta = chart.getDatasetMeta(0); // first dataset is used to discover X coordinate of a point
            const data = meta.data;
            return data[pointIndex]._model.x;
        },
        renderVerticalLine: function (chartInstance, pointIndex, color) {
            const lineLeftOffset = this.getLinePosition(chartInstance, pointIndex);
            const scale = chartInstance.scales['y-axis-0'];
            const context = chartInstance.chart.ctx;

            // render vertical line
            context.beginPath();
            context.lineWidth = 2;
            context.strokeStyle = color;
            context.moveTo(lineLeftOffset, scale.top);
            context.lineTo(lineLeftOffset, scale.bottom);
            context.stroke();
        },

        afterDatasetsDraw: function (chart, easing) {
            if (chart.config.lineAtIndex) {
                chart.config.lineAtIndex.forEach(pointIndex => {
                    var pointReal = chart.data.datasets[0].data[pointIndex];
                    var color = '#ff0000';

                    if (chart.data.datasets.length >= 3) {
                        var pointUpper = chart.data.datasets[1].data[pointIndex];
                        var pointLower = chart.data.datasets[2].data[pointIndex];

                        isUpTrend = pointReal > pointUpper;
                        isDownTrend = pointReal < pointLower;
                        color = '#ff0000';
                    }

                    return this.renderVerticalLine(chart, pointIndex, color);
                });
            }
        }
    };

    Chart.plugins.register(verticalLinePlugin);

    reloadSingle(monitorSeriesId);
    setInterval(function () {
        reloadSingle(monitorSeriesId);
    }, 10000);

    $('#sensitivity-range').change(function () {
        var sensitivity = $(this).val();
        $('#lbl-sensitivity').text(sensitivity);
        updateSensitivity(monitorSeriesId, sensitivity, function () {
            reloadSingle(monitorSeriesId);
        });
    });
});

function updateSensitivity(itemId, sensitivity, callback) {
    fetch(`/MonitorSeries/SetSensitivity?id=${itemId}&sensitivity=${sensitivity}`, {
        method: 'put',
        headers: {
            'Content-Type': 'application/json'
        },
    })
    .then(data => callback(data));
}

function fetchMonitorDetectData(itemId, callback) {
    fetch(`/MonitorSeries/DetectionResult?id=${itemId}`, {
        method: 'get',
        headers: {
            'Content-Type': 'application/json'
        },
    })
    .then(response => response.json())
    .then(data => callback(data));
}

function reloadSingle(itemId) {
    fetchMonitorDetectData(itemId, function (data) {
        var customOptions = {
            Height: 500,
            scales: {
                xAxes: [{
                    display: true
                }],
                yAxes: [{
                    ticks: {
                        beginAtZero: true
                    },
                    gridLines: {
                        drawOnChartArea: false
                    }
                }]
            },
            layout: {
                padding: {
                    bottom: 0
                }
            }
        };

        drawHistogram(data, $(`#canvas-wrapper-single-${itemId}`), `canvas-element-single-${itemId}`, customOptions);
    });
}

function reverseArr(input) {
    var ret = new Array;
    for (var i = input.length - 1; i >= 0; i--) {
        ret.push(input[i]);
    }
    return ret;
}

function drawHistogram(data, $element, canvasId, customOptions) {
    var labels = [];
    var values = [];
    var uppers = [];
    var lowers = [];
    var alertsIndex = [];

    if (data == null) {
        data = [];
    }

    data = reverseArr(data);
    $.each(data, function (index, value) {

        var mFormat = 'HH:mm';//'DD-MM-YYYY';
        labels.push(moment.unix(value.UnixTimestamp).format(mFormat));

        values.push(value.Data);
        uppers.push(value.Upper.toFixed(2));
        lowers.push(value.Lower.toFixed(2));

        if (value.IsAnomaly) {
            alertsIndex.push(index);
        }
    });

    var chartData = {
        labels: labels,
        datasets: [{
            label: 'Actual value',
            borderColor: 'rgb(54, 162, 235)',
            fill: false,
            data: values
        },
        {
            label: 'Upper expected limit',
            fill: false,
            backgroundColor: 'rgb(0,0,0)',
            data: uppers,
            pointRadius: 0
        },
        {
            label: 'Lower expected limit',
            fill: '-1', //fill until previous dataset
            backgroundColor: 'rgb(0,0,0)',
            data: lowers,
            pointRadius: 0
        }
        ]
    };

    $element.html(`<canvas id="${canvasId}" height="${(customOptions.Height || 90)}"></canvas>`);
    var ctx = document.getElementById(canvasId).getContext("2d");

    var options = {
        responsive: true,
        maintainAspectRatio: false,
        legend: false,
        title: {
            display: false
        },
        tooltips: {
            mode: 'index',
            intersect: false,
        },
        plugins: {
            filler: {
                propagate: false
            },
        },
        elements: {
            point: {
                radius: 0
            }
        },
        animation: {
            duration: 0
        }
    };

    new Chart(ctx, {
        type: 'line',
        data: chartData,
        options: {
            ...options,
            ...customOptions
        },
        lineAtIndex: alertsIndex
    });
}
