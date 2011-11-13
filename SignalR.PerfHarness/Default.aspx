<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SignalR.PerfHarness.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SignalR Performance Harness</title>
    <style>
        body { font-family: 'Segoe UI'; padding: 5px 20px; }
        h1, h2, h3, h4, h5 { font-family: 'Segoe UI'; font-weight: normal; }
        #options { margin-bottom: 24px; width: 400px; padding: 6px 12px; }
        #options label { display: block; float: left; width: 150px;  }
        #interval { width: 50px; }
        #stats div { clear: both; font-size: 18px; margin-left: 50px; }
        #stats strong { display: block; float: left; width: 150px; }
        #stats span { display: block; float: left; width: 150px; text-align: right; }
    </style>
</head>
<body>
    <h1>SignalR Performance Harness</h1>
    
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
            <label for="interval">Broadcast interval:</label>
            <input id="interval" value="0" maxlength="6" /> (0 = disabled)
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
    </fieldset>

    <div id="stats">
        
    </div>

    <script src="Scripts/jquery-1.6.4.js" type="text/javascript"></script>
    <script src="Scripts/jquery.signalR.js" type="text/javascript"></script>
    <script src="signalr/hubs"></script>
    <script>

        var hub = $.connection.perf,
            $stats = $("#stats"),
            $onReceive = $("#onReceive"),
            $interval = $("#interval"),
            $payloadSize = $("#payloadSize");

        hub.updateStats = function (stats) {
            $stats.empty();
            $.each(stats, function (key, value) {
                $stats.append("<div><strong>" + key + ":</strong><span>" + value.toLocaleString() + "</span></div>");
            });
        };

        hub.onReceiveChanged = function (behavior) {
            $onReceive.val(behavior);
        };

        hub.onIntervalChanged = function (interval) {
            $interval.val(interval);
        };

        hub.onSizeChanged = function (size) {
            $payloadSize.val(size);
        };

        function init() {
            $onReceive.change(function () {
                hub.setOnReceive($onReceive.val());
            });
            $interval.change(function () {
                hub.setBroadcastInterval($interval.val());
            });
            $payloadSize.change(function () {
                hub.setBroadcastSize($payloadSize.val());
            });
        }

        $.connection.hub.start(init);
        
    </script>
</body>
</html>