<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SignalR.Flywheel.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" />
    <title>SignalR Flywheel</title>
    <style>
        body { font-family: 'Segoe UI'; padding: 0 20px; margin: 0; }
        h1, h2, h3, h4, h5 { font-family: 'Segoe UI'; font-weight: normal; margin: 0 0 5px; }
        table { border-collapse: collapse; background-color: #fff }
            table tbody tr { background-color: #fdff6d }
            table td { border: 1px solid #808080; }
        select, input[type=text] { margin: 2px 0 }
        #options { margin-bottom: 5px; width: 400px; padding: 6px 12px; float: left; }
        #options label { display: block; float: left; width: 150px;  }
        #chart {
            float: left;
            clear: right;
            margin-top: -45px;
            width: 700px;
            height: 200px;
        }
        #rate { width: 50px; }
        #stats div { clear: both; font-size: 18px; margin-left: 50px; }
        #stats strong { display: block; float: left; width: 250px; }
        #stats span { display: block; float: left; width: 150px; text-align: right; }
    </style>
</head>
<body>
    <h1>SignalR Flywheel</h1>
    
    <fieldset id="options"><legend>Endpoint Options</legend>
        <div>
            <label for="onReceive">On receive:</label>
            <select id="onReceive">
                <option value="0">Listen only</option>
                <option value="1">Echo</option>
                <option value="2">Broadcast</option>
            </select>
        </div>

        <div>
            <label for="rate">Broadcast rate:</label>
            <input id="rate" value="0" maxlength="5" /> (per second)
        </div>

        <div>
            <label for="payloadSize">Broadcast size:</label>
            <select id="payloadSize">
                <option value="32">32 bytes</option>
                <option value="64">64 bytes</option>
                <option value="128">128 bytes</option>
                <option value="256">256 bytes</option>
                <option value="1024">1024 bytes</option>
                <option value="4096">4096 bytes</option>
            </select>
        </div>

        <div>
            <a id="resetAvg" href="#">Reset average</a>
            <a id="forceGC" href="#">Force GC</a>
        </div>
    </fieldset>

    <div id="chart"></div>

    <table id="stats">
        <thead><tr></tr></thead>
        <tbody></tbody>
    </table>

    <script src="Scripts/jquery-1.7.2.js"></script>
    <script src="Scripts/jquery.color.js"></script>
    <script src="Scripts/jquery.signalR-0.5rc.js"></script>
    <script src="Scripts/highcharts.src.js"></script>
    <script src="signalr/hubs"></script>
    <script>
        jQuery.fn.flash = function (color, duration) {
            var current = this.css("backgroundColor");
            this.animate({ backgroundColor: "rgb(" + color + ")" }, duration / 2)
                .animate({ backgroundColor: current }, duration / 2);
        };

        (function () {
            var hub = $.connection.flywheel,
                $stats = $("#stats"),
                chart,
                $onReceive = $("#onReceive"),
                $rate = $("#rate"),
                $payloadSize = $("#payloadSize");

            hub.updateStats = function (stats) {
                var $theadRow = $stats.find("thead > tr"),
                    $tbody = $stats.find("tbody"),
                    $tr = $("<tr></tr>"),
                    series = chart.series[0],
                    shift = series.data.length > 20,
                    nowDate = new Date(),
                    now = nowDate.getHours() + ":" + nowDate.getMinutes() + ":" + nowDate.getSeconds();

                if (!$theadRow.find("th").length) {
                    // Init the header columns
                    $theadRow.append("<th>Time</th>");
                    $.each(stats, function (key, value) {
                        $theadRow.append("<th>" + key + "</th>");
                    });
                }

                $tr.append("<td>" + now + "</td>");

                // Add the data columns for each stat field
                $.each(stats, function (key, value) {
                    if (typeof value === "number") {
                        value = value.toFixed(2);
                    }
                    $tr.append("<td>" + value + "</td>");
                });

                $tbody.prepend($tr);
                //$tr.flash("255,216,0", 1000);
                $tr.animate({ backgroundColor: "#fff" }, 1000);

                // Update chart
                series.addPoint(stats.SendsPerSecond, /*redraw*/ true, shift);
            };

            hub.onReceiveChanged = function (behavior) {
                $onReceive.val(behavior);
            };

            hub.onRateChanged = function (rate) {
                $rate.val(rate);
            };

            hub.onSizeChanged = function (size) {
                $payloadSize.val(size);
            };

            function init() {
                $onReceive.change(function () {
                    hub.setOnReceive($onReceive.val());
                });
                $rate.change(function () {
                    hub.setBroadcastRate($rate.val());
                });
                $payloadSize.change(function () {
                    hub.setBroadcastSize($payloadSize.val());
                });
                $("#resetAvg").click(function (e) {
                    e.preventDefault();
                    hub.resetAverage();
                });
                $("#forceGC").click(function (e) {
                    /// <param name="e" type="jQuery.Event">Description</param>
                    var link = $("#forceGC"),
                        text = link.text(),
                        href = link.prop("href");

                    e.preventDefault();

                    link.text("Collecting...")
                        .prop("href", "");

                    hub.forceGC().done(function () {
                        link.text(text)
                            .prop("href", href);
                    });
                });
            }

            chart = new Highcharts.Chart({
                chart: {
                    renderTo: "chart",
                    type: "line",
                    marginRight: 130,
                    marginBottom: 25
                },

                title: { text: "" },

                yAxis: {
                    title: { text: "Messages /sec" }
                },

                tooltip: {
                    formatter: function () {
                        return "<strong>" + this.series.name + "</strong><br/>" + this.y;
                    }
                },

                legend: {
                    layout: "vertical",
                    align: "right",
                    verticalAlign: "top",
                    x: -10,
                    y: 100,
                    borderWidth: 0
                },

                series: [{ name: "Sends", data: [] }]
            });

            $.connection.hub.start(init);
        })();
    </script>
</body>
</html>