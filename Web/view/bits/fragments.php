<?php

function DrawServer($server, $details = false) {
    $server = array_map('htmlspecialchars', $server);

    if ($server['website'] != "Unknown") {
        $website = '<a href="'.$server['website'].'">'.$server['website'].'</a>';
    }
    else {
        $website = "No website specified.";
    }

    $lagcomp = $server['lagComp'] == 1 ? "Enabled" : "Disabled";
    $last_updated = strtotime($server['lastUpdated']);
?>
    <h3 style="margin: 0 0 .4rem"><?=$server['name']?></h3>
    <table style="width: 100%" class="serverInfo">
        <tr>
            <td style="width: 50%"><b><?=$server['gameMode']?></b></td><td><b>Language</b>: <?=$server['language']?></td>
        </tr>
        <tr>
            <td><b>Players</b>: <?=$server['playersOnline']?> / <?=$server['maxPlayers']?></td>
        </tr>
    </table>

    <?php if ($details) { ?>
        <div style="margin-bottom: 0.75rem;">
            <h3 style="margin: 1rem .2rem .4rem">Details</h3>
            <table class="serverDetailsTable">
                <tr>
                    <td><b>Map</b></td><td><?=$server['mapName']?></td>
                </tr>
                <tr>
                    <td><b>Lag compensation</b></td><td><?=$lagcomp?></td>
                </tr>
                <tr>
                    <td><b>Version</b></td><td><?=$server['version']?></td>
                </tr>
                <tr>
                    <td><b>Website</b></td><td><?=$website?></td>
                </tr>
                <tr>
                    <td><b>SAMPCAC</b></td><td><?=$server['sampCac']?></td>
                </tr>
                <tr>
                    <td><b>Last updated</b></td><td><?=timeSince($last_updated)?> ago</td>
                </tr>
            </table>
            <a hx-get="server.php?&ip_addr=<?=$server['ipAddr']?>" hx-target="#main" hx-push-url="true">
                <button style="width: 100%; margin-top: 1rem; font-size: 1.25rem">All about this server</button>
            </a>
        </div>
    <?php } ?>

    <div style="float: left; margin-top: 0">
        <p class="ipAddr" id="ipAddr<?=$server['id']?>"><?=$server['ipAddr']?></p>
    </div>
    <div style="text-align: right; float: right; margin-top: 0">
        <?php if (!$details) { ?>
            <button hx-get="view/bits/fragments.php?type=details&ip_addr=<?=$server['ipAddr']?>">Details</button>
        <?php } ?>
        <button class="connectButton" onclick="CopyAddress('ipAddr<?=$server['id']?>')">Copy IP</button>
    </div>
    <div style="clear: both"></div>
<?php
}

if (isset($_GET['type'])) {
    if ($_GET['type'] == 'details') {
        $server = json_decode(file_get_contents("http://gateway.markski.ar:42069/api/GetServerByIP?ip_addr=".urlencode($_GET['ip_addr'])), true);

        DrawServer($server, true);
    }

    //..
}

function timeSince($time) {
    $time = time() - $time; // to get the time since that moment
    $time = ($time<1)? 1 : $time;
    $tokens = array (
        31536000 => 'year',
        2592000 => 'month',
        604800 => 'week',
        86400 => 'day',
        3600 => 'hour',
        60 => 'minute',
        1 => 'second'
    );

    foreach ($tokens as $unit => $text) {
        if ($time < $unit) continue;
        $numberOfUnits = floor($time / $unit);
        return $numberOfUnits.' '.$text.(($numberOfUnits>1)?'s':'');
    }
}
?>