<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SignalR.Flywheel.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" />
    <title>SignalR Flywheel</title>
    <style>
        body { font-family: 'Segoe UI'; padding: 5px 20px; }
        h1, h2, h3, h4, h5 { font-family: 'Segoe UI'; font-weight: normal; }
        #options { margin-bottom: 24px; width: 400px; padding: 6px 12px; }
        #options label { display: block; float: left; width: 150px;  }
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
                <option value="1">DirectEcho</option>
                <option value="2">Echo</option>
                <option value="3">Broadcast</option>
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

    <div id="stats">
        
    </div>

    <script src="Scripts/jquery-1.7.1.js" type="text/javascript"></script>
    <script src="Scripts/jquery.signalR.js" type="text/javascript"></script>
    <script src="signalr/hubs"></script>
    <script>

        var hub = $.connection.flywheel,
            $stats = $("#stats"),
            $onReceive = $("#onReceive"),
            $rate = $("#rate"),
            $payloadSize = $("#payloadSize");

        hub.updateStats = function (stats) {
            $stats.empty();
            $.each(stats, function (key, value) {
                if (typeof value === "number") {
                    value = value.toFixed(2);
                }
                $stats.append("<div><strong>" + key + ":</strong><span>" + value.toLocaleString() + "</span></div>");
            });
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

        $.connection.hub.start(init);
        
    </script>
</body>
</html>