/**
 * Theme: Adminto - Responsive Bootstrap 5 Admin Dashboard
 * Author: Coderthemes
 * Module/App: Dashboard
 */

window.loadDashboard = function () {
  //
  // Total Orders Chart
  //
  var colors = ["#727cf5", "#0acf97", "#fa5c7c", "#ffbc00"];
  var dataColors = $("#total-orders-chart").data("colors");
  if (dataColors) {
    colors = dataColors.split(",");
  }
  var options1 = {
    series: [65],
    chart: {
      type: "radialBar",
      height: 81,
      width: 81,
      sparkline: {
        enabled: false,
      },
    },
    plotOptions: {
      radialBar: {
        offsetY: 0,
        hollow: {
          margin: 0,
          size: "50%",
        },
        dataLabels: {
          name: {
            show: false,
          },
          value: {
            offsetY: 5,
            fontSize: "14px",
            fontWeight: "600",
            formatter: function (val) {
              return val + "k";
            },
          },
        },
      },
    },
    grid: {
      padding: {
        top: -18,
        bottom: -20,
        left: -20,
        right: -20,
      },
    },
    colors: colors,
  };

  new ApexCharts(
    document.querySelector("#total-orders-chart"),
    options1
  ).render();

  //
  // New Users Chart
  //
  var colors = ["#727cf5", "#0acf97", "#fa5c7c", "#ffbc00"];
  var dataColors = $("#new-users-chart").data("colors");
  if (dataColors) {
    colors = dataColors.split(",");
  }
  var options2 = {
    series: [75],
    chart: {
      type: "radialBar",
      height: 81,
      width: 81,
      sparkline: {
        enabled: false,
      },
    },
    plotOptions: {
      radialBar: {
        offsetY: 0,
        hollow: {
          margin: 0,
          size: "50%",
        },
        dataLabels: {
          name: {
            show: false,
          },
          value: {
            offsetY: 5,
            fontSize: "14px",
            fontWeight: "600",
            formatter: function (val) {
              return val + "k";
            },
          },
        },
      },
    },
    grid: {
      padding: {
        top: -18,
        bottom: -20,
        left: -20,
        right: -20,
      },
    },
    colors: colors,
  };

  new ApexCharts(document.querySelector("#new-users-chart"), options2).render();

  //
  // data-visits- CHART
  //
  var colors = ["#5b69bc", "#35b8e0", "#10c469", "#fa5c7c", "#e3eaef"];
  var dataColors = $("#data-visits-chart").data("colors");
  if (dataColors) {
    colors = dataColors.split(",");
  }

  var options = {
    chart: {
      height: 277,
      type: "donut",
    },
    series: [65, 14, 10, 45],
    legend: {
      show: true,
      position: "bottom",
      horizontalAlign: "center",
      verticalAlign: "middle",
      floating: false,
      fontSize: "14px",
      offsetX: 0,
      offsetY: 7,
    },
    labels: ["Direct", "Social", "Marketing", "Affiliates"], // Age groups
    colors: colors,
    stroke: {
      show: false,
    },
  };

  var chart = new ApexCharts(
    document.querySelector("#data-visits-chart"),
    options
  );

  chart.render();

  //
  // Statistics CHART
  //
  ///
  var colors = ["#5b69bc", "#10c469", "#fa5c7c", "#f9c851"];
  var dataColors = $("#statistics-chart").data("colors");
  if (dataColors) {
    colors = dataColors.split(",");
  }

  var options = {
    series: [
      {
        name: "Open Campaign",
        type: "bar",
        data: [89.25, 98.58, 68.74, 108.87, 77.54, 84.03, 51.24],
      },
    ],
    chart: { height: 301, type: "line", toolbar: { show: false } },
    stroke: {
      width: 0,
      curve: "smooth",
    },
    plotOptions: {
      bar: {
        columnWidth: "20%",
        barHeight: "70%",
        borderRadius: 5,
      },
    },
    xaxis: {
      categories: ["2019", "2020", "2021", "2022", "2023", "2024", "2025"],
    },
    colors: colors,
  };

  var chart = new ApexCharts(
    document.querySelector("#statistics-chart"),
    options
  );

  chart.render();

  //
  // REVENUE AREA CHART
  //
  ///
  var colors = ["#5b69bc", "#10c469", "#fa5c7c", "#f9c851"];
  var dataColors = $("#revenue-chart").data("colors");
  if (dataColors) {
    colors = dataColors.split(",");
  }

  var options = {
    series: [
      {
        name: "Total Income",
        data: [
          82.0, 85.0, 70.0, 90.0, 75.0, 78.0, 65.0, 50.0, 72.0, 60.0, 80.0,
          70.0,
        ],
      },
      {
        name: "Total Expenses",
        data: [
          30.0, 32.0, 40.0, 35.0, 30.0, 36.0, 37.0, 28.0, 34.0, 42.0, 38.0,
          30.0,
        ],
      },
    ],
    stroke: {
      width: 3,
      curve: "straight",
    },
    chart: {
      height: 299,
      type: "line",
      zoom: {
        enabled: false,
      },
      toolbar: { show: false },
    },
    dataLabels: {
      enabled: false,
    },
    xaxis: {
      categories: [
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
      ],
    },
    colors: colors,
    tooltip: {
      shared: true,
      y: [
        {
          formatter: function (y) {
            if (typeof y !== "undefined") {
              return "$" + y.toFixed(2) + "k";
            }
            return y;
          },
        },
        {
          formatter: function (y) {
            if (typeof y !== "undefined") {
              return "$" + y.toFixed(2) + "k";
            }
            return y;
          },
        },
      ],
    },
  };

  var chart = new ApexCharts(document.querySelector("#revenue-chart"), options);

  chart.render();
};

window.renderLineChart = function (tickerData) {
    var colors = ["#39afd1", "#fa5c7c", "#727cf5", "#0acf97", "#ffbc00", "#5b69bc", "#10c469", "#f9c851"];
    var tickers = Object.keys(tickerData);
    var series = [];
    var annotations = { xaxis: [] };
    var legend = { show: true };

    // Use a persistent chart instance to avoid full redraw
    if (!window._dashboardChart) {
        var options = {
            series: [],
            chart: {
                type: "line",
                height: "100%",
                width: "100%",
                id: "dashboardChart",
                animations: {
                    enabled: false
                }
            },
            xaxis: {
                categories: []
            },
            colors: colors,
            legend: legend,
            annotations: { xaxis: [] },
            tooltip: {
                shared: true,
                custom: function({series, seriesIndex, dataPointIndex, w}) {
                    var ticker = tickers[seriesIndex];
                    var orderLabels = tickerData[ticker].orders
                        .filter(o => o.time === w.globals.categoryLabels[dataPointIndex])
                        .map(o => "Order: " + o.side + " @ " + o.price).join("<br>");
                    var signalLabels = tickerData[ticker].signals
                        .filter(s => s.time === w.globals.categoryLabels[dataPointIndex])
                        .map(s => "Signal: " + s.type + " (" + s.confidence + ")").join("<br>");
                    return orderLabels + (orderLabels && signalLabels ? "<br>" : "") + signalLabels;
                }
            }
        };
        window._dashboardChart = new ApexCharts(document.querySelector("#line-chart-annotations"), options);
        window._dashboardChart.render();
        // Make chart fill parent container
        document.querySelector("#line-chart-annotations").style.height = "100%";
        document.querySelector("#line-chart-annotations").style.width = "100%";
    }

    // Prepare new series and annotations
    tickers.forEach(function (ticker, idx) {
        var prices = tickerData[ticker].prices.map(p => p.close);
        var labels = tickerData[ticker].prices.map(p => p.time);
        series.push({
            name: ticker + " (" + tickerData[ticker].orders.length + " orders, " + tickerData[ticker].signals.length + " signals)",
            data: prices
        });
        tickerData[ticker].orders.forEach(function (order) {
            annotations.xaxis.push({
                x: order.time,
                borderColor: colors[idx % colors.length],
                label: {
                    text: ticker + " Order: " + order.side,
                    style: {
                        background: colors[idx % colors.length],
                        color: '#fff'
                    }
                }
            });
        });
        tickerData[ticker].signals.forEach(function (signal) {
            annotations.xaxis.push({
                x: signal.time,
                borderColor: colors[idx % colors.length],
                label: {
                    text: ticker + " Signal: " + signal.type,
                    style: {
                        background: colors[idx % colors.length],
                        color: '#fff'
                    }
                }
            });
        });
    });

    // Update chart data without full redraw
    window._dashboardChart.updateOptions({
        series: series,
        xaxis: {
            categories: tickerData[tickers[0]].prices.map(p => p.time)
        },
        annotations: annotations
    }, false, true);
};
